using Godot;

public partial class CanvasManager : CanvasLayer
{
	[Export] private AnimationPlayer info_player;
	[Export] public Control store_panel;
	[ExportCategory("Bio")]
	[Export] private TextEdit name_label;
	[Export] private BaseButton show_hash_button;
	[Export] private TextureProgressBar bondship;
	[Export] private TextureRect ageDisplay;
	[Export] private Texture2D[] age = new Texture2D[2];
	[ExportCategory("Stats Knobs")]
	[Export] private Control hungerK;
	[Export] private Control thirstK, affectionK;

	public static CanvasManager Instance;
	public static bool IsInFocus, IsInMenu;
	private static Control currentMenu, currentFocus;
	private const int offsetY = -9, lengthSize = 240;
	private Aphid aphid;

    public override void _Ready()
    {
		Instance = this;
		IsInFocus = IsInMenu = false;
		currentMenu = null;

        Player.Instance.OnPickup += OnAphidPickup;
		Player.Instance.OnDrop += OnAphidDrop;
		name_label.FocusEntered += () => SetInFocus(name_label);
		name_label.FocusExited += SetName;
		show_hash_button.Pressed += CopyHash;
    }
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("escape"))
			BackToMenu();

		if (Input.IsActionJustPressed("cancel"))
			EscapeMenu();
    }
    public override void _Input(InputEvent _event)
    {
		if (!IsInFocus)
			return;

		if (_event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			if (_input.KeyLabel == Key.Enter)
			{
				GetViewport().SetInputAsHandled();
				currentFocus.ReleaseFocus();
				return;
			}

			// allow character deletion and moving through the text
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			if (name_label.Text.Length > 20)
				GetViewport().SetInputAsHandled();
		}
    }

	public static void SetMenu(Control _menu)
	{
		currentMenu?.Hide();
		if (_menu == currentMenu)
			CloseMenu();
		else
		{
			_menu.Show();
			currentMenu = _menu;
			Player.Instance.SetDisabled(true);
			IsInMenu = true;
		}
	}
	public static void CloseMenu()
	{
		currentMenu?.Hide();
		currentMenu = null;
		Player.Instance.SetDisabled(false);
		IsInMenu = false;
	}
	public static void EscapeMenu()
	{
		if (currentMenu != null)
			CloseMenu();
	}
	public static async void BackToMenu()
	{
		if (!GameManager.IsBusy && !DialogManager.IsActive)
		{
			await SaveSystem.SaveProfile();
			await GameManager.LoadScene(GameManager.MenuScenePath);
		}
	}

	public static void SetInFocus(Control _focus)
	{
		currentFocus = _focus;
		IsInFocus = true;
	}
	public static void RemoveFromFocus()
	{
		currentFocus = null;
		IsInFocus = false;
	}

	// =========| APHID INFO RELATED FUNCTIONALITY |=============
    private void SetName()
	{
		if	(aphid == null)
			return;
		
		aphid.Instance.Status.Name = name_label.Text;
		RemoveFromFocus();
	}
	private void CopyHash()
	{
		if (aphid == null)
			return;

		DisplayServer.ClipboardSet(aphid.Instance.ID);
	}

    private void OnAphidPickup(string _tag)
    {
		if (_tag != "aphid")
			return;

		info_player.Play("swipe_right");
		aphid = Player.Instance.PickupItem as Aphid;
		UpdateAphidInfo();
    }
	private void OnAphidDrop()
	{
		aphid = null;
		info_player.Play("swipe_left");
	}
	private void UpdateAphidInfo()
	{
		name_label.Text = aphid.Instance.Status.Name;

		hungerK.SetPosition(new(aphid.Instance.Status.Hunger * 0.01f * lengthSize, offsetY));
		thirstK.SetPosition(new(aphid.Instance.Status.Thirst * 0.01f * lengthSize, offsetY));
		affectionK.SetPosition(new(aphid.Instance.Status.Affection * 0.01f * lengthSize, offsetY));

		ageDisplay.Texture = age[aphid.Instance.Status.IsAdult ? 1 : 0];
		bondship.Value = aphid.Instance.Status.Bondship;
	}
}
