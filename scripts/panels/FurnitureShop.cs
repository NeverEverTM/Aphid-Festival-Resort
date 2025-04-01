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
			CameraManager.Instance.EnableFreeRoam = false;
			FreeCameraManager.SetFreeCameraHud(false);
			ResetShop();
			SoundManager.CreateSound("ui/store_bell");
		};
		menu.Close = _ =>
		{
			CameraManager.Instance.EnableFreeRoam = true;
			FreeCameraManager.SetFreeCameraHud(true);
			CleanShelf();
			return true;
		};
		open_button.Pressed += SetMenu;
	}
}
