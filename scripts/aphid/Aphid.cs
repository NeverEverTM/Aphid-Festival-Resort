using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Aphid : CharacterBody2D, Player.IPlayerInteractable
{
	public AphidInstance Instance;
	/// <summary>
	/// For aphids that do not belong to the player, acting as NPCs.
	/// </summary>
	public bool IS_FAKE = false;
	public const string Tag = "aphid";

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
	public enum StateEnum { Busy, Idle, Eat, Sleep, Play, Pet, Breed, Train }
	public Dictionary<StateEnum, IState> ActiveStates = new() {
		{ StateEnum.Busy, new AphidActions.BusyState() },
		{ StateEnum.Idle, new AphidActions.IdleState() },
		{ StateEnum.Eat, new AphidActions.HungryState() },
		{ StateEnum.Sleep, new AphidActions.SleepState() },
		{ StateEnum.Pet, new AphidActions.PetState() },
		{ StateEnum.Breed, new AphidActions.BreedState() },
	};
	public IState State = new AphidActions.IdleState();
	public EventArgs StateArgs = new();
	public readonly List<IDecayEvent> DecayActions = new()
	{
		new AphidActions.HungerDecay(),
		new AphidActions.ThirstDecay(),
		new AphidActions.RestDecay(),
		new AphidActions.AffectionDecay(),
		new AphidActions.BondshipDecay(),
		new AphidActions.LifetimeDecay(),
	};
	public readonly List<ITriggerEvent> TriggerActions = new();
	/// <summary>
	/// This is the list for active traits during runtime, to add/remove a trait permanently use the Genes.Traits list instead
	/// </summary>
	public readonly List<ITrait> Traits = new();

	public bool IsEating, IsReadyForHarvest, IsDisabled;
	private GpuParticles2D harvest_effect;

	// Breeding Params
	public bool IsBreeding;
	private float[] breeding_weights = new float[] { 65, 35 };
	private Aphid breed_partner;
	private const int breed_timeout = 60, breed_partner_timeout = 10;
	private float breed_timeout_timer, breed_partner_timeout_timer;
	private GpuParticles2D breed_effect;

	// Movement Params
	public Vector2 MovementDirection;
	public float MovementSpeed;
	public RandomNumberGenerator rng = new();

	public override void _Ready()
	{
		skin.SetInstance(Instance, this);
		skin.SetSkin("idle");
		MovementSpeed = 20; // + (0.15f * Instance.Genes.Level);
		SetTimers();

		// we call awake, which can set a last active state
		// and then we check if this didnt set a state already 
		if (!IS_FAKE)
			ActiveStates[Instance.Status.LastActiveState]?.Awake(this, new());

		// properly configures idle
		if (State.Is(StateEnum.Idle))
			State.Enter(this, new(), StateEnum.Idle);

		if (!IS_FAKE)
		{
			SetTraits();
			SetTriggers();
		}
	}
	private void SetTimers()
	{
		Timer blink_timer = new(), blink_duration_timer = new(), squeak_timer = new();

		AddChild(squeak_timer);
		squeak_timer.Timeout += () =>
		{
			if (!State.Is(StateEnum.Sleep))
				SoundManager.CreateSound2D(AudioDynamic_Idle, GlobalPosition, true);
			squeak_timer.Start(rng.RandiRange(5, 15));
		};
		squeak_timer.Start(rng.RandiRange(5, 15));

		AddChild(blink_timer);
		blink_timer.OneShot = true;
		blink_timer.Timeout += () =>
		{
			if (!State.Is(StateEnum.Sleep))
				skin.SetEyesSkin("blink");
			blink_duration_timer.Start(0.1f);
		};

		AddChild(blink_duration_timer);
		blink_duration_timer.OneShot = true;
		blink_duration_timer.Timeout += () =>
		{
			if (!State.Is(StateEnum.Sleep))
			{
				if (skin.lastEyeExpression == "blink")
					skin.lastEyeExpression = "idle";
				skin.SetEyesSkin(skin.lastEyeExpression);
			}
			blink_timer.Start(rng.RandfRange(4.5f, 6.7f));
		};
		blink_timer.Start(rng.RandfRange(4.5f, 6.7f));

		// this makes them able to blink and sqeak while being grabbed
		blink_duration_timer.ProcessMode = ProcessModeEnum.Pausable;
		blink_timer.ProcessMode = ProcessModeEnum.Pausable;
		squeak_timer.ProcessMode = ProcessModeEnum.Pausable;

		for (int i = 0; i < DecayActions.Count; i++)
			DecayActions[i].Start(this, new());
	}
	private void SetTraits()
	{
		for (int i = 0; i < Instance.Genes.Traits.Count; i++)
		{
			Traits.Add(AphidTraits.TRAITS[Instance.Genes.Traits[i]].GetTrait());
			Traits[i].Activate(this, new());
		}
	}
	private void SetTriggers()
	{
		triggerArea.BodyEntered += OnTriggerEnter;
		TriggerActions.Add(ActiveStates[StateEnum.Eat] as ITriggerEvent);
	}

	// ==========| Update Processes |===========
	public override void _Process(double delta)
	{
		if (IS_FAKE)
			return;

		float _delta = (float)delta;
		// Update Aphid status
		Instance.Status.LastActiveState = State.Type;
		Instance.Status.PositionX = GlobalPosition.X;
		Instance.Status.PositionY = GlobalPosition.Y;

		TickBreeding(_delta);
		TickHarvest(_delta);

		for (int i = 0; i < DecayActions.Count; i++)
			DecayActions[i].Tick(this, new(), _delta);
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
			ActiveStates[StateEnum.Idle].Process(this, StateArgs, _delta);
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

		// trait update process
		for(int i = 0; i < Traits.Count; i++)
			Traits[i].OnProcess(this, StateArgs, _delta);
		// State update processes
		State.Process(this, StateArgs, _delta);
		MoveAndSlide();
	}

	public bool SetState(StateEnum _new, EventArgs _specialArgs = null)
	{
		StateEnum _last = State.Type;

		// check for whitelist and blacklist of current state
		IState _state = ActiveStates[_new];
		bool _allowedToTransition = true;
		if (_state.Whitelist != null)
		{
			_allowedToTransition = false;
			for (int i = 0; i < _state.Whitelist.Length; i++)
				if (_state.Whitelist[i].Equals(_new))
				{
					_allowedToTransition = true;
					break;
				}
		}
		else if (_state.Blacklist != null)
		{
			_allowedToTransition = true;
			for (int i = 0; i < _state.Blacklist.Length; i++)
				if (_state.Blacklist[i].Equals(_new))
				{
					_allowedToTransition = false;
					break;
				}
		}

		if (!_allowedToTransition)
		{
			Logger.Print(Logger.LogPriority.Warning, $"AphidState: Cannot transition from {State} to {_new}");
			return false;
		}

		// Dispose of current state
		SetMovementDirection(Vector2.Zero);
		State.Exit(this, StateArgs, _new);

		// Start new state
		State = ActiveStates[_new];
		State.Enter(this, _specialArgs ?? new(), _last);

		for (int i = 0; i > Traits.Count; i++)
			Traits[i].OnStateChange(this, State.Type, StateArgs);

		return true;
	}
	public void SetMovementDirection(Vector2 _direction)
	{
		MovementDirection = _direction.Normalized();
	}
	public void CallTowards(Vector2 _position)
	{
		if (State.Is(StateEnum.Idle) == State.Is(StateEnum.Play))
			return;

		SetState(StateEnum.Idle);
        StateArgs = new AphidActions.IdleState.IdleArgs
        {
            timeleft = 0,
            target_position = _position
        };
        skin.DoJumpAnim();
	}
	public void PlaySound(AudioStream _audio, bool _pitchRand = false)
	{
		if (_pitchRand)
			audioPlayer.PitchScale = GlobalManager.RNG.RandfRange(0.81f, 1.27f);
		audioPlayer.Stream = _audio;
		audioPlayer.Play();
	}

	// State functions
	public bool Pet()
	{
		// Get ANGY if awoken
		if (State.Is(StateEnum.Sleep))
		{
			WakeUp(true);
			return false;
		}

		SetState(StateEnum.Pet);
		return true;
	}
	public void WakeUp(bool _forcefully = false, bool _byPassHeavySleeper = false)
	{
		if (!State.Is(StateEnum.Sleep))
			return;
		// prevent wakeup calls from disturbances such as the player
		if ((StateArgs as AphidActions.SleepState.SleepArgs).heavysleeper && !_byPassHeavySleeper)
			return;
		SetState(StateEnum.Idle);

		if (!_forcefully)
			return;
		Instance.Status.AddAffection(-5);
		AddChild(GlobalManager.EmitParticles("anger", new(), false));
		PlaySound(Audio_Hurt);
	}

	// =======| Stat Related Functions |=======
	public virtual async void Die()
	{
		SetState(StateEnum.Idle);
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
		for (int i = 0; i < 100; i++)
		{
			await Task.Delay(80);
			skin.Modulate -= new Color(0, 0, 0, 0.025f);
		}
		GenerationsTracker.Data.Add(Instance);
		GameManager.RemoveAphid(Guid.Parse(Instance.ID));
		Instance = null;
		QueueFree();
	}

	protected virtual void TickHarvest(float _delta)
	{
		if (Instance.Status.MilkBuildup < AphidData.productionCooldown)
			Instance.Status.MilkBuildup += _delta;
		else if (!IsReadyForHarvest)
		{
			IsReadyForHarvest = true;
			ShaderMaterial _outline = new()
			{
				Shader = ResourceLoader.Load<Shader>(GlobalManager.CanvasGroupOutlineShader)
			};
			_outline.SetShaderParameter("line_colour", Color.FromHtml("f25400"));
			_outline.SetShaderParameter("line_thickness", 2);
			skin.Material = _outline;
			harvest_effect = GlobalManager.EmitParticles("harvest", new(), false);
			AddChild(harvest_effect);
		}
	}
	public virtual void Harvest()
	{
		IsReadyForHarvest = false;
		// visuals
		skin.DoSquishAnim();
		skin.Material = null;
		harvest_effect.OneShot = true;
		harvest_effect = null;
		// result
		Instance.Status.MilkBuildup = 0;
		Player.Data.SetCurrency(Instance.Status.IsAdult ? AphidData.HarvestValue_Adult : AphidData.HarvestValue_Baby);
		CanvasManager.RemoveControlPrompt(Tag);
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
		else if (State.Is(StateEnum.Idle))
			SetBreed();
	}
	// tries breeding, if succesful, trigger breeding state and related effects
	public async void SetBreed(int _mode = -1)
	{
		// Set breed state
		SetState(StateEnum.Breed);
		GlobalManager.EmitParticles("mating", GlobalPosition).OneShot = true;

		// Set breed mode
		if (_mode == -1)
		{
			if (GameManager.Aphids.Count == 1)
				_mode = 1; // This is to make sure new games get a second aphid as soon as possible
			else
				_mode = GlobalManager.GetRandomByWeight(rng, breeding_weights);
		}
		Instance.Status.BreedMode = _mode;

		if (_mode == 0) // Try finding a pardner around to mate
		{
			TriggerActions.Add(ActiveStates[StateEnum.Breed] as AphidActions.ITriggerEvent);
			breed_timeout_timer = breed_timeout;
			breed_effect = GlobalManager.EmitParticles("mating", GlobalPosition);
		}
		else // Mate with yourself
		{
			IsBreeding = true;
			breed_effect = GlobalManager.EmitParticles("heart", GlobalPosition);
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
			SetState(StateEnum.Idle);

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
				breed_partner.SetState(StateEnum.Breed);
				// Dance
				breed_partner.skin.SetFlipDirection(_magnitude);
				skin.SetFlipDirection(GlobalPosition - breed_partner.GlobalPosition);
				_ = breed_partner.skin.DoDanceAnim();
				await skin.DoDanceAnim();

				// BREED
				Breed(breed_partner.Instance);
				return;
			}

			if (rng.RandiRange(0, 1000) == 0)
				SoundManager.CreateSound2D(AudioDynamic_Idle, GlobalPosition, true);
		}
		else // else do your mating dance
			skin.DoWalkAnim();
	}
	// The actual breed that causes an egg to spawn, also sets aphids back to normal
	public void Breed(AphidInstance _father, bool _alone = false)
	{
		AphidHatch _egg = ResortManager.CreateItem("aphid_egg", GlobalPosition + (_father.Entity.GlobalPosition - GlobalPosition) / 2) as AphidHatch;
		_egg.given_genes = new();
		_egg.given_genes.BreedNewAphid(_father, Instance);
		_egg.IsNatural = true;

		Instance.Status.BreedBuildup = 0;
		Instance.Status.BreedMode = -1;
		SetState(StateEnum.Idle);
		if (!_alone)
		{
			_father.Status.BreedBuildup = 0;
			_father.Entity.SetState(StateEnum.Idle);
		}
		GlobalManager.EmitParticles("heart", GlobalPosition - new Vector2(0, 10));
	}

	// =======| Collision Behaviours |========
	public void OnTriggerEnter(Node2D _node)
	{
		if (!_node.HasMeta("tag"))
			return;
		var _tag = (string)_node.GetMeta("tag");

		var _action = TriggerActions.Find((e) => e.TriggerID.Equals(_tag));
		_action?.OnTrigger(this, _node, StateArgs);
	}

	public void Interact() // player interaction
	{
		if (IsDisabled)
			return;

		if (IsReadyForHarvest) // Harvest behaviour
		{
			if (State.Is(StateEnum.Breed) || State.Is(StateEnum.Train))
				return;

			if (State.Is(StateEnum.Sleep))
				WakeUp(true);

			Harvest();
		}
		else // Pet behaviour
		{
			// Dont pet if item is in hand
			if (Player.Instance.PickupItem != null)
				return;

			// if succesful pet, make it look at you and stay yourself in place
			if (Pet())
			{
				// timer
				Player.Instance.SetDisabled(true);
				Player.Instance.RunDisabledTimer(AphidData.PET_DURATION);
				Player.Instance.MovementDirection = Vector2.Zero;

				// visuals
				Player.Instance.SetPlayerAnim("pet");
				Player.Instance.SetFlipDirection(GlobalPosition - Player.Instance.GlobalPosition);
				skin.SetFlipDirection(Player.Instance.GlobalPosition - GlobalPosition);
			}
		}
	}

	public interface IState
	{
		public StateEnum Type { get; }
		/// <summary>
		/// States from which this one can transition from. Opposite of Blacklist
		/// </summary>
		public StateEnum[] Whitelist { get; }
		/// <summary>
		/// States from which this one cannot transition from. Opposite of Whitelist
		/// </summary>
		public StateEnum[] Blacklist { get; }
		/// <summary>
		/// Called when an aphid is first loaded to resume behaviour.
		/// </summary>
		public void Awake(Aphid aphid, EventArgs args)
		{
			return;
		}
		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous);
		public void Exit(Aphid aphid, EventArgs args, StateEnum _next);
		public void Process(Aphid aphid, EventArgs args, float delta);

		public bool Is(StateEnum _state) => Type.Equals(_state);
	}
	public interface ITrait
    {
        public string Name { get; }
        public string[] IncompatibleTraits { get; }

        public void Activate(Aphid aphid, EventArgs args);
        public void Deactivate(Aphid aphid, EventArgs args);
		public void OnStateChange(Aphid aphid, StateEnum newState, EventArgs args);
        public void OnProcess(Aphid aphid, EventArgs args, float delta);

        public bool IsIncompatibleWith(string _ID)
        {
            if (IncompatibleTraits == null)
                return true;
            return IncompatibleTraits.Contains(_ID);
        }
        public ITrait GetTrait();
    }
	public class Skill
	{
		public string Name { get; set; }
		public int Points { get { return points; } set { points = Mathf.Clamp(value, 0, 10); } }
		private int points;
		public int Level { get; set; }

		public delegate void AphidSkillLevelUp(int _lastLevel, int _currentLevel);
		public event AphidSkillLevelUp OnLevelUp;

		public Skill(string Name)
		{
			this.Name = Name;
		}

		public virtual void GivePoints(int _points)
		{
			_points = Mathf.Max(0, Points + _points);

			Points += _points;
			int _levelUps = 0;
			while (Points > 10)
			{
				Points -= 10;
				_levelUps++;
			}
			GiveLevel(_levelUps);
		}
		public virtual void GiveLevel(int _level)
		{
			OnLevelUp?.Invoke(Level, Level + _level);
			Level += _level;
		}
	}
	public interface IDecayEvent
	{
		public float TimeLeft { get; set; }
		public void Tick(Aphid aphid, EventArgs args, float _delta);
		public void Start(Aphid aphid, EventArgs args)
		{
			return;
		}
	}
	public interface ITriggerEvent
	{
		public string TriggerID { get; }
		public void OnTrigger(Aphid _aphid, Node2D _node, EventArgs _args);
	}

	public interface IInteractionEvent
	{
		public int Priority { get; set; }
		public void OnInteract();
	}
}