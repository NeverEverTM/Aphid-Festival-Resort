using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Player : CharacterBody2D
{
	public static Player Instance { get; private set; }

	[Export] private Area2D interactionArea;
	[Export] private Node2D spriteBody;
	[Export] private AnimatedSprite2D animator;

	/// <summary>
	/// Disables player input interaction.
	/// </summary>
	public bool IsDisabled { get; private set; }
	private int QueuedDisabled = 0;
	private bool in_menu;

	// MARK: Movement Params
	public Vector2 MovementDirection { get; private set; }
	private const float LONG_IDLE_BASE = 6;
	private float idle_timer = LONG_IDLE_BASE;
	private bool flip_direction = true, is_running;
	protected Timer DisabledTimer;
	private AudioStream audio_step;
	int refresh_timer;

	// MARK: Interaction Params
	public static Dictionary<StringName, Action> InputActions { get; set; }
	public static Dictionary<StringName, Action<double>> HeldInputActions { get; set; }
	public static List<string> ValidInteractionTags { get; set; }

	private readonly List<Node2D> interactables_nearby = [], pickups_nearby = [];
	private readonly List<string> interactables_tags = [];
	private double held_timer;
	private bool is_moving;
	private StringName current_held_action;

	// MARK: Pickup Params
	public PickupData HeldPickup { set; get; }
	private Vector2 pickup_ground_position = new(35, 0); // facing right by default
	public record PickupData
	{
		public Node2D Item = null;
		public string tag = string.Empty;
		public bool is_aphid = false;
		public Aphid aphid = null;
		public Sprite2D sprite = null;
		public Vector2 initial_offset = new();
	}

	public delegate void PickupEventHandler(string _tag, Node2D _item);
	public delegate void InteractableEventHandler(string _tag, Node2D _item);
	public event PickupEventHandler OnPickup, OnDrop;
	public event InteractableEventHandler OnInteractableEnter, OnInteractableExit;

	// MARK: Savedata params
	internal static SaveData Data;
	public static string NewName { get; set; }
	public static string[] NewPronouns { get; set; }

	public record SaveData
	{
		public string Name { get; set; } = "Mello";
		public string[] Pronouns { get; set; } = ["They", "them"];
		public int Level { get; set; }
		public string Room { get; set; } = "resort_golden_grounds";

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
		public void ChangeCurrency(int _amount)
		{
			Currency = Mathf.Max(Currency + _amount, 0);
			CanvasManager.UpdateCurrency();
		}
	}
	public class SaveModule : SaveSystem.IDataModule<SaveData>
	{
		public void Set(SaveData _data)
		{
			Data = _data;
			Instance.GlobalPosition = new(Data.PositionX, Data.PositionY);
			CanvasManager.UpdateCurrency();
			// force camera smooth to snap to player position (probably godot jank)
			CameraManager.Focus(Instance);
			CameraManager.Instance.ForceUpdateScroll();
			CameraManager.Instance.ResetSmoothing();

			_data.Pronouns ??= ["They", "them"];
		}
		public SaveData Get()
		{
			Data.PositionX = Instance.GlobalPosition.X;
			Data.PositionY = Instance.GlobalPosition.Y;
			return Data;
		}
		public SaveData Default() => new();
	}

	private static void DeclareInputInteractions()
	{
		ValidInteractionTags = [Aphid.Tag, NPCBehaviour.Tag, "menu", StringNames.InteractableTag];

		InputActions = new()
		{
			{ InputNames.Interact, Instance.TryInteract },
			{ InputNames.Pickup, () =>
				{
					if (Instance.HeldPickup.Item == null)
						Instance.TryPickup();
					else
						Instance.Drop();
				}
			},
			{ InputNames.Pull, () =>
				{
					if (AphidInfo.Available || AphidInfo.Enabled)
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
			},
			{ InputNames.OpenInventory, PlayerInventory.Set },
			{ InputNames.ChangeMode, PlayerInventory.ChangeSellMode },
			{ InputNames.OpenGenerations, () => CanvasManager.Menus.OpenMenu(GenerationsTracker.Menu) },
			{ InputNames.ChangeCamera, () => FreeCameraManager.SetFreeCameraMode(true) },
		};

		HeldInputActions = new()
		{
			{ InputNames.Interact, (_time) =>
				{
					if (_time == 0 && Instance.HeldPickup.Item == null)
						Instance.CallAllNearbyAphids();
				}
			}
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

		void _leaveItemInGround(GlobalManager.SceneName _)
		{
			if (HeldPickup.Item != null)
				HeldPickup.Item.GlobalPosition = GlobalPosition;
			GlobalManager.OnPreLoadScene -= _leaveItemInGround;
		}
		GlobalManager.OnPreLoadScene += _leaveItemInGround;
	}
	public override void _EnterTree()
	{
		Instance = this;
		SetFlipDirection(Vector2.Right);
		HeldPickup = new();

		// TODO: move into public var
		SaveSystem.ProfileClassData.Add(new SaveSystem.SaveModule<SaveData>("player", new SaveModule(), 1000)
		{
			Extension = SaveSystem.SAVEFILE_EXTENSION,
		});
	}
	public override void _Ready()
	{
		DeclareInputInteractions();
		ConnectEvents();

		CanvasManager.Menus.OnSwitch += (_lastMenu, _menu) =>
		{
			if (_menu != null && _menu.Name.Equals("pause")
					|| (_lastMenu != null && _lastMenu.Name.Equals("pause")))
				return;

			if (CanvasManager.Menus.IsBusy != in_menu)
			{
				in_menu = CanvasManager.Menus.IsBusy;
				if (in_menu)
					SetDisabled(true, true);
				else
					SetDisabled(false);
			}
		};

		audio_step = SoundManager.GetAudioStream("player/step");
		animator.FrameChanged += () =>
		{
			if (animator.Animation == StringNames.WalkAnim)
			{
				if (animator.Frame == 0 || animator.Frame == 3)
					SoundManager.CreateSound2D(audio_step, GlobalPosition, false).VolumeDb = -10;
			}
			if (animator.Animation == StringNames.RunAnim)
			{
				if (animator.Frame == 0 || animator.Frame == 3)
					SoundManager.CreateSound2D(audio_step, GlobalPosition);
			}
		};
	}
	public override void _PhysicsProcess(double delta)
	{
		IsDisabled = QueuedDisabled > 0 || CanvasManager.Menus.IsBusy;

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
		if (refresh_timer > 0)
			refresh_timer--;
		else
		{
			refresh_timer = 30;
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

		IsDisabled = QueuedDisabled > 0 || CanvasManager.Menus.IsBusy;
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
				Drop();
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

	private async void TryPickup()
	{
		if (IsDisabled || IsInstanceValid(DisabledTimer) || pickups_nearby.Count == 0)
			return;

		var _node = pickups_nearby[0];
		var _tag = _node.HasMeta(StringNames.TagMeta) ? (string)_node.GetMeta(StringNames.TagMeta) : "none";
		// If is an aphid, do a bunch of extra shit
		if (_tag == Aphid.Tag)
		{
			if (!IsAphidBusy(_node as Aphid))
				SetAphidPickup(_node as Aphid);
			else
				return;
		}

		await Pickup(_node, _tag);
	}
	public async Task Pickup(Node2D _node, string _tag, bool _playAnim = true)
	{
		_node.SetMeta(StringNames.PickupMeta, false);
		_node.ProcessMode = ProcessModeEnum.Disabled;

		if (_playAnim)
		{
			CanvasManager.ClearControlPrompts();
			SetDisabled(true);
			SetPlayerAnim(StringNames.PickupAnim);
			SetFlipDirection(_node.GlobalPosition - GlobalPosition);
			RunDisabledTimer(0.5f, false, false);
			await Task.Delay(400);
		}

		HeldPickup.Item = _node;
		HeldPickup.tag = _tag;
		if (!HeldPickup.is_aphid)
		{
			var _children = HeldPickup.Item.FindChildren("*", "Sprite2D");
			if (_children.Count > 0)
			{
				HeldPickup.sprite = _children[0] as Sprite2D;
				HeldPickup.initial_offset = HeldPickup.sprite.Offset;
			}
		}
		else
			HeldPickup.aphid.skin.SetFlipDirection(flip_direction ? Vector2.Right : Vector2.Left, true);

		CanvasManager.AddControlPrompt("drop", InputNames.Pickup, InputNames.Pickup);
		interactables_nearby.Remove(_node);
		OnPickup?.Invoke(_tag, _node);
	}
	public static bool IsAphidBusy(Aphid _aphid)
	{
		if (_aphid.State.Is(Aphid.StateEnum.Idle) || _aphid.State.Is(Aphid.StateEnum.Sleep)
				|| _aphid.State.Is(Aphid.StateEnum.Hungry))
			return false;
		else
			return true;
	}
	private void SetAphidPickup(Aphid _aphid)
	{
		// if sleeping, get annoyed
		if (_aphid.State.Is(Aphid.StateEnum.Sleep))
			_aphid.WakeUp(true);

		_aphid.skin.SetFlipDirection(GlobalPosition - _aphid.GlobalPosition);
		HeldPickup.initial_offset = _aphid.skin.Position;
		HeldPickup.aphid = _aphid;
		HeldPickup.is_aphid = true;
		SoundManager.CreateSound2D(_aphid.AudioDynamic_Idle, _aphid.GlobalPosition, true);
	}
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
	public void Drop(bool _placeInWorld = true)
	{
		var _pickup = HeldPickup;

		if (_placeInWorld)
		{
			SetDisabled(true);
			SetPlayerAnim(StringNames.PickupAnim, true);
			RunDisabledTimer(0.45f);
			DisabledTimer.Timeout += () =>
			{
				if (HeldPickup.is_aphid)
					HeldPickup.aphid.skin.Position = HeldPickup.initial_offset;
				else if (HeldPickup.sprite != null)
					HeldPickup.sprite.Offset = HeldPickup.initial_offset;

				HeldPickup.Item.GlobalPosition = GlobalPosition + (flip_direction ? pickup_ground_position : -pickup_ground_position);
				HeldPickup.Item.ProcessMode = ProcessModeEnum.Inherit;
				HeldPickup.Item.SetMeta(StringNames.PickupMeta, true);
				HeldPickup = new();

				OnDrop?.Invoke(_pickup.tag, _pickup.Item);
			};
		}
		else
		{
			CanvasManager.RemoveControlPrompt("drop");
			HeldPickup.Item.QueueFree();
			HeldPickup = new();

			OnDrop?.Invoke(_pickup.tag, _pickup.Item);
		}
	}
	private void ProcessPickupBehaviour()
	{
		if (!IsInstanceValid(HeldPickup.Item))
		{
			HeldPickup = new();
			return;
		}
		bool _isSat = animator.Animation == StringNames.SitAnim;

		HeldPickup.Item.GlobalPosition = GlobalPosition;

		if (!HeldPickup.is_aphid)
		{
			if (HeldPickup.sprite != null)
				HeldPickup.sprite.Offset = new Vector2(0, -47 + (_isSat ? 12 : 0));
			else
				HeldPickup.Item.GlobalPosition += new Vector2(0, -48 + (_isSat ? 12 : 0));
		}
		else
		{
			HeldPickup.aphid.skin.SetFlipDirection(MovementDirection, true);
			HeldPickup.aphid.skin.Position = new(0, -39 + (_isSat ? 13 : 0));
		}
	}

	// ==========| General Functions |=============
	public void SetMovementDirection(Vector2 _direction)
	{
		MovementDirection = _direction.Normalized();
		SetFlipDirection(MovementDirection);

		is_moving = !MovementDirection.IsEqualApprox(Vector2.Zero);
		PlayMovementAnim(0, true);
	}
	private void PlayMovementAnim(float _delta, bool _ignoreDisabled = false)
	{
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
			spriteBody.Scale = new(Mathf.Lerp(spriteBody.Scale.X, -1, _delta * 6), spriteBody.Scale.Y);
		else
			spriteBody.Scale = new(Mathf.Lerp(spriteBody.Scale.X, 1, _delta * 6), spriteBody.Scale.Y);
	}
	public void SetPlayerAnim(StringName _name, bool _playBackwards = false)
	{
		if (_name.Equals(animator.Animation))
			return;
		if (!_playBackwards)
			animator.Play(_name);
		else
			animator.PlayBackwards(_name);
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
