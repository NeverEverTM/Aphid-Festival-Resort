using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BuildMenu : Control
{
	[Export] private PackedScene item_container;
	[Export] private GridContainer storage_container;
	[Export] private AnimationPlayer menu_player;
	[Export] private TextureButton build_button, show_storage_button;
	private MenuUtil.MenuInstance buildMenu;

	// Variables
	private readonly List<Building> activeBuildingsList = new();
	public enum RemoveMode { None, Sell, Store }

	private Building selectedBuilding;
	private CollisionShape2D currentShape;
	private Vector2 mouseOffset;
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

	public override void _Ready()
	{
		buildMenu = new MenuUtil.MenuInstance("build",
			menu_player, OnOpenMenu, OnCloseMenu, false);
		build_button.Pressed += () => CanvasManager.Menus.OpenMenu(buildMenu);
		show_storage_button.Pressed += () =>
		{
			if (!isStorageOpen)
				menu_player.Play("open_bar");
			else
				menu_player.Play("close_bar");
			isStorageOpen = !isStorageOpen;
		};
	}
	private void OnOpenMenu()
	{
		isStorageOpen = false;
		ResortGUI.SetFreeCameraHud(false);
		CanvasManager.SetCurrencyElement(false);

		// Sets all building rects
		if (activeBuildingsList.Count == 0)
		{
			for (int i = 0; i < ResortManager.Instance.StructureRoot.GetChildCount(); i++)
				CreateBuilding(ResortManager.Instance.StructureRoot.GetChild(i) as Node2D);
		}

		// cleans the window
		for (int i = 0; i < storage_container.GetChildCount(); i++)
			storage_container.GetChild(i).QueueFree();

		// Sets the storage inventory
		for (int i = 0; i < Player.Data.Storage.Count; i++)
		{
			TextureButton _item = item_container.Instantiate<TextureButton>();
			string _structure = Player.Data.Storage[i];
			(_item.GetChild(0) as TextureRect).Texture = GameManager.GetIcon(_structure);
			_item.Pressed += () =>
			{
				GrabFromStorage(_structure);
				Player.Data.Storage.Remove(_structure);
				_item.QueueFree();
			};
			storage_container.AddChild(_item);
		}
	}
	private async void OnCloseMenu(MenuUtil.MenuInstance _nextMenu)
	{
		if (_nextMenu != null && _nextMenu.ID == "furniture")
		{
			CanvasManager.SetCurrencyElement(true);
		}
		else // we exit build mode, back to free camera hud
		{
			activeBuildingsList.Clear();
			ResortGUI.SetFreeCameraHud(true);
			CanvasManager.SetCurrencyElement(true);
		}

		while (menu_player.CurrentAnimation == "close")
			await Task.Delay(1);
		// cleans the window
		if (menu_player.CurrentAnimation != "open")
		{
			for (int i = 0; i < storage_container.GetChildCount(); i++)
				storage_container.GetChild(i).QueueFree();
		}
	}

	public override void _Process(double delta)
	{
		if (!Visible)
		{
			if (selectedBuilding != null)
				UnassignStructure();
			return;
		}

		if (Input.IsActionJustPressed("interact"))
			OnSelect();

		if (selectedBuilding != null)
			ProcessStructureInteraction();
	}
	private void ProcessStructureInteraction()
	{
		isHoveringBuilding = selectedBuilding.Collider.HasPoint(GameManager.Utils.GetMouseToWorldPosition());
		bool _isSelectPressed = Input.IsActionPressed("select");
		if (!isMovingBuilding)
		{
			// if selecting while hovering the structure, start Move function
			// otherwise, selecting out of the bounds of it tries selecting a new one
			if (Input.IsActionJustPressed("select"))
			{
				if (isHoveringBuilding)
					StartMoveStructure();
				else if (!OnSelect())
					UnassignStructure();
			}
		}
		else if (_isSelectPressed) // if let go, stop moving completly
			MoveBuilding();
		else
			StopMoveStructure();

		if (Input.IsActionJustPressed("deselect"))
		{
			UnassignStructure();
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
	/// <summary>
	/// Tries selecting a structure under the mouse, if it fails then returns false, otherwise true.
	/// </summary>
	private bool OnSelect()
	{
		Building _structure = GetStructureUnderMouse();

		if (_structure == null)
			return false;

		AssignStructure(_structure);
		return true;
	}
	private void StartMoveStructure()
	{
		ResortGUI.Instance.EnableMouseCameraControl = true;
		isMovingBuilding = true;
		mouseOffset = selectedBuilding.Self.GlobalPosition - GameManager.Utils.GetMouseToWorldPosition();
		for (int i = 0; i < selectedBuilding.Self.GetChildCount(); i++)
		{
			if (selectedBuilding.Self.GetChild(i) is PhysicsBody2D && selectedBuilding.Self.GetChild(i).GetChildCount() > 0)
			{
				currentShape = selectedBuilding.Self.GetChild(i).GetChild(0) as CollisionShape2D;
				currentShape.Disabled = true;
			}
		}
	}
	private void StopMoveStructure()
	{
		ResortGUI.Instance.EnableMouseCameraControl = false;
		isMovingBuilding = false;

		if (IsInstanceValid(currentShape))
		{
			currentShape.Disabled = false;
			currentShape = null;
		}
	}

	public void AssignStructure(Building _structure)
	{
		// set new closest strucuture
		UnassignStructure();
		selectedBuilding = _structure;

		// Set highlights for selected item
		ShaderMaterial _outline = new()
		{
			Shader = ResourceLoader.Load<Shader>(GameManager.OutlineShader)
		};
		_outline.SetShaderParameter("color", new Color(0.15f, 0, 0.8f));
		_outline.SetShaderParameter("pattern", 1);
		_outline.SetShaderParameter("add_margins", true);
		_structure.Self.Material = _outline;
	}
	public void UnassignStructure()
	{
		ResortGUI.Instance.EnableMouseCameraControl = false;

		if (selectedBuilding != null)
		{
			selectedBuilding.Self.Material = null;
			isHoveringBuilding = false;
		}

		StopMoveStructure();
		selectedBuilding = null;
	}
	public Building GetStructureUnderMouse()
	{
		Vector2 _mousePosition = GameManager.Utils.GetMouseToWorldPosition();

		for (int i = 0; i < activeBuildingsList.Count; i++)
		{
			if (activeBuildingsList[i].Collider.HasPoint(_mousePosition))
				return activeBuildingsList[i];
		}
		return null;
	}

	public void GrabFromStorage(string _structureName)
	{
		Node2D _structure = ResourceLoader.Load<PackedScene>
		(GameManager.StructuresPath + $"/{_structureName}.tscn").Instantiate() as Node2D;
		if (!IsInstanceValid(_structure))
		{
			Logger.Print(Logger.LogPriority.Warning, "BuildMenu: ", $"{_structureName} is not a valid structure");
			return;
		}
		_structure.GlobalPosition = GameManager.GlobalCamera.GlobalPosition;
		ResortManager.Instance.StructureRoot.AddChild(_structure);

		Building _building = CreateBuilding(_structure);
		if (_building == null)
			return;
		AssignStructure(_building);
	}
	public Building CreateBuilding(Node2D _self)
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
		activeBuildingsList.Add(_building);
		return _building;
	}
	public void RemoveBuilding(RemoveMode _mode = RemoveMode.None)
	{
		Building _building = selectedBuilding;
		UnassignStructure();
		activeBuildingsList.Remove(selectedBuilding);

		if (_mode == RemoveMode.Sell)
		{
			Player.Data.SetCurrency(GameManager.G_ITEMS[_building.Self.GetMeta("id").ToString()].cost / 2);
			SoundManager.CreateSound(CanvasManager.Audio_SellSound);
		}
		if (_mode == RemoveMode.Store)
		{
			Player.Data.Storage.Add(_building.Self.GetMeta("id").ToString());
			SoundManager.CreateSound(CanvasManager.Audio_StoreSound);
		}
		_building.Self.QueueFree();
	}
	public void MoveBuilding()
	{
		selectedBuilding.Self.GlobalPosition = GameManager.Utils.GetMouseToWorldPosition() + mouseOffset;
		Vector2 _size = selectedBuilding.Collider.Size,
		_origin = selectedBuilding.Self.GlobalPosition + selectedBuilding.Offset - _size / 2;
		selectedBuilding.Collider = new(_origin, _size);
	}
}
