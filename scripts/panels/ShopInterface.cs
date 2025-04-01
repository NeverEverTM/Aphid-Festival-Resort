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
	[Export] protected TextureButton itemBuyButton;
	[ExportCategory("Customizables")]
	[Export] protected Color bgColorSlot = new("cyan");
	[Export] protected Texture2D defaultIcon;
	protected MenuUtil.MenuInstance menu;
	protected string currentItem;
	protected int currentCost;

	// ===============| Shelf products |=============
	public override void _EnterTree()
	{
		CleanShelf();
		menu = new MenuUtil.MenuInstance(shopTag,
			storePlayer,
			Open: _ => 
			{
				ResetShop();
				SoundManager.CreateSound("ui/store_bell");
			},
			Close: _ => 
			{
				CleanShelf();
				return true;
			},
			true
		);
		itemBuyButton.Pressed += () => SelectItem(currentItem);
	}
	protected virtual void ResetShop()
	{
		currentItem = string.Empty;
		itemName.Text = Tr($"store_{shopTag}_name");
		itemDescription.Text = Tr($"store_{shopTag}_desc");
		itemCost.Text = Tr($"store_{shopTag}_phrase");
		itemIcon.Texture = defaultIcon;
		itemBuyButton.Hide();
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
			TryPurchase();
	}
	protected virtual void SetItem(string _itemName)
	{
		currentItem = _itemName;
		currentCost = GlobalManager.G_ITEMS[_itemName].cost;
		itemCost.Text = currentCost.ToString();

		itemName.Text = Tr(_itemName + "_name");
		itemDescription.Text = Tr(_itemName + "_desc");

		itemIcon.Texture = GlobalManager.GetIcon(_itemName);
		SoundManager.CreateSound("ui/button_select");
		itemBuyButton.Show();
	}
	/// <summary>
	/// Function that returns whether or not an item can be purchased.
	/// Must include a if (base.CanPurchase()) to correctly handle a purchase.
	/// </summary>
	/// <returns></returns>
	protected virtual bool CanPurchase()
	{
		if (string.IsNullOrEmpty(currentItem))
			return false;
		if ((Player.Data.Currency - currentCost) < 0)
			return false;
		return true;
	}
	/// <summary>
	/// Method used when a purchase needs to be verified before proceeding.
	/// </summary>
	protected virtual void TryPurchase()
	{
		if (CanPurchase())
			Purchase();
		else
			SoundManager.CreateSound("ui/button_fail");
	}
	/// <summary>
	/// Override for what it should give/set for buying this shop's items.
	/// Must include a base.Purchase() to correctly handle a purchase.
	/// </summary>
	protected virtual void Purchase()
	{
		Player.Data.AddCurrency(-currentCost);
		SoundManager.CreateSound("ui/kaching");
	}

	public void SetMenu()
	{
		if (CanvasManager.Menus.CurrentMenu != menu)
			CanvasManager.Menus.OpenMenu(menu);
	}
}