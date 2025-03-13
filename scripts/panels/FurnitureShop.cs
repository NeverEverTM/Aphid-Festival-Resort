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
		menu.Open = () =>
		{
			FreeCameraManager.SetFreeCameraHud(false);
			ResetShop();
		};
		menu.Close = (MenuUtil.MenuInstance _) =>
		{
			FreeCameraManager.SetFreeCameraHud(true);
			CleanShelf();
		};
		open_button.Pressed += SetMenu;
	}
}
