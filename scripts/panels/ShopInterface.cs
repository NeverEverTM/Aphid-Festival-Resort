using System.Collections.Generic;
using System.Linq;
using Godot;

// Used for the UI interface you interact with
public partial class ShopInterface : MenuControl
{
    public override string ID => GetShopTagName();

    [Export] protected InteractableArea2D interactArea;
	[ExportGroup("Inmutables")]
	[Export] protected GridContainer itemGrid;
	[Export] protected RichTextLabel itemName, itemDescription;
	[Export] protected RichTextLabel itemCost;
	[Export] protected Label currencyLabel;
	[Export] protected TextureRect itemIcon;
	[Export] protected PackedScene itemContainer;
	[Export] protected BaseButton itemBuyButton;
	[ExportCategory("Customizables")]
	[Export] protected ItemData.ShopOwner shopTag;
	[Export] protected Color bgColorSlot = new("cyan");
	[Export] protected Texture2D defaultIcon;

	protected ItemData current_item;
	protected ItemData[] current_store_list;

	// ===============| Shelf products |=============
	public override void _EnterTree()
	{
		CleanShelf();
		itemBuyButton.Pressed += TryPurchase;
		if (IsInstanceValid(interactArea))
			interactArea.OnInteractOnly.Add(() => _ = CanvasManager.Menus.SetTo(GetMenuInstance()));
	}
	protected override void Open(MenuInstance _last)
    {
        if (current_store_list == null)
			SetStoreList();
		ResetShop();
		SoundManager.CreateSound("ui/store_bell");
    }
    protected override void Close(MenuInstance _next)
    {
        CleanShelf();
    }

	protected virtual void ResetShop()
	{
		current_item = ItemData.Empty;
		itemName.Text = Tr($"store_{GetShopTagName()}_name");
		itemDescription.Text = Tr($"store_{GetShopTagName()}_desc");
		itemCost.Text = Tr($"store_{GetShopTagName()}_phrase");
		currencyLabel.Text = Player.Data.Currency.ToString();
		itemIcon.Texture = defaultIcon;
		itemBuyButton.Hide();
		CreateShelfFromList();
	}
	/// <summary>
	/// Generates the list of items based on the item list given by FetchStoreList().
	/// </summary>
	protected void SetStoreList()
	{
		List<ItemData> _list = FetchStoreList();
		int _tier = GameManager.GetPlayerUpgradeElseEmpty("membership_tier").Level;
		// Fetch item datas and order them
		current_store_list = [.. _list
				.Where(i => i.Shop == shopTag)
				.Where(i => i.TierRequirement <= _tier)
				.OrderBy(i => i.ShopOrderPriority)];
	}
	/// <summary>
	/// Fetches the list of items to sell at this particular storefront.
	/// </summary>
	protected virtual List<ItemData> FetchStoreList()
	{
		return [.. GlobalManager.G_ITEMS.Values];
	}
	protected virtual void CreateShelfFromList()
	{
		// Create items slots
		for (int i = 0; i < current_store_list.Length; i++)
		{
			// create item slot
			TextureButton _itemSlot = itemContainer.Instantiate() as TextureButton;
			itemGrid.AddChild(_itemSlot);

			// set icon
			(_itemSlot.GetChild(1) as TextureRect).Texture = GlobalManager.GetIcon(current_store_list[i].ID);
			(_itemSlot.GetChild(0) as Control).SelfModulate = bgColorSlot;

			// set behaviour
			var _index = i;
			_itemSlot.Pressed += () => SelectItem(current_store_list[_index]);
		}
	}
	protected virtual void CleanShelf()
	{
		for (int i = 0; i < itemGrid.GetChildCount(); i++)
			itemGrid.GetChild(i).QueueFree();
	}
	/// <summary>
	/// Sets the currently displayed item, override to affect how selection works.
	/// </summary>
	/// <param name="_item"></param>
	protected virtual void SelectItem(ItemData _item)
	{
		if (current_item != _item)
			DisplayItem(_item);
	}
	/// <summary>
	/// Displays the currently set item, override to affect how display works.
	/// </summary>
	/// <param name="_item"></param>
	protected virtual void DisplayItem(ItemData _item)
	{
		current_item = _item;

		itemCost.Text = $"{StringNames.BerryIcon} {_item.Cost}";
		itemName.Text = _item.ID + "_name";
		itemDescription.Text = _item.ID + "_desc";
		itemIcon.Texture = GlobalManager.GetIcon(_item.ID);

		itemBuyButton.Show();
		SoundManager.CreateSound("ui/button_select");
	}
	/// <summary>
	/// Function that returns whether or not an item can be purchased.
	/// Must include a if (base.CanPurchase()) to correctly handle a purchase.
	/// </summary>
	/// <returns></returns>
	protected virtual bool CanPurchase()
	{
		if (string.IsNullOrEmpty(current_item.ID))
			return false;
		if ((Player.Data.Currency - current_item.Cost) < 0)
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
		{
			currencyLabel.Modulate = new Color("red");
			Tween _flashRed = CreateTween();
			_flashRed.TweenProperty(currencyLabel, "modulate", new Color("white"), 0.2);
			SoundManager.CreateSound("ui/button_fail");
		}
	}
	/// <summary>
	/// Override for what it should give/set for buying this shop's items.
	/// Must include a base.Purchase() to correctly handle a purchase.
	/// </summary>
	protected virtual void Purchase()
	{
		Player.RemoveCurrency(current_item.Cost);
		currencyLabel.Text = Player.Data.Currency.ToString();
		SoundManager.CreateSound("ui/kaching");
	}
	public string GetShopTagName() => shopTag.ToString().ToLower();
}