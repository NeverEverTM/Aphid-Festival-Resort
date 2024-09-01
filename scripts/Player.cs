using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class Player : CharacterBody2D
{
	public static Player Instance { get; private set; }

	[Export] private Camera2D camera;
	[Export] private Area2D interactionArea;
	[Export] private Node2D spriteBody;
	[Export] private AnimatedSprite2D animator;
	[Export] private AudioStreamPlayer2D audioPlayer;
	[Export] public PlayerInventory inventory;

	/// <summary>
	/// Disables player input interaction.
	/// </summary>
	public bool IsDisabled { get; private set; }
	private int QueuedDisabled = 0;

	// Movement Params
	public Vector2 MovementDirection, FacingDirection;
	private float MovementSpeed = 100;
	private bool flipSwitch, IsRunning;
	private string currentAnimState = "";

	// Collision Params
	private string[] ValidInteractionTags = new string[]
	{
		"aphid", "npc", "menu"
	};

	// Pickup Params
	public Node2D PickupItem { set; get; }
	private Vector2 interactionAreaPosition = new(25, -10), pickup_ground_position = new(0, 4);
	private bool pickup_isAphid;
	public Timer LockPositionCooldown;

	public delegate void PickupEventHandler(string _tag);
	public delegate void DropEventHandler();
	public event PickupEventHandler OnPickup;
	public event DropEventHandler OnDrop;

	// Input Params
	private int interact_hold_cycles;
	private bool interact_is_being_held;

	public override void _EnterTree()
	{
		Instance = this;

		// facing
		FacingDirection = Vector2.Left;
		SetFlipDirection(MovementDirection);
		interactionArea.Position = new(flipSwitch ? interactionAreaPosition.X : -interactionAreaPosition.X,
		interactionAreaPosition.Y);
		// timers
		LockPositionCooldown = new()
		{
			OneShot = true
		};
		LockPositionCooldown.Timeout += () =>
		{
			SetDisabled(false, false);
			SetPlayerAnim("idle");
		};
		AddChild(LockPositionCooldown);

		SaveSystem.AddToProfileData(Instance);
	}
	private bool menuCheck;
	public override void _Ready()
	{
		GameManager.GlobalCamera = camera;
		animator.Play("idle");

		CanvasManager.Menus.OnSwitch += (bool _state, MenuUtil.MenuInstance _menu) =>
		{
			if (menuCheck != _state) // prevent multiple queues 
			{
				menuCheck = _state;
				SetDisabled(_state, _menu?.ID != "pause");
			}
		};
	}
	public override void _Process(double delta)
	{
		if (!IsDisabled)
			ReadKeyInput();

		if (PickupItem != null)
			ProcessPickupBehaviour();
	}
	public override void _PhysicsProcess(double delta)
	{
		if (MovementDirection != Vector2.Zero)
		{
			SetFlipDirection(MovementDirection);
			interactionArea.Position = new(flipSwitch ? interactionAreaPosition.X : -interactionAreaPosition.X,
				interactionAreaPosition.Y);
		}
		TickFlip((float)delta);

		MovementDirection = Vector2.Zero;

		if (!IsDisabled && !CanvasManager.Instance.IsInFocus)
		{
			ReadMovementInput();
			ReadHeldInput();
		}

		Velocity = MovementDirection * MovementSpeed;
		DoWalkAnim();
		MoveAndSlide();
	}
	/// <summary>
	/// Disables player interaction by queuing disable requests. 
	/// One must be careful to track and handle their requests.
	/// </summary>
	/// <param name="_queuedState">State to be queued. True adds a queue and False removes a queue</param>
	public void SetDisabled(bool _queuedState, bool _cancelActions = true)
	{
		QueuedDisabled += _queuedState ? 1 : -1;

		if (QueuedDisabled < 0)
		{
			QueuedDisabled = 0;
			Logger.Print(Logger.LogPriority.Error, "Player was requested to unqueue a disable call, but there was no queued disables!");
		}

		IsDisabled = QueuedDisabled > 0;

		if (IsDisabled && _cancelActions) // Disallow some behaviours from persisting after disabled
		{
			if (PickupItem != null)
				Drop();
			inventory?.SetInventoryHUD(false);
			SetPlayerAnim("idle");
		}
	}

	// =========| Input Related |========
	private void ReadMovementInput()
	{
		FacingDirection = Input.GetVector("left", "right", "up", "down");
		MovementDirection = Input.GetVector("left", "right", "up", "down").Normalized();
		if (IsRunning)
			MovementSpeed = 195;
		else
			MovementSpeed = 90;
	}
	private void DoWalkAnim()
	{
		if (!MovementDirection.IsEqualApprox(Vector2.Zero))
		{
			if (IsRunning)
				SetPlayerAnim("run");
			else
				SetPlayerAnim("walk");
		}
		else if (!IsDisabled)
			SetPlayerAnim("idle");
	}
	private void ReadKeyInput()
	{
		IsRunning = Input.IsActionPressed("run");
		// TODO: Input reader with names and functions
		if (Input.IsActionJustPressed("open_generations"))
		{
			CanvasManager.Menus.OpenMenu(GenerationsTracker.Instance.Menu);
			return;
		}

		if (Input.IsActionJustPressed("open_inventory"))
		{
			inventory?.SetInventoryHUD(!inventory.Visible);
			return;
		}

		if (Input.IsActionJustPressed("interact"))
		{
			TryInteract();
			return;
		}

		if (Input.IsActionJustPressed("pickup"))
		{
			if (PickupItem == null)
				TryPickup();
			else
				Drop();
			return;
		}

		if (Input.IsActionJustPressed("pull"))
		{
			// either pull the first item or store it in inventory
			if (PickupItem != null)
				inventory.StoreCurrentItem();
			else
				inventory.PullItem(0);
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

	// ======| Interactions |=======
	private void TryInteract()
	{
		Array<Node2D> _collisionList = interactionArea.GetOverlappingBodies();

		if (_collisionList.Count == 0)
			return;

		for (int i = 0; i < _collisionList.Count; i++)
		{
			if (!_collisionList[i].HasMeta("tag"))
				continue;
			if (CheckForTag(_collisionList[i]))
			{
				if (_collisionList[i].HasMethod("Interact")) // Within CollisionObject
					_collisionList[i].CallDeferred("Interact");
				else if (_collisionList[i].GetParent().HasMethod("Interact")) // Parent of CollisionObject
					_collisionList[i].GetParent().CallDeferred("Interact");
				return;
			}
		}
	}
	private bool CheckForTag(Node2D _item)
	{
		var tag = (string)_item.GetMeta("tag");
		return ValidInteractionTags.Contains(tag);
	}
	private void CallAllNearbyAphids()
	{
		PlaySound(ResourceLoader.Load<AudioStream>(GameManager.SFXPath + "/player/whistle.wav"));
		for (int i = 0; i < ResortManager.Instance.AphidsOnResort.Count; i++)
		{
			Aphid _aphid = ResortManager.Instance.AphidsOnResort[i];
			if (_aphid == PickupItem) // Dont call an Aphid if you are goddamn holding it
				continue;
			if (_aphid.GlobalPosition.DistanceTo(GlobalPosition) < 400)
				_aphid.CallTowards(GlobalPosition);
		}
	}

	// ======| Pickup |========
	private void TryPickup()
	{
		// Find a node
		Node _node = GetOverlappingNodeWithMeta("pickup");
		if (_node == null)
			return;

		if (!(bool)_node.GetMeta("pickup"))
			return;

		var _tag = _node.HasMeta("tag") ? (string)_node.GetMeta("tag") : null;
		// If is an aphid, do a bunch of extra shit
		if (_tag.Equals("aphid") && !OnAphidPickup(_node as Aphid))
			return;

		Pickup(_node as Node2D, _tag);
	}
	public async void Pickup(Node2D _node, string _tag)
	{
		SetDisabled(true, false);
		SetPlayerAnim("pickup");
		float _duration = animator.SpriteFrames.GetFrameCount("pickup") / 11f;
		LockPositionCooldown.Start(_duration);
		await Task.Delay((int)(_duration * 1000) - 100);

		_node.SetMeta("pickup", false);
		_node.ProcessMode = ProcessModeEnum.Disabled;
		_node.ZIndex = 1;
		PickupItem = _node;
		OnPickup?.Invoke(_tag);
	}
	private bool OnAphidPickup(Aphid _aphid)
	{
		if (_aphid.IsEating)
			return false;

		// if sleeping, get annoyed
		if (_aphid.OurState == Aphid.AphidState.Sleep)
			_aphid.WakeUp(true);

		_aphid.SetAphidState(Aphid.AphidState.Idle);
		_aphid.skin.SetFlipDirection(FacingDirection, true);
		pickup_isAphid = true;
		SoundManager.CreateSound2D(_aphid.AudioDynamic_Idle, _aphid.GlobalPosition, true);
		return true;
	}
	public async void Drop(bool _setPosition = true)
	{
		if (LockPositionCooldown.TimeLeft > 0)
			return;
		SetDisabled(true, false);
		SetPlayerAnim("pickup", true);
		float _duration = animator.SpriteFrames.GetFrameCount("pickup") / 13f;
		LockPositionCooldown.Start(_duration);
		await Task.Delay((int)(_duration * 1000) - 200);

		OnDrop?.Invoke();
		pickup_isAphid = false;
		if (_setPosition)
			PickupItem.GlobalPosition = interactionArea.GlobalPosition + pickup_ground_position;
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
	public static void PlaySound(AudioStream _audio, bool _pitchRand = false)
		=> SoundManager.CreateSound2D(_audio, Instance.GlobalPosition, _pitchRand);

	private Node GetOverlappingNodeWithMeta(string _meta)
	{
		var _collisionList = interactionArea.GetOverlappingBodies();
		if (_collisionList.Count <= 0)
			return null;

		for (int i = 0; i < _collisionList.Count; i++)
		{
			// Is this an aphid?
			var _item = _collisionList[i];
			if (!_item.HasMeta(_meta))
				continue;

			return _item;
		}
		return null;
	}
	private Node GetOverlappingNodeWithTag(string _tag)
	{
		var _collisionList = interactionArea.GetOverlappingBodies();
		if (_collisionList.Count <= 0)
			return null;

		for (int i = 0; i < _collisionList.Count; i++)
		{
			// Is this an aphid?
			var _item = _collisionList[i];
			if (!_item.HasMeta("_tag") && !((string)_item.GetMeta("tag")).Equals(_tag))
				continue;

			return _item;
		}
		return null;
	}

	public interface IObjectInteractable
	{
		/// <summary>
		/// Function used to dictate what happens when the player interacts with this.
		/// Must be the parent of or a CollisionObject2D itself with a CollisionShape2D acting as an interaction area.
		/// To be valid, make sure the CollisionObject has a tag and that it is included in the Player.ValidInteractionTags list. 
		/// </summary>
		public void Interact();
	}
}
