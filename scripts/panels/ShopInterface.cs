using System.Linq;
using Godot;

// Used for the UI interface you interact with
public partial class ShopInterface : Control
{
	public MenuInstance Menu { get; protected set; }

	[Export] protected InteractableArea2D interactArea;
	[ExportGroup("Inmutables")]
	[Export] protected AnimationPlayer storePlayer;
	[Export] protected GridContainer itemGrid;
	[Export] protected RichTextLabel itemName, itemDescription;
	[Export] protected RichTextLabel itemCost;
	[Export] protected TextureRect itemIcon;
	[Export] protected PackedScene itemContainer;
	[Export] protected BaseButton itemBuyButton;
	[ExportCategory("Customizables")]
	[Export] protected ItemData.ShopOwner shopTag;
	[Export] protected Color bgColorSlot = new("cyan");
	[Export] protected Texture2D defaultIcon;

	protected ItemData current_item;
	protected ItemData[] current_list;

	// ===============| Shelf products |=============
	public override void _EnterTree()
	{
		FetchItemList();
		CleanShelf();
		Menu = new MenuInstance(GetShopTagName(),
			storePlayer,
			Open: _ =>
			{
				ResetShop();
				SoundManager.CreateSound("ui/store_bell");
			},
			null,
			Close: _ => CleanShelf()
		);
		itemBuyButton.Pressed += TryPurchase;
		if (IsInstanceValid(interactArea))
			interactArea.OnInteractOnly.Add(SetMenu);
	}
	protected virtual void ResetShop()
	{
		current_item = ItemData.Empty;
		itemName.Text = Tr($"store_{GetShopTagName()}_name");
		itemDescription.Text = Tr($"store_{GetShopTagName()}_desc");
		itemCost.Text = Tr($"store_{GetShopTagName()}_phrase");
		itemIcon.Texture = defaultIcon;
		itemBuyButton.Hide();
		CreateShelf();
	}
	protected virtual void FetchItemList()
	{
		// Fetch item datas and order them
		current_list = [.. GlobalManager.G_ITEMS.Values.Where(i => i.Shop == shopTag)];
		current_list = [.. current_list.OrderBy(i => i.ShopOrderPriority)];
	}
	protected virtual void CreateShelf()
	{
		// Create items slots
		for (int i = 0; i < current_list.Length; i++)
		{
			// create item slot
			TextureButton _itemSlot = itemContainer.Instantiate() as TextureButton;
			itemGrid.AddChild(_itemSlot);

			// set icon
			(_itemSlot.GetChild(1) as TextureRect).Texture = GlobalManager.GetIcon(current_list[i].ID);
			(_itemSlot.GetChild(0) as Control).SelfModulate = bgColorSlot;

			// set behaviour
			var _index = i;
			_itemSlot.Pressed += () => SelectItem(current_list[_index]);
		}
	}
	protected virtual void CleanShelf()
	{
		for (int i = 0; i < itemGrid.GetChildCount(); i++)
			itemGrid.GetChild(i).QueueFree();
	}

	protected virtual void SelectItem(ItemData _item)
	{
		// set this as current displayed item
		if (current_item != _item)
			SetItem(_item);
	}
	protected virtual void SetItem(ItemData _item)
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
			SoundManager.CreateSound("ui/button_fail");
	}
	/// <summary>
	/// Override for what it should give/set for buying this shop's items.
	/// Must include a base.Purchase() to correctly handle a purchase.
	/// </summary>
	protected virtual void Purchase()
	{
		Player.RemoveCurrency(current_item.Cost);
		SoundManager.CreateSound("ui/kaching");
	}

	public void SetMenu()
	{
		if (CanvasManager.Menus.Current != Menu)
			_ = CanvasManager.Menus.SetTo(Menu);
	}
	public string GetShopTagName() => shopTag.ToString().ToLower();
}