using Godot;
using static Player;

public partial class PlayerInventory : Control
{
	[Export] private AnimationPlayer inventory_player;
	[Export] private HBoxContainer inventoryGrid;
	[Export] private PackedScene invItemContainer;

	// =======| GUI |========
	public void SetInventoryHUD(bool _state)
	{
		if (_state)
		{
			CreateInventory();
			inventory_player.Play("slide_up");
			// Grab first item as focus
			if (inventoryGrid.GetChildCount() > 0)
				inventoryGrid.GetChild(0).CallDeferred(Control.MethodName.GrabFocus);
		}
		else
			inventory_player.Play("slide_down");
	}
	private void CreateInventory()
	{
		for (int i = 0; i < inventoryGrid.GetChildCount(); i++)
			inventoryGrid.GetChild(i).QueueFree();

		for (int i = 0; i < Data.Inventory.Count; i++)
			CreateInvItem(i);
	}
	private void CreateInvItem(int _index)
	{
		TextureButton _item = invItemContainer.Instantiate() as TextureButton;
		var _item_name = Data.Inventory[_index];
		_item.SetMeta("id", _item_name);

		// check for available icon
		(_item.GetChild(0) as TextureRect).Texture = GameManager.GetIcon(_item_name);

		// press function
		_item.Pressed += () =>
		{
			PullItem(_item_name);
			_item.QueueFree();
			inventory_player.Play("slide_down");
		};
		inventoryGrid.AddChild(_item);
	}

	// =======| Functional |========
	private void PullItem(string _item_name)
	{
		if (Data.Inventory.Count == 0 || !Data.Inventory.Contains(_item_name))
			return;

		// get rid of inventory slot
		for (int i = 0; i < inventoryGrid.GetChildCount(); i++)
		{
			Node _child = inventoryGrid.GetChild(i);
			if (_child.GetMeta("id").ToString() == _item_name)
			{
				_child.QueueFree();
				break;
			}
		}

		if (Instance.PickupItem != null)
			Instance.Drop();
		Node2D _item = ResortManager.CreateItem(_item_name, GlobalPosition);
		Instance.Pickup(_item, _item.GetMeta("tag").ToString());
		Data.Inventory.Remove(_item_name);
	}
	public void PullItem(int _index)
	{
		if (Data.Inventory.Count == 0 || _index >= Data.Inventory.Count)
			return;

		// get rid of inventory slot if inventory is visible
		if (Visible)
			inventoryGrid.GetChild(_index).QueueFree();

		if (Instance.PickupItem != null)
			Instance.Drop();
		Node2D _item = ResortManager.CreateItem(Data.Inventory[_index], GlobalPosition);
		Instance.Pickup(_item, _item.GetMeta("tag").ToString());
		Data.Inventory.RemoveAt(_index);
	}
	public static bool StoreItem(string _item)
	{
		if (Data.Inventory.Count >= 15)
			return false;
		Data.Inventory.Add(_item);

		if (Instance.inventory.Visible)
			Instance.inventory.CreateInvItem(Data.Inventory.Count - 1);

		return true;
	}
	public void StoreCurrentItem()
	{
		if (Instance.PickupItem.GetMeta("tag").ToString() == "aphid")
			return;

		if (!StoreItem(Instance.PickupItem.GetMeta("id").ToString()))
			return;

		Instance.PickupItem.QueueFree();
		Instance.PickupItem = null;
	}
}
