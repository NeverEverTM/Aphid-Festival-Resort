using System;
using Godot;
using Godot.Collections;

public partial class Player : CharacterBody2D
{
	public static Player Instance { get; private set; }
	public static AudioStream Audio_Whistle { get; set; }

	[Export] private Camera2D camera;
	[Export] private Area2D interactionArea;
	[Export] private Node2D spriteBody;
	[Export] private AnimationPlayer animator;
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
	private string[] tagsToLookFor = new string[]
	{
		"aphid", "npc"
	};
	private Action<Node2D>[] collisionActions;

	// Pickup Params
	public Node2D PickupItem { set; get; }

	private Vector2 pickupitem_position = new(25, -10), pickup_ground_position = new(0, 4);
	private bool pickup_isAphid;

	public delegate void PickupEventHandler(string _tag);
	public delegate void DropEventHandler();
	public event PickupEventHandler OnPickup;
	public event DropEventHandler OnDrop;

	// Pet Params
	private Timer pet_timer;

	// Input Params
	private int interact_hold_cycles;
	private bool interact_is_being_held;

	public override void _EnterTree()
	{
		Instance = this;

		// facing
		FacingDirection = Vector2.Left;
		SetFlipDirection(MovementDirection);
		Vector2 _position;
		_position.X = flipSwitch ? pickupitem_position.X : -pickupitem_position.X;
		_position.Y = pickupitem_position.Y;
		interactionArea.Position = _position;

		// timers
		pet_timer = new()
		{
			OneShot = true
		};
		pet_timer.Timeout += () =>
		{
			SetDisabled(false);
			SetPlayerAnim("idle");
		};
		AddChild(pet_timer);

		SaveSystem.AddToProfileData(Instance);
	}
	private bool menuCheck;
	public override void _Ready()
	{
		GameManager.GlobalCamera = camera;
		SetPlayerAnim("idle");

		// Collision Interactions
		collisionActions = new Action<Node2D>[]
		{
			Instance.TryAphidInteract, (Node2D _node) => {
				if (_node.HasMethod(NPCDialog.MethodName.Interact))
					_node.Call(NPCDialog.MethodName.Interact);
			 }
		};

		CanvasManager.Menus.OnSwitch += (bool _state, MenuUtil.MenuInstance _) =>
		{
			if (menuCheck != _state) // prevent multiple queues 
			{
				menuCheck = _state;
				SetDisabled(_state);
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
			Vector2 _position;
			_position.X = flipSwitch ? pickupitem_position.X : -pickupitem_position.X;
			_position.Y = pickupitem_position.Y;
			interactionArea.Position = _position;
		}
		TickFlip((float)delta);

		Vector2 _lastVector = MovementDirection;
		MovementDirection = Vector2.Zero;

		if (!IsDisabled && !CanvasManager.Instance.IsInFocus)
		{
			ReadMovementInput();
			ReadHeldInput();
		}

		Velocity = MovementDirection * MovementSpeed;
		DoWalkAnim(_lastVector);
		MoveAndSlide();
	}
	/// <summary>
	/// Disables player interaction by queuing disable requests. 
	/// One must be careful to track and handle their requests.
	/// </summary>
	/// <param name="_queuedState">State to be queued. True adds a queue and False removes a queue</param>
	public void SetDisabled(bool _queuedState)
	{
		QueuedDisabled += _queuedState ? 1 : -1;

		if (QueuedDisabled < 0)
		{
			QueuedDisabled = 0;
			Logger.Print(Logger.LogPriority.Error, "Player was requested to unqueue a disable call, but there was no queued disables!");
		}

		IsDisabled = QueuedDisabled > 0;

		if (IsDisabled) // Disallow some behaviours from persisting after disabled
		{
			if (PickupItem != null)
				Drop();
			inventory?.SetInventoryHUD(false);
		}
	}

	// =========| Input Related |========
	private void ReadMovementInput()
	{
		FacingDirection = Input.GetVector("left", "right", "up", "down");
		MovementDirection = Input.GetVector("left", "right", "up", "down").Normalized();
	}
	private void DoWalkAnim(Vector2 _lastVector)
	{
		if (!MovementDirection.IsEqualApprox(Vector2.Zero))
		{
			if (IsRunning)
			{
				MovementSpeed = 175;
				SetPlayerAnim("run");
			}
			else
			{
				MovementSpeed = 100;
				SetPlayerAnim("walk");
			}
		}
		else if (!_lastVector.IsEqualApprox(Vector2.Zero))
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

		if (Input.IsActionJustPressed("cancel") && !CanvasManager.Menus.IsInMenu)
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
			int _index = CheckForTag(_collisionList[i]);
			if (_index != -1)
			{
				collisionActions[_index](_collisionList[i]);
				return;
			}
		}
	}
	private int CheckForTag(Node2D _item)
	{
		var tag = (string)_item.GetMeta("tag");
		for (int t = 0; t < tagsToLookFor.Length; t++)
		{
			if (tag.Equals(tagsToLookFor[t]))
				return t;
		}
		return -1;
	}
	private void TryAphidInteract(Node2D _node)
	{
		Aphid _aphid = _node as Aphid;

		if (_aphid.IsDisabled)
			return;

		if (_aphid.IsReadyForHarvest) // Harvest behaviour
		{
			if (_aphid.OurState == Aphid.AphidState.Breed || _aphid.OurState == Aphid.AphidState.Train)
				return;

			if (_aphid.OurState == Aphid.AphidState.Sleep)
				_aphid.WakeUp(true);

			_aphid.Harvest();
		}
		else // Pet behaviour
		{
			// Dont pet if item is in hand
			if (PickupItem != null)
				return;

			// if succesful pet, make it look at you and stay yourself in place
			if (_aphid.Pet())
			{
				animator.Play("player/pet");
				_aphid.skin.SetFlipDirection(GlobalPosition - _aphid.GlobalPosition);
				MovementDirection = Vector2.Zero;

				// timer
				SetDisabled(true);
				pet_timer.Start(AphidData.PET_DURATION);
			}
		}
	}
	private void CallAllNearbyAphids()
	{
		PlaySound(Audio_Whistle);
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
	public void Pickup(Node2D _node, string _tag)
	{
		_node.ProcessMode = ProcessModeEnum.Disabled;
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
	public void Drop(bool _setPosition = true)
	{
		OnDrop?.Invoke();
		pickup_isAphid = false;
		if (_setPosition)
			PickupItem.GlobalPosition = interactionArea.GlobalPosition + pickup_ground_position;
		PickupItem.ProcessMode = ProcessModeEnum.Inherit;
		PickupItem = null;
	}
	private void ProcessPickupBehaviour()
	{
		if (!IsInstanceValid(PickupItem))
		{
			PickupItem = null;
			return;
		}
		// This object is no longer a valid pickup item, drop it
		if (!(bool)PickupItem.GetMeta("pickup"))
		{
			Drop(false);
			return;
		}

		// Put it in front of you at the right depth
		PickupItem.GlobalPosition = interactionArea.GlobalPosition;
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
	public void SetPlayerAnim(string _name)
	{
		var _action = currentAnimState switch
		{
			"behind" => "/behind", // Currently unused (Maybe itll remain unused?)
			_ => ""
		};
		var _anim = $"player{_action}/{_name}";
		if (_anim.Equals(animator.CurrentAnimation))
			return;
		animator.Play($"player{_action}/{_name}");
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
}
