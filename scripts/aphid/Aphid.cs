using System;
using System.Collections.Generic;
using Godot;
using static AphidData;

public partial class Aphid : CharacterBody2D, IInteractableArea
{
	public AphidInstance Instance;
	[Export] public AphidSkin Skin;
	[Export] private Area2D interactArea;

	public enum StateEnum { Busy, Idle, Hungry, Eat, Sleep, Train, Pet, Breed, Play, Social, Chase }
	public List<IState> ActiveStates = [
		new BusyState(),
		new IdleState(),
		new HungryState(),
		new EatingState(),
		new SleepState(),
		new PetState(),
		new BreedState(),
		new TrainState(),
		new PlayState(),
		new SocialState()
	];
	public IState State;
	/// <summary>
	/// Timers that run on the background while this aphid is active, to run a timer at all times, add it to the list of timers in the Instance instead.
	/// </summary>
	public readonly List<CustomBaseTimer<Aphid>> ActiveTimers = [];
	public readonly List<IInteractionEvent> AreaEvents = [];

	public Vector2 MovementDirection { get; private set; }
	public float MovementSpeed { get; set; } = 20;
	public bool IsDisabled { get; private set; }

	public readonly static RandomNumberGenerator MISC_RNG = new();
	private double last_unix_time;
	private int refresh_interaction_timer = 10;

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
	private Material last_material;

	public override void _EnterTree()
	{
		SetMeta(StringNames.TagMeta, (int)Tag);
		last_unix_time = Time.GetUnixTimeFromSystem();
	}
    public override void _ExitTree()
    {
        Instance.Entity = null;
    }
	public override void _Notification(int what)
    {
        if (what != NotificationUnpaused)
            return;

        // updates job times back to present after resuming from pausing
        last_unix_time = Time.GetUnixTimeFromSystem();
    }

	public void SetReady()
	{
		Skin.SetInstance(Instance);
		Skin.SetTo("idle");

		if (Instance.Status.IsDead)
		{
			PrepareToDie();
			return;
		}

		StartDecorativeTimers();
		OnInteractOnly.Add(InteractByPlayer);

		// set state properly
		var _lastState = ActiveStates.Find(s => s.Type == Instance.Status.LastActiveState);
		if (_lastState.CanBeStartingState)
			State = _lastState;
		else
			State = ActiveStates.Find(s => s.Type == StateEnum.Idle);

		for(int i = 0; i < ActiveStates.Count; i++)
			ActiveStates[i].Awake(this);
		State.Enter(this, StateEnum.Idle, null);

		// call trait events
		for (int i = 0; i < Instance.Traits.Count; i++)
			Instance.Traits[i].OnSpawn(this);
		
		// misc actions
		void set_movement_speed(Skill.SkillArgs _args) =>
			MovementSpeed = 20 + 0.2f * _args.CurrentLevel;
		Instance.Genes.Skills["speed"].AddEventListener(set_movement_speed, Skill.SkillEvents.OnLevelChange);
		Instance.Genes.Skills["speed"].GiveLevel(0);
		Instance.Status.LastTimeLoaded = GameManager.Data.Playtime;

		if (Instance.Status.IsReadyForHarvest)
			HighlightHarvest();
	}
	private void StartDecorativeTimers()
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
				Skin.SetEyesSkin("blink");
			blink_duration_timer.Start(0.1f);
		};

		AddChild(blink_duration_timer);
		blink_duration_timer.OneShot = true;
		blink_duration_timer.Timeout += () =>
		{
			if (Skin.currentEyeExpression == "blink" && !State.Is(StateEnum.Sleep) && !IsDisabled)
				Skin.SetEyesSkin(Skin.lastEyeExpression);
			blink_timer.Start(MISC_RNG.RandfRange(4.5f, 6.7f));
		};
		blink_timer.Start(MISC_RNG.RandfRange(4.5f, 6.7f));

		// this makes them able to blink and sqeak while being grabbed
		blink_duration_timer.ProcessMode = ProcessModeEnum.Pausable;
		blink_timer.ProcessMode = ProcessModeEnum.Pausable;
		squeak_timer.ProcessMode = ProcessModeEnum.Pausable;
	}
	public void InteractByPlayer() // player interaction
	{
		if (IsDisabled)
			return;

		if (Instance.Status.IsReadyForHarvest) // Harvest behaviour
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

	// ==========| Update Processes |===========
	public override void _Process(double delta)
	{
		if (GlobalManager.IsBusy)
			return;

		// Update Aphid status
		Instance.Status.PositionX = GlobalPosition.X;
		Instance.Status.PositionY = GlobalPosition.Y;

		float _timeDifference = (float)(Time.GetUnixTimeFromSystem() - last_unix_time);
		last_unix_time = Time.GetUnixTimeFromSystem();
		for (int i = 0; i < ActiveTimers.Count; i++)
			ActiveTimers[i].Update(_timeDifference);
	}
	public override void _PhysicsProcess(double delta)
	{
		if (GlobalManager.IsBusy)
			return;

		float _delta = (float)delta;

		// Set default movement state
		Velocity = MovementDirection * MovementSpeed;
		if (!MovementDirection.IsEqualApprox(Vector2.Zero))
			Skin.SetFlipDirection(MovementDirection);

		if (refresh_interaction_timer > 0)
			refresh_interaction_timer--;
		else
		{
			refresh_interaction_timer = 15;
			RegisterNearbyInteractables();
		}

		// trait update process
		for (int i = 0; i < Instance.Traits.Count; i++)
			Instance.Traits[i].OnProcess(this, _delta);

		// State update processes
		State.Process(this, _delta);
		MoveAndSlide();
		Skin.StartWalk(MovementDirection);
	}
	private void RegisterNearbyInteractables()
	{
		var _areasList = interactArea.GetOverlappingAreas();
		var _bodiesList = interactArea.GetOverlappingBodies();
		List<ulong> ids = [];

		foreach(var _area in _areasList)
		{
			if (ids.Contains(_area.GetInstanceId()) || _area.GetInstanceId().Equals(this.GetInstanceId()))
				continue;
			TriggerInteractEvent(_area);
			ids.Add(_area.GetInstanceId());
		}
		foreach(var _body in _bodiesList)
		{
			if (ids.Contains(_body.GetInstanceId()) || _body.GetInstanceId().Equals(this.GetInstanceId()))
				continue;
			TriggerInteractEvent(_body);
			ids.Add(_body.GetInstanceId());
		}
	}
	public void TriggerInteractEvent(Node2D _node)
	{
		if (_node.GetInstanceId().Equals(GetInstanceId()) || !_node.HasMeta(StringNames.TagMeta))
			return;

		StringNames.GlobalTags _tag = (StringNames.GlobalTags)(int)_node.GetMeta(StringNames.TagMeta);
		for (int i = 0; i < AreaEvents.Count; i++)
			AreaEvents[i].OnTrigger(this, _node, _tag);
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
		State = ActiveStates.Find(s => s.Type == _newState);
		Instance.Status.LastActiveState = State.Type;

		for (int i = 0; i < Instance.Traits.Count; i++)
			Instance.Traits[i].OnPreStateChange(this, _lastState);
		State.Enter(this, _lastState, _specialArgs);
		for (int i = 0; i < Instance.Traits.Count; i++)
			Instance.Traits[i].OnPostStateChange(this, _lastState);

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
		Skin.DoHop();
	}
	public bool WakeUp(bool _forcefully = false, bool _byPassHeavySleeper = false)
	{
		if (!State.Is(StateEnum.Sleep))
			return false;
		// prevent wakeup calls from disturbances such as the player
		if (Instance.BoolFlags[AphidInstance.FlagsEnum.IsHeavySleeper].Value && !_byPassHeavySleeper)
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
		Skin.SetLegsSkin("sleep");
		Skin.Position = new(0, 2);
		ProcessMode = ProcessModeEnum.Disabled;

		CreateTimer(() =>
		{
			Skin.SetEyesSkin("blink");
			Skin.ProcessMode = ProcessModeEnum.Pausable;
			Skin.CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Linear,
					"modulate", new Color(0), 5).Finished += Kill;
		}, 8, true);
	}
	public void Kill()
	{
		GameManager.AddToArchive(Instance);
		GameManager.RemoveAphid(Guid.Parse(Instance.ID));
		QueueFree();
		GameManager.CheckForGameOver();
	}

	/// <summary>
	/// Highlights the aphid for harvest
	/// </summary>
	public virtual void HighlightHarvest()
	{
		last_material = Skin.Material;
		Skin.Material = ResourceLoader.Load<ShaderMaterial>("uid://b5q5fbeq3dilm");
		harvest_effect = GlobalManager.EmitParticles("harvest", new(), this, false);
	}
	/// <summary>
	/// Harvests an aphid for its reward, removes the hightlight, and restarts the harvest timer
	/// </summary>
	public virtual void Harvest()
	{
		// reset
		Instance.Status.IsReadyForHarvest = false;
		Instance.Status.HarvestBuildup = 0;
		Instance.Timers.Find((t) => t is HarvestTimer).Start();

		// result
		float _multiplier = 0.5f + (Instance.Status.Hunger + Instance.Status.Thirst) / 200;
		int _base = Instance.Status.IsAdult ? HARVEST_VALUE_ADULT : HARVEST_VALUE_BABY;
		float _harvestMulti = Instance.FloatFlags[AphidInstance.FlagsEnum.HarvestMultiplier].Value;
		Player.AddCurrency(Mathf.RoundToInt(_base * _multiplier * _harvestMulti), Player.CurrencySource.AphidGain);

		// visuals
		CanvasManager.RemoveControlPrompt(CanvasManager.ControlPrompt.HarvestAphid);
		CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.PetAphid);
		Skin.Material = last_material;
		if (harvest_effect != null)
			harvest_effect.OneShot = true;
		harvest_effect = null;
		last_material = null;
		Skin.DoSquish();
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
		Instance.AddRest(-50);

		if (!_alone)
		{
			_father.Entity.SetState(StateEnum.Idle);
			_father.Status.BreedBuildup = 0;
			_father.AddAffection(100);
			_father.AddRest(-50);

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