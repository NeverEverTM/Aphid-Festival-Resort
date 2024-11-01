using Godot;
using static Player;

public partial class PlayerInventory : Control
{
	[Export] private AnimationPlayer inventory_player;
	[Export] private HBoxContainer inventoryGrid;
	[Export] private PackedScene invItemContainer;
	[Export] private Label inventoryCount;

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton && (@event as InputEventMouseButton).Pressed)
			GetViewport().SetInputAsHandled();
	}

	// =======| GUI |========
	public void Enable(bool _state)
	{
		if (_state)
		{
			Update();
			inventory_player.Play("slide_up");
		}
		else
		{
			for (int i = 0; i < inventoryGrid.GetChildCount(); i++)
				inventoryGrid.GetChild(i).QueueFree();
			inventory_player.Play("slide_down");
		}
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

			(_item_slot.GetChild(0) as TextureRect).Texture = GameManager.GetIcon(_item_name);
			// press function
			_item_slot.Pressed += () =>
			{
				if (Instance.IsDisabled)
					return;
				if (Instance.PickupItem != null)
					StoreCurrentItem();
				PullItem(_item_name);
				inventory_player.Play("slide_down");
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

		if (Instance.PickupItem != null)
			await Instance.Drop();
		Node2D _item = ResortManager.CreateItem(_item_name, GlobalPosition);
		await Instance.Pickup(_item, _item.GetMeta("tag").ToString(), false);
		Data.Inventory.Remove(_item_name);
	}
	public void PullItem(int _index)
	{
		if (_index >= Data.Inventory.Count || _index < 0)
			return;

		PullItem(Data.Inventory[_index]);
	}
	public static bool StoreItem(string _item)
	{
		if (Data.Inventory.Count >= Data.InventoryMaxCapacity)
			return false;
		Data.Inventory.Add(_item);

		if (Instance.inventory.Visible)
			Instance.inventory.Update();

		return true;
	}
	public static void StoreCurrentItem()
	{
		if (Instance.PickupItem.GetMeta("tag").ToString() == "aphid")
			return;

		if (!StoreItem(Instance.PickupItem.GetMeta("id").ToString()))
			return;

		Instance.PickupItem.QueueFree();
		Instance.PickupItem = null;
	}
}
