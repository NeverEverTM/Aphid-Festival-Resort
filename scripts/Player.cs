using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Player : CharacterBody2D
{
	public static Player Instance { get; private set; }

	[Export] private Area2D interactionArea;
	[Export] public AnimatedSprite2D animatorNode;

	/// <summary>
	/// Disables player input interaction.
	/// </summary>
	public bool IsDisabled { get; private set; }
	private int QueuedDisabled = 0;
	private bool in_menu;
	protected Timer DisabledTimer;

	// Movement Params
	/// <summary>
	/// Prohibits use of the movement keys to move, alternate to disabling the whole player altogether.
	/// </summary>
	public bool LockMovement { get; set; }
	public Vector2 MovementDirection { get; private set; }
	private const float LONG_IDLE_BASE = 6;
	private float idle_timer = LONG_IDLE_BASE;
	private bool flip_direction = true, is_running;
	private AudioStream audio_step;

	// Interaction Params
	public Dictionary<StringName, Action> InputActions { get; set; }
	public Dictionary<StringName, Action<double>> HeldInputActions { get; set; }
	public static List<string> ValidInteractionTags { get; set; }

	private readonly List<Node2D> interactables_nearby = [], pickups_nearby = [];
	private readonly List<string> interactables_tags = [];
	private double held_timer;
	private bool is_moving;
	private int held_refresh_timer;
	private StringName current_held_action;

	// Pickup Params
	public PickupData HeldPickup { set; get; } = new();
	private Vector2 pickup_ground_position = new(35, 0); // facing right by default
	public record PickupData
	{
		public Node2D Item = null;
		public string Tag = string.Empty;
		public bool IsAphid = false;
		public Aphid AphidEntity = null;
		public Sprite2D Sprite = null;
		public Vector2 InitialOffset = new();
		public Vector2 LastValidPosition = new();
	}

	public delegate void PickupEventHandler(string _tag, Node2D _item);
	public delegate void InteractableEventHandler(string _tag, Node2D _item);
	public event PickupEventHandler OnPickup, OnDrop;
	public event InteractableEventHandler OnInteractableEnter, OnInteractableExit;

	// Savedata params
	internal static SaveData Data;
	internal static SaveSystem.SaveModule<SaveData> SaveModule;
	public static string NewName { get; set; }
	public static string[] NewPronouns { get; set; }
	public Vector2 LastPosition { get; set; }

	// MARK: SaveData Implementation
	public record SaveData
	{
		public string Name { get; set; } = "Mello";
		public string[] Pronouns { get; set; } = ["They", "them"];
		public int Level { get; set; }

		public float PositionX { get; set; }
		public float PositionY { get; set; }

		public List<string> Inventory { get; set; } = [];
		public List<string> Storage { get; set; } = [];
		public List<string> RecipesDiscovered { get; set; } = [];
		public int Currency { get; set; } = 30;
		public int InventoryMaxCapacity { get; set; } = 15;

		public SaveData()
		{
			Name = NewName;
			Pronouns = NewPronouns;
		}
		public void AddCurrency(int _amount)
		{
			Currency = Mathf.Max(Currency + _amount, 0);
			CanvasManager.UpdateCurrency();
		}
	}
	public class PlayerDataModule : SaveSystem.IDataModule<SaveData>
	{
		public void Set(SaveData _data)
		{
			Data = _data;
			CanvasManager.UpdateCurrency();

			if (!GameManager.IsNewGame)
			{
				Vector2 _position = new(Data.PositionX, Data.PositionY);
				if (GameManager.APPLY_OUTOFBOUND_PATCH && (GameManager.IsOutOfBounds(_position) || GameManager.IsInsideGeometry(_position)))
					Instance.GlobalPosition = FieldManager.Instance.Doors[0].GlobalPosition + (-FieldManager.Instance.Doors[0].entryDirection) * 5;
				else
					Instance.GlobalPosition = _position;
				CameraManager.Focus(Instance);
				CameraManager.ForceCameraPosition(Instance.GlobalPosition);
			}

			_data.Name ??= "Mello";
			_data.Pronouns ??= ["They", "them"];
		}
		public SaveData Get()
		{
			if (!SceneManager.IsBusy)
			{
				Data.PositionX = Instance.GlobalPosition.X;
				Data.PositionY = Instance.GlobalPosition.Y;
			}
			else
			{
				Data.PositionX = Instance.LastPosition.X;
				Data.PositionY = Instance.LastPosition.Y;
			}
			return Data;
		}
		public SaveData Default() => new();
	}

	// MARK: Initialization
	public override void _EnterTree()
	{
		Instance = this;
		if (SaveModule == null)
		{
			SaveModule = new SaveSystem.SaveModule<SaveData>("player", new PlayerDataModule(), 1000)
			{
				Extension = SaveSystem.SAVEFILE_EXTENSION
			};
			SaveSystem.AddSaveModule(SaveModule);
		}
		CanvasManager.Menus.OnSwitch.Add(OnSwitchMenu);
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

		CameraManager.Focus(Instance);
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
		ValidInteractionTags = [Aphid.Tag, NPCBehaviour.Tag, "menu", StringNames.InteractableTag];
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

		void _leaveItemInGround(string _c, bool _s)
		{
			if (HeldPickup.Item != null)
				HeldPickup.Item.GlobalPosition = GlobalPosition;
			SceneManager.OnPreLoad -= _leaveItemInGround;
		}
		SceneManager.OnPreLoad += _leaveItemInGround;
	}
	private void InputAction_Pickup()
	{
		if (Instance.HeldPickup.Item == null)
			Instance.TryPickup();
		else
			Instance.Drop();
	}
	private void InputAction_ChangeInventoryMode() =>
		PlayerInventory.Instance.ChangeInventoryMode();
	private void InputAction_PullItem()
	{
		if (IsInstanceValid(AphidInfo.Instance) && (AphidInfo.Available || AphidInfo.Enabled))
		{
			AphidInfo.SetAphid();
			return;
		}

		// either pull the first item or store it in the inventory
		if (Instance.HeldPickup.Item != null)
			PlayerInventory.StoreCurrentItem();
		else
			PlayerInventory.PullItem(0);
	}
	private void HeldInputAction_CallAphids(double _time)
	{
		if (_time == 0 && Instance.HeldPickup.Item == null)
			Instance.CallAllNearbyAphids();
	}

	// MARK: Processing
	public override void _PhysicsProcess(double delta)
	{
		IsDisabled = QueuedDisabled > 0 || CanvasManager.Menus.IsActive;

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
		if (HeldPickup.Item != null && !GlobalManager.IsBusy)
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
			Logger.Print(Logger.LogPriority.Error, "Player was requested to unqueue a disable call, but there was no queued disables!");
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
			SetPlayerAnim(StringNames.IdleAnim);
			if (HeldPickup.Item != null)
			{
				if (GlobalManager.IsBusy)
					DropNoAnim(false);
				else
					Drop();
			}
			PlayerInventory.SetTo(false);
			AphidInfo.Display(false);
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
		float _minDistance = GlobalPosition.DistanceTo(_node.GlobalPosition);

		for (int i = 0; i < interactables_nearby.Count; i++)
		{
			float _distanceFromPlayer = GlobalPosition.DistanceTo(interactables_nearby[i].GlobalPosition);
			if (_distanceFromPlayer < _minDistance)
			{
				_minDistance = _distanceFromPlayer;
				_node = interactables_nearby[i];
			}
		}
		// Attempts interacting with the CollisionObject itself
		if (_node.HasMethod(StringNames.InteractFunc))
			_node.CallDeferred(StringNames.InteractFunc);
		// Otherwise, attempts to interact with its parent instead
		else if (_node.GetParent().HasMethod(StringNames.InteractFunc))
			_node.GetParent().CallDeferred(StringNames.InteractFunc);
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
		List<string> _previous_tags = [.. interactables_tags];

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
				CanvasManager.RemoveControlPrompt(_previous_tags[i]);
			}
		}
	}
	private string CheckForInteractable(Node2D _node)
	{
		if (!_node.HasMeta(StringNames.TagMeta))
			return string.Empty;

		string _tag = (string)_node.GetMeta(StringNames.TagMeta);
		if (!ValidInteractionTags.Contains(_tag))
			return string.Empty;

		interactables_nearby.Add(_node);
		interactables_tags.Add(_tag);
		SetPrompt(_tag, _node);
		OnInteractableEnter?.Invoke(_tag, _node);
		return _tag;
	}
	private void CheckForPickup(Node2D _node)
	{
		if (HeldPickup.Item != null || !_node.HasMeta(StringNames.PickupMeta) ||
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
		string _tag = (string)_node.GetMeta(StringNames.TagMeta);
		interactables_nearby.Remove(_node);
		if (!HasInteractable(_tag))
			CanvasManager.RemoveControlPrompt(_tag);
		OnInteractableExit?.Invoke(_tag, _node);
	}
	private void RemovePickup(Node2D _node)
	{
		if (!_node.HasMeta(StringNames.PickupMeta)) // is it a pickup
			return;
		pickups_nearby.Remove(_node);
		if (pickups_nearby.Count == 0 && HeldPickup.Item == null)
			CanvasManager.RemoveControlPrompt(InputNames.Pickup);
	}
	public bool HasInteractable(string _tag)
	{
		for (int i = 0; i < interactables_nearby.Count; i++)
		{
			if (!interactables_nearby[i].HasMeta(StringNames.TagMeta))
				continue;
			if (interactables_nearby[i].GetMeta(StringNames.TagMeta).ToString() == _tag)
				return true;
		}
		return false;
	}
	private static void SetPrompt(string _tag, Node2D _node = null)
	{
		switch (_tag)
		{
			case Aphid.Tag:
				if ((_node as Aphid).IsReadyForHarvest)
					CanvasManager.AddControlPrompt("harvest", Aphid.Tag, InputNames.Interact);
				else
					CanvasManager.AddControlPrompt("pet", Aphid.Tag, InputNames.Interact);
				break;
			case NPCBehaviour.Tag:
				CanvasManager.AddControlPrompt("talk", NPCBehaviour.Tag, InputNames.Interact);
				break;
			case "menu":
				CanvasManager.AddControlPrompt("open_menu", "menu", InputNames.Interact);
				break;
			default:
				CanvasManager.AddControlPrompt(InputNames.Interact, StringNames.InteractableTag, InputNames.Interact);
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

		var _tag = _node.HasMeta(StringNames.TagMeta) ? (string)_node.GetMeta(StringNames.TagMeta) : "none";
		// If is an aphid, do a bunch of extra shit
		if (_tag == Aphid.Tag)
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
	public async Task Pickup(Node2D _node, string _tag)
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
	public void PickupNoAnim(Node2D _node, string _tag)
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
	private void SetPickupHeldItem(Node2D _node, string _tag)
	{
		HeldPickup = new()
		{
			Item = _node,
			Tag = _tag,
			LastValidPosition = _node.GlobalPosition
		};

		if (_tag is Aphid.Tag)
		{
			HeldPickup.IsAphid = true;
			HeldPickup.AphidEntity = _node as Aphid;
			HeldPickup.AphidEntity.skin.SetFlipDirection(flip_direction ? Vector2.Right : Vector2.Left, true);
			HeldPickup.InitialOffset = HeldPickup.AphidEntity.skin.Position;

			SoundManager.CreateSound2D(HeldPickup.AphidEntity.AudioDynamic_Idle, HeldPickup.AphidEntity.GlobalPosition, true);
		}


		if (!HeldPickup.IsAphid)
		{
			// get relevant sprite information
			var _children = HeldPickup.Item.FindChildren("*", "Sprite2D");
			if (_children.Count > 0)
			{
				HeldPickup.Sprite = _children[0] as Sprite2D;
				HeldPickup.InitialOffset = HeldPickup.Sprite.Offset;
			}
		}
		else
		{

			
		}

		CanvasManager.AddControlPrompt("drop", InputNames.Pickup, InputNames.Pickup);
		try
		{
			OnPickup?.Invoke(_tag, _node);
		}
		catch (Exception _error)
		{
			Logger.Print(Logger.LogPriority.Error, "Player: Error on picking up object (noanim).", _error);
		}
	}

	/// <summary>
	/// Checks if the player can proceed with a Drop() call. Does not matter for DropNoAnim().
	/// </summary>
	/// <returns></returns>
	public bool CanDrop()
	{
		if (IsDisabled || IsInstanceValid(DisabledTimer))
			return false;

		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition,
				GlobalPosition + (flip_direction ? pickup_ground_position : -pickup_ground_position));
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
		// TODO: turn this into a list of actions and give them pickup data instead.
		PickupData _pickup = HeldPickup;
		CanvasManager.RemoveControlPrompt("drop");
		try
		{
			OnDrop?.Invoke(_pickup.Tag, _pickup.Item);
		}
		catch (Exception _error)
		{
			Logger.Print(Logger.LogPriority.Error, "Player: Error on dropping object (noanim).", _error);
		}

		if (_queueFree)
			HeldPickup.Item.QueueFree();
		else
			DisposePickup(true);
	}
	/// <summary>
	/// Runs the drop animation and restarts current picked item data.
	/// </summary>
	public void Drop()
	{
		PickupData _pickup = HeldPickup;
		SetDisabled(true);
		SetPlayerAnim(StringNames.PickupAnim, true);
		RunDisabledTimer(0.45f);
		DisabledTimer.Timeout += () =>
		{
			DisposePickup(false);
			OnDrop?.Invoke(_pickup.Tag, _pickup.Item);
		};
	}
	private void DisposePickup(bool _setAtLastPosition)
	{
		if (HeldPickup.IsAphid)
			HeldPickup.AphidEntity.skin.Position = HeldPickup.InitialOffset;
		else if (IsInstanceValid(HeldPickup.Sprite))
			HeldPickup.Sprite.Offset = HeldPickup.InitialOffset;

		if (_setAtLastPosition)
			HeldPickup.Item.GlobalPosition = HeldPickup.LastValidPosition;
		else
			HeldPickup.Item.GlobalPosition = GlobalPosition +
					(flip_direction ? pickup_ground_position : -pickup_ground_position);
		HeldPickup.Item.ProcessMode = ProcessModeEnum.Inherit;
		HeldPickup.Item.SetMeta(StringNames.PickupMeta, true);
		HeldPickup = new();
	}
	private void ProcessPickupBehaviour()
	{
		if (!IsInstanceValid(HeldPickup.Item))
		{
			HeldPickup = new();
			return;
		}
		bool _isSat = animatorNode.Animation == StringNames.SitAnim;

		HeldPickup.Item.GlobalPosition = GlobalPosition;

		if (!HeldPickup.IsAphid)
		{
			if (HeldPickup.Sprite != null)
				HeldPickup.Sprite.Offset = new Vector2(0, -47 + (_isSat ? 12 : 0));
			else
				HeldPickup.Item.GlobalPosition += new Vector2(0, -48 + (_isSat ? 12 : 0));
		}
		else
		{
			HeldPickup.AphidEntity.skin.SetFlipDirection(MovementDirection, true);
			HeldPickup.AphidEntity.skin.Position = new(0, -39 + (_isSat ? 13 : 0));
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

	public interface IInteractEvent
	{
		/// <summary>
		/// Function used to dictate what happens when the player interacts with this.
		/// Must be the parent of or a CollisionObject2D itself with a CollisionShape2D acting as an interaction area.
		/// To be valid, make sure the CollisionObject has a tag and that it is included in the Player.ValidInteractionTags list. 
		/// </summary>
		public void Interact();
	}
}
