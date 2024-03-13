using Godot;

public partial class KitchenStore : Node2D
{
	[Export] private Area2D triggerArea, dialogArea;
	[ExportCategory("Store")]
	[Export] private AnimationPlayer storePanel;
	[Export] private GridContainer itemGrid;
	[Export] private Label itemName, itemDescription, itemCost;
	[Export] private TextureRect itemIcon;
	[Export] private TextureButton buyItem;
	[Export] private PackedScene itemContainer;
	[Export] private AnimationPlayer storeAnimator;

	private bool IsActive;
	private string currentItem;
	private int currentCost;
	private enum Trigger { None, Store, Dialog }
	private Trigger trigger = Trigger.None;

	public override void _Ready()
	{
		triggerArea.AreaEntered += (Area2D _node) => trigger = Trigger.Store;
		triggerArea.AreaExited += (Area2D _node) => trigger = Trigger.None;
		dialogArea.AreaEntered += (Area2D _node) => trigger = Trigger.Dialog;
		dialogArea.AreaExited += (Area2D _node) => trigger = Trigger.None;
		buyItem.Pressed += PurchaseItem;
	}

	public override void _Process(double delta)
	{
		if (trigger == Trigger.None)
			return;

		if (IsActive)
		{
			if (!CanvasManager.IsInMenu)
				CloseStore();
			return;
		}

		if (Input.IsActionJustPressed("interact"))
		{
			if (trigger == Trigger.Store)
				OpenStore();
			else if (!DialogManager.IsActive)
				_ = DialogManager.OpenDialog(new string[] { "kut_shop_0", "kut_shop_1" }, "kut");
        }
    }

    // Store
    public void OpenStore()
	{
		IsActive = true;
		CreateShelf();
		CanvasManager.SetMenu(storePanel);
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