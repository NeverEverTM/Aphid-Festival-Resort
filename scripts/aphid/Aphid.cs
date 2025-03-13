using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Aphid : CharacterBody2D, Player.IPlayerInteractable
{
	public AphidInstance Instance;
	/// <summary>
	/// For aphids that do not belong to the player, acting as NPCs. TODO: Implement proper fake aphid
	/// </summary>
	public bool IS_FAKE = false;
	public const string Tag = "aphid";

	[Export] public AphidSkin skin;
	[Export] private Area2D triggerArea;
	[Export] private AudioStreamPlayer2D audioPlayer;

	// Aphid state
	public enum StateEnum { Busy, Idle, Hungry, Eat, Sleep, Play, Pet, Breed, Train }
	public Dictionary<StateEnum, IState> ActiveStates = new() {
		{ StateEnum.Busy, new AphidActions.BusyState() },
		{ StateEnum.Idle, new AphidActions.IdleState() },
		{ StateEnum.Hungry, new AphidActions.HungryState() },
		{ StateEnum.Eat, new AphidActions.EatingState() },
		{ StateEnum.Sleep, new AphidActions.SleepState() },
		{ StateEnum.Pet, new AphidActions.PetState() },
		{ StateEnum.Breed, new AphidActions.BreedState() },
	};
	public IState State = new AphidActions.IdleState();
	public EventArgs StateArgs = new();
	public readonly List<IDecayEvent> DecayActions =
    [
        new AphidActions.HungerDecay(),
		new AphidActions.ThirstDecay(),
		new AphidActions.RestDecay(),
		new AphidActions.AffectionDecay(),
		new AphidActions.BondshipDecay(),
		new AphidActions.LifetimeDecay(),
	];
	public readonly List<ITriggerEvent> TriggerActions = [];
	/// <summary>
	/// This is the list for active traits during runtime, to add/remove a trait permanently use the Genes.Traits list instead
	/// </summary>
	public readonly List<ITrait> Traits = [];

	public Vector2 MovementDirection;
	public float MovementSpeed;
	public bool IsReadyForHarvest;
	public bool IsDisabled;
	private GpuParticles2D harvest_effect;
	public RandomNumberGenerator rng = new();

	// replace these with proper stored vars or single sound calls
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
	}

	public override void _Ready()
	{
		skin.SetInstance(Instance, this);
		skin.SetSkin("idle");
		MovementSpeed = 20; // + (0.15f * Instance.Genes.Level);
		SetTimers();

		// patch for 0.1.3v files, give aphid new skills and traits
		if (Instance.Genes.Traits == null)
		{
			Instance.Genes.GenerateTraits();
			Instance.Genes.GenerateSkills();
			Instance.Genes.GenerateFoodPreferences();
		}

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
			triggerArea.BodyEntered += OnTriggerEnter;
			triggerArea.AreaEntered += OnTriggerEnter;
			TriggerActions.Add(ActiveStates[StateEnum.Hungry] as ITriggerEvent);
			DecayActions.Add(ActiveStates[StateEnum.Breed] as IDecayEvent);
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
			Traits.Add(AphidTraits.GetTraitByName(Instance.Genes.Traits[i]));
			Traits[i].Activate(this, new());
		}
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

		TickHarvest(_delta);

		for (int i = 0; i < DecayActions.Count; i++)
			DecayActions[i].Tick(this, new(), _delta);
	}
	public override void _PhysicsProcess(double delta)
	{
		float _delta = (float)delta;
		
		// Set default movement state
		Velocity = MovementDirection * MovementSpeed;
		if (!MovementDirection.IsEqualApprox(Vector2.Zero))
			skin.SetFlipDirection(MovementDirection);
		skin.StartWalk();
		skin.TickFlip(_delta);

		if (IS_FAKE)
		{
			ActiveStates[StateEnum.Idle].Process(this, StateArgs, _delta);
			MoveAndSlide();
			return;
		}
		
		// trait update process
		for (int i = 0; i < Traits.Count; i++)
			Traits[i].OnProcess(this, StateArgs, _delta);

		// State update processes
		State.Process(this, StateArgs, _delta);
		MoveAndSlide();
	}

	public bool SetState(StateEnum _newState, EventArgs _specialArgs = null)
	{
		StateEnum _lastState = State.Type;
		if (!State.CanTransitionInto(_newState) && !IsDisabled)
		{
			Logger.Print(Logger.LogPriority.Warning, $"AphidState: Cannot transition from {State.Type} to {_newState}");
			return false;
		}

		// Dispose of current state
		SetMovementDirection(Vector2.Zero);
		State.Exit(this, StateArgs, _newState);

		// Start new state
		State = ActiveStates[_newState];
		State.Enter(this, _specialArgs ?? new(), _lastState);

		for (int i = 0; i < Traits.Count; i++)
			Traits[i].OnStateChange(this, StateArgs, _lastState);

		return true;
	}
	/// <summary>
	/// Sets the direction the aphid will be walking towards.
	/// </summary>
	/// <param name="_vector">To this vector, normalization is done by default.</param>
	/// <param name="_absolute">Should it use the vector as an absolute position instead?</param>
	public void SetMovementDirection(Vector2 _vector, bool _absolute = false) =>
		MovementDirection = _absolute ? (_vector - GlobalPosition).Normalized() : _vector.Normalized();
	public void CallTowards(Vector2 _position)
	{
		if (State.Is(StateEnum.Idle) == State.Is(StateEnum.Play))
			return;

		SetState(StateEnum.Idle);
		StateArgs = new AphidActions.IdleState.IdleArgs
		{
			timeleft = 0,
			target_position = _position,
			timeout = -15
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
	public bool WakeUp(bool _forcefully = false, bool _byPassHeavySleeper = false)
	{
		if (!State.Is(StateEnum.Sleep))
			return false;
		// prevent wakeup calls from disturbances such as the player
		if ((StateArgs as AphidActions.SleepState.SleepArgs).heavysleeper && !_byPassHeavySleeper)
			return false;
		SetState(StateEnum.Idle);

		if (!_forcefully)
			return true;
		Instance.Status.AddAffection(-5);
		GlobalManager.EmitParticles("anger", new(), this);
		PlaySound(Audio_Hurt);
		return true;
	}
	public virtual async void Die()
	{
		IsDisabled = true;
		SetState(StateEnum.Idle);
		if (IsInstanceValid(harvest_effect))
			harvest_effect.QueueFree();

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
			skin.SetEyesSkin("blink");
			skin.Modulate -= new Color(0, 0, 0, 0.025f);
		}
		GenerationsTracker.Data.Add(Instance);
		GameManager.RemoveAphid(Guid.Parse(Instance.ID));
		Instance = null;
		QueueFree();
	}

	protected virtual void TickHarvest(float _delta)
	{
		if (Instance.Status.MilkBuildup < AphidData.Harvest_Cooldown)
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
			harvest_effect = GlobalManager.EmitParticles("harvest", new(), this, false);
			skin.LightMask = 0;
		}
	}
	public virtual void Harvest()
	{
		IsReadyForHarvest = false;
		// result
		Instance.Status.MilkBuildup = 0;
		Player.Data.AddCurrency(Instance.Status.IsAdult ? AphidData.HARVEST_VALUE_ADULT : AphidData.HARVEST_VALUE_BABY);
		CanvasManager.RemoveControlPrompt(Tag);
		// visuals
		skin.DoSquishAnim();
		skin.Material = null;
		if (harvest_effect != null)
			harvest_effect.OneShot = true;
		harvest_effect = null;
		skin.LightMask = 1;
	}

	// The actual breed that causes an egg to spawn, also sets aphids back to normal
	public void LayAnEgg(AphidInstance _father, bool _alone = false)
	{
		AphidHatch _egg = ResortManager.CreateItem("aphid_egg", GlobalPosition + (_father.Entity.GlobalPosition - GlobalPosition) / 2) as AphidHatch;
		_egg.given_genes = new();
		_egg.given_genes.BreedNewAphid(_father, Instance);
		_egg.IsNatural = true;

		Instance.Status.BreedBuildup = 0;
		Instance.Status.BreedMode = AphidActions.BreedState.BreedEnum.Inactive;
		Instance.Status.AddAffection(100);
		Instance.Status.AddTiredness(50);
		SetState(StateEnum.Idle);
		if (!_alone)
		{
			_father.Status.BreedBuildup = 0;
			_father.Status.AddAffection(100);
			_father.Status.AddTiredness(50);
			_father.Entity.SetState(StateEnum.Idle);
		}
		GlobalManager.EmitParticles("heart", GlobalPosition - new Vector2(0, 10));
	}

	// =======| Collision Behaviours |========
	public void OnTriggerEnter(Node2D _node)
	{
		if (!_node.HasMeta(GlobalManager.StringNames.TagMeta))
			return;
		var _tag = (string)_node.GetMeta(GlobalManager.StringNames.TagMeta);

		var _action = TriggerActions.Find((e) => e.TriggerID == _tag);
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
			if (Player.IsAphidBusy(this))
				return;

			// Get ANGY if awoken
			if (State.Is(StateEnum.Sleep))
			{
				if (WakeUp(true)) // if it cant wake up, dont pet
					SetState(StateEnum.Pet);
			}
			else
				SetState(StateEnum.Pet);
		}
	}

	public interface IState
	{
		public StateEnum Type { get; }
		/// <summary>
		/// States from which this one can transition from.
		/// </summary>
		public StateEnum[] TransitionList { get; }
		/// <summary>
		/// Allows this state to dictate wheter it can transition from any state to this.
		/// Allowing it makes the transition list a blacklist, and disallowing it a whitelist.
		/// </summary>
		public bool TransitionToAnything { get => false; }
		/// <summary>
		/// Prevents transitioning while locked, useful for timed actions that require full focus.
		/// </summary>
		public bool Locked { get; set; }
		/// <summary>
		/// Called when an aphid spawns.
		/// </summary>
		public void Awake(Aphid aphid, EventArgs args)
		{
			return;
		}
		/// <summary>
		/// Called when the aphid enters this state. Aphid.StateArgs must be set here beforehand.
		/// </summary>
		/// <param name="args">Special args that the aphid can pass when setting the state. Empty EventArgs by default.</param>
		/// <param name="_previous">Last state we were in</param>
		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous);
		public void Exit(Aphid aphid, EventArgs args, StateEnum _next);
		public void Process(Aphid aphid, EventArgs args, float delta);

		/// <summary>
		/// Checks if the type of this state equals the given one.
		/// </summary>
		public bool Is(StateEnum _state) => Type.Equals(_state);
		public bool CanTransitionInto(StateEnum _state)
		{
			if (Locked)
				return false;

			if (TransitionList != null)
			{
				if (TransitionList.Contains(_state))
					return !TransitionToAnything;
				else
					return TransitionToAnything;
			}

			return TransitionToAnything;
		}
	}
	public interface ITrait
	{
		public string[] IncompatibleTraits { get; }

		/// <summary>
		/// Called when a trait is first loaded.
		/// </summary>
		public void Activate(Aphid _aphid, EventArgs _args);
		public void Deactivate(Aphid _aphid, EventArgs args);
		/// <summary>
		/// Called after an aphid changes its state, it is NOT called when trait is first loaded.
		/// </summary>
		public void OnStateChange(Aphid _aphid, EventArgs _args, StateEnum _previousState)
		{
			return;
		}
		public void OnProcess(Aphid _aphid, EventArgs _args, float _delta)
		{
			return;
		}

		public bool IsIncompatibleWith(string _ID)
		{
			if (IncompatibleTraits == null)
				return false;
			return IncompatibleTraits.Contains(_ID);
		}
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
}