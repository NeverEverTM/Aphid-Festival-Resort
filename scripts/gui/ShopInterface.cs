using System.Collections.Generic;
using Godot;

// Used for the UI interface you interact with
public partial class ShopInterface : Control
{
	[Export] private string shopTag;
	[Export] private ShopEntity entity;
	[Export] private AnimationPlayer storePanel;
	[Export] private GridContainer itemGrid;
	[Export] private Label itemName, itemDescription, itemCost;
	[Export] private TextureRect itemIcon;
	[Export] private PackedScene itemContainer;

	[Export] private AudioStream buySound, selectSound;
	private string currentItem;
	private int currentCost;

	public void OpenStore()
	{
		entity.IsActive = true;

		// Reset the expositor
		currentItem = string.Empty;
		itemName.Text = Tr($"store_{shopTag}_name");
		itemDescription.Text = Tr($"store_{shopTag}_desc");
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
		// Create items
		foreach (KeyValuePair<string, GameManager.Item> _pair in GameManager.G_ITEMS)
		{
			// is it related to this store?
			if (!_pair.Value.shopTag.Equals(shopTag))
				continue;

			// create item slot
			TextureButton _itemSlot = itemContainer.Instantiate() as TextureButton;
			itemGrid.AddChild(_itemSlot);

			// set icon
			(_itemSlot.GetChild(0) as TextureRect).Texture = GameManager.G_ICONS
			[GameManager.G_ICONS.ContainsKey(_pair.Key) ? _pair.Key : "missing"];

			// set behaviour
			_itemSlot.Pressed += () => GetShelfItem(_pair.Key);
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