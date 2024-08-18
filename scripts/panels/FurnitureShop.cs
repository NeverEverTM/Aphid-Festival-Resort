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
		open_button.Pressed += () => CanvasManager.Menus.OpenMenu(menu);
    }
}
