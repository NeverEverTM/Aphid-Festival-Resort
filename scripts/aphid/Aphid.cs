using System;
using System.Collections.Generic;
using Godot;
using static AphidTraits;
using static AphidActions;
using static AphidData;

public partial class Aphid : CharacterBody2D, IInteractableArea
{
	public AphidInstance Instance;
	[Export] public AphidSkin skin;
	[Export] private InteractableArea2D interactArea;

	// Aphid state
	public enum StateEnum { Busy, Idle, Hungry, Eat, Sleep, Train, Pet, Breed, Play, Social, Chase }
	public Dictionary<StateEnum, IState> ActiveStates = new() {
		{ StateEnum.Busy, new BusyState() },
		{ StateEnum.Idle, new IdleState() },
		{ StateEnum.Hungry, new HungryState() },
		{ StateEnum.Eat, new EatingState() },
		{ StateEnum.Sleep, new SleepState() },
		{ StateEnum.Pet, new PetState() },
		{ StateEnum.Breed, new BreedState() },
		{ StateEnum.Train, new TrainState() },
		{ StateEnum.Play, new PlayState() },
		{ StateEnum.Social, new SocialState() }
	};
	public IState State;
	/// <summary>
	/// Timers that run on the background for this aphid.
	/// </summary>
	public readonly List<CustomBaseTimer<Aphid>> Timers = [];
	public readonly List<IAreaEvent> AreaEvents = [];
	public readonly Dictionary<ValueFlagsEnum, float> ValueFlags = new()
	{
		{ ValueFlagsEnum.IdleTimeMultiplier, 1 },
		{ ValueFlagsEnum.RestTimeMultitplier, 1 },
		{ ValueFlagsEnum.BreedTimeMultiplier, 1 },
	};
	public readonly Dictionary<BoolFlagsEnum, bool> BoolFlags = new()
	{
		{ BoolFlagsEnum.IsHeavySleeper, false },
		{ BoolFlagsEnum.IsPicky, false },
		{ BoolFlagsEnum.CanOvereat, false }
	};

	/// <summary>
	/// This is the list for active traits during runtime, to add/remove a trait permanently use the Genes.Traits list instead
	/// </summary>
	public readonly List<ITrait> Traits = [];

	public Vector2 MovementDirection { get; set; }
	public float MovementSpeed { get; set; } = 20;
	public bool IsReadyForHarvest { get; private set; }
	public bool IsDisabled { get; private set; }
	public readonly static RandomNumberGenerator CORE_RNG = new();
	public readonly static RandomNumberGenerator MISC_RNG = new();
	private double current_time;

	public AudioStream AudioDynamic_Idle
	{
		get
		{
			if (Instance.Status.IsAdult)
				return SoundManager.GetAudioStream("aphid/idle");
			else
				return SoundManager.GetAudioStream("aphid/baby_idle");
		}
	}

	public List<Action<Node2D, StringNames.GlobalTags>> OnInteract { get; set; } = [];
	public List<Action> OnInteractOnly { get; set; } = [];
	public StringNames.GlobalTags Tag { get; set; } = StringNames.GlobalTags.Aphid;

	public static AudioStream GetIdleAudio(bool _isAdult)
	{
		if (_isAdult)
			return SoundManager.GetAudioStream("aphid/idle");
		else
			return SoundManager.GetAudioStream("aphid/baby_idle");
	}
	private GpuParticles2D harvest_effect;

	public override void _EnterTree()
	{
		SetMeta(StringNames.TagMeta, (int)Tag);
		current_time = Time.GetUnixTimeFromSystem();
	}
    public override void _ExitTree()
    {
        Instance.Entity = null;
    }

	public void SetReady()
	{
		MovementSpeed = 20;
		skin.SetInstance(Instance);
		skin.SetSkin("idle");
		SetTimers();

		// setup triggers and decays
		OnInteractOnly.Add(InteractByPlayer);

		Timers.Add(new HungerDecay(BASE_FOOD_DECAY));
		Timers.Add(new ThirstDecay(BASE_THIRST_DECAY));
		Timers.Add(new RestDecay(BASE_SLEEP_DECAY));
		Timers.Add(new AffectionDecay(BASE_AFFECTION_DECAY));
		Timers.Add(new BondshipDecay(BASE_BONDSHIP_GRACE));

		Timers.Add(new LifetimeDecay(Age_Death, Age_Death - Instance.Status.Age, true));
		Timers.Add(new BreedTimer(Breed_Cooldown, Breed_Cooldown - Instance.Status.BreedBuildup, true));
		Timers.Add(new HarvestTimer(Harvest_Cooldown, Harvest_Cooldown - Instance.Status.HarvestBuildup, true));

		// set state properly
		if (ActiveStates[Instance.Status.LastActiveState].CanBeStartingState)
			State = ActiveStates[Instance.Status.LastActiveState];
		else
			State = ActiveStates[StateEnum.Idle];

		foreach (var _pair in ActiveStates)
			_pair.Value.Awake(this);
		State.Enter(this, StateEnum.Idle, null);

		// translate trait IDs as their class equivalent
		for (int i = 0; i < Instance.Genes.Traits.Count; i++)
		{
			Traits.Add(GetTraitByName(Instance.Genes.Traits[i]));
			Traits[i].OnEnter(this);
		}
		
		// misc actions
		void set_movement_speed(int _, int _level) =>
			MovementSpeed = 20 + 0.2f * _level;
		Instance.Genes.Skills["speed"].OnLevelUp += set_movement_speed;
		set_movement_speed(0, Instance.Genes.Skills["speed"].Level);

		// update lifetime from last time it was loaded
		Instance.Status.Age -= (float)(GameManager.Data.Playtime - Instance.Status.LastTimeLoaded);
		Instance.Status.LastTimeLoaded = GameManager.Data.Playtime;
	}
	private void SetTimers()
	{
		Timer blink_timer = new(), blink_duration_timer = new(), squeak_timer = new();

		AddChild(squeak_timer);
		squeak_timer.Timeout += () =>
		{
			if (!State.Is(StateEnum.Sleep))
				SoundManager.CreateSound2D(AudioDynamic_Idle, GlobalPosition, true);
			squeak_timer.Start(MISC_RNG.RandiRange(5, 15));
		};
		squeak_timer.Start(MISC_RNG.RandiRange(5, 15));

		AddChild(blink_timer);
		blink_timer.OneShot = true;
		blink_timer.Timeout += () =>
		{
			if (!State.Is(StateEnum.Sleep) && !IsDisabled)
				skin.SetEyesSkin("blink");
			blink_duration_timer.Start(0.1f);
		};

		AddChild(blink_duration_timer);
		blink_duration_timer.OneShot = true;
		blink_duration_timer.Timeout += () =>
		{
			if (skin.currentEyeExpression == "blink" && !State.Is(StateEnum.Sleep) && !IsDisabled)
				skin.SetEyesSkin(skin.lastEyeExpression);
			blink_timer.Start(MISC_RNG.RandfRange(4.5f, 6.7f));
		};
		blink_timer.Start(MISC_RNG.RandfRange(4.5f, 6.7f));

		// this makes them able to blink and sqeak while being grabbed
		blink_duration_timer.ProcessMode = ProcessModeEnum.Pausable;
		blink_timer.ProcessMode = ProcessModeEnum.Pausable;
		squeak_timer.ProcessMode = ProcessModeEnum.Pausable;
	}

	// ==========| Update Processes |===========
	public override void _Process(double delta)
	{
		if (GlobalManager.IsBusy)
			return;

		float _delta = (float)delta;

		// Update Aphid status
		Instance.Status.PositionX = GlobalPosition.X;
		Instance.Status.PositionY = GlobalPosition.Y;

		float _timeDifference = (float)(Time.GetUnixTimeFromSystem() - current_time);
		current_time = Time.GetUnixTimeFromSystem();
		for (int i = 0; i < Timers.Count; i++)
			Timers[i].Process(this, _timeDifference);
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GlobalManager.IsBusy)
			return;

		float _delta = (float)delta;

		// Set default movement state
		Velocity = MovementDirection * MovementSpeed;
		if (!MovementDirection.IsEqualApprox(Vector2.Zero))
			skin.SetFlipDirection(MovementDirection);

		RefreshNearbyBodies();

		// trait update process
		for (int i = 0; i < Traits.Count; i++)
			Traits[i].OnProcess(this, _delta);

		// State update processes
		State.Process(this, _delta);
		MoveAndSlide();
		skin.StartWalk(MovementDirection);
	}
	private void RefreshNearbyBodies()
	{
		var _areasList = interactArea.GetOverlappingAreas();
		var _bodiesList = interactArea.GetOverlappingBodies();
		List<ulong> ids = [];

		foreach(var _area in _areasList)
		{
			if (ids.Contains(_area.GetInstanceId()))
				continue;
			OnTriggerStay(_area);
			ids.Add(_area.GetInstanceId());
		}
		foreach(var _body in _bodiesList)
		{
			if (ids.Contains(_body.GetInstanceId()))
				continue;
			OnTriggerStay(_body);
			ids.Add(_body.GetInstanceId());
		}
	}

	/// <param name="_newState">The state to be set</param>
	/// <param name="_specialArgs">The special parameters to be given, remember that each state manages its special parameters differently or not at all</param>
	/// <returns>Wheter or not the state was able to be set</returns>
	public bool SetState(StateEnum _newState, EventArgs _specialArgs = null)
	{
		if (IsDisabled || State.Type == _newState)
			return false;
		StateEnum _lastState = State.Type;

		if (!State.CanTransitionInto(_newState))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"AphidState: Cannot transition from {State.Type} to {_newState}");
			return false;
		}

		// Dispose of current state
		SetMovementDirection(Vector2.Zero);
		State.Exit(this, _newState);

		// Start new state
		State = ActiveStates[_newState];
		State.Enter(this, _lastState, _specialArgs);

		for (int i = 0; i < Traits.Count; i++)
			Traits[i].OnStateChange(this, _lastState);

		Instance.Status.LastActiveState = State.Type;
		return true;
	}
	/// <summary>
	/// Sets the direction the aphid will be walking towards.
	/// </summary>
	/// <param name="_to">To vector, normalization is done by default.</param>
	/// <param name="_absolute">Treat To as an absolute global position.</param>
	public void SetMovementDirection(Vector2 _to, bool _absolute = false) =>
		MovementDirection = _absolute ? (_to - GlobalPosition).Normalized() : _to.Normalized();
	public void CallTowards(Vector2 _position)
	{
		if (!State.Is(StateEnum.Idle))
			return;

		SetState(StateEnum.Busy); // force new state
		SetState(StateEnum.Idle, new IdleState.IdleArgs(_position));
		skin.DoHop();
	}
	public bool WakeUp(bool _forcefully = false, bool _byPassHeavySleeper = false)
	{
		if (!State.Is(StateEnum.Sleep))
			return false;
		// prevent wakeup calls from disturbances such as the player
		if (BoolFlags[BoolFlagsEnum.IsHeavySleeper] && !_byPassHeavySleeper)
			return false;
		SetState(StateEnum.Idle);

		if (!_forcefully)
			return true;
		Instance.AddAffection(-5);
		GlobalManager.EmitParticles("anger", new(), this);
		SoundManager.CreateSound2D("aphid/hurt", GlobalPosition, false);
		return true;
	}
	public virtual void PrepareToDie()
	{
		SetState(StateEnum.Idle);
		IsDisabled = true;
		if (IsInstanceValid(harvest_effect))
			harvest_effect.QueueFree();

		// Lay down and prepare yourself
		skin.SetFlipDirection(Vector2.Right, true);
		skin.SetLegsSkin("sleep");
		skin.Position = new(0, 2);
		ProcessMode = ProcessModeEnum.Disabled;

		CreateTimer(() =>
		{
			skin.SetEyesSkin("blink");
			skin.ProcessMode = ProcessModeEnum.Pausable;
			skin.CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Linear,
					"modulate", new Color(0), 5).Finished += Kill;
		}, 8, true);
	}
	public void Kill()
	{
		GameManager.AddToArchive(Instance);
		GameManager.RemoveAphid(Guid.Parse(Instance.ID));
		Instance = null;
		QueueFree();
		GameManager.CheckForGameOver();
	}

	public virtual void AllowHarvest()
	{
		IsReadyForHarvest = true;
		ShaderMaterial _outline = new()
		{
			Shader = ResourceLoader.Load<Shader>(GlobalManager.OUTLINE_SHADER)
		};
		_outline.SetShaderParameter("color", new Color(0.7f, 0, 0.7f));
		_outline.SetShaderParameter("pattern", 1);
		skin.Material = _outline;
		harvest_effect = GlobalManager.EmitParticles("harvest", new(), this, false);
		skin.LightMask = 0;
	}
	public virtual void Harvest()
	{
		// result
		IsReadyForHarvest = false;
		Instance.Status.HarvestBuildup = 0;
		float _multiplier = 0.5f + (Instance.Status.Hunger + Instance.Status.Thirst) / 200;
		Player.AddCurrency(Mathf.CeilToInt((Instance.Status.IsAdult ?
				HARVEST_VALUE_ADULT : HARVEST_VALUE_BABY)
				* _multiplier));
		Timers.Find((t) => t is HarvestTimer).Start();

		// visuals
		CanvasManager.RemoveControlPrompt(CanvasManager.ControlPrompt.HarvestAphid);
		CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.PetAphid);
		skin.Material = null;
		if (harvest_effect != null)
			harvest_effect.OneShot = true;
		harvest_effect = null;
		skin.LightMask = 1;
		skin.DoSquish();
	}
	// The actual breed that causes an egg to spawn, also sets aphids back to normal
	public void LayAnEgg(AphidInstance _father, bool _alone = false)
	{
		AphidHatch _egg = ResortManager.CreateItem("aphid_egg", GlobalPosition + (_father.Entity.GlobalPosition - GlobalPosition) / 2) as AphidHatch;
		_egg.given_genes = new();
		_egg.given_genes.BreedNewAphid(_father, Instance, _alone);
		_egg.IsNatural = true;

		SetState(StateEnum.Idle);
		Instance.Status.BreedBuildup = 0;
		Instance.AddAffection(100);
		Instance.AddTiredness(50);

		if (!_alone)
		{
			_father.Entity.SetState(StateEnum.Idle);
			_father.Status.BreedBuildup = 0;
			_father.AddAffection(100);
			_father.AddTiredness(50);

			Instance.Genes.Relationships[_father.GUID].AddToTotal(30);
			_father.Genes.Relationships[Instance.GUID].AddToTotal(30);
		}
		GlobalManager.EmitParticles("heart", GlobalPosition - new Vector2(0, 10), false);
	}

	public Timer CreateTimer(Action _timeout, float _duration, bool _bypassDisable = false)
	{
		Timer _timer = new()
		{
			OneShot = true
		};
		AddChild(_timer);
		_timer.Timeout += _timeout;
		_timer.Start(_duration);
		if (_bypassDisable)
			_timer.ProcessMode = ProcessModeEnum.Pausable;
		return _timer;
	}

	public void OnTriggerStay(Node2D _node)
	{
		if (_node.GetInstanceId().Equals(GetInstanceId()) || !_node.HasMeta(StringNames.TagMeta))
			return;
		StringNames.GlobalTags _tag = (StringNames.GlobalTags)(int)_node.GetMeta(StringNames.TagMeta);

		for (int i = 0; i < AreaEvents.Count; i++)
		{
			if (AreaEvents[i].Tag == _tag)
				AreaEvents[i].OnNodeStay(this, _node);
		}
	}
	public void InteractByPlayer() // player interaction
	{
		if (IsDisabled)
			return;

		if (IsReadyForHarvest) // Harvest behaviour
		{
			if (State.Is(StateEnum.Breed))
				return;

			if (State.Is(StateEnum.Sleep))
				WakeUp(true);

			Harvest();
		}
		else // Pet behaviour
		{
			if (IsBusy())
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
	/// <summary>
	/// Indicates wheter the aphid is busy or not. Hardcoded.
	/// </summary>
	public bool IsBusy()
	{
		if (IsDisabled)
			return true;
		return State.Type switch
		{
			StateEnum.Idle or
			StateEnum.Sleep or
			StateEnum.Hungry => false,
			_ => true,
		};
	}
}