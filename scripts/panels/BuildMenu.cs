using System.Collections.Generic;
using Godot;

public partial class BuildMenu : Control
{
	internal static BuildMenu Instance { get; private set; }
	internal static MenuInstance Menu { get; private set; }

	[Export] private GridContainer storageContainer;
	[Export] private AnimationPlayer menuPlayer;
	[Export] private TextureButton buildButton, storageButton;
	[Export] private Label controlPrompt;
	[Export] private PackedScene itemContainer;

	internal static bool DEBUG_SHOW_RECTS { get; set; }
	private enum RemovalMode { Sell, Store }
	public enum PlaceableArea { NotValid = -1, Ground, Water, Wall }

	private readonly List<Building> active_buildings = [];
	private Building selected_building;
	private Vector2 mouse_offset, last_valid_position = new();
	private bool is_hovering_building, is_moving_building, is_storage_open;

	// last properties of current structure
	private int previous_light_mask;
	private Material previous_material;
	private uint previous_collision_layer; // TODO: Check if this doesnt cause any issues (shouldnt it be just deacct phyiscs?)

	public override void _EnterTree()
	{
		Instance = this;
		storageButton.Pressed += SetStorage;
		controlPrompt.Text = ControlsManager.GetLocalizedActionName(InputNames.OpenInventory);
	}
	public override void _Ready()
	{
		Menu = new MenuInstance("build", menuPlayer,
			_ => OnOpenMenu(), OnCloseMenu);
		buildButton.Pressed += () => _ = CanvasManager.Menus.SetTo(Menu);
		ResortManager.Current.SaveModule.AddEventListener(APPLY_OUTOFBOUND_PATCH, SaveSystem.SaveEventsEnum.OnLoadFinish);
	}

	public void OnOpenMenu()
	{
		is_storage_open = false;
		FreeCameraManager.SetHUDTo(false);
		if (IsInstanceValid(CameraManager.FocusedObject))
			FreeCameraManager.StopFocus();

		// Sets all building rects
		if (active_buildings.Count == 0)
			GenerateBuildingList();

		UpdateStorage();
	}
	public bool OnCloseMenu(MenuInstance _next)
	{
		// close storage if open first
		if (is_storage_open)
		{
			SetStorage(false);
			return false;
		}
		if (_next == null)
			FreeCameraManager.SetHUDTo(true);

		ClearBuildingList();

		return true;
	}
	public void APPLY_OUTOFBOUND_PATCH(SaveSystem.SaveEventArgs _)
	{
		if (!GameManager.APPLY_OUTOFBOUND_PATCH)
			return;

		GenerateBuildingList();
		ClearBuildingList();
		GameManager.APPLY_OUTOFBOUND_PATCH = false;
		DebugLogger.Print(DebugLogger.LogPriority.Info, "BuildMenu: OUTOFBOUND patch finalized.");
	}

	private void UpdateStorage(int _startIndex = 0)
	{
		// cleans the window
		for (int i = _startIndex; i < storageContainer.GetChildCount(); i++)
			storageContainer.GetChild(i).QueueFree();

		// Sets the storage inventory
		for (int i = _startIndex; i < Player.Data.Storage.Count; i++)
		{
			TextureButton _item = itemContainer.Instantiate<TextureButton>();
			string _structure = Player.Data.Storage[i];
			_item.TooltipText = GlobalManager.Utils.GetTooltipText(_structure);
			(_item.GetChild(1) as TextureRect).Texture = GlobalManager.GetIcon(_structure);
			_item.Pressed += () => GrabFromStorage(_structure, _item);
			storageContainer.AddChild(_item);
		}
	}
	private void GenerateBuildingList()
	{
		for (int i = 0; i < ResortManager.Current.StructureRoot.GetChildCount(); i++)
		{
			var _structure = ResortManager.Current.StructureRoot.GetChild<Node2D>(i);
			var _building = CreateBuilding(_structure);

			// patch to get structures outside the playable area
			if (GameManager.APPLY_OUTOFBOUND_PATCH && _building == null)
			{
				Player.Data.Storage.Add(_structure.GetMeta(StringNames.IdMeta).ToString());
				_structure.QueueFree();
			}
		}
	}
	private void ClearBuildingList()
	{
		active_buildings.Clear();
		for (int i = 0; i < storageContainer.GetChildCount(); i++)
			storageContainer.GetChild(i).QueueFree();
	}

	public override void _Process(double delta)
	{
		if (!Visible)
		{
			if (selected_building != null)
				UnassignBuilding();
			return;
		}

		if (selected_building != null)
		{
			if (DEBUG_SHOW_RECTS)
				QueueRedraw();
			ProcessBuildingInteraction();
		}
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event.IsActionPressed(InputNames.Interact))
			SelectBuilding();

		if (@event.IsActionPressed(InputNames.OpenInventory))
			SetStorage();
	}
	public override void _Draw()
	{
		if (selected_building == null || !DEBUG_SHOW_RECTS)
			return;
		Vector2 _position = CameraManager.GetWorldToCanvasPosition(selected_building.Rect.Position),
			_topRight = CameraManager.GetWorldToCanvasPosition(selected_building.Rect.Position + new Vector2(selected_building.Rect.Size.X, 0)),
			_bottomRight = CameraManager.GetWorldToCanvasPosition(selected_building.Rect.End),
			_bottomLeft = CameraManager.GetWorldToCanvasPosition(selected_building.Rect.Position + new Vector2(0, selected_building.Rect.Size.Y)),
			_position_self = CameraManager.GetWorldToCanvasPosition(selected_building.Self.GlobalPosition);

		// line from its real global position to its offset
		DrawLine(_position_self, CameraManager.GetWorldToCanvasPosition(selected_building.Self.Position + selected_building.Offset), new Color("red"));
		// bounding box
		DrawPolyline([_position, _topRight, _bottomRight, _bottomLeft, _position], new Color("blue"));
	}

	// MARK: Building Creation & Manipulation
	public record class Building(Rect2 Rect, Node2D Self, Vector2 Offset, PlaceableArea Area, CollisionObject2D Collider = null)
	{
		/// <summary>
		/// The rect that can be picked up by the mouse
		/// </summary>
		public Rect2 Rect { get; set; } = Rect;
		/// <summary>
		/// The object to which the rect belongs to
		/// </summary>
		public Node2D Self { get; set; } = Self;
		/// <summary>
		/// Used to recalculate the Rect's origin
		/// </summary>
		public Vector2 Offset { get; set; } = Offset;
		/// <summary>
		/// The terrain in which a building can be put on.
		/// </summary>
		public PlaceableArea Area { get; set; } = Area;
		
		/// <summary>
		/// The physical collider of this building.
		/// </summary>
		public CollisionObject2D Collider { get; set; } = Collider;
	}
	private Building CreateBuilding(Node2D _self)
	{
		Vector2 _offset, _size;

		// check for which type of node it is and gather data to create the Rect
		if (_self.IsClass("Sprite2D"))
		{
			Sprite2D _selfSprite = _self as Sprite2D;
			_offset = _selfSprite.Offset;
			_size = (_self as Sprite2D).Texture.GetSize();
		}
		else if (_self.IsClass("AnimatedSprite2D"))
		{
			_offset = (_self as AnimatedSprite2D).Offset;
			_size = (_self as AnimatedSprite2D).SpriteFrames.GetFrameTexture(StringNames.DefaultAnim, 0).GetSize();
		}
		else if (_self.HasMeta(StringNames.SizeMeta) && _self.HasMeta(StringNames.OffsetMeta))
		{
			_offset = (Vector2)_self.GetMeta(StringNames.OffsetMeta);
			_size = (Vector2)_self.GetMeta(StringNames.SizeMeta);
		}
		else
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, "BuildMenu: ",
				$"{_self.Name} does not have the needed properties to create its Rect bounding box");
			return null;
		}

		PlaceableArea _area = _self.HasMeta(StringNames.PlaceableAreaMeta) ?
			(PlaceableArea)(int)_self.GetMeta(StringNames.PlaceableAreaMeta)
			: PlaceableArea.Ground;

		// get StaticBody2D
		PhysicsBody2D _body = null;
		for (int i = 0; i < _self.GetChildCount(); i++)
		{
			if (_self.GetChild(i) is StaticBody2D)
			{
				_body = _self.GetChild<StaticBody2D>(i);
				break;
			}
		}

		Vector2 _origin = _self.GlobalPosition - _size / 2 + _offset;
		Building _building = new(new Rect2(_origin, _size), _self, _offset, _area, _body);

		if (IsBeingObstructed(_building))
			return null;

		active_buildings.Add(_building);
		return _building;
	}
	private void RemoveBuilding(RemovalMode _mode)
	{
		active_buildings.Remove(selected_building);

		if (_mode == RemovalMode.Sell)
		{
			Player.AddCurrency(GlobalManager.G_STRUCTURES[selected_building.Self.GetMeta(StringNames.IdMeta).ToString()].Cost / 2);
			SoundManager.CreateSound("ui/kaching");
		}
		if (_mode == RemovalMode.Store)
		{
			Player.Data.Storage.Add(selected_building.Self.GetMeta(StringNames.IdMeta).ToString());
			UpdateStorage(Player.Data.Storage.Count - 1);
			SoundManager.CreateSound("ui/backpack_close");
		}
		selected_building.Self.QueueFree();
		UnassignBuilding();
	}
	private void MoveBuilding(Vector2 _position, bool _checkForObstruction = true)
	{
		selected_building.Self.GlobalPosition = _position;
		Vector2 _size = selected_building.Rect.Size,
		_origin = selected_building.Self.GlobalPosition - _size / 2 + selected_building.Offset;
		selected_building.Rect = new(_origin, _size);

		if (!_checkForObstruction)
			return;

		// check if is obstructed and highlight if so
		if (IsBeingObstructed(selected_building))
			selected_building.Self.Modulate = new Color("red");
		else
		{
			selected_building.Self.Modulate = new Color("white");
			last_valid_position = _position;
		}
	}
	private void AssignBuilding(Building _structure)
	{
		// set new closest strucuture
		UnassignBuilding();
		selected_building = _structure;

		// Set highlights for selected item
		previous_light_mask = selected_building.Self.LightMask;
		selected_building.Self.LightMask = 0;
		previous_material = selected_building.Self.Material;
		ShaderMaterial _outline = new()
		{
			Shader = ResourceLoader.Load<Shader>(GlobalManager.OUTLINE_SHADER)
		};
		_outline.SetShaderParameter("color", new Color(0.15f, 0, 0.8f));
		_outline.SetShaderParameter("pattern", 1);
		_outline.SetShaderParameter("add_margins", true);
		_structure.Self.Material = _outline;
	}
	private void UnassignBuilding()
	{
		StopMoveBuilding();

		if (selected_building != null)
		{
			if (selected_building.Self != null)
			{
				selected_building.Self.Modulate = new Color("white");
				selected_building.Self.Material = previous_material;
				selected_building.Self.LightMask = previous_light_mask;
			}
			is_hovering_building = false;
		}

		selected_building = null;
	}

	// MARK: Building Interactions
	private void ProcessBuildingInteraction()
	{
		is_hovering_building = selected_building.Rect.HasPoint(CameraManager.GetMouseToWorldPosition());
		bool _isSelectPressed = Input.IsActionPressed(InputNames.Select);

		// if isnt already moving a building, attempt to do so
		if (!is_moving_building)
		{
			if (Input.IsActionJustPressed(InputNames.Select))
			{
				// if is hovering it, then move it
				if (is_hovering_building)
					StartMoveBuilding();
				// otherwise attempt to select a new near one, if fail to do so, unselect our current one
				else if (!SelectBuilding())
					UnassignBuilding();
			}
		}
		// move for as long as the button is pressed
		else if (_isSelectPressed)
		{
			Vector2 _position = CameraManager.GetMouseToWorldPosition() + mouse_offset;
			if (Input.IsActionPressed(InputNames.AlignToGrid))
				_position = (_position / 10).Round() * 10;
			MoveBuilding(_position);
		}
		else
			StopMoveBuilding();

		if (Input.IsActionJustPressed(InputNames.Sell))
			RemoveBuilding(RemovalMode.Sell);
		else if (Input.IsActionJustPressed(InputNames.Store))
			RemoveBuilding(RemovalMode.Store);
	}
	private bool SelectBuilding()
	{
		Building _structure = GetStructureUnderMouse();

		if (_structure == null)
			return false;

		AssignBuilding(_structure);
		return true;
	}
	private void StartMoveBuilding()
	{
		// setup the interface
		CameraManager.EnableMouseFollow = true;
		is_moving_building = true;
		mouse_offset = selected_building.Self.GlobalPosition - CameraManager.GetMouseToWorldPosition();
		last_valid_position = selected_building.Self.GlobalPosition;

		selected_building.Self.ProcessMode = ProcessModeEnum.Disabled;

		// add corresponding possible actions
		CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.SellBuilding);
		CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.StoreBuilding);
		CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.AlignToGridBuilding);
	}
	private void StopMoveBuilding()
	{
		is_moving_building = false;
		if (selected_building != null)
			selected_building.Self.ProcessMode = ProcessModeEnum.Inherit;
		CanvasManager.ClearControlPrompts();
		CameraManager.EnableMouseFollow = false;

		// prevent furniture from being placed in invalid areas
		if (IsBeingObstructed(selected_building))
			MoveBuilding(last_valid_position, false);
	}

	private Building GetStructureUnderMouse()
	{
		Vector2 _mousePosition = CameraManager.GetMouseToWorldPosition();

		for (int i = 0; i < active_buildings.Count; i++)
		{
			if (active_buildings[i].Rect.HasPoint(_mousePosition))
			{
				// furniture that implements holding an aphid, cannot be moved while aphid is present
				if (active_buildings[i].Self is IAphidAccess)
				{
					IAphidAccess _furniture = active_buildings[i].Self as IAphidAccess;
					if (_furniture.IsAphidAvailable)
						continue;
				}
				return active_buildings[i];
			}
		}
		return null;
	}
	private static bool IsBeingObstructed(Building _building)
	{
		if (_building == null)
			return false;

		if (!_building.Rect.Intersects(RoomInstance.RoomBounds)) // outside the bounds
			return true;

		List<Godot.Collections.Dictionary> _colliders;
		// build raycast rect that will check for collisions
		if (_building.Collider != null)
		{
			var _collisionBox = _building.Collider.GetChild<CollisionShape2D>(0);
			Vector2 _size = _collisionBox.Shape.GetRect().Size;
			Vector2 _origin = _building.Self.GlobalPosition - _size / 2 + _collisionBox.Position;
			// physical collision rect that the player and other objects collide with
			_colliders = GlobalManager.Utils.RaycastRect(new Rect2(_origin, _size), [_building.Collider.GetRid()]);
		}
		else
		{
			// visual rect decided by sprite
			_colliders = GlobalManager.Utils.RaycastRect(_building.Rect, null);
		}

		for (int i = 0; i < _colliders.Count; i++)
		{
			if (_colliders[i] == null)
			{
				if (_building.Area != PlaceableArea.Ground)
					return true;
				else
					continue;
			}

			Node _collider = _colliders[i]["collider"].As<Node>();
			PlaceableArea _colliderTerrainType = PlaceableArea.NotValid;
			
			if (_collider.HasMeta(StringNames.PlaceableAreaMeta))
				_colliderTerrainType = (PlaceableArea)(int)_collider.GetMeta(StringNames.PlaceableAreaMeta);

			if (_building.Collider == null)
			{
				// for objects that have no collision box (aka, structures like rugs/carpets)
				if (_building.Area == PlaceableArea.Ground && 
					(_colliderTerrainType == PlaceableArea.Ground || _colliderTerrainType == PlaceableArea.NotValid))
					continue;

				// only checks for level geometry, otherwise ignore
				if (_colliderTerrainType == _building.Area)
					continue;
				else
					return true;
			}
			else
			{
				if (_colliderTerrainType != PlaceableArea.Ground)
					return true;
			}
		}

		// if there was no collision of any kind, its free to go
		return false;
	}

	// MARK: Storage Related
	private void GrabFromStorage(string _structureName, Control _slot = null)
	{
		Node2D _structure = ResortManager.CreateStructure(_structureName, CameraManager.Instance.GlobalPosition);
		if (!IsInstanceValid(_structure))
			return;
		Building _building = CreateBuilding(_structure);
		if (_building == null)
		{
			GlobalManager.CREATE_POPUP("warning_invalid_building", this);
			_structure.QueueFree();
			return;
		}

		_slot?.QueueFree();
		last_valid_position = _building.Self.GlobalPosition;
		AssignBuilding(_building);
		SetStorage(false);
		Player.Data.Storage.Remove(_structureName);
	}
	public void SetStorage(bool _state)
	{
		if (_state == is_storage_open)
			return;
		is_storage_open = _state;
		if (is_storage_open)
			menuPlayer.Play("open_bar");
		else
			menuPlayer.Play("close_bar");
	}
	public void SetStorage() =>
		SetStorage(!is_storage_open);
}
