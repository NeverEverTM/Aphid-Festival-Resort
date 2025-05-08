using Godot;

public partial class PlayerInventory : Control
{
	public static PlayerInventory Instance { get; private set; }
	private AudioStream audio_inventory_open, audio_inventory_close;
	private bool is_selling, enabled;
	private PackedScene itemContainer;

	[Export] private AnimationPlayer player;
	[Export] private HBoxContainer grid;
	[Export] private TextureButton modeButton, inventoryButton;
	[Export] private Texture2D[] buttonSprites = new Texture2D[2];
	[Export] private Label inventoryCountLabel;
	[Export] private RichTextLabel modeControlLabel, inventoryControlLabel;

	public override void _Ready()
	{
		Instance = this;
		audio_inventory_open = SoundManager.GetAudioStream("ui/backpack_open");
		audio_inventory_close = SoundManager.GetAudioStream("ui/backpack_close");
		itemContainer = ResourceLoader.Load("uid://boxaly7dtxe0r") as PackedScene;

		modeControlLabel.Text = ControlsManager.GetActionName(InputNames.ChangeMode);
		inventoryControlLabel.Text = ControlsManager.GetActionName(InputNames.OpenInventory);
		inventoryButton.Pressed += () => SetTo(!enabled);
		modeButton.Pressed += ChangeSellMode;
		ControlsManager.OnControlChanged += ChangeControlPrompt;
	}
	public override void _ExitTree()
	{
		ControlsManager.OnControlChanged -= ChangeControlPrompt;
	}

	// =======| GUI |========
	public static void SetTo(bool _state)
	{
		if (!IsInstanceValid(Instance) || _state == Instance.enabled)
			return;

		Instance.enabled = _state;
		Instance.is_selling = false;
		if (!_state)
		{
			for (int i = 0; i < Instance.grid.GetChildCount(); i++)
				Instance.grid.GetChild(i).ProcessMode = ProcessModeEnum.Disabled;
		}
		else
			Update();

		Instance.player.Play(_state ? StringNames.OpenAnim : StringNames.CloseAnim);
		Instance.inventoryButton.TextureNormal = Instance.buttonSprites[_state ? 0 : 1];
		SoundManager.CreateSound(_state ? Instance.audio_inventory_open : Instance.audio_inventory_close);
	}
	public static void Set() => SetTo(!Instance.enabled);
	public static void Update()
	{
		if (!IsInstanceValid(Instance) || !Instance.enabled)
			return;

		for (int i = 0; i < Instance.grid.GetChildCount(); i++)
			Instance.grid.GetChild(i).QueueFree();

		for (int i = 0; i < Player.Data.InventoryMaxCapacity; i++)
		{
			TextureButton _item = Instance.itemContainer.Instantiate() as TextureButton;
			Instance.SetInventorySlot(_item, i < Player.Data.Inventory.Count ? Player.Data.Inventory[i] : "none");
			Instance.grid.AddChild(_item);
		}
		Instance.inventoryCountLabel.Text = Instance.is_selling ?
				"$$$" : Player.Data.Inventory.Count + "/" + Player.Data.InventoryMaxCapacity;
	}
	private void SetInventorySlot(TextureButton _item_slot, string _item_name)
	{
		if (!IsInstanceValid(Instance) || !Instance.enabled)
			return;

		if (_item_name == "none")
		{
			(_item_slot.GetChild(0) as TextureRect).Texture = null;
			return;
		}

		_item_slot.SetMeta(StringNames.IdMeta, _item_name);
		var _desc = Tr(_item_name + "_desc");
		_item_slot.TooltipText = Tr(_item_name + "_name") + "\n" +
			_desc + (_desc.Length == 20 ? "\n" : string.Empty);

		(_item_slot.GetChild(0) as TextureRect).Texture = GlobalManager.GetIcon(_item_name);
		// press function
		if (!is_selling)
		{
			void _pressed_store()
			{
				if (Player.Instance.IsDisabled)
					return;
				if (Player.Instance.HeldPickup.Item != null && !StoreCurrentItem())
				{
					SoundManager.CreateSound("ui/button_fail");
					return;
				}

				PullItem(_item_name);
				SetTo(false);
			}
			_item_slot.Pressed += _pressed_store;
		}
		else
		{
			void _pressed_selling()
			{
				if (Player.Instance.IsDisabled)
					return;

				if (_item_name == "aphid_egg")
				{
					SoundManager.CreateSound("ui/button_fail");
					return;
				}

				Player.Data.Inventory.Remove(_item_name);
				Player.Data.ChangeCurrency(GlobalManager.G_ITEMS[_item_name].cost / 2);
				Update();
				SoundManager.CreateSound("ui/kaching");
			}
			_item_slot.Pressed += _pressed_selling;
		}
	}
	private void ChangeControlPrompt(string _, StringName _action)
	{
		if (_action == InputNames.OpenInventory)
			inventoryControlLabel.Text = ControlsManager.GetActionName(InputNames.OpenInventory);
		else if (_action == InputNames.ChangeMode)
			modeControlLabel.Text = ControlsManager.GetActionName(InputNames.ChangeMode);
	}

	// =======| Functional |========
	public static async void PullItem(string _item_name)
	{
		if (Player.Data.Inventory.Count == 0 || !Player.Data.Inventory.Contains(_item_name) || Player.Instance.IsDisabled)
			return;

		if (Player.Instance.HeldPickup.Item != null)
			Player.Instance.Drop();
		Node2D _item = ResortManager.CreateItem(_item_name, Player.Instance.GlobalPosition);
		await Player.Instance.Pickup(_item, _item.GetMeta(StringNames.TagMeta).ToString(), false);
		Player.Data.Inventory.Remove(_item_name);
		Update();
		SoundManager.CreateSound(Instance.audio_inventory_close);
	}
	public static void PullItem(int _index)
	{
		if (_index >= Player.Data.Inventory.Count || _index < 0)
			return;

		PullItem(Player.Data.Inventory[_index]);
	}
	public static bool StoreItem(string _item, bool _force = false)
	{
		if (string.IsNullOrEmpty(_item))
		{
			Logger.Print(Logger.LogPriority.Error, "PlayerInventory: This object is empty/null and cannot be stored.");
			return false;
		}

		if (!CanStoreItem())
		{
			if (_force)
				ResortManager.CreateItem(_item, Player.Instance.GlobalPosition);
			else
				SoundManager.CreateSound("ui/button_fail");
			return false;
		}

		Player.Data.Inventory.Add(_item);
		Update();

		return true;
	}
	public static bool CanStoreItem(int _amount = 1) =>
		Player.Data.Inventory.Count + (_amount - 1) < Player.Data.InventoryMaxCapacity;

	public static bool StoreCurrentItem()
	{
		if (Player.Instance.HeldPickup.Item.GetMeta(StringNames.TagMeta).ToString() == Aphid.Tag)
			return false;

		var _id = Player.Instance.HeldPickup.Item.GetMeta(StringNames.IdMeta).ToString();

		if (_id == "aphid_egg") // aphid eggs cannot be stored back
			return false;

		if (!Player.Instance.CanDrop())
			return false;

		if (!StoreItem(_id))
			return false;

		Player.Instance.Drop(false);
		SoundManager.CreateSound(Instance.audio_inventory_close);
		return true;
	}
	public static void ChangeSellMode()
	{
		if (!IsInstanceValid(Instance))
			return;

		if (!Instance.enabled)
			return;

		if (Instance.player.IsPlaying())
			return;

		Instance.is_selling = !Instance.is_selling;
		if (Instance.is_selling)
			Instance.player.Play("switch_to_sell");
		else
			Instance.player.Play("switch_to_normal");
		SoundManager.CreateSound("ui/switch_mode", false);
		Update();
	}
}
