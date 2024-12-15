using Godot;

public partial class NewGameMenu : Control
{
	public const string newGameCategory = "new_game";

	[Export] private BaseButton new_game_button;
	[Export] private TextEdit resort_name_input, player_name_input, pronouns_input;
	[Export] private AnimationPlayer menuPlayer;
	[Export] private Control popup_anchor;

	private const int resort_charLimit = 40, name_charLimit = 15;
	private const string defaultName = "Mello";

	public override void _Ready()
	{
		player_name_input.Text = defaultName;
		new_game_button.Pressed += CreateResort;
	}
	public override void _Process(double delta)
	{
		if (!Visible)
			menuPlayer.Play("RESET");
	}
	public override void _Input(InputEvent @event)
	{
		// we separate it cause, UnhandledInput does not detect it if is hold
		if (!Visible)
			return;

		bool _resortIsFocused = resort_name_input.HasFocus(),
		_playerIsFocused = player_name_input.HasFocus();

		if ((_resortIsFocused || _playerIsFocused) && @event is InputEventKey && @event.IsPressed())
		{
			InputEventKey _input = @event as InputEventKey;

			// move through inputs
			if (_input.KeyLabel == Key.Up)
			{
				if (_resortIsFocused)
					new_game_button.GrabFocus();
				else if (_playerIsFocused)
					resort_name_input.GrabFocus();
				else
					player_name_input.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}
			if (_input.KeyLabel == Key.Down)
			{
				if (_resortIsFocused)
					player_name_input.GrabFocus();
				else if (_playerIsFocused)
					new_game_button.GrabFocus();
				else
					resort_name_input.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}

			// confirm input
			if (_input.KeyLabel == Key.Enter)
			{
				if (_resortIsFocused)
					player_name_input.GrabFocus();
				else
					new_game_button.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}

			// allow character deletion and moving through the text
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			if ((_resortIsFocused && resort_name_input.Text.Length >= resort_charLimit) ||
			(_playerIsFocused && player_name_input.Text.Length >= name_charLimit))
				GetViewport().SetInputAsHandled();
			return;
		}
	}

	public void AddMenuAction()
	{

		MainMenu.Instance.CreateMenuAction(newGameCategory, () =>
		{
			resort_name_input.Text = "";
			player_name_input.Text = "";
			pronouns_input.Text = "";
			resort_name_input.GrabFocus();
			MainMenu.Instance.SetMenu(this);
			menuPlayer.Play("open");
		});
	}
	private async void CreateResort()
	{
		if (GameManager.IsBusy)
			return;

		string _resortName = resort_name_input.Text;

		// Its a secreeeeeet
		if (_resortName == "iblamemar")
		{
			DebugConsole.IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt = true;
			SoundManager.CreateSound(Aphid.Audio_Hurt);
			return;
		}

		// Invalid resort names
		if (string.IsNullOrWhiteSpace(_resortName) || !_resortName.IsValidFileName())
		{
			GameManager.CreatePopup("warning_invalid_name", popup_anchor);
			return;
		}

		// Already Exists
		SaveSystem.SelectProfile(_resortName);
		if (DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			GameManager.CreatePopup("warning_already_exists", popup_anchor);
			return;
		}

		// Start the game
		ResortManager.IsNewGame = true;
		Player.NewName = !string.IsNullOrWhiteSpace(player_name_input.Text) ? player_name_input.Text : defaultName;
		Player.NewPronouns = !string.IsNullOrWhiteSpace(pronouns_input.Text) ? pronouns_input.Text.Split("/") : new string[] { "They", "them" };
		await SaveSystem.CreateProfile();
		MainMenu.LoadResort();
	}
}
