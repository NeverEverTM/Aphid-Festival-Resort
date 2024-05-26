using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Aphid : CharacterBody2D
{
	public AphidInstance Instance;
	/// <summary>
	/// For aphids that do not belong to the player, acting as NPCs.
	/// </summary>
	public bool IS_FAKE = false;

	[Export] public AphidSkin skin;
	[Export] private Area2D triggerArea;
	[Export] private AudioStreamPlayer2D audioPlayer;

	public static AudioStream Audio_Nom, Audio_Idle, Audio_Idle_Baby, Audio_Step, Audio_Jump, Audio_Hurt,
			Audio_Boing;
	public AudioStream AudioDynamic_Idle
	{
		get
		{
			if (Instance.Status.IsAdult)
				return Audio_Idle;
			else
				return Audio_Idle_Baby;
		}
		set
		{
			Audio_Idle = value;
		}
	}

	// Aphid state
	public enum AphidState { Idle, Eat, Sleep, Play, Pet, Breed, Train }
	public AphidState OurState = AphidState.Idle;
	public bool IsDisabled;

	// Idling Params
	private Vector2 idle_position;
	private int idle_rand_range = 100;
	private float idle_timer, idle_timeout_timer;
	private const float idle_timer_range = 1.15f, idle_timeout = 4f;
	private Timer blink_timer = new(), blink_duration_timer = new(), squeak_timer = new();

	// Eating Params
	public bool IsEating;
	private Node2D food_item;
	private Vector2 food_item_position, food_item_direction;
	private int foodgobble_shutter_speed;
	private bool is_food_favorite, food_item_switch;
	private float gobble_timer, hunger_decay_timer;
	private const float gobble_duration = 2f, hunger_decay = 14.5f, food_pursue_duration = 5f;
	private Timer food_pursue_timer = new(), food_gc_timer = new();
	private readonly List<Node2D> food_ignore_list = new();
	// Drinking Params (Drinking shares values with Eating but not the other way around)
	private float thirst_decay_timer;
	private const float thirst_decay = 8.8f;

	// Sleeping Params
	private float sleep_decay_timer, sleep_gain_timer;
	private const float sleep_decay = 9.5f, sleep_gain = 4.5f;
	private GpuParticles2D sleep_effect;

	// Petting Params And Affection too
	private float pet_timer, affection_decay_timer;
	private const float affection_decay = 13.2f;

	// Breeding Params
	public bool IsBreeding;
	private float[] breeding_weights = new float[] { 65, 35 };
	private Aphid breed_partner;
	private const int breed_timeout = 60, breed_partner_timeout = 10;
	private float breed_timeout_timer, breed_partner_timeout_timer;
	private GpuParticles2D breed_effect;

	// Production Params
	public bool IsReadyForHarvest;

	// Movement Params
	public Vector2 MovementDirection;
	public float MovementSpeed;

	// General Params
	private RandomNumberGenerator behaviourRNG = new();
	public readonly Dictionary<string, Action<Node2D>> TriggerActions = new();
	private float[] player_seek_weights = new float[] { 55, 15, 30 };
	private float interaction_cd_timer;
	private const float interaction_cd = 1.35f;

	public override void _Ready()
	{
		// ===| Set Default Params |===
		idle_position = GlobalPosition;
		food_item_position = new(25, -10);
		SetTimers();

		// Set Aphid Data
		skin.SetInstance(Instance, this);
		skin.SetSkin("idle");
		MovementSpeed = 20; // + (0.15f * Instance.Genes.Level);

		// Triggers
		triggerArea.BodyEntered += OnTriggerEnter;
		TriggerActions.Add("food", (Node2D n) => OnFoodTrigger(n));
		TriggerActions.Add("player", OnPlayerTrigger);
	}
	private void SetTimers()
	{
		AddChild(squeak_timer);
		squeak_timer.Timeout += () =>
		{
			if (OurState != AphidState.Sleep)
				SoundManager.CreateSound2D(AudioDynamic_Idle, GlobalPosition, true);
			squeak_timer.Start(behaviourRNG.RandiRange(5, 15));
		};

		AddChild(blink_timer);
		blink_timer.OneShot = true;
		blink_timer.Timeout += () =>
		{
			if (OurState == AphidState.Sleep)
				return;
			skin.SetEyesSkin("blink");
			blink_duration_timer.Start(0.1f);
		};

		AddChild(blink_duration_timer);
		blink_duration_timer.OneShot = true;
		blink_duration_timer.Timeout += () =>
		{
			if (OurState == AphidState.Sleep)
				return;
			if (skin.lastEyeExpression == "blink")
				skin.lastEyeExpression = "idle";
			skin.SetEyesSkin(skin.lastEyeExpression);
			blink_timer.Start(behaviourRNG.RandfRange(4.5f, 6.7f));
		};

		blink_timer.Start(behaviourRNG.RandfRange(4.5f, 6.7f));

		AddChild(food_pursue_timer);
		food_pursue_timer.OneShot = true;
		food_pursue_timer.Timeout += () =>
		{
			food_ignore_list.Add(food_item);
			food_item = null;
			SetAphidState(AphidState.Idle);
		};

		AddChild(food_gc_timer);
		food_gc_timer.Timeout += () => food_ignore_list.Clear();
		food_gc_timer.Start(30);
	}
	private void OnStartupState()
	{
		switch (Instance.Status.LastActiveState)
		{
			case AphidState.Sleep:
				GD.Print("was sleepy");
				Sleep();
				break;
			case AphidState.Breed:
				if (Instance.Status.BreedMode == -1)
					return;
				SetBreed(Instance.Status.BreedMode);
				break;
		}
	}

	// ==========| Update Processes |===========
	public override void _Process(double delta)
	{
		if (IS_FAKE)
			return;

		float _delta = (float)delta;
		Instance.Status.LastActiveState = OurState;

		Instance.Status.PositionX = GlobalPosition.X;
		Instance.Status.PositionY = GlobalPosition.Y;

		TickInteractionCooldown(_delta);
		TickLifetime(_delta);
		TickAffectionDecay(_delta);
		TickHungerDecay(_delta);
		TickThirstDecay(_delta);
		TickSleepDecay(_delta);
		TickBreeding(_delta);
		TickProduction(_delta);
	}
	public override void _PhysicsProcess(double delta)
	{
		float _delta = (float)delta;

		// Set default movement state
		Velocity = MovementDirection * MovementSpeed;
		skin.StartWalking();
		if (MovementDirection != Vector2.Zero)
			skin.SetFlipDirection(MovementDirection);
		skin.TickFlip(_delta);

		if (IS_FAKE)
		{
			OnIdleLoop(_delta);
			MoveAndSlide();
			return;
		}

		// Register triggers areas
		var _collisionList = triggerArea.GetOverlappingBodies();
		if (_collisionList.Count > 0)
		{
			for (int i = 0; i < _collisionList.Count; i++)
				OnTriggerEnter(_collisionList[i]);
		}

		// State update processes
		switch (OurState)
		{
			case AphidState.Idle:
				OnIdleLoop(_delta);
				break;
			case AphidState.Eat:
				if (gobble_timer > 0)
					TickFoodGobble(_delta);
				else
					WaddleToFood();
				break;
			case AphidState.Pet:
				TickPetTime(_delta);
				break;
			case AphidState.Sleep:
				TickSleepRecover(_delta);
				break;
			case AphidState.Breed:
				OnBreedLoop(_delta);
				break;
		}

		MoveAndSlide();
	}

	public void SetAphidState(AphidState _new)
	{
		// Properly disposed of previous state if needed
		SetMovementDirection(Vector2.Zero);
		switch (OurState)
		{
			case AphidState.Eat:
				food_item = null;
				is_food_favorite = false;
				break;
			case AphidState.Sleep:
				skin.SetSkin("idle");
				skin.Position = new(0, 0);
				sleep_effect.OneShot = true;
				sleep_effect = null;
				break;
			case AphidState.Breed:
				IsBreeding = false;
				skin.OverrideMovementAnim = false;
				TriggerActions.Remove("aphid");
				breed_partner = null;
				if (breed_effect != null)
				{
					breed_effect.OneShot = true;
					breed_effect = null;
				}
				break;
		}

		// Setup new state as needed
		switch (_new)
		{
			case AphidState.Idle:
				idle_timer = behaviourRNG.RandfRange(idle_timer_range, idle_timer_range * 2);
				idle_position = GlobalPosition;
				break;
			case AphidState.Pet:
				pet_timer = AphidData.PET_DURATION;
				break;
			case AphidState.Breed:
				skin.OverrideMovementAnim = true;
				break;
		}

		OurState = _new;
	}
	public void SetMovementDirection(Vector2 _direction)
	{
		MovementDirection = _direction.Normalized();
	}
	public void CallTowards(Vector2 _position)
	{
		if (OurState == AphidState.Idle == (OurState == AphidState.Play))
			return;

		SetAphidState(AphidState.Idle);
		skin.DoJumpAnim();
		idle_timer = 0;
		idle_position = _position;
		idle_timeout_timer = -10;
	}
	public void PlaySound(AudioStream _audio, bool _pitchRand = false)
	{
		if (_pitchRand)
			audioPlayer.PitchScale = GameManager.RNG.RandfRange(0.81f, 1.27f);
		audioPlayer.Stream = _audio;
		audioPlayer.Play();
	}

	// State functions
	protected void OnIdleLoop(float _delta)
	{
		// standing still wait time
		if (idle_timer > 0)
		{
			idle_timer -= _delta;
			return;
		}

		// we are close to idle pos, generate a new one and stand still for a few seconds
		if (GlobalPosition.DistanceTo(idle_position) < 20)
		{
			idle_position = new Vector2(behaviourRNG.RandfRange(-idle_rand_range, idle_rand_range), behaviourRNG.RandfRange(-idle_rand_range, idle_rand_range)) + GlobalPosition;
			SetMovementDirection(Vector2.Zero);
			idle_timer = behaviourRNG.RandfRange(idle_timer_range, idle_timer_range * 2);
			return;
		}

		// move to idle pos, timeout if you cant
		SetMovementDirection(idle_position - GlobalPosition);
		idle_timeout_timer += _delta;
		if (idle_timeout_timer > idle_timeout)
		{
			idle_position = GlobalPosition;
			idle_timeout_timer = 0;
		}
	}
	protected void WaddleToFood()
	{
		// if is not valid, too far away, or claimed by someone, let go
		if (!IsInstanceValid(food_item) || GlobalPosition.DistanceTo(food_item.GlobalPosition) > 200 || !food_item.HasMeta("tag"))
		{
			SetAphidState(AphidState.Idle);
			return;
		}

		// If close, eat it, else try walk to it
		if (GlobalPosition.DistanceTo(food_item.GlobalPosition) < 30)
		{
			SetMovementDirection(Vector2.Zero);
			food_item.RemoveMeta("tag"); // Stops others from eating it
			food_item.SetMeta("pickup", false);
			IsEating = true;

			food_item_direction.X = skin.IsFlipped ? -food_item_position.X : food_item_position.X;
			food_item_direction.Y = food_item_position.Y;
			food_item.GlobalPosition = GlobalPosition + food_item_direction;

			for (int i = 0; i < food_item.GetChildCount(); i++)
			{
				if (food_item.GetChild(i).IsClass("CollisionShape2D"))
				{
					(food_item.GetChild(i) as CollisionShape2D).Disabled = true;
					break;
				}
			}
			
			food_pursue_timer.Stop();
			gobble_timer = gobble_duration;
		}
		else if (food_pursue_timer.TimeLeft > 0)
			SetMovementDirection(food_item.GlobalPosition - GlobalPosition);
	}
	protected void TickFoodGobble(float _delta)
	{
		// Gobbling it up, yum yum
		gobble_timer -= _delta;

		// eating motion
		if (foodgobble_shutter_speed == 0)
		{
			food_item.GlobalPosition = GlobalPosition + food_item_direction
			+ (food_item_switch ? Vector2.Up : Vector2.Zero);
			food_item_switch = !food_item_switch;
			foodgobble_shutter_speed = 8;
			PlaySound(Audio_Nom, true);
		}
		else
			foodgobble_shutter_speed--;

		// finished meal
		if (gobble_timer <= 0)
		{
			// Dispose of the food item now
			if (IsInstanceValid(food_item))
			{
				string _id = food_item.GetMeta("id").ToString();
				GameManager.Food _food = GameManager.G_FOOD[_id];
				float _multi = Instance.Genes.FoodMultipliers[(int)_food.type];
				if (_food.food_value > 0)
					SetHunger(_food.food_value * _multi);

				if (_food.drink_value > 0)
					SetThirst(_food.drink_value * _multi);

				food_item.QueueFree();
			}
			IsEating = false;
			SetAphidState(AphidState.Idle);
		}
	}
	public bool Pet()
	{
		// no petties while crunching grub, or already petting
		if (gobble_timer > 0 || OurState == AphidState.Pet)
			return false;

		// Get ANGY if awoken
		if (OurState == AphidState.Sleep)
		{
			WakeUp(true);
			return false;
		}

		SetAphidState(AphidState.Pet);
		return true;
	}
	protected void TickPetTime(float _delta)
	{
		if (pet_timer > 0)
			pet_timer -= _delta;
		else
		{
			// If low on affection, raise bondship too
			if (Instance.Status.Affection < 80)
			{
				GameManager.EmitParticles("heart", GlobalPosition - new Vector2(0, 10));
				SetBondship(1);
			}
			SetAffection(10);
			SetAphidState(AphidState.Idle);
			skin.DoJumpAnim();
		}
	}
	protected void Sleep()
	{
		if (OurState != AphidState.Idle)
			return;

		// SLEEP!!!!!
		skin.SetEyesSkin("sleep");
		skin.SetLegsSkin("sleep");
		sleep_effect = GameManager.EmitParticles("sleep", GlobalPosition);
		skin.Position = new(0, 2);
		SetAphidState(AphidState.Sleep);
	}
	public void WakeUp(bool _forcefully = false)
	{
		SetAphidState(AphidState.Idle);
		if (!_forcefully)
			return;
		SetAffection(-5);

		// Visual and Audio clues
		var _particles = GameManager.EmitParticles("anger", new());
		_particles.ProcessMode = ProcessModeEnum.Always;
		_particles.GetParent().RemoveChild(_particles);
		PlaySound(Audio_Hurt);
		AddChild(_particles);
	}
	protected void TickSleepRecover(float _delta)
	{
		if (sleep_gain_timer > 0)
			sleep_gain_timer -= _delta;
		else
		{
			SetSleepiness(-behaviourRNG.RandfRange(0.75f, 1.95f));
			sleep_gain_timer = sleep_gain;
		}

		// bare minimum sleep
		if (Instance.Status.Sleepiness > 20)
			return;

		// 1 in 10000 chance to wake up, increased by the less sleepiness you have
		if (behaviourRNG.RandiRange(0, (int)(500 * Instance.Status.Sleepiness)) == 0)
			WakeUp();
	}

	// =======| Stat Related Functions |=======
	protected virtual void TickLifetime(float _delta)
	{
		// All things have an end, specifically, a few hours after birth
		Instance.Status.Age += _delta;

		// Grow up into adult
		if (!Instance.Status.IsAdult && Instance.Status.Age > AphidData.adulthoodAge)
		{
			Instance.Status.IsAdult = true;
			skin.SetSkin("idle");
		}

		if (Instance.Status.Age > AphidData.deathAge)
			Die();
	}
	public virtual async void Die()
	{
		// If you got saved to generations but didnt got removed from current, then discard yourself now
		if (GenerationsTracker.Data.Generations.ContainsKey(new Guid(Instance.ID)))
		{
			SaveSystem.RemoveAphidInstance(Guid.Parse(Instance.ID));
			QueueFree(); 
			return;
		}

		SetAphidState(AphidState.Idle);
		IsDisabled = true;

		// Lay down and prepare yourself
		skin.SetFlipDirection(Vector2.Right, true);
		skin.SetLegsSkin("sleep");
		skin.Position = new(0, 2);
		ProcessMode = ProcessModeEnum.Disabled;
		// play sound
		await Task.Delay(10000);

		// fade away :(
		skin.SetEyesSkin("blink");
		await Task.Delay(1000);
		for(int i = 0; i < 100; i++)
		{
			await Task.Delay(80);
			skin.Modulate -= new Color(0,0,0,0.025f);
		}
		GenerationsTracker.Data.AddAphid(Instance);
		SaveSystem.RemoveAphidInstance(Guid.Parse(Instance.ID));
	}
	public virtual void SetHunger(float _amount, bool _setAsCurrent = false)
	{
		if (_setAsCurrent)
			Instance.Status.Hunger = Math.Clamp(_amount, 0, 100);
		else
			Instance.Status.Hunger = Math.Clamp(Instance.Status.Hunger + _amount, 0, 100);
	}
	protected virtual void TickHungerDecay(float _delta)
	{
		if (hunger_decay_timer > 0)
			hunger_decay_timer -= _delta;
		else
		{
			SetHunger(-1);

			hunger_decay_timer = OurState == AphidState.Sleep ?
				hunger_decay * 2f :
				hunger_decay;
		}
	}
	public virtual void SetThirst(float _amount, bool _setAsCurrent = false)
	{
		if (_setAsCurrent)
			Instance.Status.Thirst = Math.Clamp(_amount, 0, 100);
		else
			Instance.Status.Thirst = Math.Clamp(Instance.Status.Thirst + _amount, 0, 100);
	}
	protected virtual void TickThirstDecay(float _delta)
	{
		if (thirst_decay_timer > 0)
			thirst_decay_timer -= _delta;
		else
		{
			SetThirst(-1);

			thirst_decay_timer = OurState == AphidState.Sleep ?
				thirst_decay * 2f :
				thirst_decay;
		}
	}
	public virtual void SetBondship(int _amount, bool _setAsCurrent = false)
	{
		if (_setAsCurrent)
			Instance.Status.Bondship = Math.Clamp(_amount, 0, 100);
		else
			Instance.Status.Bondship = Math.Clamp(Instance.Status.Bondship + _amount, 0, 100);
	}
	public virtual void SetAffection(int _amount, bool _setAsCurrent = false)
	{
		if (_setAsCurrent)
			Instance.Status.Affection = Math.Clamp(_amount, 0, 100);
		else
			Instance.Status.Affection = Math.Clamp(Instance.Status.Affection + _amount, 0, 100);
	}
	protected virtual void TickAffectionDecay(float _delta)
	{
		if (OurState == AphidState.Pet)
			return;

		if (affection_decay_timer > 0)
			affection_decay_timer -= _delta;
		else
		{
			SetAffection(-1);
			affection_decay_timer =
			OurState == AphidState.Sleep ?
				affection_decay * 2f :
				affection_decay;
		}
	}
	public virtual void SetSleepiness(float _amount, bool _setAsCurrent = false)
	{
		if (_setAsCurrent)
			Instance.Status.Sleepiness = Math.Clamp(_amount, 0, 100);
		else
			Instance.Status.Sleepiness = Math.Clamp(Instance.Status.Sleepiness + _amount, 0, 100);
	}
	protected virtual void TickSleepDecay(float _delta)
	{
		if (OurState == AphidState.Sleep)
			return;

		if (sleep_decay_timer > 0)
			sleep_decay_timer -= _delta;
		else
		{
			SetSleepiness(behaviourRNG.RandfRange(0.5f, 1.5f));
			sleep_decay_timer = sleep_decay;
		}

		// Only if we are tired or are in idle (dont sleep while other action is up)
		if (Instance.Status.Sleepiness < 75 || OurState != AphidState.Idle)
			return;

		// 1 in 12500 chance to sleep, chances increase the more sleepiness you have
		if (behaviourRNG.RandiRange(0, Mathf.FloorToInt(500 * 100 - Instance.Status.Sleepiness)) == 0)
			Sleep();
	}
	public virtual void Harvest()
	{
		IsReadyForHarvest = false;
		// visuals
		skin.DoSquishAnim();
		skin.Material = null;
		// result
		Instance.Status.MilkBuildup = 0;
		Player.Data.Currency += Instance.Status.IsAdult ? AphidData.moneyPerHarvest_adult : AphidData.moneyPerHarvest_baby;
	}
	protected virtual void TickProduction(float _delta)
	{
		if (Instance.Status.MilkBuildup < AphidData.productionCooldown)
			Instance.Status.MilkBuildup += _delta;
		else if (!IsReadyForHarvest)
		{
			IsReadyForHarvest = true;
			ShaderMaterial _outline = new()
			{
				Shader = ResourceLoader.Load<Shader>("res://scripts/shaders/outline.gdshader")
			};
			_outline.SetShaderParameter("color", Color.FromHtml("e33d00"));
			skin.Material = _outline;
		}
	}

	// ========| Breeding Control|==========
	// the breeding timer and the conditions to tick down
	protected virtual void TickBreeding(float _delta)
	{
		if (!Instance.Status.IsAdult)
			return;
		
		if (Instance.Status.Hunger < 10 || Instance.Status.Thirst < 10)
			return;

		if (Instance.Status.BreedBuildup < AphidData.breedTimer)
			Instance.Status.BreedBuildup += _delta;
		else if (OurState == AphidState.Idle)
			SetBreed();
	}
	// tries breeding, if succesful, trigger breeding state and related effects
	public async void SetBreed(int _mode = -1)
	{
		// Set breed state
		SetAphidState(AphidState.Breed);
		GameManager.EmitParticles("mating", GlobalPosition).OneShot = true;

		// Set breed mode
		if (_mode == -1)
		{
			if (SaveSystem.Aphids.Count == 1) 
				_mode = 1; // This is to make sure new games get a second aphid as soon as possible
			else
				_mode = GameManager.GetRandomByWeight(behaviourRNG, breeding_weights);
		}
		Instance.Status.BreedMode = _mode;

		if (_mode == 0) // Try finding a pardner around to mate
		{
			TriggerActions.Add("aphid", OnBreedTrigger);
			breed_timeout_timer = breed_timeout;
			breed_effect = GameManager.EmitParticles("mating", GlobalPosition);
		}
		else // Mate with yourself
		{
			IsBreeding = true;
			breed_effect = GameManager.EmitParticles("heart", GlobalPosition);
			breed_effect.OneShot = false;
			await skin.DoDanceAnim();
			Breed(Instance, true);
		}
	}
	// the state update process of breeding
	protected async virtual void OnBreedLoop(float _delta)
	{
		if (IsBreeding) // If already on it, dont bother
			return;

		if (breed_timeout_timer > 0)
			breed_timeout_timer -= _delta;
		else
			SetAphidState(AphidState.Idle);

		// as long as we have a valid partner, wait for them
		if (!IsInstanceValid(breed_partner))
			breed_partner = null;

		if (breed_partner != null)
		{
			if (breed_partner_timeout_timer > 0)
				breed_partner_timeout_timer -= _delta;
			else
			{
				breed_partner = null;
				return;
			}

			Vector2 _magnitude = breed_partner.GlobalPosition - GlobalPosition;
			// Checks for distance, done this way because aphids have a larger horizontal hitbox
			// so 20 X units may not be close enough, but 20 Y units is
			if (_magnitude.X < 50 && _magnitude.X > -50 && _magnitude.Y < 20 && _magnitude.Y > -20)
			{
				// Set both to breed
				IsBreeding = breed_partner.IsBreeding = true;
				breed_partner.SetAphidState(AphidState.Breed);
				// Dance
				breed_partner.skin.SetFlipDirection(_magnitude);
				skin.SetFlipDirection(GlobalPosition - breed_partner.GlobalPosition);
				_ = breed_partner.skin.DoDanceAnim();
				await skin.DoDanceAnim();

				// BREED
				Breed(breed_partner.Instance);
				return;
			}

			if (behaviourRNG.RandiRange(0, 1000) == 0)
				SoundManager.CreateSound2D(AudioDynamic_Idle, GlobalPosition, true);
		}
		else // else do your mating dance
			skin.DoWalkAnim();
	}
	// The actual breed that causes an egg to spawn, also sets aphids back to normal
	public void Breed(AphidInstance _father, bool _alone = false)
	{
		AphidInstance _mother = Instance;
		AphidInstance[] _parents = new AphidInstance[] { _mother, _father };
		AphidData.Genes _genes = new()
		{
			AntennaType = _parents[GameManager.RNG.RandiRange(0, 1)].Genes.AntennaType,
			EyeType = _parents[GameManager.RNG.RandiRange(0, 1)].Genes.EyeType,
			BodyType = _parents[GameManager.RNG.RandiRange(0, 1)].Genes.BodyType,
			LegType = _parents[GameManager.RNG.RandiRange(0, 1)].Genes.LegType,
			AntennaColor = GameManager.Utils.LerpColor(_mother.Genes.AntennaColor, _father.Genes.AntennaColor),
			EyeColor = GameManager.Utils.LerpColor(_mother.Genes.EyeColor, _father.Genes.EyeColor),
			BodyColor = GameManager.Utils.LerpColor(_mother.Genes.BodyColor, _father.Genes.BodyColor),
			LegColor = GameManager.Utils.LerpColor(_mother.Genes.LegColor, _father.Genes.LegColor),
			Father = _father.Genes.Name,
			Mother = _mother.Genes.Name
		};
		
		AphidHatch _egg = ResortManager.CreateItem("aphid_egg", _mother.Entity.GlobalPosition + (_father.Entity.GlobalPosition - _mother.Entity.GlobalPosition) / 2) as AphidHatch;
		_egg.naturalGenes = _genes;
		_egg.IsNatural = true;

		Instance.Status.BreedBuildup = 0;
		Instance.Status.BreedMode = -1;
		SetAphidState(AphidState.Idle);
		if (!_alone)
		{
			_father.Status.BreedBuildup = 0;
			_father.Entity.SetAphidState(AphidState.Idle);
		}
	}

	// =======| Collision Behaviours |========
	public void OnTriggerEnter(Node2D _node)
	{
		if (!_node.HasMeta("tag"))
			return;
		var _tag = (string)_node.GetMeta("tag");

		if (TriggerActions.ContainsKey(_tag))
			TriggerActions[_tag](_node);
	}

	protected virtual void OnFoodTrigger(Node2D _node)
	{
		// if is 1-currently consuming, 2- in the wrong state, 
		// 3-already pursuing said item, or 4-its marked for ignore, dont bother
		if (IsEating || (OurState == AphidState.Idle == (OurState == AphidState.Eat)) ||
		(food_item != null && food_item.Equals(_node)) ||
		(food_ignore_list.Count > 0 && food_ignore_list.Exists((Node2D _n) => CheckIfEqual(_n, _node))))
			return;

		var _foodItem = GameManager.G_FOOD[_node.GetMeta("id").ToString()];
		var _flavor = _foodItem.type;
		// if Vile, reject it cause yucky, unless you like it for some reason
		if (_flavor == AphidData.FoodType.Vile && _flavor != Instance.Genes.FoodPreference)
			return;

		// If not hungy/thirsty, cancel, items that give both, wait until you are full to return
		// favorites ignore how full you are
		var _isfavorite = Instance.Genes.FoodPreference == _flavor;
		if (!_isfavorite)
		{
			bool _givesFood = _foodItem.food_value > 0, _givesDrink = _foodItem.drink_value > 0;

			if (_givesDrink && Instance.Status.Thirst >= (_givesFood ? 100 : 80)) 
				return;

			if (_givesFood && Instance.Status.Hunger >= (_givesDrink ? 100 : 80)) 
				return;
		}

		// if you are pursuing a food already
		// and the current one is valid
		if (food_item != null && IsInstanceValid(food_item))
		{
			// if either are a favorite or neither, check which one is closer
			// else if this one isnt a favorite, ignore it, this means if it IS, just grab it
			if (_isfavorite == is_food_favorite)
			{
				if (GlobalPosition.DistanceTo(_node.GlobalPosition) > GlobalPosition.DistanceTo(food_item.GlobalPosition))
					return;
			}
			else if (!_isfavorite)
				return;
		}

		// Set current food item to pursue
		food_item = _node;
		food_pursue_timer.Start(food_pursue_duration);
		is_food_favorite = _isfavorite;

		if (OurState == AphidState.Idle)
			SetAphidState(AphidState.Eat);
	}
	private static bool CheckIfEqual(Node2D _n, Node2D _node)
	{
		if (!IsInstanceValid(_n))
			return false;
		return _n.Equals(_node);
	}

	protected virtual void OnPlayerTrigger(Node2D _node)
	{
		if (OurState != AphidState.Idle)
			return;

		// cooldown or is waiting to finish current idle loop
		if (interaction_cd_timer > 0 || idle_timer <= 0)
			return;

		interaction_cd_timer = interaction_cd;
		// 0 Nothing : 1 Walk Towards : 2 Flip At It 
		int _interaction = GameManager.GetRandomByWeight(behaviourRNG, player_seek_weights);
		if (_interaction == 1 && Instance.Status.Bondship >= 20)
		{
			idle_position = _node.GlobalPosition;
			idle_timeout_timer = 0;
		}
		else if (_interaction == 2)
		{
			idle_position = GlobalPosition;
			skin.SetFlipDirection(_node.GlobalPosition - GlobalPosition);
			SoundManager.CreateSound2D(AudioDynamic_Idle, GlobalPosition, true);
		}
	}
	private void TickInteractionCooldown(float _delta)
	{
		if (interaction_cd_timer > 0)
			interaction_cd_timer -= _delta;
	}

	// constantly calls aphids around to complete the breeding process
	private void OnBreedTrigger(Node2D _node)
	{
		Aphid _aphid = _node as Aphid;

		if (breed_partner != null || !_aphid.Instance.Status.IsAdult || _aphid.Instance.Status.Hunger == 0 || _aphid.Instance.Status.Thirst == 0)
			return;

		if (_aphid.OurState == AphidState.Idle)
		{
			breed_partner = _aphid;
			breed_partner_timeout_timer = breed_partner_timeout;
			_aphid.CallTowards(GlobalPosition + (skin.IsFlipped ? Vector2.Left : Vector2.Right) * 5);
		}
	}
}
