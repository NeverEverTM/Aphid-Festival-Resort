public partial class ItemShop : ShopInterface
{
	protected override void PurchaseItem()
	{
		if (string.IsNullOrEmpty(currentItem))
			return;
		if (Player.Data.Currency - currentCost < 0)
			return;

		if (PlayerInventory.StoreItem(currentItem))
		{
			Player.Data.SetCurrency(-currentCost);
			SoundManager.CreateSound(buySound, true);
		}
	}
}
