using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Player : CharacterBody2D
{
	public static Player Instance { get; private set; }

	[Export] private Camera2D camera;
	[Export] private Area2D interactionArea;
	[Export] private Node2D spriteBody;
	[Export] private AnimatedSprite2D animator;
	[Export] private AudioStream Audio_Whistle;
	[Export] public PlayerInventory inventory;

	/// <summary>
	/// Disables player input interaction.
	/// </summary>
	public bool IsDisabled { get; private set; }
	private int QueuedDisabled = 0;

	// Movement Params
	public Vector2 MovementDirection { get; set; }
	public Vector2 FacingDirection { get; set; }
	public float MovementSpeed = 100;
	private const float idleTimer_base = 4;
	private float idleTimer = idleTimer_base;
	private bool flipSwitch, IsRunning;
	private string currentAnimState = "";

	// Interaction Params
	public const string InteractableTag = "interactable";
	private string[] ValidInteractionTags = new string[]
	{
		Aphid.Tag, NPCBehaviour.Tag, "menu", InteractableTag
	};
	private readonly List<Node2D> interactables_nearby = new();
	private readonly List<Node2D> pickups_nearby = new();
	private readonly List<string> current_prompts = new();
	private Timer overlapping_monitoring = new();
	private int interact_hold_cycles;
	private bool interact_is_being_held;

	// Pickup Params
	public Node2D PickupItem { set; get; }
	private Vector2 pickup_ground_position = new(35, 0); // facing right by default
	private bool pickup_isAphid;
	protected Timer LockPositionCooldown;

	public delegate void PickupEventHandler(string _tag);
	public delegate void DropEventHandler();
	public event PickupEventHandler OnPickup;
	public event DropEventHandler OnDrop;

	// Audio
	private AudioStream audio_step;

	// Savedata params
	internal static SaveData Data;
	public static string NewName { get; set; }
	public static string[] NewPronouns { get; set; }

	public record SaveData
	{
		public string Name { get; set; }
		public string[] Pronouns { get; set; }
		public int Level { get; set; }
		public string Room { get; set; }

		public float PositionX { get; set; }
		public float PositionY { get; set; }

		public List<string> Inventory { get; set; }
		public List<string> Storage { get; set; }
		public int Currency { get; set; }
		public int InventoryMaxCapacity { get; set; }

		public SaveData()
		{
			Room = "resort_golden_grounds";
			Inventory = new();
			Storage = new();
			Currency = 30;
			Name = NewName;
			Pronouns = NewPronouns;
			InventoryMaxCapacity = 15;
		}
		public void SetCurrency(int _amount, bool _setToValue = false)
		{
			if (_setToValue)
				Currency = _amount;
			else
				Currency += _amount;
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
		}
		public SaveData Get()
		{
			Data.PositionX = Instance.GlobalPosition.X;
			Data.PositionY = Instance.GlobalPosition.Y;
			return Data;
		}
		public SaveData Default() => new();
	}

	public override void _EnterTree()
	{
		Instance = this;
		GlobalManager.GlobalCamera = camera;

		void _leaveItemInGround(GlobalManager.SceneName _)
		{
			if (PickupItem != null)
				PickupItem.GlobalPosition = GlobalPosition;
			GlobalManager.OnPreLoadScene -= _leaveItemInGround;
		}
		GlobalManager.OnPreLoadScene += _leaveItemInGround;

		// facing
		FacingDirection = Vector2.Left;
		SetFlipDirection(MovementDirection);
		// timers
		LockPositionCooldown = new()
		{
			OneShot = true
		};
		LockPositionCooldown.Timeout += () =>
		{
			SetDisabled(false);
			SetPlayerAnim("idle");
		};
		AddChild(LockPositionCooldown);

		SaveSystem.ProfileClassData.Add(new SaveSystem.SaveModule<SaveData>("player", new SaveModule(), 1000));
	}
	public override void _Ready()
	{
		audio_step = ResourceLoader.Load<AudioStream>(GlobalManager.SFXPath + "/player/step.wav");
		animator.FrameChanged += () =>
		{
			if (animator.Animation == "walk")
			{
				if (animator.Frame == 0 || animator.Frame == 3)
					SoundManager.CreateSound2D(audio_step, GlobalPosition, false).VolumeDb = -10;
			}
			if (animator.Animation == "run")
			{
				if (animator.Frame == 0 || animator.Frame == 3)
					SoundManager.CreateSound2D(audio_step, GlobalPosition);
			}
		};

		AddChild(overlapping_monitoring);
		overlapping_monitoring.Timeout += () =>
		{
			if (IsDisabled)
				return;

			CheckForInteractables();
			ChechForPickups();
		};
		overlapping_monitoring.Start(0.15f);

		CanvasManager.Menus.OnSwitch += (MenuUtil.MenuInstance _lastMenu, MenuUtil.MenuInstance _menu) =>
		{
			if (_menu != null && _menu.ID != "pause")
			{
				if (PickupItem != null)
					_ = Drop();
				inventory?.SetTo(false);
				if (animator.Animation == "idle" == (animator.Animation == "sit"))
					SetPlayerAnim("idle");
			}
		};
	}
	public override void _PhysicsProcess(double delta)
	{
		IsDisabled = QueuedDisabled > 0 || CanvasManager.Menus.IsBusy || CanvasManager.Instance.IsInFocus;
		// Calculate player movement
		MovementDirection = Vector2.Zero;
		if (!IsDisabled)
		{
			IsRunning = OptionsManager.Settings.SettingAutoRun ? !Input.IsActionPressed("run") : Input.IsActionPressed("run");
			ReadMovementInput();
			ReadHeldInput();
		}
		Velocity = MovementDirection * MovementSpeed;

		// apply movement phyiscs and animations
		TickFlip((float)delta);
		ProcessMovementAnimations((float)delta);
		MoveAndSlide();

		if (PickupItem != null && !GlobalManager.IsBusy)
			ProcessPickupBehaviour();
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsDisabled)
		{
			if (@event.IsActionPressed("open_generations") && CanvasManager.Menus.CurrentMenu.Equals(GenerationsTracker.Menu))
				CanvasManager.Menus.OpenMenu(GenerationsTracker.Menu);
			return;
		}

		//TODO: SET INPUT SYSTEM FOR THIS

		if (@event.IsActionPressed("open_generations"))
		{
			CanvasManager.Menus.OpenMenu(GenerationsTracker.Menu);
			return;
		}

		if (@event.IsActionPressed("open_inventory"))
		{
			inventory?.SetTo(!inventory.Visible);
			return;
		}

		if (@event.IsActionPressed("interact"))
		{
			TryInteract();
			return;
		}

		if (@event.IsActionPressed("pickup"))
		{
			if (PickupItem == null)
				TryPickup();
			else
				_ = Drop();
			return;
		}

		if (@event.IsActionPressed("pull"))
		{
			// either pull the first item or store it in inventory
			if (PickupItem != null)
				PlayerInventory.StoreCurrentItem();
			else
				inventory.PullItem(0);
		}
	}
	/// <summary>
	/// Disables player interaction by queuing disable requests. 
	/// One must be careful to track and handle their requests.
	/// </summary>
	/// <param name="_queuedState">State to be queued. True adds a queue and False removes a queue</param>
	public void SetDisabled(bool _queuedState, bool _cancelActions = false)
	{
		QueuedDisabled += _queuedState ? 1 : -1;

		if (QueuedDisabled < 0)
		{
			QueuedDisabled = 0;
			Logger.Print(Logger.LogPriority.Error, "Player was requested to unqueue a disable call, but there was no queued disables!");
		}

		IsDisabled = QueuedDisabled > 0 || CanvasManager.Menus.IsBusy || CanvasManager.Instance.IsInFocus;

		if (_cancelActions)
		{
			if (PickupItem != null)
				_ = Drop();
			inventory?.SetTo(false);
			if (animator.Animation == "idle" == (animator.Animation == "sit"))
				SetPlayerAnim("idle");
		}
	}
	/// <summary>
	/// Runs a global player timer that deactivates the disable state after the timer runs out.
	/// This function DOES NOT call SetDisabled, and it can be overriden if someone calls it midway through.
	/// (It properly disposes of its last call if so)
	/// </summary>
	public void RunDisabledTimer(float _secondsDuration)
	{
		if (LockPositionCooldown.TimeLeft > 0) // end early
		{
			SetDisabled(false);
			SetPlayerAnim("idle");
		}

		LockPositionCooldown.Start(_secondsDuration);
	}

	// =========| Interaction Related |========
	private void ReadMovementInput()
	{
		FacingDirection = Input.GetVector("left", "right", "up", "down");
		MovementDirection = Input.GetVector("left", "right", "up", "down").Normalized();

		if (IsRunning)
			MovementSpeed = 195;
		else
			MovementSpeed = 90;
	}
	private void ProcessMovementAnimations(float _delta)
	{
		if (!MovementDirection.IsEqualApprox(Vector2.Zero))
		{
			idleTimer = idleTimer_base;
			if (IsRunning)
				SetPlayerAnim("run");
			else
				SetPlayerAnim("walk");
			SetFlipDirection(MovementDirection);
		}
		else if (!IsDisabled)
		{
			if (idleTimer > 0)
			{
				idleTimer -= _delta;
				SetPlayerAnim("idle");
			}
			else
				SetPlayerAnim("sit");
		}
	}
	private void ReadHeldInput()
	{
		interact_is_being_held = Input.IsActionPressed("interact");

		if (interact_is_being_held)
		{
			interact_hold_cycles++;
			if (interact_hold_cycles == 30)
				CallAllNearbyAphids();
		}
		else
			interact_hold_cycles = 0;
	}

	private void CheckForInteractables()
	{
		Godot.Collections.Array<Node2D> _collisionList = interactionArea.GetOverlappingBodies();
		List<string> _last_prompts = current_prompts.ToList();
		current_prompts.Clear();
		interactables_nearby.Clear();

		for (int i = 0; i < _collisionList.Count; i++)
		{
			if (!_collisionList[i].HasMeta("tag"))
				continue;
			string _tag = (string)_collisionList[i].GetMeta("tag");
			if (ValidInteractionTags.Contains(_tag))
			{
				interactables_nearby.Add(_collisionList[i]);
				current_prompts.Add(_tag);
			}
		}

		// we set the popup control prompts to indicate interactibility with objects nearby
		SetPromptsIfAny();

		// then we remove any previous instances where the player is no longer nearby an object that triggers the prompt
		for (int i = 0; i < _last_prompts.Count; i++)
		{
			if (current_prompts.Contains(_last_prompts[i]))
				continue;

			CanvasManager.RemoveControlPrompt(_last_prompts[i]);
		}
	}
	private void ChechForPickups()
	{
		if (PickupItem != null)
			return;

		var _collisionList = interactionArea.GetOverlappingBodies();
		pickups_nearby.Clear();

		for (int i = 0; i < _collisionList.Count; i++)
		{
			var _item = _collisionList[i];
			if (!_item.HasMeta("pickup")) // is it a pickup
				continue;

			if (!(bool)_item.GetMeta("pickup")) // can it be picked up
				return;

			pickups_nearby.Add(_item);
		}

		if (pickups_nearby.Count > 0)
			CanvasManager.AddControlPrompt(CanvasManager.Prompt_Pickup, InputNames.Pickup, InputNames.Pickup);
		else
			CanvasManager.RemoveControlPrompt(InputNames.Pickup);
	}
	internal void TryInteract()
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

		if (_node.HasMethod("Interact")) // Within CollisionObject
			_node.CallDeferred("Interact");
		else if (_node.GetParent().HasMethod("Interact")) // Parent of CollisionObject
			_node.GetParent().CallDeferred("Interact");
	}
	private void SetPromptsIfAny()
	{
		if (PickupItem != null)
			return;

		if (current_prompts.Contains(Aphid.Tag))
		{
			if ((interactables_nearby.Find((e) => e is Aphid) as Aphid).IsReadyForHarvest)
				CanvasManager.AddControlPrompt(CanvasManager.Prompt_Harvest, Aphid.Tag, InputNames.Interact);
			else
				CanvasManager.AddControlPrompt(CanvasManager.Prompt_Pet, Aphid.Tag, InputNames.Interact);
		}

		if (current_prompts.Contains(NPCBehaviour.Tag))
			CanvasManager.AddControlPrompt(CanvasManager.Prompt_Talk, NPCBehaviour.Tag, InputNames.Interact);

		if (current_prompts.Contains("menu"))
			CanvasManager.AddControlPrompt("prompt_open_menu", "menu", InputNames.Interact);

		if (current_prompts.Contains("interactable"))
			CanvasManager.AddControlPrompt(CanvasManager.Prompt_Interact, InteractableTag, InputNames.Interact);
	}

	private async void CallAllNearbyAphids()
	{
		if (PickupItem != null)
			return;
		MovementDirection = Vector2.Zero;
		SetDisabled(true);
		SetPlayerAnim("whistle");
		await Task.Delay(500);
		SoundManager.CreateSound2D(Audio_Whistle, Instance.GlobalPosition, true);
		for (int i = 0; i < ResortManager.CurrentResort.AphidsOnResort.Count; i++)
		{
			Aphid _aphid = ResortManager.CurrentResort.AphidsOnResort[i];
			if (_aphid.GlobalPosition.DistanceTo(GlobalPosition) < 400)
				_aphid.CallTowards(GlobalPosition);
		}
		await Task.Delay(600);
		SetDisabled(false);
	}

	private void TryPickup()
	{
		if (IsDisabled || pickups_nearby.Count == 0)
			return;

		var _node = pickups_nearby[0];
		var _tag = _node.HasMeta("tag") ? (string)_node.GetMeta("tag") : "item";
		// If is an aphid, do a bunch of extra shit
		if (_tag == Aphid.Tag && IsAphidBusy(_node as Aphid))
			return;

		_ = Pickup(_node, _tag);
	}
	public async Task Pickup(Node2D _node, string _tag, bool _playAnim = true)
	{
		if (LockPositionCooldown.TimeLeft > 0)
			return;

		
		_node.SetMeta("pickup", false);
		_node.ProcessMode = ProcessModeEnum.Disabled;

		if (_playAnim)
		{
			SetDisabled(true);
			SetPlayerAnim("pickup");
			SetFlipDirection(_node.GlobalPosition - GlobalPosition);
			RunDisabledTimer(0.5f);
			await Task.Delay((int)(0.5f * 1000) - 100);
		}

		_node.ZIndex = 1;
		PickupItem = _node;
		CanvasManager.RemoveControlPrompt(InputNames.Pickup);
		CanvasManager.AddControlPrompt(CanvasManager.Prompt_Drop, InputNames.Pickup, InputNames.Pickup);
		OnPickup?.Invoke(_tag);
	}
	private bool IsAphidBusy(Aphid _aphid)
	{
		if (_aphid.IsEating)
			return true;

		// if sleeping, get annoyed
		if (_aphid.State.Is(Aphid.StateEnum.Sleep))
			_aphid.WakeUp(true);

		_aphid.SetState(Aphid.StateEnum.Idle);
		_aphid.skin.SetFlipDirection(FacingDirection, true);
		pickup_isAphid = true;
		SoundManager.CreateSound2D(_aphid.AudioDynamic_Idle, _aphid.GlobalPosition, true);
		return false;
	}
	public async Task Drop(bool _setPosition = true)
	{
		if (LockPositionCooldown.TimeLeft > 0 ||
		 GlobalManager.Utils.Raycast(GlobalPosition, flipSwitch ? pickup_ground_position : -pickup_ground_position, null).Count > 0)
			return;
		CanvasManager.RemoveControlPrompt(InputNames.Pickup);
		SetDisabled(true);
		SetPlayerAnim("pickup", true);
		RunDisabledTimer(0.5f);
		await Task.Delay((int)(0.4f * 1000));

		OnDrop?.Invoke();
		pickup_isAphid = false;
		if (_setPosition)
			PickupItem.GlobalPosition = GlobalPosition + (flipSwitch ? pickup_ground_position : -pickup_ground_position);
		PickupItem.ProcessMode = ProcessModeEnum.Inherit;
		PickupItem.ZIndex = 0;
		PickupItem.SetMeta("pickup", true);
		PickupItem = null;
	}
	private void ProcessPickupBehaviour()
	{
		if (!IsInstanceValid(PickupItem))
		{
			PickupItem = null;
			return;
		}

		// Put it in front of you at the right depth
		PickupItem.GlobalPosition = GlobalPosition - new Vector2(0, 38 + (pickup_isAphid ? 0 : 8));
		if (pickup_isAphid) // flip it to face towards your facing direction
			(PickupItem as Aphid).skin.SetFlipDirection(FacingDirection, true);
	}

	// ==========| General Functions |=============
	public void SetFlipDirection(Vector2 _direction)
	{
		// True : Facing Right - False : Facing Left
		if (_direction.X < 0)
			flipSwitch = false;
		else if (_direction.X > 0)
			flipSwitch = true;
	}
	private void TickFlip(float _delta)
	{
		// True : Facing Right - False : Facing Left
		if (flipSwitch)
			spriteBody.Scale = new(Mathf.Lerp(spriteBody.Scale.X, -1, _delta * 6), spriteBody.Scale.Y);
		else
			spriteBody.Scale = new(Mathf.Lerp(spriteBody.Scale.X, 1, _delta * 6), spriteBody.Scale.Y);
	}
	public void SetPlayerAnim(string _name, bool _playBackwards = false)
	{
		if (_name.Equals(animator.Animation))
			return;
		if (!_playBackwards)
			animator.Play(_name);
		else
			animator.PlayBackwards(_name);
	}

	public interface IPlayerInteractable
	{
		/// <summary>
		/// Function used to dictate what happens when the player interacts with this.
		/// Must be the parent of or a CollisionObject2D itself with a CollisionShape2D acting as an interaction area.
		/// To be valid, make sure the CollisionObject has a tag and that it is included in the Player.ValidInteractionTags list. 
		/// </summary>
		public void Interact();
	}
}
