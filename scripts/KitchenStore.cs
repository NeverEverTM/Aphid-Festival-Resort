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
	[Export] private AnimationPlayer storeAnimator;

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
			// Deactivate store after you are fully out
			if (!DialogManager.IsActive && !CanvasManager.IsInMenu)
				CloseStore();
			return;
		}

		if (Input.IsActionJustPressed("interact"))
			OpenStore();
	}

	// Store
	public void OpenStore()
	{
		IsActive = true;

		if (Player.Instance.PickupItem != null)
			Player.Instance.Drop();
		//await DialogManager.OpenDialog(new string[] { Tr("kut_shop_0"), Tr("kut_shop_1") } );
		CreateShelf();
		CanvasManager.SetMenu(CanvasManager.Instance.store_panel);
		storeAnimator.Play("open");
	}
	private void CloseStore()
	{
		IsActive = false;
		storeAnimator.Play("RESET");
		itemName.Text = Tr("store_name_placeholder");
		itemDescription.Text = Tr("store_desc_placeholder");
		itemCost.Text = "$";
	}

	// ===============| Shelf products |=============
	private void CreateShelf()
	{
		CleanShelf();
		string[] _globalItems = DirAccess.GetFilesAt(GameManager.ItemPath);
		for (int i = 0; i < _globalItems.Length; i++)
		{
			string _itemName = _globalItems[i].Remove(_globalItems[i].Length - 5); // delete the .tscn extension
			if (!GameManager.G_ITEMS[_itemName].canBeBought)
				continue;

			TextureButton _itemContainer = itemContainer.Instantiate() as TextureButton;

			(_itemContainer.GetChild(0) as TextureRect).Texture = GameManager.G_ICONS[_itemName];
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
		currentCost = GameManager.G_ITEMS[_itemName].cost;
		itemCost.Text = currentCost.ToString();

		itemName.Text = Tr(_itemName + "_name");
		itemDescription.Text = Tr(_itemName + "_desc");
		itemIcon.Texture = GameManager.G_ICONS[_itemName];
	}
	private void PurchaseItem()
	{
		//if (Player.Currency - currentCost < 0)
		//return; // TODO: Some notif about this

		Player.Currency -= currentCost;
		Player.StoreItem(currentItem);
	}
}