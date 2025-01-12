public partial class ItemShop : ShopInterface
{
    protected override bool CanPurchase()
    {
		if (base.CanPurchase())
        	return PlayerInventory.CanStoreItem();
		else
			return false;
    }
	protected override void Purchase()
	{
		base.Purchase();
		PlayerInventory.StoreItem(currentItem);
	}
}
