using System;
using Godot;
using Godot.Collections;

public partial class Player : CharacterBody2D
{
	[Export] private Camera2D camera;
	[Export] private Area2D interactionArea;
	[Export] private Node2D spriteBody;
	[Export] private AnimationPlayer animator;
	[ExportCategory("Inventory")]
	[Export] private Control inventory_panel;
	[Export] private AnimationPlayer inventory_player;
	[Export] private HBoxContainer inventoryGrid;
	[Export] private PackedScene invItemContainer;

	public static Player Instance;

	// Disable
	public bool IsExplicitlyDisabled { get; private set; }
	private bool IsDisabled, IsRunning;

	// Movement Params
	public Vector2 MovementDirection, FacingDirection;
	private float MovementSpeed = 100;
	private bool flipSwitch;
	private string currentAnimState = "";

	// Collision Params
	private string[] tagsToLookFor = new string[]
	{
		"aphid",
	};
	private Action<Node2D>[] collisionActions;

	// Pickup Params
	public Node2D PickupItem { private set; get; }

	private Vector2 pickup_ground_position = new(0, 8);
	private bool pickup_isAphid;

	public delegate void PickupEventHandler(string _tag);
	public delegate void DropEventHandler();
	public event PickupEventHandler OnPickup;
	public event DropEventHandler OnDrop;

	// Pet Params
	private float pet_timer;

	public override void _EnterTree()
	{
		Instance = this;
		FacingDirection = Vector2.Left;

		if (ResortManager.IsNewGame)
			Instance.CreateInvItem(0);
	}
	public override void _Ready()
	{
		GameManager.GlobalCamera = camera;
		SetPlayerAnim("idle");

		collisionActions = new Action<Node2D>[]
		{
			Instance.TryPet,
		};
	}
	public override void _Process(double delta)
	{
		ZIndex = (int)GlobalPosition.Y + 8;
		savedata.PositionX = GlobalPosition.X;
		savedata.PositionY = GlobalPosition.Y;

		// Pet Timer
		if (pet_timer > 0)
			pet_timer -= (float)delta;

		// Disable functions if any of the left conditions is met, or is explicitly disabled
		IsDisabled = pet_timer > 0 || CanvasManager.IsInFocus || IsExplicitlyDisabled;
		if (IsDisabled)
			return;

		// Process Inputs and Behaviours
		ReadMovementInput();
		ReadKeyInput();

		interactionArea.Position = FacingDirection * 25;
		if (PickupItem != null)
			ProcessPickupBehaviour();
	}
	public override void _PhysicsProcess(double delta)
	{
		if (MovementDirection != Vector2.Zero)
			SetFlipDirection(MovementDirection);
		TickFlip((float)delta);
		if (IsDisabled)
			return;
		Velocity = MovementDirection * MovementSpeed;
		MoveAndSlide();
	}

	public void SetDisabled(bool _state)
	{
		IsExplicitlyDisabled = _state;
		if (_state)
			animator.Play("player/idle");
		if (inventory_panel.Visible)
			inventory_panel.Hide();
	}

	// =========| Input Related |========
	private void ReadMovementInput()
	{
		var _up = Input.IsActionPressed("up");
		var _down = Input.IsActionPressed("down");
		var _left = Input.IsActionPressed("left");
		var _right = Input.IsActionPressed("right");

		if (_left)
			FacingDirection = Vector2.Left;
		else if (_right)
			FacingDirection = Vector2.Right;

		MovementDirection = new Vector2((_left ? -1 : 0) + (_right ? 1 : 0),
			(_up ? -1 : 0) + (_down ? 1 : 0)).Normalized();

		if (MovementDirection != Vector2.Zero)
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
		else
			SetPlayerAnim("idle");
	}
	private void ReadKeyInput()
	{
		if (Input.IsActionJustPressed("interact"))
			TryInteract();

		if (Input.IsActionJustPressed("pickup"))
		{
			if (PickupItem == null)
				TryPickup();
			else
				Drop();
		}

		if (Input.IsActionJustPressed("cancel") && PickupItem != null)
			StoreCurrentItem();

		if (Input.IsActionJustPressed("open_inventory"))
		{
			if (!Instance.inventory_panel.Visible)
				inventory_player.Play("slide_up");
			else
				inventory_player.Play("slide_down");
		}

		IsRunning = Input.IsActionPressed("run");
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
				collisionActions[_index](_collisionList[i]);
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
	private void TryPet(Node2D _node)
	{
		// Dont pet if item is in hand
		if (PickupItem != null)
			return;

		// if succesful pet, make it look at you and stay yourself in place
		var _aphid = _node as Aphid;
		if (_aphid.Pet())
		{
			animator.Play("player/pet");
			_aphid.skin.SetFlipDirection(GlobalPosition - _aphid.GlobalPosition);
			pet_timer = 2;
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
	private void Pickup(Node2D _node, string _tag)
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
		if (_aphid.OurState == Aphid.AphidState.Sleeping)
			_aphid.WakeUp(true);

		_aphid.SetAphidState(Aphid.AphidState.Idle);
		_aphid.skin.SetFlipDirection(FacingDirection, true);
		pickup_isAphid = true;
		_aphid.PlaySound(_aphid.idle, true);
		return true;
	}
	public void Drop(bool _setPosition = true)
	{
		OnDrop?.Invoke();
		pickup_isAphid = false;
		if (_setPosition)
			PickupItem.GlobalPosition = interactionArea.GlobalPosition + pickup_ground_position;
		PickupItem.ZIndex = (int)PickupItem.GlobalPosition.Y;
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
		PickupItem.ZIndex = (int)PickupItem.GlobalPosition.Y + 14;
		if (pickup_isAphid) // flip it to face towards your facing direction
			(PickupItem as Aphid).skin.SetFlipDirection(FacingDirection, true);
	}

	// =======| Inventory |========
	private void PullItem(string _item_name)
	{
		savedata.Inventory.Remove(_item_name);
		Node2D _item = ResortManager.CreateItem(_item_name);

		if (PickupItem != null)
			Drop();
		Pickup(_item, _item.GetMeta("tag").ToString());
	}
	public bool StoreItem(string _item)
	{
		if (savedata.Inventory.Count >= 15)
			return false;
		savedata.Inventory.Add(_item);
		CreateInvItem(savedata.Inventory.Count - 1);

		// Notify about addon

		return true;
	}
	public void StoreCurrentItem()
	{
		if (PickupItem.GetMeta("tag").ToString() == "aphid")
			return;

		if (!StoreItem(PickupItem.GetMeta("id").ToString()))
			return;

		PickupItem.QueueFree();
		PickupItem = null;
	}

	private void CreateInvItem(int _index)
	{
		TextureButton _item = invItemContainer.Instantiate() as TextureButton;
		(_item.GetChild(0) as TextureRect).Texture = GameManager.G_ICONS[savedata.Inventory[_index]];
		var _item_name = savedata.Inventory[_index];
		_item.Pressed += () =>
		{
			PullItem(_item_name);
			_item.QueueFree();
		};
		inventoryGrid.AddChild(_item);
	}

	// General Functions
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
	private void SetPlayerAnim(string _name)
	{
		var _action = currentAnimState switch
		{
			"behind" => "/behind",
			_ => ""
		};
		var _anim = $"player{_action}/{_name}";
		if (_anim.Equals(animator.CurrentAnimation))
			return;
		animator.Play($"player{_action}/{_name}");
	}

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
