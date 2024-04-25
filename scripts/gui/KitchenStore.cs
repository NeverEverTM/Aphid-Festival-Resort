using Godot;

public partial class KitchenStore : Node2D
{
	[Export] private Area2D triggerArea;
	[Export] private AudioStream buySound, selectSound;
	[ExportCategory("Store")]
	[Export] private AnimationPlayer storePanel;
	[Export] private GridContainer itemGrid;
	[Export] private Label itemName, itemDescription, itemCost;
	[Export] private TextureRect itemIcon;
	[Export] private PackedScene itemContainer;

	private bool IsActive, IsNearby;
	private string currentItem;
	private int currentCost;

	public override void _Ready()
	{
		triggerArea.AreaEntered += (Area2D _node) => IsNearby = true;
		triggerArea.AreaExited += (Area2D _node) => IsNearby = false;
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

		CanvasManager.OpenMenu(storePanel);
		(itemGrid.GetChild(0) as Control).GrabFocus();
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

			if (!GameManager.G_ICONS.ContainsKey(_itemName))
				(_item.GetChild(0) as TextureRect).Texture = GameManager.G_ICONS["missing"];
			else
				(_item.GetChild(0) as TextureRect).Texture = GameManager.G_ICONS[_itemName];
			_item.Pressed += () => GetShelfItem(_itemName);

			itemGrid.AddChild(_item);
		}
	}
	private void CleanShelf()
	{
		for (int i = 0; i < itemGrid.GetChildCount(); i++)
			itemGrid.GetChild(i).QueueFree();
	}
	private void GetShelfItem(string _itemName)
	{
		// set this as current displayed item
		if (currentItem != _itemName)
			SetItem(_itemName);
		else // but if is already displayed, then buy it
			PurchaseItem();
	}
	private void SetItem(string _itemName)
	{
		currentItem = _itemName;
		currentCost = GameManager.G_ITEMS[_itemName].cost;
		itemCost.Text = currentCost.ToString();

		itemName.Text = Tr(_itemName + "_name");
		itemDescription.Text = Tr(_itemName + "_desc");

		if (!GameManager.G_ICONS.ContainsKey(_itemName))
			itemIcon.Texture = GameManager.G_ICONS["missing"];
		else
			itemIcon.Texture = GameManager.G_ICONS[_itemName];
		SoundManager.CreateSound(selectSound);
	}
	private void PurchaseItem()
	{
		if (string.IsNullOrEmpty(currentItem))
			return;
		if (Player.Data.Currency - currentCost < 0)
			return;

		if (Player.Instance.StoreItem(currentItem))
		{
			Player.Data.Currency -= currentCost;
			SoundManager.CreateSound(buySound, true);
		}
	}
}