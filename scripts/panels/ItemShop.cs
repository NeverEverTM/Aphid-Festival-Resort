public partial class ItemShop : ShopInterface
{
	protected override void Purchase()
	{
		if (!PlayerInventory.StoreItem(currentItem))
		{
			Player.Data.SetCurrency(currentCost);
			SoundManager.CreateSound(errorSound, true);
		}
	}
}
