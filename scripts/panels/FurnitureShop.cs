using System.Collections.Generic;
using Godot;

public partial class FurnitureShop : ShopInterface
{
	internal static FurnitureShop Instance { get; private set; }
	[Export] private TextureButton open_button;

	public override void _Ready()
	{
		Instance = this;
		open_button.Pressed += () => _ = CanvasManager.Menus.SetTo(GetMenuInstance());
	}
    public override void _ExitTree()
    {
        Instance = null;
    }
    protected override void Open(MenuInstance _last)
    {
		CameraManager.EnableFreeRoam = false;
		FreeCameraManager.SetHUDTo(false);
        base.Open(_last);
    }
    protected override void Close(MenuInstance _next)
    {
		CameraManager.EnableFreeRoam = true;
		if (_next == null)
			FreeCameraManager.SetHUDTo(true);
        base.Close(_next);
    }

	protected override void Purchase()
	{
		base.Purchase();
		Player.Data.Storage.Add(current_item.ID);
	}
	protected override List<ItemData> FetchStoreList()
	{
		return [.. GlobalManager.G_STRUCTURES.Values];
	}	
}
