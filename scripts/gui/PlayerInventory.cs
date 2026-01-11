using Godot;

public partial class PlayerInventory : Control
{
	public static PlayerInventory Instance { get; private set; }
	public bool IsSelling;
	private bool enabled;
	private PackedScene item_container;
	private const string ITEM_CONTAINER_PREFAB = "uid://cn7d8wjyx78a3";
	public const int MAX_CAPACITY = 15;

	[Export] private AnimationPlayer animator;
	[Export] private HBoxContainer grid;
	[Export] private TextureButton modeButton, inventoryButton;
	[Export] private Texture2D[] buttonSprites = new Texture2D[2];
	[Export] private Label inventoryCountLabel;
	[Export] private RichTextLabel modeControlLabel, inventoryControlLabel;
	[Export] private Color slotColor;

	public override void _Ready()
	{
		Instance = this;
		item_container = ResourceLoader.Load(ITEM_CONTAINER_PREFAB) as PackedScene;

		modeControlLabel.Text = ControlsManager.GetLocalizedActionName(InputNames.ChangeMode);
		inventoryControlLabel.Text = ControlsManager.GetLocalizedActionName(InputNames.OpenInventory);
		inventoryButton.Pressed += () => SetTo(!enabled);
		modeButton.Pressed += ChangeInventoryMode;
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
		Instance.IsSelling = false;
		if (!_state)
		{
			for (int i = 0; i < Instance.grid.GetChildCount(); i++)
				Instance.grid.GetChild(i).ProcessMode = ProcessModeEnum.Disabled;
		}
		else
			Update();

		Instance.animator.Play(_state ? StringNames.OpenAnim : StringNames.CloseAnim);
		Instance.inventoryButton.TextureNormal = Instance.buttonSprites[_state ? 0 : 1];
		SoundManager.CreateSound(_state ? "ui/backpack_open" : "ui/backpack_close");
	}
	public static void Set() => SetTo(!Instance.enabled);
	public static void Update()
	{
		if (!IsInstanceValid(Instance) || !Instance.enabled)
			return;

		for (int i = 0; i < Instance.grid.GetChildCount(); i++)
			Instance.grid.GetChild(i).QueueFree();

		for (int i = 0; i < MAX_CAPACITY; i++)
		{
			TextureButton _item = Instance.item_container.Instantiate() as TextureButton;
			(_item.GetChild(0) as Control).SelfModulate = Instance.slotColor;
			Instance.SetInventorySlot(_item, i < Player.Data.Inventory.Count ? Player.Data.Inventory[i] : "none");
			Instance.grid.AddChild(_item);
		}
		Instance.inventoryCountLabel.Text = Instance.IsSelling ?
				"$$$" : Player.Data.Inventory.Count + "/" + MAX_CAPACITY;
	}
	private void SetInventorySlot(TextureButton _node, string _item_name)
	{
		if (!IsInstanceValid(Instance) || !Instance.enabled)
			return;

		TextureRect _slot = _node.GetChild(1) as TextureRect;

		if (_item_name == "none")
			return;

		_node.SetMeta(StringNames.IdMeta, _item_name);
		_node.TooltipText = GlobalManager.Utils.GetTooltipText(_item_name);
		_node.FocusMode = FocusModeEnum.None;
		_slot.Texture = GlobalManager.GetIcon(_item_name);

		// press function
		if (IsSelling)
			_node.Pressed += () => OnPressedSell(_item_name);
		else
			_node.Pressed += () => OnPressedPull(_item_name);
	}
	private void OnPressedSell(string _item_name)
	{
		if (Player.Instance.IsDisabled || _item_name == "aphid_egg" || !Player.Data.Inventory.Remove(_item_name))
		{
			SoundManager.CreateSound("ui/button_fail");
			return;
		}

		PlayerData.AddCurrency(GlobalManager.G_ITEMS[_item_name].Cost / 2);
		Update();

		GameManager.Data.ItemsSold++;
		SoundManager.CreateSound("ui/kaching");
	}
	private void OnPressedPull(string _item_name)
	{
		if (Player.Instance.IsDisabled || !IsInstanceValid(ResortManager.Current))
			SoundManager.CreateSound("ui/button_fail");
		else if (PullItem(_item_name))
			SetTo(false); // close inventory right after
	}
	private void ChangeControlPrompt(string _, StringName _action)
	{
		if (_action == InputNames.OpenInventory)
			inventoryControlLabel.Text = ControlsManager.GetLocalizedActionName(InputNames.OpenInventory);
		else if (_action == InputNames.ChangeMode)
			modeControlLabel.Text = ControlsManager.GetLocalizedActionName(InputNames.ChangeMode);
	}

	// =======| Functional |========
	public static bool PullItem(string _item_name)
	{
		if (Player.Instance.IsDisabled ||
				!Player.Data.Inventory.Contains(_item_name))
			return false;

		if (!StoreCurrentItem(true))
			return false;

		if (Player.Data.Inventory.Remove(_item_name))
		{
			Node2D _item = ResortManager.CreateItem(_item_name, Player.Instance.GlobalPosition);
			Player.Instance.PickupNoAnim(_item, (StringNames.GlobalTags)(int)_item.GetMeta(StringNames.TagMeta));
			Update();
			SoundManager.CreateSound("ui/backpack_open");
			return true;
		}
		else
			return false;
	}
	public static bool PullItem(int _index)
	{
		if (_index >= Player.Data.Inventory.Count || _index < 0)
			return false;

		return PullItem(Player.Data.Inventory[_index]);
	}
	/// <summary>
	/// Stores an item in the player's inventory.
	/// </summary>
	/// <param name="_id">The ID of the object.</param>
	/// <param name="_byPassCheck">Skip the check of CanBeStored(). Only set this to true if you already done it yourself.</param>
	/// <returns></returns>
	public static bool StoreItem(string _id, bool _byPassCheck = false)
	{
		return StoreItem(_id, 1, _byPassCheck);
	}

	/// <summary>
	/// Stores an item in the player's inventory.
	/// </summary>
	/// <param name="_id">The ID of the object.</param>
	/// <param name="_amount">The amount of items to be stored.</param>
	/// <param name="_byPassCheck">Skip the check of CanBeStored(). Only set this to true if you already done it yourself.</param>
	/// <returns></returns>
	public static bool StoreItem(string _id, int _amount, bool _byPassCheck = false)
	{
		if (!_byPassCheck && !CanBeStored(_id, _amount))
		{
			SoundManager.CreateSound("ui/button_fail");
			return false;
		}

		for (int i = 0; i < _amount; i++)
			Player.Data.Inventory.Add(_id);
		Update();
		return true;
	}

	/// <summary>
	/// Attempts to store the current item in hand, fails if is there none or the holding item isn't apt. (ex. if it is an aphid)
	/// </summary>
	/// <param name="_okayWithEmpty">Set this to true if you are okay with it not storing anything if there is no held item to store.</param>
	/// <returns>Wheter it could store the item.</returns>
	public static bool StoreCurrentItem(bool _okayWithEmpty = false)
	{
		if (Player.Instance.HeldPickup == null)
			return _okayWithEmpty;
			
		if (Player.Instance.HeldPickup.Tag == StringNames.GlobalTags.Aphid)
			return false;

		var _id = Player.Instance.HeldPickup.Entity.GetMeta(StringNames.IdMeta).ToString();

		if (StoreItem(_id))
		{
			Player.Instance.DropNoAnim(true);
			SoundManager.CreateSound("ui/backpack_close", false);
			return true;
		}
		else
			return false;
	}
	public void ChangeInventoryMode()
	{
		if (!enabled || animator.IsPlaying())
			return;

		IsSelling = !IsSelling;
		if (IsSelling)
			animator.Play("switch_to_sell");
		else
			animator.Play("switch_to_normal");
		SoundManager.CreateSound("ui/switch_mode", false);
		Update();
	}

	// MARK: Verifier Methods
	/// <summary>
    /// Checks if the inventory would be full with another item added.
    /// </summary>
    /// <param name="_amount">If two or more items need to be stored, check against this value instead.</param>
    /// <returns></returns>
	public static bool WouldInventoryBeFullWith(int _amount = 1)
	{
		if (_amount <= 0)
			return false;
		else
			return Player.Data.Inventory.Count + _amount > MAX_CAPACITY;
	}
	
	/// <summary>
	/// Checks wheter it is allowed to store this item or not.
	/// </summary>
	/// <param name="_id">The ID of the object.</param>
	/// <param name="_amount">The total amount that will be stored.</param>
	/// <returns></returns>
	public static bool CanBeStored(string _id, int _amount = 1)
	{
		if (string.IsNullOrEmpty(_id))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "PlayerInventory: This object is empty/null and cannot be stored.");
			return false;
		}

		if (WouldInventoryBeFullWith(_amount))
			return false;

		return true;
	}
}
