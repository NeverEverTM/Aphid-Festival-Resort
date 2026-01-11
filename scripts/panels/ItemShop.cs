public partial class ItemShop : ShopInterface
{
    protected override bool CanPurchase()
    {
		if (base.CanPurchase())
        	return !PlayerInventory.WouldInventoryBeFullWith();
		else
			return false;
    }
	protected override void Purchase()
	{
		base.Purchase();
		PlayerInventory.StoreItem(current_item.ID);
		GameManager.Data.ItemsBought++;
	}
}
