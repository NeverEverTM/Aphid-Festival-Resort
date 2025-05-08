using Godot;

public partial class FurnitureShop : ShopInterface
{
	internal static FurnitureShop Instance { get; private set; }
	[ExportCategory("Furniture Menu")]
	[Export] private TextureButton open_button;

	protected override void Purchase()
	{
		base.Purchase();
		Player.Data.Storage.Add(currentItem);
	}

	public override void _Ready()
	{
		Instance = this;
		Menu.Open = _ =>
		{
			CameraManager.Instance.EnableFreeRoam = false;
			FreeCameraManager.SetFreeCameraHud(false);
			ResetShop();
			SoundManager.CreateSound("ui/store_bell");
		};
		Menu.Close = _next =>
		{
			CameraManager.Instance.EnableFreeRoam = true;
			if (_next == null)
				FreeCameraManager.SetFreeCameraHud(true);
			CleanShelf();
			return true;
		};
		open_button.Pressed += SetMenu;
	}
}
