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
	public float MovementSpeed = 90;
	private const float LONG_IDLE_BASE = 4;
	private float idle_timer = LONG_IDLE_BASE;
	private bool flip_direction = true, is_running;
	protected Timer LockPositionCooldown;

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
	public PickupData HeldPickup { set; get; }
	private Vector2 pickup_ground_position = new(35, 0); // facing right by default
	public record PickupData
	{
		public Node2D Item;
		public bool is_aphid;
		public Aphid aphid;
		public Sprite2D sprite;
		public Vector2 initial_offset;
	}

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
		public List<string> RecipesDiscovered { get; set; }
		public int Currency { get; set; }
		public int InventoryMaxCapacity { get; set; }

		public SaveData()
		{
			Room = "resort_golden_grounds";
			Inventory = new();
			Storage = new();
			RecipesDiscovered = new();
			Currency = 30;
			Name = NewName;
			Pronouns = NewPronouns;
			InventoryMaxCapacity = 15;
		}
		public void AddCurrency(int _amount)
		{
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
			if (HeldPickup.Item != null)
				HeldPickup.Item.GlobalPosition = GlobalPosition;
			GlobalManager.OnPreLoadScene -= _leaveItemInGround;
		}
		GlobalManager.OnPreLoadScene += _leaveItemInGround;
		HeldPickup = new();

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

		SaveSystem.ProfileClassData.Add(new SaveSystem.SaveModule<SaveData>("player", new SaveModule(), 1000)
		{
			Extension = SaveSystem.SAVEFILE_EXTENSION,
		});
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
				if (HeldPickup.Item != null)
					_ = Drop();
				inventory?.SetTo(false);
				if (animator.Animation != "idle")
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
			is_running = OptionsManager.Settings.SettingAutoRun ? !Input.IsActionPressed("run") : Input.IsActionPressed("run");
			ReadMovementDirection();
			ReadHeldInput();
		}
		Velocity = MovementDirection * (MovementSpeed * (is_running ? 2.2f : 1));

		// apply movement phyiscs and animations
		TickFlip((float)delta);
		ProcessMovementAnimations((float)delta);
		MoveAndSlide();
		if (HeldPickup.Item != null && !GlobalManager.IsBusy)
			ProcessPickupBehaviour();
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsDisabled)
		{
			if (@event.IsActionPressed("open_generations") && CanvasManager.Menus.CurrentMenu != null && CanvasManager.Menus.CurrentMenu.Equals(GenerationsTracker.Menu))
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
			if (HeldPickup.Item == null)
				TryPickup();
			else
				_ = Drop();
			return;
		}

		if (@event.IsActionPressed("pull"))
		{
			// either pull the first item or store it in inventory
			if (HeldPickup.Item != null)
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
			if (HeldPickup.Item != null)
				_ = Drop();
			inventory?.SetTo(false);
			if (animator.Animation != "idle")
				SetPlayerAnim("idle");
			MovementDirection = Vector2.Zero;
		}
	}
	/// <summary>
	/// Runs a timer that deactivates the disable state after the timer runs out.
	/// This function initially does not call SetDisabled and must be called beforehand. It can be overriden if someone calls it midway through.
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

	// interactables
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

		if (_node.HasMethod("Interact")) // Within CollisionObject
			_node.CallDeferred("Interact");
		else if (_node.GetParent().HasMethod("Interact")) // Parent of CollisionObject
			_node.GetParent().CallDeferred("Interact");
	}
	private void ProcessMovementAnimations(float _delta)
	{
		if (!MovementDirection.IsEqualApprox(Vector2.Zero))
		{
			idle_timer = LONG_IDLE_BASE;
			if (is_running)
				SetPlayerAnim("run");
			else
				SetPlayerAnim("walk");
			SetFlipDirection(MovementDirection);
		}
		else if (!IsDisabled)
		{
			if (idle_timer > 0)
			{
				idle_timer -= _delta;
				SetPlayerAnim("idle");
			}
			else
				SetPlayerAnim("sit");
		}
		else
		{
			if (CanvasManager.Instance.IsInFocus)
				SetPlayerAnim("idle");
			idle_timer = LONG_IDLE_BASE;
		}
	}
	private void ReadMovementDirection()
	{
		Vector2 _direction = Input.GetVector("left", "right", "up", "down");

		SetMovementDirection(_direction);
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
	private async void CallAllNearbyAphids()
	{
		if (HeldPickup.Item != null)
			return;

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

	// control prompts
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
		if (HeldPickup.Item != null)
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
	private void SetPromptsIfAny()
	{
		if (HeldPickup.Item != null && CanvasManager.Menus.IsBusy)
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

	// pickups
	private void TryPickup()
	{
		if (IsDisabled || pickups_nearby.Count == 0)
			return;

		var _node = pickups_nearby[0];
		var _tag = _node.HasMeta("tag") ? (string)_node.GetMeta("tag") : "item";
		// If is an aphid, do a bunch of extra shit
		if (_tag == Aphid.Tag)
		{
			if (!IsAphidBusy(_node as Aphid))
				SetAphidPickup(_node as Aphid);
			else
				return;
		}

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

		HeldPickup.Item = _node;
		if (!HeldPickup.is_aphid)
		{
			var _children = HeldPickup.Item.FindChildren("*", "Sprite2D");
			if (_children.Count > 0)
			{
				HeldPickup.sprite = _children[0] as Sprite2D;
				HeldPickup.initial_offset = HeldPickup.sprite.Offset;
			}
		}
		CanvasManager.RemoveControlPrompt(InputNames.Pickup);
		CanvasManager.AddControlPrompt(CanvasManager.Prompt_Drop, InputNames.Pickup, InputNames.Pickup);
		OnPickup?.Invoke(_tag);
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

		_aphid.skin.SetFlipDirection(FacingDirection, true);
		HeldPickup.initial_offset = _aphid.skin.Position;
		HeldPickup.is_aphid = true;
		HeldPickup.aphid = _aphid;
		SoundManager.CreateSound2D(_aphid.AudioDynamic_Idle, _aphid.GlobalPosition, true);
	}
	public async Task Drop(bool _setPosition = true)
	{
		if (LockPositionCooldown.TimeLeft > 0 ||
		 GlobalManager.Utils.Raycast(GlobalPosition, flip_direction ? pickup_ground_position : -pickup_ground_position, null).Count > 0)
			return;
		CanvasManager.RemoveControlPrompt(InputNames.Pickup);
		SetDisabled(true);
		SetPlayerAnim("pickup", true);
		RunDisabledTimer(0.5f);
		await Task.Delay((int)(0.4f * 1000));

		OnDrop?.Invoke();
		if (HeldPickup.is_aphid)
			HeldPickup.aphid.skin.Position = HeldPickup.initial_offset;
		if (HeldPickup.sprite != null)
			HeldPickup.sprite.Offset = HeldPickup.initial_offset;

		if (_setPosition)
			HeldPickup.Item.GlobalPosition = GlobalPosition + (flip_direction ? pickup_ground_position : -pickup_ground_position);
		HeldPickup.Item.ProcessMode = ProcessModeEnum.Inherit;
		HeldPickup.Item.SetMeta("pickup", true);
		HeldPickup = new();
	}
	private void ProcessPickupBehaviour()
	{
		if (!IsInstanceValid(HeldPickup.Item))
		{
			HeldPickup = new();
			return;
		}
		bool _isSat = animator.Animation == "sit";

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
			HeldPickup.aphid.skin.SetFlipDirection(FacingDirection, true);
			HeldPickup.aphid.skin.Position = new(0, -39 + (_isSat ? 13 : 0));
		}
	}

	// ==========| General Functions |=============
	private void SetMovementDirection(Vector2 _direction)
	{
		FacingDirection = _direction;
		MovementDirection = _direction.Normalized();
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
