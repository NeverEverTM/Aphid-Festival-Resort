using Godot;

public partial class NewGameMenu : Control
{
	public const string newGameCategory = "new_game";

	[Export] private BaseButton new_game_button;
	[Export] private TextEdit resort_name_input, player_name_input, pronouns_input;
	[Export] private AnimationPlayer menuPlayer;
	[Export] private Control popup_anchor;

	private const int resort_char_limit = 40, name_char_limit = 15, pronouns_char_limit = 25;
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
		_playerIsFocused = player_name_input.HasFocus(), _pronounsIsFocused = pronouns_input.HasFocus();

		if ((_resortIsFocused || _playerIsFocused || _pronounsIsFocused) && @event is InputEventKey && @event.IsPressed())
		{
			InputEventKey _input = @event as InputEventKey;

			// confirm input
			if (_input.KeyLabel == Key.Enter || _input.KeyLabel == Key.Tab) 
			{
				if (_playerIsFocused)
					pronouns_input.GrabFocus();
				else if (_pronounsIsFocused)
					resort_name_input.GrabFocus();
				else
					new_game_button.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}

			// allow character deletion and moving through ui
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			if ((_resortIsFocused && resort_name_input.Text.Length >= resort_char_limit) || // Resort Name Char Limit
				(_playerIsFocused && player_name_input.Text.Length >= name_char_limit) || // Player Name Char Limit
				(_pronounsIsFocused && (pronouns_input.Text.Split('/').Length > 4 // Pronouns Max Elements
					|| pronouns_input.Text.Length >= pronouns_char_limit))) // Pronouns Char Limit
				GetViewport().SetInputAsHandled();
		}
	}

	public void AddMenuAction()
	{
		MainMenu.Instance.CreateMenuAction(newGameCategory, () =>
		{
			resort_name_input.Text = string.Empty;
			player_name_input.Text = string.Empty;
			pronouns_input.Text = string.Empty;
			player_name_input.GrabFocus();
			MainMenu.Instance.SetMenu(this);
			menuPlayer.Play("open");
		});
		MainMenu.Instance.SetCategory(newGameCategory);
	}
	private async void CreateResort()
	{
		if (GlobalManager.IsBusy)
			return;

		string _resortName = resort_name_input.Text;

		// Its a secreeeeeet
		if (_resortName == "iblamemar")
		{
			DebugConsole.IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt = true;
			SoundManager.CreateSound(Aphid.Audio_Hurt);
			return;
		}
		else if (_resortName.ToLower() == "medic!")
		{
			SoundManager.CreateSound("misc/medic_prognosis", false);
			MainMenu.Instance.CloseMenu();
			return;
		}

		// Invalid resort names
		if (string.IsNullOrWhiteSpace(_resortName) || !_resortName.IsValidFileName())
		{
			GlobalManager.CreatePopup("warning_invalid_name", popup_anchor);
			return;
		}

		// Already Exists
		SaveSystem.SelectProfile(_resortName);
		if (DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			GlobalManager.CreatePopup("warning_already_exists", popup_anchor);
			return;
		}

		// Start the game
		GameManager.IsNewGame = true;
		Player.NewName = !string.IsNullOrWhiteSpace(player_name_input.Text) ? 
				player_name_input.Text : defaultName;
		if (!string.IsNullOrWhiteSpace(pronouns_input.Text))
		{
			Player.NewPronouns = pronouns_input.Text.Split("/");
			Player.NewPronouns[0].Capitalize();
		} 
		else
			Player.NewPronouns = new string[] { Tr("pronouns_they"), Tr("pronouns_them") };
		await SaveSystem.CreateProfile();
		MainMenu.LoadResort();
	}
}
