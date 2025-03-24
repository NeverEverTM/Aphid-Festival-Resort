using Godot;

public partial class FurnitureShop : ShopInterface
{
	[ExportCategory("Furniture Menu")]
	[Export] private TextureButton open_button;

	protected override void Purchase()
	{
		base.Purchase();
		Player.Data.Storage.Add(currentItem);
	}

	public override void _Ready()
	{
		menu.Open = _ =>
		{
			FreeCameraManager.SetFreeCameraHud(false);
			ResetShop();
		};
		menu.Close = _ =>
        {
			FreeCameraManager.SetFreeCameraHud(true);
			CleanShelf();
			return true;
		};
		open_button.Pressed += SetMenu;
	}
}
