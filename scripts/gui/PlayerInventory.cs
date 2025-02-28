using Godot;
using static Player;

public partial class PlayerInventory : Control
{
	[Export] private AnimationPlayer inventory_player;
	[Export] private HBoxContainer inventoryGrid;
	[Export] private PackedScene invItemContainer;
	[Export] private Label inventoryCount;
	[Export] private AudioStream audio_inventory_open, audio_inventory_close;

	// =======| GUI |========
	public void SetTo(bool _state)
	{
		if (_state == Visible)
			return;

		if (_state)
		{
			Update();
			inventory_player.Play("open");
		}
		else
		{
			for (int i = 0; i < inventoryGrid.GetChildCount(); i++)
				inventoryGrid.GetChild(i).ProcessMode = ProcessModeEnum.Disabled;
			inventory_player.Play("close");
		}

		SoundManager.CreateSound(_state ? audio_inventory_open : audio_inventory_close);
		CanvasManager.UpdateInventoryIcon(_state);
	}
	public void Update()
	{
		for (int i = 0; i < inventoryGrid.GetChildCount(); i++)
			inventoryGrid.GetChild(i).QueueFree();

		for (int i = 0; i < Data.InventoryMaxCapacity; i++)
		{
			TextureButton _item = invItemContainer.Instantiate() as TextureButton;
			SetInventorySlot(_item, i < Data.Inventory.Count ? Data.Inventory[i] : "none");
			inventoryGrid.AddChild(_item);
		}
		inventoryCount.Text = Data.Inventory.Count + "/" + Data.InventoryMaxCapacity;
	}
	private void SetInventorySlot(TextureButton _item_slot, string _item_name)
	{
		if (_item_name != "none")
		{
			_item_slot.SetMeta("id", _item_name);
			_item_slot.TooltipText = $"{Tr(_item_name + "_name")}\n"
				+ $"{Tr(_item_name + "_desc")}";

			(_item_slot.GetChild(0) as TextureRect).Texture = GlobalManager.GetIcon(_item_name);
			// press function
			_item_slot.Pressed += () =>
			{
				if (Instance.IsDisabled)
					return;
				if (Instance.HeldPickup.Item != null)
					StoreCurrentItem();
				PullItem(_item_name);
				SetTo(false);
			};
		}
		else
			(_item_slot.GetChild(0) as TextureRect).Texture = null;
	}

	// =======| Functional |========
	public async void PullItem(string _item_name)
	{
		if (Data.Inventory.Count == 0 || !Data.Inventory.Contains(_item_name) || Instance.IsDisabled)
			return;

		if (Instance.HeldPickup.Item != null)
			await Instance.Drop();
		Node2D _item = ResortManager.CreateItem(_item_name, GlobalPosition);
		await Instance.Pickup(_item, _item.GetMeta("tag").ToString(), false);
		Data.Inventory.Remove(_item_name);
		SoundManager.CreateSound("ui/backpack_close");
	}
	public void PullItem(int _index)
	{
		if (_index >= Data.Inventory.Count || _index < 0)
			return;

		PullItem(Data.Inventory[_index]);
	}
	public static bool StoreItem(string _item)
	{
		if (string.IsNullOrEmpty(_item))
		{
			Logger.Print(Logger.LogPriority.Error, "PlayerInventory: This object is empty/null and cannot be stored.");
			return false;
		}

		if (!CanStoreItem())
			return false;
		Data.Inventory.Add(_item);

		if (Instance.inventory.Visible)
			Instance.inventory.Update();

		return true;
	}
	public static bool CanStoreItem() =>
		Data.Inventory.Count < Data.InventoryMaxCapacity;
	public static void StoreCurrentItem()
	{
		if (Instance.HeldPickup.Item.GetMeta("tag").ToString() == Aphid.Tag)
			return;

		if (!StoreItem(Instance.HeldPickup.Item.GetMeta("id").ToString()))
			return;

		SoundManager.CreateSound("ui/backpack_close");
		Instance.HeldPickup.Item.QueueFree();
		Instance.HeldPickup = new();
	}
}
