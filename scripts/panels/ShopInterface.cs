using System.Collections.Generic;
using Godot;

// Used for the UI interface you interact with
public partial class ShopInterface : Control, MenuTrigger.ITrigger
{
	[Export] protected string shopTag;
	[Export] protected AnimationPlayer storePlayer;
	[Export] protected GridContainer itemGrid;
	[Export] protected RichTextLabel itemName, itemDescription;
	[Export] protected Label itemCost;
	[Export] protected TextureRect itemIcon;
	[Export] protected PackedScene itemContainer;
	[ExportCategory("Customizables")]
	[Export] protected Color bgColorSlot = new("cyan");
	[Export] protected Texture2D default_item_icon;
	[Export] protected AudioStream errorSound;
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
		itemCost.Text = Tr($"store_{shopTag}_phrase");
		itemIcon.Texture = default_item_icon;
		CreateShelf();
	}
	protected virtual void CreateShelf()
	{
		// Create items
		foreach (KeyValuePair<string, GlobalManager.Item> _pair in GlobalManager.G_ITEMS)
		{
			// is it related to this store?
			if (!_pair.Value.shopTag.Equals(shopTag))
				continue;

			// create item slot
			TextureButton _itemSlot = itemContainer.Instantiate() as TextureButton;
			itemGrid.AddChild(_itemSlot);

			// set icon
			(_itemSlot.GetChild(0) as TextureRect).Texture = GlobalManager.GetIcon(_pair.Key);
			_itemSlot.SelfModulate = bgColorSlot;

			// set behaviour
			_itemSlot.Pressed += () => SelectItem(_pair.Key);
		}
	}
	protected virtual void CleanShelf()
	{
		for (int i = 0; i < itemGrid.GetChildCount(); i++)
			itemGrid.GetChild(i).QueueFree();
	}
	protected virtual void SelectItem(string _itemName)
	{
		// set this as current displayed item
		if (currentItem != _itemName)
			SetItem(_itemName);
		else // but if is already displayed, then buy it
			Purchase();
	}
	protected virtual void SetItem(string _itemName)
	{
		currentItem = _itemName;
		currentCost = GlobalManager.G_ITEMS[_itemName].cost;
		itemCost.Text = currentCost.ToString();

		itemName.Text = Tr(_itemName + "_name");
		itemDescription.Text = Tr(_itemName + "_desc");

		itemIcon.Texture = GlobalManager.GetIcon(_itemName);
		SoundManager.CreateSound(CanvasManager.AudioStore);
	}
	protected virtual bool CanPurchase()
	{
		if (string.IsNullOrEmpty(currentItem))
			return false;
		if (Player.Data.Currency - currentCost < 0)
			return false;
		return true;
	}
	protected virtual void Purchase()
	{
		if (CanPurchase())
		{
			Player.Data.SetCurrency(-currentCost);
			SoundManager.CreateSound(CanvasManager.AudioSell, true);
		}
		else
			SoundManager.CreateSound(errorSound);
	}

	public void SetMenu()
	{
		if (CanvasManager.Menus.CurrentMenu != menu)
			CanvasManager.Menus.OpenMenu(menu);
	}
}