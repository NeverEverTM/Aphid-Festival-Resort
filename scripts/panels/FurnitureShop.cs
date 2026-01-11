using System.Linq;
using Godot;

public partial class FurnitureShop : ShopInterface
{
	internal static FurnitureShop Instance { get; private set; }
	[ExportCategory("Furniture Menu")]
	[Export] private TextureButton open_button;

	protected override void Purchase()
	{
		base.Purchase();
		Player.Data.Storage.Add(current_item.ID);
	}
	protected override void FetchItemList()
	{
		// Fetch item datas and order them
		current_list = [.. GlobalManager.G_STRUCTURES.Values.Where(i => i.Shop == shopTag)];
		current_list = [.. current_list.OrderBy(i => i.ShopOrderPriority)];
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
		};
		open_button.Pressed += SetMenu;
	}
}
