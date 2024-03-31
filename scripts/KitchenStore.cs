using Godot;

public partial class KitchenStore : Node2D
{
	[Export] private Area2D triggerArea;
	[Export] private AudioStream buySound;
	[ExportCategory("Store")]
	[Export] private AnimationPlayer storePanel;
	[Export] private GridContainer itemGrid;
	[Export] private Label itemName, itemDescription, itemCost;
	[Export] private TextureRect itemIcon;
	[Export] private TextureButton buyItem;
	[Export] private PackedScene itemContainer;
	[Export] private AnimationPlayer storeAnimator;

	private bool IsActive, IsNearby;
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
			if (!CanvasManager.IsInMenu)
				IsActive = false;
			return;
		}

		if (Input.IsActionJustPressed("interact"))
			OpenStore();
    }

    // Store
    public void OpenStore()
	{
		IsActive = true;

		// Reset the expositor
		currentItem = string.Empty;
		itemName.Text = Tr("store_name_placeholder");
		itemDescription.Text = Tr("store_desc_placeholder");
		itemCost.Text = "$";
		itemIcon.Texture = null;
		CleanShelf();
		CreateShelf();

		CanvasManager.SetMenu(storePanel);
	}

	// ===============| Shelf products |=============
	private void CreateShelf()
	{
		string[] _globalItems = DirAccess.GetFilesAt(GameManager.ItemPath);

		// Create items
		for (int i = 0; i < _globalItems.Length; i++)
		{
			string _itemName = _globalItems[i].Remove(_globalItems[i].Length - 5); // delete the .tscn extension
			if (!GameManager.G_ITEMS[_itemName].canBeBought)
				continue;

			TextureButton _item = itemContainer.Instantiate() as TextureButton;

			(_item.GetChild(0) as TextureRect).Texture = GameManager.G_ICONS[_itemName];
			_item.Pressed += () => SetItemAsCurrent(_itemName);

			itemGrid.AddChild(_item);
		}

		(itemGrid.GetChild(0) as Control).GrabFocus();
	}
	private void CleanShelf()
	{
		for (int i = 0; i < itemGrid.GetChildCount(); i++)
			itemGrid.GetChild(i).QueueFree();
	}
	private void SetItemAsCurrent(string _itemName)
	{
		currentItem = _itemName;
		currentCost = GameManager.G_ITEMS[_itemName].cost;
		itemCost.Text = currentCost.ToString();

		itemName.Text = Tr(_itemName + "_name");
		itemDescription.Text = Tr(_itemName + "_desc");
		itemIcon.Texture = GameManager.G_ICONS[_itemName];
	}
	private void PurchaseItem()
	{
		if (string.IsNullOrEmpty(currentItem))
			return;
		//if (Player.Currency - currentCost < 0)
		// return;

		if (Player.Instance.StoreItem(currentItem))
		{
			Player.savedata.Currency -= currentCost;
			SoundManager.CreateSound(buySound, true);
		}
	}
}