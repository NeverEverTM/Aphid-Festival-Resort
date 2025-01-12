using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BuildMenu : Control
{
	[Export] private PackedScene item_container;
	[Export] private GridContainer storage_container;
	[Export] public AnimationPlayer menu_player;

	// Variables
	private readonly List<Building> active_buildings = new();
	public enum RemoveMode { Sell, Store }

	private Building selected_building;
	private CollisionShape2D current_shape;
	private Vector2 mouse_offset, last_valid_position = new();
	private int previous_light_mask;
	private bool isHoveringBuilding, isMovingBuilding, isStorageOpen;

	public record class Building
	{
		public Rect2 Collider { get; set; }
		public Node2D Self { get; set; }
		public Vector2 Offset { get; set; }

		public Building(Rect2 Collider, Node2D Self, Vector2 Offset)
		{
			this.Collider = Collider;
			this.Self = Self;
			this.Offset = Offset;
		}
	}

	public void OnOpenMenu()
	{
		isStorageOpen = false;
		ResortGUI.SetFreeCameraHud(false);

		// Sets all building rects
		if (active_buildings.Count == 0)
		{
			for (int i = 0; i < ResortManager.CurrentResort.StructureRoot.GetChildCount(); i++)
				CreateBuilding(ResortManager.CurrentResort.StructureRoot.GetChild(i) as Node2D);
		}

		UpdateStorage();
	}
	public async void OnCloseMenu(MenuUtil.MenuInstance _)
	{
		active_buildings.Clear();
		ResortGUI.SetFreeCameraHud(true);

		while (menu_player.CurrentAnimation == "close")
			await Task.Delay(1);
		// cleans the window
		if (menu_player.CurrentAnimation != "open")
		{
			for (int i = 0; i < storage_container.GetChildCount(); i++)
				storage_container.GetChild(i).QueueFree();
		}
	}

	private void UpdateStorage(int _startIndex = 0)
	{
		// cleans the window
		for (int i = _startIndex; i < storage_container.GetChildCount(); i++)
			storage_container.GetChild(i).QueueFree();

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
			storage_container.AddChild(_item);
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
			ProcessBuildingInteraction();
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed(InputNames.Interact))
			SelectBuilding();
	}

	private void ProcessBuildingInteraction()
	{
		isHoveringBuilding = selected_building.Collider.HasPoint(GlobalManager.Utils.GetMouseToWorldPosition());
		bool _isSelectPressed = Input.IsActionPressed("select");
		if (!isMovingBuilding)
		{
			// if selecting while hovering the structure, start Move function
			// otherwise, selecting out of the bounds of it tries selecting a new one
			if (Input.IsActionJustPressed("select"))
			{
				if (isHoveringBuilding)
					StartMoveBuilding();
				else if (!SelectBuilding())
					UnassignBuilding();
			}
		}
		else if (_isSelectPressed) // if let go, stop moving completly
		{
			Vector2 _position = GlobalManager.Utils.GetMouseToWorldPosition() + mouse_offset;
			if (Input.IsActionPressed(InputNames.AlignToGrid))
				_position = (_position / 10).Round() * 10;
			MoveBuilding(_position);
		}
		else
			StopMoveBuilding();

		if (Input.IsActionJustPressed("deselect"))
		{
			UnassignBuilding();
			return;
		}

		if (Input.IsActionJustPressed("sell"))
		{
			RemoveBuilding(RemoveMode.Sell);
			return;
		}

		if (Input.IsActionJustPressed("store"))
		{
			RemoveBuilding(RemoveMode.Store);
			return;
		}
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
		ResortGUI.Instance.EnableMouseCameraControl = true;
		isMovingBuilding = true;
		mouse_offset = selected_building.Self.GlobalPosition - GlobalManager.Utils.GetMouseToWorldPosition();
		last_valid_position = selected_building.Self.GlobalPosition;

		CanvasManager.AddControlPrompt("prompt_" + InputNames.Sell, InputNames.Sell, InputNames.Sell);
		CanvasManager.AddControlPrompt("prompt_" + InputNames.Store, InputNames.Store, InputNames.Store);
		CanvasManager.AddControlPrompt("prompt_" + InputNames.AlignToGrid, InputNames.AlignToGrid, InputNames.AlignToGrid);

		// Deactivate collision
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
		ResortGUI.Instance.EnableMouseCameraControl = false;
		isMovingBuilding = false;

		CanvasManager.RemoveControlPrompt(InputNames.Sell);
		CanvasManager.RemoveControlPrompt(InputNames.Store);
		CanvasManager.RemoveControlPrompt(InputNames.AlignToGrid);

		// prevent furniture from being placed in invalid areas
		if (IsBeingObstructed())
		{
			MoveBuilding(last_valid_position);
			return;
		}

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
			Shader = ResourceLoader.Load<Shader>(GlobalManager.OutlineShader)
		};
		_outline.SetShaderParameter("color", new Color(0.15f, 0, 0.8f));
		_outline.SetShaderParameter("pattern", 1);
		_outline.SetShaderParameter("add_margins", true);
		_structure.Self.Material = _outline;
	}
	private void UnassignBuilding()
	{
		ResortGUI.Instance.EnableMouseCameraControl = false;
		StopMoveBuilding();

		if (selected_building != null)
		{
			if (selected_building.Self != null)
			{
				selected_building.Self.Modulate = new Color("white");
				selected_building.Self.Material = null;
				selected_building.Self.LightMask = previous_light_mask;
			}
			isHoveringBuilding = false;
		}

		selected_building = null;
	}

	private Building GetStructureUnderMouse()
	{
		Vector2 _mousePosition = GlobalManager.Utils.GetMouseToWorldPosition();

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
		else
			return GlobalManager.Utils.Raycast(selected_building.Collider.Position + new Vector2(0, selected_building.Collider.Size.Y),
				new Vector2(selected_building.Collider.Size.X, 0), null).Count > 0;
	}
	private void GrabFromStorage(string _structureName)
	{
		Node2D _structure = ResourceLoader.Load<PackedScene>
		(GlobalManager.StructuresPath + $"/{_structureName}.tscn").Instantiate() as Node2D;
		if (!IsInstanceValid(_structure))
		{
			Logger.Print(Logger.LogPriority.Warning, "BuildMenu: ", $"{_structureName} is not a valid structure");
			return;
		}
		_structure.GlobalPosition = GlobalManager.GlobalCamera.GlobalPosition;
		ResortManager.CurrentResort.StructureRoot.AddChild(_structure);

		Building _building = CreateBuilding(_structure);
		if (_building == null)
			return;
		last_valid_position = _building.Self.GlobalPosition;
		AssignBuilding(_building);
		SetStorage(false);
	}
	public void SetStorage(bool _state)
	{
		if (_state == isStorageOpen)
			return;
		isStorageOpen = _state;
		if (isStorageOpen)
			menu_player.Play("open_bar");
		else
			menu_player.Play("close_bar");
	}
	public void SetStorage() => 
		SetStorage(!isStorageOpen);

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
			_size = (_self as AnimatedSprite2D).SpriteFrames.GetFrameTexture("default", 0).GetSize();
		}
		else if (_self.HasMeta("size") && _self.HasMeta("offset"))
		{
			_offset = (Vector2)_self.GetMeta("offset");
			_size = (Vector2)_self.GetMeta("size");
		}
		else
		{
			Logger.Print(Logger.LogPriority.Warning, "BuildMenu: ", $"{_self.Name} does not have the needed properties to create its Rect bounding box");
			return null;
		}

		Vector2 _origin = _self.GlobalPosition + _offset - _size / 2;
		Building _building = new(new Rect2(_origin, _size), _self, _offset);
		active_buildings.Add(_building);
		return _building;
	}
	private void RemoveBuilding(RemoveMode _mode)
	{
		active_buildings.Remove(selected_building);

		if (_mode == RemoveMode.Sell)
		{
			Player.Data.SetCurrency(GlobalManager.G_ITEMS[selected_building.Self.GetMeta("id").ToString()].cost / 2);
			SoundManager.CreateSound(CanvasManager.AudioSell);
		}
		if (_mode == RemoveMode.Store)
		{
			Player.Data.Storage.Add(selected_building.Self.GetMeta("id").ToString());
			SoundManager.CreateSound(CanvasManager.AudioStore);
			UpdateStorage(Player.Data.Storage.Count - 1);
		}
		selected_building.Self.QueueFree();
		UnassignBuilding();
	}
	private void MoveBuilding(Vector2 _position)
	{
		selected_building.Self.GlobalPosition = _position;
		Vector2 _size = selected_building.Collider.Size,
		_origin = selected_building.Self.GlobalPosition + selected_building.Offset - _size / 2;
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
