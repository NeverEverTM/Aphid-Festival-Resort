using System.Collections.Generic;
using Godot;

// Used for the UI interface you interact with
public partial class ShopInterface : Control
{
	[Export] protected string shopTag;
	[Export] protected AnimationPlayer storePlayer;
	[Export] protected GridContainer itemGrid;
	[Export] protected RichTextLabel itemName, itemDescription;
	[Export] protected Label itemCost;
	[Export] protected TextureRect itemIcon;
	[Export] protected PackedScene itemContainer;

	[Export] protected AudioStream buySound, selectSound;
	protected MenuUtil.MenuInstance menu;
	protected string currentItem;
	protected int currentCost;

    // ===============| Shelf products |=============
    public override void _EnterTree()
    {
        CleanShelf();
        menu = new MenuUtil.MenuInstance(shopTag,
			storePlayer,
			ResetShop,
			(MenuUtil.MenuInstance _) => CleanShelf(),
			true
		);
    }
    protected virtual void ResetShop()
	{
		currentItem = string.Empty;
		itemName.Text = Tr($"store_{shopTag}_name");
		itemDescription.Text = Tr($"store_{shopTag}_desc");
		itemCost.Text = "$";
		itemIcon.Texture = null;
		CreateShelf();
	}
	protected virtual void CreateShelf()
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
			(_itemSlot.GetChild(0) as TextureRect).Texture = GameManager.GetIcon(_pair.Key);

			// set behaviour
			_itemSlot.Pressed += () => GetShelfItem(_pair.Key);
		}
	}
	protected virtual void CleanShelf()
	{
		for (int i = 0; i < itemGrid.GetChildCount(); i++)
			itemGrid.GetChild(i).QueueFree();
	}
	protected virtual void GetShelfItem(string _itemName)
	{
		// set this as current displayed item
		if (currentItem != _itemName)
			SetItem(_itemName);
		else // but if is already displayed, then buy it
			PurchaseItem();
	}
	protected virtual void SetItem(string _itemName)
	{
		currentItem = _itemName;
		currentCost = GameManager.G_ITEMS[_itemName].cost;
		itemCost.Text = currentCost.ToString();

		itemName.Text = Tr(_itemName + "_name");
		itemDescription.Text = Tr(_itemName + "_desc");

		itemIcon.Texture = GameManager.GetIcon(_itemName);
		SoundManager.CreateSound(selectSound);
	}
	protected virtual void PurchaseItem()
	{

	}
}