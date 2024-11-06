using Godot;

public partial class FurnitureShop : ShopInterface
{
	[ExportCategory("Furniture Menu")]
	[Export] private TextureButton open_button;

	protected override void PurchaseItem()
	{
		if (string.IsNullOrEmpty(currentItem))
			return;
		if (Player.Data.Currency - currentCost < 0)
			return;

		Player.Data.Storage.Add(currentItem);
		Player.Data.SetCurrency(-currentCost);
		SoundManager.CreateSound(buySound, true);
	}

	public override void _Ready()
	{
		menu.Open = () =>
		{
			ResortGUI.SetFreeCameraHud(false);
			ResetShop();
		};
		menu.Close = (MenuUtil.MenuInstance _) =>
		{
			ResortGUI.SetFreeCameraHud(true);
			CleanShelf();
		};
		open_button.Pressed += SetMenu;
	}
}
