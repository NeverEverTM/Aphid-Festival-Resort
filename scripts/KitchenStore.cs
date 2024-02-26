using Godot;

public partial class KitchenStore : Node2D
{
	[Export] private Area2D triggerArea;
	[ExportCategory("Store")]
	[Export] private GridContainer itemGrid;
	[Export] private Label itemName, itemDescription, itemCost;
	[Export] private TextureRect itemIcon;
	[Export] private TextureButton buyItem;
	[Export] private PackedScene itemContainer;

	private bool IsNearby, IsActive;
	private string currentItem;
	private int currentCost;

    public override void _Ready()
    {
		triggerArea.AreaEntered += (Area2D _node) => IsNearby = true;
		triggerArea.AreaExited += (Area2D _node) => IsNearby = false;
		buyItem.Pressed += PurchaseItem;
    }

    public override void _Process(double delta)
    {
		if (!IsNearby)
			return;

		if (IsActive)
		{
			if (!DialogManager.IsActive && !CanvasManager.IsInMenu)
				IsActive = false;
			return;
		}

		if (Input.IsActionJustPressed("interact"))
			OpenStore();
    }

	private void CreateShelfItems()
	{
		CleanShelf();
		string[] _globalItems = DirAccess.GetFilesAt(GameManager.ItemPath);
		for(int i = 0; i < _globalItems.Length; i++)
		{
			TextureButton _itemContainer = itemContainer.Instantiate() as TextureButton;

			string _itemName = _globalItems[i].Remove(_globalItems[i].Length - 5); // delete the .tscn extension
			(_itemContainer.GetChild(0) as TextureRect).Texture = GameManager.ICONS[_itemName];
			_itemContainer.Pressed += () => SetItemAsCurrent(_itemName);
			
			itemGrid.AddChild(_itemContainer);
		}
	}
	private void CleanShelf()
	{
		for (int i = 0; i < itemGrid.GetChildCount(); i++)
			itemGrid.GetChild(i).QueueFree();
	}
	private void SetItemAsCurrent(string _itemName)
	{
		currentItem = _itemName;
		// I have to instantiate it to get the metadata, otherwise it cant read it
		var _temp = ResourceLoader.Load<PackedScene>($"{GameManager.ItemPath}/{_itemName}.tscn").Instantiate();
		currentCost = (int)_temp.GetMeta("cost");
		itemCost.Text = currentCost.ToString();
		_temp.QueueFree();

		itemName.Text = Tr(_itemName + "_name");
		itemDescription.Text = Tr(_itemName + "_desc");
		itemIcon.Texture = GameManager.ICONS[_itemName];
	}
	private void PurchaseItem()
	{
		if (Player.Currency - currentCost < 0)
			return; // TODO: Some notif about this

		Player.StoreItem(currentItem);
	}

    public void OpenStore()
	{
		IsActive = true;

		if (Player.Instance.PickupItem != null)
			Player.Instance.Drop();
		//await DialogManager.OpenDialog(new string[] { Tr("kut_shop_0"), Tr("kut_shop_1") } );
		CreateShelfItems();
		CanvasManager.SetMenu(CanvasManager.Instance.store_panel);
	}
}