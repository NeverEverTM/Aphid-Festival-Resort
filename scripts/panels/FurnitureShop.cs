using System.Linq;
using Godot;

public partial class FurnitureShop : ShopInterface
{
	internal static FurnitureShop Instance { get; private set; }
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
			CameraManager.EnableFreeRoam = false;
			FreeCameraManager.SetHUDTo(false);
			ResetShop();
			SoundManager.CreateSound("ui/store_bell");
		};
		Menu.Close = _next =>
		{
			CameraManager.EnableFreeRoam = true;
			if (_next == null)
				FreeCameraManager.SetHUDTo(true);
			CleanShelf();
		};
		open_button.Pressed += SetMenu;
	}
}
