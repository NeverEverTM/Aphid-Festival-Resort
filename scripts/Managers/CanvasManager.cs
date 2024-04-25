using Godot;

public partial class CanvasManager : CanvasLayer
{
	[Export] public AnimationPlayer generations_panel;
	[Export] private Label currency_text;

	public static CanvasManager Instance;
	public static bool IsInFocus, IsInMenu;
	private static Control currentFocus;
	private static AnimationPlayer currentMenu;

	public override void _Ready()
	{
		Instance = this;
		IsInFocus = IsInMenu = false;
		currentMenu = null;
	}
	public override void _Process(double delta)
	{
		UpdateCurrency();

		if (Input.IsActionJustPressed("escape") && !IsInMenu && !PauseMenu.Instance.Visible)
			PauseMenu.Instance.SetPauseMenu(true);

		if ((Input.IsActionJustPressed("cancel") || Input.IsActionJustPressed("escape")) && !GetTree().Paused)
			EscapeMenu();
	}

	private static void UpdateCurrency()
	{
		Instance.currency_text.Text = Player.Data.Currency.ToString("000");
	}

	public static void OpenMenu(AnimationPlayer _menu)
	{
		if (Player.Instance.PickupItem != null)
			Player.Instance.Drop();

		if (_menu.Equals(currentMenu))
			CloseMenu();
		else
			ChangeMenu(_menu);
	}
	public static void ChangeMenu(AnimationPlayer _menu)
	{
		currentMenu?.Play("close");
		_menu.Play("open");
		currentMenu = _menu;
		Player.Instance.SetDisabled(true);
		IsInMenu = true;
	}
	public static void CloseMenu()
	{
		currentMenu?.Play("close");
		currentMenu = null;
		Player.Instance.SetDisabled(false);
		IsInMenu = false;
	}
	public static void EscapeMenu()
	{
		if (currentMenu != null)
			CloseMenu();
	}

	public static void SetFocus(Control _focus)
	{
		currentFocus = _focus;
		currentFocus.GrabFocus();
		IsInFocus = true;
	}
	public static void RemoveFocus()
	{
		currentFocus?.ReleaseFocus();
		currentFocus = null;
		IsInFocus = false;
	}
}
