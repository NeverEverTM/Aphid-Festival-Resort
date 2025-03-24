using Godot;
using static Player;

public partial class PlayerInventory : Control
{
	public static PlayerInventory Singleton { get; private set;}
	private AudioStream audio_inventory_open, audio_inventory_close;
	private bool is_selling;
	private PackedScene itemContainer;

	[Export] private AnimationPlayer player;
	[Export] private HBoxContainer grid;
	[Export] private TextureButton modeButton, inventoryButton;
	[Export] private Texture2D[] buttonSprites = new Texture2D[2];
	[Export] private Label inventoryCountLabel;
	[Export] private RichTextLabel modeControlLabel, inventoryControlLabel;

    public override void _Ready()
    {
        Singleton = this;
		audio_inventory_open = SoundManager.GetAudioStream("ui/backpack_open");
		audio_inventory_close = SoundManager.GetAudioStream("ui/backpack_close");
		itemContainer = ResourceLoader.Load("uid://boxaly7dtxe0r") as PackedScene;

		modeControlLabel.Text = ControlsManager.GetActionName(InputNames.ChangeMode);
		inventoryControlLabel.Text = ControlsManager.GetActionName(InputNames.OpenInventory);
		inventoryButton.Pressed += () => Instance.inventory.SetTo(!Visible);
		modeButton.Pressed += () =>
		{
			is_selling = !is_selling;
			if (is_selling)
				player.Play("switch_to_sell");
			else
				player.Play("switch_to_normal");
				SoundManager.CreateSound("ui/switch_mode",false);
			Update();
		};
		ControlsManager.OnControlChanged += ChangeControlPrompt;
	}
    public override void _ExitTree()
    {
        ControlsManager.OnControlChanged -= ChangeControlPrompt;
    }

    // =======| GUI |========
    public void SetTo(bool _state)
	{
		if (_state == Visible)
			return;

		is_selling = false;
		if (_state)
			Update();
		else
		{
			for (int i = 0; i < grid.GetChildCount(); i++)
				grid.GetChild(i).ProcessMode = ProcessModeEnum.Disabled;
		}

		player.Play(_state ? StringNames.OpenAnim : StringNames.CloseAnim);
		SoundManager.CreateSound(_state ? audio_inventory_open : audio_inventory_close);
		inventoryButton.TextureNormal = buttonSprites[_state ? 0 : 1];
	}
	public void Update()
	{
		for (int i = 0; i < grid.GetChildCount(); i++)
			grid.GetChild(i).QueueFree();

		for (int i = 0; i < Data.InventoryMaxCapacity; i++)
		{
			TextureButton _item = itemContainer.Instantiate() as TextureButton;
			SetInventorySlot(_item, i < Data.Inventory.Count ? Data.Inventory[i] : "none");
			grid.AddChild(_item);
		}
		inventoryCountLabel.Text = is_selling ? 
				"$$$" : Data.Inventory.Count + "/" + Data.InventoryMaxCapacity;
	}
	private void SetInventorySlot(TextureButton _item_slot, string _item_name)
	{
		if (_item_name != "none")
		{
			_item_slot.SetMeta(StringNames.IdMeta, _item_name);
			_item_slot.TooltipText = $"{Tr(_item_name + "_name")}\n"
				+ $"{Tr(_item_name + "_desc")}";

			(_item_slot.GetChild(0) as TextureRect).Texture = GlobalManager.GetIcon(_item_name);
			// press function
			if (!is_selling)
			{
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
			{
				_item_slot.Pressed += () =>
				{
					if (Instance.IsDisabled)
						return;
					Data.Inventory.Remove(_item_name);
					Data.AddCurrency(GlobalManager.G_ITEMS[_item_name].cost / 2);
					Update();
					SoundManager.CreateSound("ui/kaching");
				};
			}
		}
		else
			(_item_slot.GetChild(0) as TextureRect).Texture = null;
	}
	private void ChangeControlPrompt(string _, StringName _action)
	{
		if (_action == InputNames.OpenInventory)
			inventoryControlLabel.Text = ControlsManager.GetActionName(InputNames.OpenInventory);
		else if (_action == InputNames.ChangeMode)
			modeControlLabel.Text = ControlsManager.GetActionName(InputNames.ChangeMode);
	}

	// =======| Functional |========
	public async void PullItem(string _item_name)
	{
		if (Data.Inventory.Count == 0 || !Data.Inventory.Contains(_item_name) || Instance.IsDisabled)
			return;

		if (Instance.HeldPickup.Item != null)
			await Instance.Drop();
		Node2D _item = ResortManager.CreateItem(_item_name, GlobalPosition);
		await Instance.Pickup(_item, _item.GetMeta(StringNames.TagMeta).ToString(), false);
		Data.Inventory.Remove(_item_name);
		Update();
		SoundManager.CreateSound(audio_inventory_close);
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
		if (Instance.HeldPickup.Item.GetMeta(StringNames.TagMeta).ToString() == Aphid.Tag)
			return;

		if (!StoreItem(Instance.HeldPickup.Item.GetMeta(StringNames.IdMeta).ToString()))
			return;

		SoundManager.CreateSound(Singleton.audio_inventory_close);
		Instance.HeldPickup.Item.QueueFree();
		Instance.HeldPickup = new();
	}
}
