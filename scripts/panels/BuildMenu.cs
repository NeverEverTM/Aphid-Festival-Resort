using System.Collections.Generic;
using Godot;

public partial class BuildMenu : Control
{
	[Export] private GridContainer storageContainer;
	[Export] public AnimationPlayer menuPlayer;
	[Export] private TextureButton buildButton, storageButton;
	[Export] private Label controlPrompt;
	private const string ITEM_CONTAINER_SCENE = "uid://cn7d8wjyx78a3";
	public static StringName WATER_PLACEABLE = new("allow_water"), WALL_PLACEABLE = new("allow_wallmount");
	private PackedScene item_container;
	private MenuUtil.MenuInstance build_menu;
	public static bool DEBUG_SHOW_RECTS { get; set; }

	// Variables
	public bool IsStorageOpen;
	private readonly List<Building> active_buildings = [];
	private enum RemoveMode { Sell, Store }

	private Building selected_building;
	private CollisionShape2D current_shape;
	private Vector2 mouse_offset, last_valid_position = new();
	private int previous_light_mask;
	private bool is_hovering_building, is_moving_building;
	/// <summary>
	/// All furnitures have their anchor slightly up, why? i have no idea
	/// </summary>
	private static Vector2 MAGIC_NUMBER_OFFSET = new(0, 16);

	public record class Building
	{
		/// <summary>
		/// The rect that can be picked up by the mouse
		/// </summary>
		public Rect2 Collider { get; set; }
		/// <summary>
		/// The object to which the collider rect belongs to
		/// </summary>
		public Node2D Self { get; set; }
		/// <summary>
		/// Used to recalculate the Rect's origin
		/// </summary>
		public Vector2 Offset { get; set; }

		public Building(Rect2 Collider, Node2D Self, Vector2 Offset)
		{
			this.Collider = Collider;
			this.Self = Self;
			this.Offset = Offset;
		}
	}

	public override void _Ready()
	{
		item_container = ResourceLoader.Load(ITEM_CONTAINER_SCENE) as PackedScene;
		build_menu = new MenuUtil.MenuInstance("build", menuPlayer,
			_ => OnOpenMenu(), _ => OnCloseMenu(), false);

		buildButton.Pressed += () => CanvasManager.Menus.OpenMenu(build_menu);
		storageButton.Pressed += SetStorage;

		controlPrompt.Text = ControlsManager.GetActionName(InputNames.OpenInventory);
	}
	public void OnOpenMenu()
	{
		IsStorageOpen = false;
		FreeCameraManager.SetFreeCameraHud(false);
		if (IsInstanceValid(CameraManager.FocusedObject))
			FreeCameraManager.Instance.StopFocus();

		// Sets all building rects
		if (active_buildings.Count == 0)
		{
			for (int i = 0; i < ResortManager.CurrentResort.StructureRoot.GetChildCount(); i++)
				CreateBuilding(ResortManager.CurrentResort.StructureRoot.GetChild(i) as Node2D);
		}

		UpdateStorage();
	}
	public bool OnCloseMenu()
	{
		// close storage if open first
		if (IsStorageOpen)
		{
			SetStorage(false);
			return false;
		}
		active_buildings.Clear();
		FreeCameraManager.SetFreeCameraHud(true);

		for (int i = 0; i < storageContainer.GetChildCount(); i++)
			storageContainer.GetChild(i).QueueFree();
		return true;
	}

	private void UpdateStorage(int _startIndex = 0)
	{
		// cleans the window
		for (int i = _startIndex; i < storageContainer.GetChildCount(); i++)
			storageContainer.GetChild(i).QueueFree();

		// Sets the storage inventory
		for (int i = _startIndex; i < Player.Data.Storage.Count; i++)
		{
			TextureButton _item = item_container.Instantiate<TextureButton>();
			string _structure = Player.Data.Storage[i];
			_item.TooltipText = Tr($"{_structure}_name") + "\n" + Tr($"{_structure}_desc");
			(_item.GetChild(0) as TextureRect).Texture = GlobalManager.GetIcon(_structure);
			_item.Pressed += () =>
			{
				GrabFromStorage(_structure);
				Player.Data.Storage.Remove(_structure);
				_item.QueueFree();
			};
			storageContainer.AddChild(_item);
		}
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
		Vector2 _position = CameraManager.GetWorldToCanvasPosition(selected_building.Collider.Position),
			_topleft = CameraManager.GetWorldToCanvasPosition(selected_building.Collider.Position + new Vector2(selected_building.Collider.Size.X, 0)),
			bottomleft = CameraManager.GetWorldToCanvasPosition(selected_building.Collider.Position + selected_building.Collider.Size),
			_bottomright = CameraManager.GetWorldToCanvasPosition(selected_building.Collider.Position + new Vector2(0, selected_building.Collider.Size.Y)),
			_position_self = CameraManager.GetWorldToCanvasPosition(selected_building.Self.GlobalPosition);

       	DrawLine(_position_self, _position, new Color("red"));
		DrawPolyline([_position, _topleft, bottomleft, _bottomright, _position], new Color("blue"));
    }

	private void ProcessBuildingInteraction()
	{
		is_hovering_building = selected_building.Collider.HasPoint(CameraManager.GetMouseToWorldPosition());
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
			RemoveBuilding(RemoveMode.Sell);
		else if (Input.IsActionJustPressed(InputNames.Store))
			RemoveBuilding(RemoveMode.Store);
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
		CameraManager.Instance.EnableMouseFollow = true;
		is_moving_building = true;
		mouse_offset = selected_building.Self.GlobalPosition - CameraManager.GetMouseToWorldPosition();
		last_valid_position = selected_building.Self.GlobalPosition;

		// add corresponding possible actions
		CanvasManager.AddControlPrompt("prompt_" + InputNames.Sell, InputNames.Sell, InputNames.Sell);
		CanvasManager.AddControlPrompt("prompt_" + InputNames.Store, InputNames.Store, InputNames.Store);
		CanvasManager.AddControlPrompt("prompt_" + InputNames.AlignToGrid, InputNames.AlignToGrid, InputNames.AlignToGrid);

		// Deactivate collision of the structure
		// WARNING: This does not handle multiple collision shapes within the structure
		for (int i = 0; i < selected_building.Self.GetChildCount(); i++)
		{
			if (selected_building.Self.GetChild(i) is PhysicsBody2D && selected_building.Self.GetChild(i).GetChildCount() > 0)
			{
				current_shape = selected_building.Self.GetChild(i).GetChild(0) as CollisionShape2D;
				current_shape.Disabled = true;
				break;
			}
		}
	}
	private void StopMoveBuilding()
	{
		// set interface back to normal
		CameraManager.Instance.EnableMouseFollow = false;
		is_moving_building = false;

		// remove possible action prompts
		CanvasManager.RemoveControlPrompt(InputNames.Sell);
		CanvasManager.RemoveControlPrompt(InputNames.Store);
		CanvasManager.RemoveControlPrompt(InputNames.AlignToGrid);

		// prevent furniture from being placed in invalid areas
		if (IsBeingObstructed())
		{
			MoveBuilding(last_valid_position);
			return;
		}

		// give collision back if any
		if (IsInstanceValid(current_shape))
		{
			current_shape.Disabled = false;
			current_shape = null;
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
		CameraManager.Instance.EnableMouseFollow = false;
		StopMoveBuilding();

		if (selected_building != null)
		{
			if (selected_building.Self != null)
			{
				selected_building.Self.Modulate = new Color("white");
				selected_building.Self.Material = null;
				selected_building.Self.LightMask = previous_light_mask;
			}
			is_hovering_building = false;
		}

		selected_building = null;
	}

	private Building GetStructureUnderMouse()
	{
		Vector2 _mousePosition = CameraManager.GetMouseToWorldPosition();

		for (int i = 0; i < active_buildings.Count; i++)
		{
			if (active_buildings[i].Collider.HasPoint(_mousePosition))
				return active_buildings[i];
		}
		return null;
	}
	private bool IsBeingObstructed()
	{
		if (selected_building == null)
			return false;

		Godot.Collections.Dictionary _list = GlobalManager.Utils.Raycast(
			selected_building.Collider.Position + new Vector2(0, selected_building.Collider.Size.Y),
			new Vector2(selected_building.Collider.Size.X, 0),
			null);

		if (selected_building.Self.HasMeta(WATER_PLACEABLE))
		{
			if (_list.Count == 0)
				return true;
			if (_list["collider"].ToString().Contains("ground"))
				return false;
		}
		if (selected_building.Self.HasMeta(WALL_PLACEABLE))
		{
			if (_list.Count == 0)
				return true;
			if (_list["collider"].ToString().Contains("wall"))
				return false;
		}

		return _list.Count > 0;
	}
	private void GrabFromStorage(string _structureName)
	{
		Node2D _structure = ResortManager.CreateStructure(_structureName, CameraManager.Instance.GlobalPosition);
		if (!IsInstanceValid(_structure))
			return;
		Building _building = CreateBuilding(_structure);
		if (_building == null)
			return;
		last_valid_position = _building.Self.GlobalPosition;
		AssignBuilding(_building);
		SetStorage(false);
	}
	public void SetStorage(bool _state)
	{
		if (_state == IsStorageOpen)
			return;
		IsStorageOpen = _state;
		if (IsStorageOpen)
			menuPlayer.Play("open_bar");
		else
			menuPlayer.Play("close_bar");
	}
	public void SetStorage() =>
		SetStorage(!IsStorageOpen);

	private Building CreateBuilding(Node2D _self)
	{
		Vector2 _offset, _size;

		// check for which type of node it is and gather data to create the Rect
		if (_self is Sprite2D)
		{
			_offset = (_self as Sprite2D).Offset;
			_size = (_self as Sprite2D).Texture.GetSize();
		}
		else if (_self is AnimatedSprite2D)
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
			Logger.Print(Logger.LogPriority.Warning, "BuildMenu: ", $"{_self.Name} does not have the needed properties to create its Rect bounding box");
			return null;
		}

		Vector2 _origin = _self.GlobalPosition - _size / 2 + _offset + MAGIC_NUMBER_OFFSET;
		Building _building = new(new Rect2(_origin, _size), _self, _offset);
		active_buildings.Add(_building);
		return _building;
	}
	private void RemoveBuilding(RemoveMode _mode)
	{
		active_buildings.Remove(selected_building);

		if (_mode == RemoveMode.Sell)
		{
			Player.Data.AddCurrency(GlobalManager.G_ITEMS[selected_building.Self.GetMeta(StringNames.IdMeta).ToString()].cost / 2);
			SoundManager.CreateSound("ui/kaching");
		}
		if (_mode == RemoveMode.Store)
		{
			Player.Data.Storage.Add(selected_building.Self.GetMeta(StringNames.IdMeta).ToString());
			SoundManager.CreateSound("ui/backpack_close");
			UpdateStorage(Player.Data.Storage.Count - 1);
		}
		selected_building.Self.QueueFree();
		UnassignBuilding();
	}
	private void MoveBuilding(Vector2 _position)
	{
		selected_building.Self.GlobalPosition = _position;
		Vector2 _size = selected_building.Collider.Size,
		_origin = selected_building.Self.GlobalPosition - _size / 2 + selected_building.Offset + MAGIC_NUMBER_OFFSET;
		selected_building.Collider = new(_origin, _size);
		
		// check if is obstructed and highlight if so
		if (IsBeingObstructed())
			selected_building.Self.Modulate = new Color("red");
		else
		{
			selected_building.Self.Modulate = new Color("white");
			last_valid_position = _position;
		}
	}
}
