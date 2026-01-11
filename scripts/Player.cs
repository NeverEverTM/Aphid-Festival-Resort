using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Player : CharacterBody2D
{
	[Export] protected Marker2D droppingPoint;
	[Export] private Area2D interactionArea;
	[Export] public AnimatedSprite2D animatorNode;

	public static Player Instance { get; private set; }
	// Savedata params
	internal static PlayerData.Savefile Data;
	internal static SaveSystem.SaveModule<PlayerData.Savefile> SaveModule;
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
		StringNames.GlobalTags.Menu,
		StringNames.GlobalTags.Interactable
	];

	private readonly List<Node2D> interactables_nearby = [], pickups_nearby = [];
	private readonly List<StringNames.GlobalTags> interactables_tags = [];
	private double held_timer;
	private int held_refresh_timer;
	private StringName current_held_action;

	private bool in_menu;
	protected Timer DisabledTimer;
	private AudioStream audio_step;

	// Pickup Params
	public PickupArgs HeldPickup { protected set; get; }
	public enum PickupEventEnum { OnPickup, OnDrop }
	protected List<Action<PickupArgs>> OnPickup = [], OnDrop = [];
	public class PickupArgs : EventArgs
	{
		public Node2D Entity = null;
		public Aphid Entity_Aphid = null;
		public StringNames.GlobalTags Tag;
		public bool IsAphid = false;
		public Sprite2D Sprite = null;
		public Vector2 InitialOffset = new();
		public Vector2 LastValidPosition = new();
	}

	protected List<Action<InteractableEventArgs>> OnInteractableEnter = [], OnInteractableExit = [];
	public enum InteractableEventEnum
	{
		/// <summary>
		/// Triggered when a node enters the area and/or is detected inside of the same. This event can be triggered multiple times, make sure to check for repeated results.
		/// </summary>
		OnInteractableEnter,
		/// <summary>
		/// Triggered once when node leaves the area.
		/// </summary>
		OnInteractableExit
	}
	public class InteractableEventArgs : EventArgs
	{
		public StringNames.GlobalTags Tag;
		public Node2D Entity;
	}

	/// <summary>
	/// Adds listeners for Pickup related events.
	/// </summary>
	public void AddEventListener(Action<PickupArgs> _action, PickupEventEnum _event)
	{
		switch (_event)
		{
			case PickupEventEnum.OnPickup:
				OnPickup.Add(_action);
				break;
			case PickupEventEnum.OnDrop:
				OnDrop.Add(_action);
				break;
		}
	}
	/// <summary>
	/// Adds listeners for Interactable related events. Check enums for more information.
	/// </summary>
	public void AddEventListener(Action<InteractableEventArgs> _action, InteractableEventEnum _event)
	{
		switch (_event)
		{
			case InteractableEventEnum.OnInteractableEnter:
				OnInteractableEnter.Add(_action);
				break;
			case InteractableEventEnum.OnInteractableExit:
				OnInteractableExit.Add(_action);
				break;
		}
	}
	/// <summary>
	/// Removes listeners for Pickup related events.
	/// </summary>
	public void RemoveEventListener(Action<PickupArgs> _action, PickupEventEnum _event)
	{
		switch (_event)
		{
			case PickupEventEnum.OnPickup:
				OnPickup.Remove(_action);
				break;
			case PickupEventEnum.OnDrop:
				OnDrop.Remove(_action);
				break;
		}
	}
	/// <summary>
	/// Removes listeners for Interactable related events.
	/// </summary>
	public void RemoveEventListener(Action<InteractableEventArgs> _action, InteractableEventEnum _event)
	{
		switch (_event)
		{
			case InteractableEventEnum.OnInteractableEnter:
				OnInteractableEnter.Remove(_action);
				break;
			case InteractableEventEnum.OnInteractableExit:
				OnInteractableExit.Remove(_action);
				break;
		}
	}

	/// <summary>
	/// Used to store the last safe position during Busy times. Manually set by the entity that needs it.
	/// </summary>
	public Vector2 LastPosition { get; set; }

	// MARK: Initialization
	public override void _EnterTree()
	{
		Instance = this;
		if (SaveModule == null)
		{
			SaveModule = new SaveSystem.SaveModule<PlayerData.Savefile>("player", new PlayerData.PlayerDataModule(), 1000)
			{
				Extension = SaveSystem.SAVEFILE_EXTENSION
			};
			SaveSystem.AddSaveModule(SaveModule);
		}
		CanvasManager.Menus.OnSwitch.Add(OnSwitchMenu);
		SetMeta(StringNames.TagMeta, (int)StringNames.GlobalTags.Player);
	}
	public override void _ExitTree()
	{
		Instance = null;
	}

	public override void _Ready()
	{
		DeclareInputInteractions();
		ConnectEvents();
		SetFlipDirection(Vector2.Right);

		// player footsteps sound
		audio_step = SoundManager.GetAudioStream("player/step");
		animatorNode.FrameChanged += () =>
		{
			if (animatorNode.Animation == StringNames.WalkAnim || animatorNode.Animation == StringNames.RunAnim)
			{
				if (animatorNode.Frame == 0 || animatorNode.Frame == 3)
					SoundManager.CreateSound(audio_step).Bus = "Sounds";
			}
		};

		CameraManager.Focus(this);
	}
	private void OnSwitchMenu(MenuInstance _lastMenu, MenuInstance _menu)
	{
		if (_menu != null && _menu.Name.Equals("pause"))
			return;

		if (_lastMenu != null && _lastMenu.Name.Equals("pause"))
			return;

		if (CanvasManager.Menus.IsActive != in_menu)
		{
			in_menu = CanvasManager.Menus.IsActive;
			if (in_menu)
				SetDisabled(true, true);
			else
				SetDisabled(false);
		}

	}
	/// <summary>
	/// Declares all default input actions and valid tags for the player.
	/// </summary>
	private void DeclareInputInteractions()
	{
		InputActions = new()
		{
			{ InputNames.Interact, Instance.TryInteract },
			{ InputNames.Pickup, InputAction_Pickup },
			{ InputNames.Pull, InputAction_PullItem },
			{ InputNames.OpenInventory, PlayerInventory.Set },
			{ InputNames.ChangeMode,  InputAction_ChangeInventoryMode},
			{ InputNames.OpenGenerations, GenerationsPanel.InputAction_OpenGenerations },
			{ InputNames.ChangeCamera, () => FreeCameraManager.SetFreeCameraMode(true) },
		};

		HeldInputActions = new()
		{
			{ InputNames.Interact, HeldInputAction_CallAphids}
		};
	}
	private void ConnectEvents()
	{
		void _find(Node2D _node)
		{
			if (IsDisabled)
				return;
			CheckForInteractable(_node);
			CheckForPickup(_node);
		}
		void _lose(Node2D _node)
		{
			if (IsDisabled)
				return;
			RemoveInteractable(_node);
			RemovePickup(_node);
		}
		interactionArea.AreaEntered += _find;
		interactionArea.BodyEntered += _find;
		interactionArea.AreaExited += _lose;
		interactionArea.BodyExited += _lose;

		SceneManager.AddEventListener((_) =>
		{
			if (HeldPickup != null)
				HeldPickup.Entity.GlobalPosition = GlobalPosition;
		}, SceneManager.EventEnum.OnPreLoad);
	}
	private void InputAction_Pickup()
	{
		if (Instance.HeldPickup == null)
			Instance.TryPickup();
		else
			Instance.Drop();
	}
	private void InputAction_ChangeInventoryMode() =>
		PlayerInventory.Instance.ChangeInventoryMode();
	private void InputAction_PullItem()
	{
		// either pull the first item or store it in the inventory
		if (HeldPickup != null)
		{
			if (HeldPickup.IsAphid)
				AphidInfo.Instance.Display(!AphidInfo.Instance.IsBeingDisplayed);
			else
				PlayerInventory.StoreCurrentItem();
		}
		else
		{
			if (AphidInfo.Instance.AreAphidsNearby)
				AphidInfo.Instance.DisplayClosestAphid();
			else
				PlayerInventory.PullItem(0);
		}
	}

	private void HeldInputAction_CallAphids(double _time)
	{
		if (_time == 0 && Instance.HeldPickup == null)
			Instance.CallAllNearbyAphids();
	}

	// MARK: Processing
	public override void _PhysicsProcess(double delta)
	{
		IsDisabled = QueuedDisabled > 0;

		// Calculate player movement
		if (!IsDisabled)
		{
			is_running = OptionsManager.Settings.SettingAutoRun ?
				!Input.IsActionPressed(InputNames.Run) :
				Input.IsActionPressed(InputNames.Run);
		}

		Velocity = !is_moving ?
				Vector2.Zero :
				MovementDirection * (100 * (is_running ? 2.1f : 1));

		// apply movement phyiscs and animations
		TickFlip((float)delta);
		MoveAndSlide();
		PlayMovementAnim((float)delta);
		if (HeldPickup != null && !GlobalManager.IsBusy)
			ProcessPickupBehaviour();

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
		if (held_refresh_timer > 0)
			held_refresh_timer--;
		else
		{
			held_refresh_timer = 30;
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

		IsDisabled = QueuedDisabled > 0 || CanvasManager.Menus.IsActive;

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
			if (HeldPickup != null)
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
		var _collisionList = interactionArea.GetOverlappingBodies();
		_collisionList.AddRange(interactionArea.GetOverlappingAreas());
		List<Node2D> _previous_interactables = [.. interactables_nearby];
		List<StringNames.GlobalTags> _previous_tags = [.. interactables_tags];

		interactables_nearby.Clear();
		interactables_tags.Clear();
		pickups_nearby.Clear();

		// clear prompt stack if you want to start from zero
		if (_resetPrompts)
			CanvasManager.ClearControlPrompts();

		for (int i = 0; i < _collisionList.Count; i++)
			CheckForInteractable(_collisionList[i]);
		for (int i = 0; i < _collisionList.Count; i++)
			CheckForPickup(_collisionList[i]);

		// if we didnt restart, remove prompts from interactables that are now gone
		if (!_resetPrompts)
		{
			for (int i = 0; i < _previous_interactables.Count; i++)
			{
				if (interactables_nearby.Contains(_previous_interactables[i]))
					continue;
				CanvasManager.RemoveControlPrompt(StringNames.GlobalTagsNames[(int)_previous_tags[i]]);
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
		interactables_tags.Add(_tag);
		SetPrompt(_tag, _node);

		InteractableEventArgs _args = new()
		{
			Tag = _tag,
			Entity = _node,
		};
		GlobalManager.Utils.InvokeEventListeners(ref OnInteractableEnter, _args);
		return _tag;
	}
	private void CheckForPickup(Node2D _node)
	{
		if (HeldPickup != null || !_node.HasMeta(StringNames.PickupMeta) ||
				!(bool)_node.GetMeta(StringNames.PickupMeta))
			return;

		pickups_nearby.Add(_node);
		CanvasManager.AddControlPrompt("pickup", InputNames.Pickup, InputNames.Pickup);
		return;
	}
	private void RemoveInteractable(Node2D _node)
	{
		if (!_node.HasMeta(StringNames.TagMeta))
			return;
		StringNames.GlobalTags _tag = (StringNames.GlobalTags)(int)_node.GetMeta(StringNames.TagMeta);
		interactables_nearby.Remove(_node);
		if (!HasInteractable(_tag))
			CanvasManager.RemoveControlPrompt(StringNames.GlobalTagsNames[(int)_tag]);

		InteractableEventArgs _args = new()
		{
			Tag = _tag,
			Entity = _node,
		};
		GlobalManager.Utils.InvokeEventListeners(ref OnInteractableExit, _args);
	}
	private void RemovePickup(Node2D _node)
	{
		if (!_node.HasMeta(StringNames.PickupMeta)) // is it a pickup
			return;
		pickups_nearby.Remove(_node);
		if (pickups_nearby.Count == 0 && HeldPickup == null)
			CanvasManager.RemoveControlPrompt(InputNames.Pickup);
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
	private static void SetPrompt(StringNames.GlobalTags _tag, Node2D _node = null)
	{
		switch (_tag)
		{
			case StringNames.GlobalTags.Aphid:
				if ((_node as Aphid).IsReadyForHarvest)
					CanvasManager.AddControlPrompt("harvest", StringNames.GlobalTagsNames[(int)StringNames.GlobalTags.Aphid], InputNames.Interact);
				else
					CanvasManager.AddControlPrompt("pet", StringNames.GlobalTagsNames[(int)StringNames.GlobalTags.Aphid], InputNames.Interact);
				break;
			case StringNames.GlobalTags.NPC:
				CanvasManager.AddControlPrompt("talk", StringNames.GlobalTagsNames[(int)StringNames.GlobalTags.NPC], InputNames.Interact);
				break;
			case StringNames.GlobalTags.Menu:
				CanvasManager.AddControlPrompt("open_menu", StringNames.GlobalTagsNames[(int)StringNames.GlobalTags.Menu], InputNames.Interact);
				break;
			default:
				CanvasManager.AddControlPrompt(InputNames.Interact, StringNames.GlobalTagsNames[(int)StringNames.GlobalTags.Interactable], InputNames.Interact);
				break;
		}
	}

	// MARK: Pickups
	/// <summary>
	/// Attempts to pick the nearest pickable item.
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

		_ = Pickup(_node, _tag);
	}
	/// <summary>
	/// Runs the pickup animation and sets current held item.
	/// </summary>
	/// <param name="_node">The object's node</param>
	/// <param name="_tag">The tag of the object</param>
	public async Task Pickup(Node2D _node, StringNames.GlobalTags _tag)
	{
		_node.SetMeta(StringNames.PickupMeta, false);
		_node.ProcessMode = ProcessModeEnum.Disabled;

		CanvasManager.ClearControlPrompts();
		SetDisabled(true);
		RunDisabledTimer(0.5f, false, false);
		SetPlayerAnim(StringNames.PickupAnim);
		SetFlipDirection(_node.GlobalPosition - GlobalPosition);
		await Task.Delay(400);

		SetPickupHeldItem(_node, _tag);
		interactables_nearby.Remove(_node);
	}
	/// <summary>
	/// Same as Pickup but without the timed animations.
	/// </summary>
	public void PickupNoAnim(Node2D _node, StringNames.GlobalTags _tag)
	{
		_node.SetMeta(StringNames.PickupMeta, false);
		_node.ProcessMode = ProcessModeEnum.Disabled;

		SetPickupHeldItem(_node, _tag);
	}
	/// <summary>
	/// Sets the current pickup held item.
	/// </summary>
	/// <param name="_node">The object's node</param>
	/// <param name="_tag">The tag of the object</param>
	private void SetPickupHeldItem(Node2D _node, StringNames.GlobalTags _tag)
	{
		HeldPickup = new()
		{
			Entity = _node,
			Tag = _tag,
			LastValidPosition = _node.GlobalPosition
		};

		if (_tag == StringNames.GlobalTags.Aphid)
		{
			HeldPickup.IsAphid = true;
			HeldPickup.Entity_Aphid = _node as Aphid;
			HeldPickup.Entity_Aphid.skin.SetFlipDirection(flip_direction ? Vector2.Right : Vector2.Left, true);
			HeldPickup.InitialOffset = HeldPickup.Entity_Aphid.skin.Position;

			SoundManager.CreateSound2D(HeldPickup.Entity_Aphid.AudioDynamic_Idle, HeldPickup.Entity_Aphid.GlobalPosition, true);
		}

		if (!HeldPickup.IsAphid)
		{
			// get relevant sprite information
			var _children = HeldPickup.Entity.FindChildren("*", "Sprite2D");
			if (_children.Count > 0)
			{
				HeldPickup.Sprite = _children[0] as Sprite2D;
				HeldPickup.InitialOffset = HeldPickup.Sprite.Offset;
			}
		}

		CanvasManager.AddControlPrompt("drop", InputNames.Pickup, InputNames.Pickup);
		GlobalManager.Utils.InvokeEventListeners(ref OnPickup, HeldPickup);
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
	/// Does the same as Drop() but without timed animations. It can also free the item if no further handling is needed.
	/// </summary>
	/// <param name="_queueFree"></param>
	public void DropNoAnim(bool _queueFree)
	{
		CanvasManager.RemoveControlPrompt("drop");
		GlobalManager.Utils.InvokeEventListeners(ref OnDrop, HeldPickup);

		if (_queueFree)
		{
			HeldPickup.Entity.QueueFree();
			HeldPickup = null;
		}
		else
			DisposePickup(true);
	}
	/// <summary>
	/// Runs the drop animation and restarts current picked item data.
	/// </summary>
	public void Drop()
	{
		PickupArgs _args = new()
		{
			Tag = HeldPickup.Tag,
			Entity = HeldPickup.Entity,
		};
		SetDisabled(true);
		SetPlayerAnim(StringNames.PickupAnim, true);
		RunDisabledTimer(0.45f);
		DisabledTimer.Timeout += () =>
		{
			DisposePickup(false);
			GlobalManager.Utils.InvokeEventListeners(ref OnDrop, _args);
		};
	}
	private void DisposePickup(bool _setAtLastPosition)
	{
		if (HeldPickup.IsAphid)
			HeldPickup.Entity_Aphid.skin.Position = HeldPickup.InitialOffset;
		else if (IsInstanceValid(HeldPickup.Sprite))
			HeldPickup.Sprite.Offset = HeldPickup.InitialOffset;

		if (_setAtLastPosition)
			HeldPickup.Entity.GlobalPosition = HeldPickup.LastValidPosition;
		else
		// based on current facing direction
			HeldPickup.Entity.GlobalPosition = GlobalPosition + (animatorNode.Scale.X < 0 ? droppingPoint.Position : -droppingPoint.Position); 
		HeldPickup.Entity.ProcessMode = ProcessModeEnum.Inherit;
		HeldPickup.Entity.SetMeta(StringNames.PickupMeta, true);
		HeldPickup = null;
	}
	private void ProcessPickupBehaviour()
	{
		if (!IsInstanceValid(HeldPickup.Entity))
		{
			HeldPickup = null;
			return;
		}
		bool _isSat = animatorNode.Animation == StringNames.SitAnim;

		HeldPickup.Entity.GlobalPosition = GlobalPosition;

		if (!HeldPickup.IsAphid)
		{
			if (HeldPickup.Sprite != null)
				HeldPickup.Sprite.Offset = new Vector2(0, -47 + (_isSat ? 12 : 0));
			else
				HeldPickup.Entity.GlobalPosition += new Vector2(0, -48 + (_isSat ? 12 : 0));
		}
		else
		{
			HeldPickup.Entity_Aphid.skin.SetFlipDirection(MovementDirection, true);
			HeldPickup.Entity_Aphid.skin.Position = new(0, -39 + (_isSat ? 13 : 0));
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