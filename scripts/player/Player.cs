using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Player : CharacterBody2D
{
	[Export] protected Marker2D droppingPoint;
	[Export] private Area2D interactionArea;
	[Export] public AnimatedSprite2D animatorNode;
	[Export] private Label test_label;

	public static Player Instance { get; private set; }
	// Savedata params

	public bool IsDisabled { get; private set; }
	/// <summary>
	/// The amount of queued disable calls, as long as there is more than one, the player will remain disabled.
	/// </summary>
	private int QueuedDisabled = 0;
	// Movement Params
	/// <summary>
	/// Prohibits use of the movement keys to move, alternate to disabling the whole player altogether.
	/// </summary>
	public bool LockMovement { get; set; }
	public Vector2 MovementDirection { get; private set; }
	public float RunSpeedMultiplier = 2.1f;
	public int MovementSpeed = 100;

	private const float LONG_IDLE_BASE = 6;
	private float idle_timer = LONG_IDLE_BASE;

	private bool flip_direction = true, is_running, is_moving;

	// Interaction Params
	public Dictionary<StringName, Action> InputActions { get; set; }
	public Dictionary<StringName, Action<double>> HeldInputActions { get; set; }
	/// <summary>
	/// Tags of the object that the player can interact with.
	/// </summary>
	public static List<StringNames.GlobalTags> ValidInteractionTags { get; set; } =
	[
		StringNames.GlobalTags.Aphid,
		StringNames.GlobalTags.NPC,
		StringNames.GlobalTags.MenuTrigger,
		StringNames.GlobalTags.Interactable
	];

	private readonly List<Node2D> interactables_nearby = [], pickups_nearby = [];
	private readonly List<CanvasManager.ControlPrompt> prompts = [];
	private double held_timer;
	private int refresh_timer_ticks;
	private const int refresh_cooldown_ticks = 10;
	private StringName current_held_action;

	private bool in_menu = false;
	protected Timer DisabledTimer;
	private AudioStream audio_step;

	// Pickup Params
	public PickupArgs HeldItem { protected set; get; }

	/// <summary>
	/// Used to store the last safe position during Busy times. Manually set by the entity that needs it.
	/// </summary>
	public Vector2 LastPosition { get; set; }

	// MARK: Initialization
	public override void _EnterTree()
	{
		Instance = this;
		SetMeta(StringNames.TagMeta, (int)StringNames.GlobalTags.Player);
	}
	public override void _ExitTree()
	{
		Instance = null;
	}
	public override void _Ready()
	{
		SaveSystem.AddSaveModule(SaveModule);
		ConnectEvents();
		DeclareInputInteractions();

		CameraManager.Focus(this);
		audio_step = SoundManager.GetAudioStream("player/step");
		SetFlipDirection(Vector2.Right);
	}

	// Events
	private void ConnectEvents()
	{
		void _walkSound()
		{
			if (animatorNode.Animation == StringNames.WalkAnim || animatorNode.Animation == StringNames.RunAnim)
			{
				if (animatorNode.Frame == 0 || animatorNode.Frame == 3)
					SoundManager.CreateSound(audio_step).Bus = "Sounds";
			}
		}
		animatorNode.FrameChanged += _walkSound;

		SceneManager.AddEventListener(OnPreLoad, SceneManager.EventEnum.OnPreLoad);
		SceneManager.AddEventListener(OnPostLoad, SceneManager.EventEnum.OnPostLoad);
		SaveModule.AddEventListener((_) => OnLoadFinish(), SaveSystem.SaveEventsEnum.OnLoadFinish);
		CanvasManager.Menus.AddEventListener(OnPreSwitch, MenuHandler.MenuEvents.OnPreSwitch);
	}
	private void OnLoadFinish()
	{
		if (!GameManager.IsANewSavefile) // check for out of bounds
		{
			Vector2 _position = new(Data.PositionX, Data.PositionY);
			if (GameManager.APPLY_OUTOFBOUND_PATCH && (GameManager.IsOutOfBounds(_position) || GameManager.IsInsideGeometry(_position)))
				Instance.GlobalPosition = RoomInstance.Instance.Doors[0].GlobalPosition + (-RoomInstance.Instance.Doors[0].entryDirection) * 5;
			else
				Instance.GlobalPosition = _position;
		}
	}
	private void OnPreLoad(SceneManager.SceneArgs _args)
	{
		// Drop items on hand
		if (HeldItem != null)
			HeldItem.Entity.GlobalPosition = GlobalPosition;
	}
	private void OnPostLoad(SceneManager.SceneArgs _)
	{
		if (!GameManager.IsANewSavefile)
			CameraManager.ForceCameraPosition(Instance.GlobalPosition);
	}
	private void OnGameInit()
	{
		SaveSystem.AddSaveModule(Instance.SaveModule);
	}
	private void OnPreSwitch(MenuHandler.MenuArgs _args)
	{
		if (_args.Current?.Name == "pause" || _args.Next?.Name == "pause")
			return;

		if (_args.IsActive != in_menu)
		{
			in_menu = _args.IsActive;
			if (in_menu)
			{
				CanvasManager.ClearControlPrompts();
				SetDisabled(true, true);
			}
			else
				SetDisabled(false);
		}
	}

	public static async Task INSTANTIATE_PLAYER(SceneManager.SceneArgs args)
	{
		Node2D _player = (await GlobalManager.PRELOAD_RESOURCE(GlobalManager.PLAYER_PREFAB, true) as PackedScene).Instantiate() as Node2D;
		GlobalManager.Instance.GetTree().CurrentScene.AddChild(_player);

		if (!args.IsSwitching) // only disable if we came from a room transition, not from the main menu
			Instance.SetDisabled(true);
	}

	// Input Actions
	private void DeclareInputInteractions()
	{
		InputActions = new()
		{
			{ InputNames.Interact, Instance.TryInteract },
			{ InputNames.Pickup, InputAction_Pickup },
			{ InputNames.ShowInfo, InputAction_ShowInfo },
			{ InputNames.OpenGenerations, GenerationsPanel.InputAction_OpenGenerations },
		};

		if (IsInstanceValid(PlayerInventory.Instance))
		{
			InputActions.Add(InputNames.Pull, InputAction_PullItem );
			InputActions.Add(InputNames.ChangeMode,  InputAction_ChangeInventoryMode);
			InputActions.Add(InputNames.OpenInventory, PlayerInventory.Set);
		}

		if (IsInstanceValid(FreeCameraManager.Instance))
			InputActions.Add(InputNames.ChangeCamera, FreeCameraManager.Set);

		HeldInputActions = new()
		{
			{ InputNames.Interact, HeldInputAction_CallAphids}
		};
	}
	private void InputAction_Pickup()
	{
		if (Instance.HeldItem == null)
			Instance.TryPickup();
		else
			Instance.Drop();
	}
	private void InputAction_ChangeInventoryMode() =>
		PlayerInventory.Instance.ChangeInventoryMode();
	private void InputAction_PullItem()
	{
		// either pull the first item or store it in the inventory
		if (HeldItem != null)
			PlayerInventory.StoreCurrentItem();
		else
			PlayerInventory.PullItem(0);
	}
	private void InputAction_ShowInfo()
	{
		// either display the held aphid, or choose the closest one to you
		if (HeldItem != null && HeldItem.IsAphid)
			AphidInfo.Instance.SetByPickup();
		else if (AphidInfo.Instance.AreAphidsNearby)
			AphidInfo.Instance.SetByClosest();
	}
	private void HeldInputAction_CallAphids(double _time)
	{
		if (_time == 0 && Instance.HeldItem == null)
			Instance.CallAllNearbyAphids();
	}

	// MARK: Processing
	public override void _PhysicsProcess(double delta)
	{
		IsDisabled = QueuedDisabled > 0;

		// Calculate player movement
		if (!IsDisabled)
		{
			is_running = OptionsManager.Settings.BoolFlags["AutoRun"].Value ?
				!Input.IsActionPressed(InputNames.Run) :
				Input.IsActionPressed(InputNames.Run);
		}
		
		Velocity = !is_moving ?
				Vector2.Zero :
				MovementDirection * (MovementSpeed * (is_running ? RunSpeedMultiplier : 1));

		// apply movement phyiscs and animations
		TickFlip((float)delta);
		MoveAndSlide();
		PlayMovementAnim((float)delta);
		if (HeldItem != null && !GlobalManager.IsBusy)
			ProcessHeldItemBehaviour();

		if (IsDisabled)
			return;

		if (current_held_action != null)
		{
			if (held_timer > 0)
				held_timer -= delta;
			else
			{
				HeldInputActions[current_held_action].Invoke(0);
				current_held_action = null;
			}
		}
		if (refresh_timer_ticks > 0)
			refresh_timer_ticks--;
		else
		{
			refresh_timer_ticks = refresh_cooldown_ticks;
			RefreshNearbyBodies(false);
		}
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsDisabled)
			return;

		SetMovementDirection(Input.GetVector(InputNames.Left, InputNames.Right, InputNames.Up, InputNames.Down));

		// revise input actions
		foreach (var _pair in InputActions)
		{
			if (!@event.IsActionPressed(_pair.Key))
				continue;

			_pair.Value();
			break;
		}

		// do not process held interactions if we are in the middle of one
		if (current_held_action != null)
			return;

		// revise held inputs
		foreach (var _pair in HeldInputActions)
		{
			if (!@event.IsActionPressed(_pair.Key))
				continue;

			current_held_action = _pair.Key;
			held_timer = 1f;
			break;
		}
	}
	public override void _Notification(int what)
	{
		// we check for either windowfocusout  for popups, or if we exit the app window
		if (what == NotificationWMWindowFocusOut || what == NotificationApplicationFocusOut)
		{
			// if released, call the action early with the time it was left
			// thus up to the function to do its behaviour accordingly
			if (current_held_action != null)
			{
				HeldInputActions[current_held_action].Invoke(held_timer);
				held_timer = 0;
				current_held_action = null;
				return;
			}
		}
	}
	public override void _Input(InputEvent @event)
	{
		// if released, call the action early with the time it was left
		// thus up to the function to do its behaviour accordingly
		if (current_held_action != null && @event.IsActionReleased(current_held_action))
		{
			HeldInputActions[current_held_action].Invoke(held_timer);
			held_timer = 0;
			current_held_action = null;
			return;
		}
	}

	/// <summary>
	/// Disables player interaction by queuing disable requests. 
	/// One must be careful to track and handle their requests.
	/// </summary>
	/// <param name="_queuedState">State to be queued. True adds a queue and False removes a queue</param>
	public void SetDisabled(bool _queuedState, bool _cancelActions = false, bool _refresh = true)
	{
		QueuedDisabled += _queuedState ? 1 : -1;

		if (QueuedDisabled < 0)
		{
			QueuedDisabled = 0;
			DebugLogger.Print(DebugLogger.LogPriority.Error, "Player was requested to unqueue a disable call, but there was no queued disables!");
		}

		IsDisabled = QueuedDisabled > 0;

		SetProcessUnhandledInput(!IsDisabled);
		if (!IsDisabled)
		{
			SetMovementDirection(Input.GetVector(InputNames.Left, InputNames.Right, InputNames.Up, InputNames.Down));

			if (_refresh)
				RefreshNearbyBodies(true);
		}
		else
			SetMovementDirection(Vector2.Zero);

		if (_cancelActions)
		{
			if (animatorNode.Animation == StringNames.WalkAnim || animatorNode.Animation == StringNames.RunAnim)
				SetPlayerAnim(StringNames.IdleAnim);
			if (HeldItem != null)
			{
				if (GlobalManager.IsBusy)
					DropNoAnim(false);
				else
					Drop();
			}
			PlayerInventory.SetTo(false);
		}
	}
	/// <summary>
	/// Runs a timer that deactivates the disable state after the timer runs out.
	/// This function initially does not call SetDisabled and must be called beforehand. It can be overriden if someone calls it midway through.
	/// (It properly disposes of its last call if so)
	/// </summary>
	public void RunDisabledTimer(float _secondsDuration, bool _cancelActions = false, bool _refresh = true)
	{
		if (IsInstanceValid(DisabledTimer)) // end early
			DisabledTimer.EmitSignal("Timeout");
		DisabledTimer = new()
		{
			OneShot = true
		};
		DisabledTimer.Timeout += () =>
		{
			SetDisabled(false, _cancelActions, _refresh);
			SetPlayerAnim(StringNames.IdleAnim);
			DisabledTimer.QueueFree();
		};
		AddChild(DisabledTimer);
		DisabledTimer.Start(_secondsDuration);
	}

	// MARK: Interactions
	private void TryInteract()
	{
		if (IsDisabled || interactables_nearby.Count == 0)
			return;

		Node2D _node = interactables_nearby[0];

		// get the closest interactable object
		float _minDistance = GlobalPosition.DistanceSquaredTo(_node.GlobalPosition);
		for (int i = 0; i < interactables_nearby.Count; i++)
		{
			float _distanceFromPlayer = GlobalPosition.DistanceSquaredTo(interactables_nearby[i].GlobalPosition);
			if (_distanceFromPlayer < _minDistance)
			{
				_minDistance = _distanceFromPlayer;
				_node = interactables_nearby[i];
			}
		}
		// Attempts interacting
		try
		{
			if (_node is InteractableArea2D)
				(_node as InteractableArea2D).Interact(this, StringNames.GlobalTags.Player);
			else if (_node is IInteractableArea)
				(_node as IInteractableArea).Interact(this, StringNames.GlobalTags.Player);
		}
		catch (Exception _error)
		{
			GD.Print(_error);
		}
	}
	private async void CallAllNearbyAphids()
	{
		SetDisabled(true, true);
		SetPlayerAnim(StringNames.WhistleAnim);
		await Task.Delay(500);

		SoundManager.CreateSound2D("player/whistle", Instance.GlobalPosition, true);
		for (int i = 0; i < ResortManager.Current.Aphids.Count; i++)
		{
			Aphid _aphid = ResortManager.Current.Aphids[i];
			if (_aphid.GlobalPosition.DistanceTo(GlobalPosition) < 400)
				_aphid.CallTowards(GlobalPosition);
		}

		RunDisabledTimer(0.6f);
	}
	/// <summary>
	/// Checks for nearby interactables and pickups manually.
	/// </summary>
	private void RefreshNearbyBodies(bool _resetPrompts)
	{
		Godot.Collections.Array<Node2D> _collisionList = [];
		var _areasList = interactionArea.GetOverlappingAreas();
		var _bodiesList = interactionArea.GetOverlappingBodies();
		List<ulong> ids = [];

		foreach (var _area in _areasList)
		{
			if (ids.Contains(_area.GetInstanceId()))
				continue;
			ids.Add(_area.GetInstanceId());
			_collisionList.Add(_area);
		}
		foreach (var _body in _bodiesList)
		{
			if (ids.Contains(_body.GetInstanceId()))
				continue;
			ids.Add(_body.GetInstanceId());
			_collisionList.Add(_body);
		}

		List<Node2D> _previousInteractables = [.. interactables_nearby];
		List<Node2D> _previousPickups = [.. pickups_nearby];
		List<CanvasManager.ControlPrompt> _previousPrompts = [.. prompts];

		interactables_nearby.Clear();
		pickups_nearby.Clear();
		prompts.Clear();

		// clear prompt stack if you want to start from zero
		if (_resetPrompts)
			CanvasManager.ClearControlPrompts();

		for (int i = 0; i < _collisionList.Count; i++)
			CheckForInteractable(_collisionList[i]);
		for (int i = 0; i < _collisionList.Count; i++)
			CheckForPickup(_collisionList[i]);

		for (int i = 0; i < _previousInteractables.Count; i++)
		{
			if (interactables_nearby.Contains(_previousInteractables[i]))
				continue;
			RemoveInteractable(_previousInteractables[i]);
		}
		for (int i = 0; i < _previousPickups.Count; i++)
		{
			if (pickups_nearby.Contains(_previousPickups[i]))
				continue;
			RemovePickup(_previousPickups[i]);
		}

		// if we didnt restart, remove prompts from interactables that are now gone
		if (!_resetPrompts)
		{
			for (int i = 0; i < _previousPrompts.Count; i++)
			{
				if (prompts.Contains(_previousPrompts[i]))
					continue;
				CanvasManager.RemoveControlPrompt(_previousPrompts[i]);
			}
		}
	}
	private StringNames.GlobalTags CheckForInteractable(Node2D _node)
	{
		if (!_node.HasMeta(StringNames.TagMeta))
			return StringNames.GlobalTags.None;

		StringNames.GlobalTags _tag = (StringNames.GlobalTags)(int)_node.GetMeta(StringNames.TagMeta);
		if (!ValidInteractionTags.Contains(_tag))
			return StringNames.GlobalTags.None;

		interactables_nearby.Add(_node);
		SetPrompt(_tag, _node);

		InteractableArgs _args = new()
		{
			Tag = _tag,
			Entity = _node,
		};
		GlobalManager.Utils.InvokeEventListeners(InteractableEventsList[InteractableEvents.OnInteractableEnter], _args);
		return _tag;
	}
	private void CheckForPickup(Node2D _node)
	{
		if (HeldItem != null || !_node.HasMeta(StringNames.PickupMeta) ||
				!(bool)_node.GetMeta(StringNames.PickupMeta))
			return;

		pickups_nearby.Add(_node);
		CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.PickupItem);
		return;
	}
	private void RemoveInteractable(Node2D _node, CanvasManager.ControlPrompt _prompt = CanvasManager.ControlPrompt.None)
	{
		interactables_nearby.Remove(_node);
		if (!IsInstanceValid(_node) || !_node.HasMeta(StringNames.TagMeta))
			return;
		StringNames.GlobalTags _tag = (StringNames.GlobalTags)(int)_node.GetMeta(StringNames.TagMeta);
		InteractableArgs _args = new()
		{
			Tag = _tag,
			Entity = _node,
		};
		GlobalManager.Utils.InvokeEventListeners(InteractableEventsList[InteractableEvents.OnInteractableExit], _args);
	}
	private void RemovePickup(Node2D _node)
	{
		pickups_nearby.Remove(_node);
		if (!IsInstanceValid(_node) || !_node.HasMeta(StringNames.PickupMeta)) // is it a pickup
			return;
		if (pickups_nearby.Count == 0 && HeldItem == null)
			CanvasManager.RemoveControlPrompt(CanvasManager.ControlPrompt.PickupItem);
	}
	public bool HasInteractable(StringNames.GlobalTags _tag)
	{
		for (int i = 0; i < interactables_nearby.Count; i++)
		{
			if (!interactables_nearby[i].HasMeta(StringNames.TagMeta))
				continue;
			if ((StringNames.GlobalTags)(int)interactables_nearby[i].GetMeta(StringNames.TagMeta) == _tag)
				return true;
		}
		return false;
	}
	private void SetPrompt(StringNames.GlobalTags _tag, Node2D _node)
	{
		int _priority = (int)(GlobalPosition.DistanceSquaredTo(_node.GlobalPosition) / (800 * 800));
		CanvasManager.ControlPrompt _prompt = CanvasManager.ControlPrompt.Interact;
		switch (_tag)
		{
			case StringNames.GlobalTags.Aphid:
				if ((_node as Aphid).IsReadyForHarvest)
				{
					_prompt = CanvasManager.ControlPrompt.HarvestAphid;
					_priority++;
				}
				else
					_prompt = CanvasManager.ControlPrompt.PetAphid;
				break;
			case StringNames.GlobalTags.NPC:
				_prompt = CanvasManager.ControlPrompt.TalkToNPC;
				break;
			case StringNames.GlobalTags.MenuTrigger:
				_prompt = CanvasManager.ControlPrompt.OpenMenu;
				break;
			default:
				_priority--;
				break;
		}
		CanvasManager.AddControlPrompt(_prompt, _priority);
		prompts.Add(_prompt);
	}

	// MARK: Pickups
	/// <summary>
	/// Attempts to held the nearest pickup.
	/// </summary>
	private void TryPickup()
	{
		if (IsDisabled || IsInstanceValid(DisabledTimer) || pickups_nearby.Count == 0)
			return;

		var _node = pickups_nearby[0];

		if (_node.IsQueuedForDeletion())
			return;

		StringNames.GlobalTags _tag = _node.HasMeta(StringNames.TagMeta) ?
				(StringNames.GlobalTags)(int)_node.GetMeta(StringNames.TagMeta) : StringNames.GlobalTags.None;
		// If is an aphid, do a bunch of extra shit
		if (_tag == StringNames.GlobalTags.Aphid)
		{
			Aphid _aphid = _node as Aphid;
			if (!_aphid.IsBusy())
			{
				// if sleeping, get annoyed
				if (_aphid.State.Is(Aphid.StateEnum.Sleep))
					_aphid.WakeUp(true);
				_aphid.skin.SetFlipDirection(GlobalPosition - _aphid.GlobalPosition);
			}
			else
				return;
		}

		Pickup(_node);
	}
	/// <summary>
	/// Runs the pickup animation and sets the currently held item.
	/// </summary>
	public void Pickup(Node2D _node)
	{
		_node.SetMeta(StringNames.PickupMeta, false);
		_node.ProcessMode = ProcessModeEnum.Disabled;

		// animation
		CanvasManager.ClearControlPrompts();
		SetDisabled(true);
		RunDisabledTimer(0.5f, false, false);
		SetPlayerAnim(StringNames.PickupAnim);
		SetFlipDirection(_node.GlobalPosition - GlobalPosition);

		var _heldItem = new PickupArgs(_node);
		_ = PickupWait(_heldItem);
	}
	private async Task PickupWait(PickupArgs _heldItem)
	{
		await Task.Delay(400);
		PickupHeldItem(_heldItem);
	}
	/// <summary>
	/// Same as Pickup but without the timed animations.
	/// </summary>
	public void PickupNoAnim(Node2D _node)
	{
		_node.SetMeta(StringNames.PickupMeta, false);
		_node.ProcessMode = ProcessModeEnum.Disabled;

		PickupHeldItem(new PickupArgs(_node) { IsNoAnim = true });
	}
	/// <summary>
	/// Properly sets the currently held item.
	/// </summary>
	private void PickupHeldItem(PickupArgs _heldItem)
	{
		HeldItem = _heldItem;

		if (_heldItem.Tag == StringNames.GlobalTags.Aphid)
		{
			HeldItem.IsAphid = true;
			HeldItem.Entity_Aphid = _heldItem.Entity as Aphid;
			HeldItem.Entity_Aphid.skin.SetFlipDirection(flip_direction ? Vector2.Right : Vector2.Left, true);
			HeldItem.InitialOffset = HeldItem.Entity_Aphid.skin.Position;

			SoundManager.CreateSound2D(HeldItem.Entity_Aphid.AudioDynamic_Idle, HeldItem.Entity_Aphid.GlobalPosition, true);
		}

		if (!HeldItem.IsAphid)
		{
			// get relevant sprite information
			var _children = HeldItem.Entity.FindChildren("*", "Sprite2D");
			if (_children.Count > 0)
			{
				HeldItem.Sprite = _children[0] as Sprite2D;
				HeldItem.InitialOffset = HeldItem.Sprite.Offset;
			}
		}

		CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.DropItem, 1);
		RemoveInteractable(_heldItem.Entity);
		GlobalManager.Utils.InvokeEventListeners(PickupEventsList[PickupEvents.OnPickup], HeldItem);
	}

	/// <summary>
	/// Checks if the player can proceed with a Drop() call. Does not matter for DropNoAnim().
	/// </summary>
	/// <returns></returns>
	public bool CanDrop()
	{
		if (IsDisabled || IsInstanceValid(DisabledTimer))
			return false;

		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + (flip_direction ? droppingPoint.Position : -droppingPoint.Position));
		query.HitFromInside = false;

		if (GlobalManager.Utils.Raycast(query).Count > 0)
			return false;

		return true;
	}
	/// <summary>
	/// Runs the drop animation and clears currently held item data.
	/// </summary>
	public void Drop()
	{
		SetDisabled(true);
		SetPlayerAnim(StringNames.PickupAnim, true);
		RunDisabledTimer(0.45f);
		DisabledTimer.Timeout += () =>
		{
			DropHeldItem(false);
		};
	}
	/// <summary>
	/// Does the same as Drop() but without timed animations. It can also free the item if no further handling is needed.
	/// </summary>
	/// <param name="_queueFree"></param>
	public void DropNoAnim(bool _queueFree, bool _setAtLastPosition = true)
	{
		HeldItem.IsNoAnim = true;
		CanvasManager.RemoveControlPrompt(CanvasManager.ControlPrompt.DropItem);

		if (_queueFree)
		{
			GlobalManager.Utils.InvokeEventListeners(PickupEventsList[PickupEvents.OnDrop], HeldItem);
			HeldItem.Entity.QueueFree();
			HeldItem = null;
		}
		else
			DropHeldItem(_setAtLastPosition);
	}
	/// <summary>
	/// Properly disposes of the Held item without queuing free.
	/// </summary>
	/// <param name="_setAtLastPosition"></param>
	private void DropHeldItem(bool _setAtLastPosition)
	{
		if (HeldItem.IsAphid)
			HeldItem.Entity_Aphid.skin.Position = HeldItem.InitialOffset;
		else if (IsInstanceValid(HeldItem.Sprite))
			HeldItem.Sprite.Offset = HeldItem.InitialOffset;

		if (_setAtLastPosition)
			HeldItem.Entity.GlobalPosition = HeldItem.LastValidPosition;
		else
			// based on current facing direction
			HeldItem.Entity.GlobalPosition = GlobalPosition + (animatorNode.Scale.X < 0 ? droppingPoint.Position : -droppingPoint.Position);

		HeldItem.Entity.ProcessMode = ProcessModeEnum.Inherit;
		HeldItem.Entity.SetMeta(StringNames.PickupMeta, true);
		GlobalManager.Utils.InvokeEventListeners(PickupEventsList[PickupEvents.OnDrop], HeldItem);
		HeldItem = null;
	}
	
	private void ProcessHeldItemBehaviour()
	{
		if (!IsInstanceValid(HeldItem.Entity))
		{
			HeldItem = null;
			return;
		}
		bool _isSat = animatorNode.Animation == StringNames.SitAnim;

		HeldItem.Entity.GlobalPosition = GlobalPosition;

		if (!HeldItem.IsAphid)
		{
			if (HeldItem.Sprite != null)
				HeldItem.Sprite.Offset = new Vector2(0, -47 + (_isSat ? 12 : 0));
			else
				HeldItem.Entity.GlobalPosition += new Vector2(0, -48 + (_isSat ? 12 : 0));
		}
		else
		{
			HeldItem.Entity_Aphid.skin.SetFlipDirection(MovementDirection, true);
			HeldItem.Entity_Aphid.skin.Position = new(0, -39 + (_isSat ? 13 : 0));
		}
	}

	// MARK: General Functions
	public void SetMovementDirection(Vector2 _direction)
	{
		if (!LockMovement)
			MovementDirection = _direction.Normalized();
		else
			MovementDirection = Vector2.Zero;
		SetFlipDirection(_direction);

		is_moving = !MovementDirection.IsEqualApprox(Vector2.Zero);
		if (!LockMovement)
			PlayMovementAnim(0, true);
	}
	private void PlayMovementAnim(float _delta, bool _ignoreDisabled = false)
	{
		if (LockMovement)
			return;

		if (is_moving)
		{
			if (is_running)
				SetPlayerAnim(StringNames.RunAnim);
			else
				SetPlayerAnim(StringNames.WalkAnim);
			idle_timer = LONG_IDLE_BASE;
		}
		else if (!IsDisabled || _ignoreDisabled)
		{
			if (idle_timer > 0)
			{
				idle_timer -= _delta;
				SetPlayerAnim(StringNames.IdleAnim);
			}
			else
				SetPlayerAnim(StringNames.SitAnim);
		}
	}
	public void SetFlipDirection(Vector2 _direction)
	{
		// True : Facing Right - False : Facing Left
		if (_direction.X < 0)
			flip_direction = false;
		else if (_direction.X > 0)
			flip_direction = true;
	}
	private void TickFlip(float _delta)
	{
		// True : Facing Right - False : Facing Left
		if (flip_direction)
			animatorNode.Scale = new(Mathf.Lerp(animatorNode.Scale.X, -1, _delta * 6), animatorNode.Scale.Y);
		else
			animatorNode.Scale = new(Mathf.Lerp(animatorNode.Scale.X, 1, _delta * 6), animatorNode.Scale.Y);
	}
	public void SetPlayerAnim(StringName _name, bool _playBackwards = false)
	{
		if (_name.Equals(animatorNode.Animation))
			return;
		if (!_playBackwards)
			animatorNode.Play(_name);
		else
			animatorNode.PlayBackwards(_name);
	}
}