using Godot;
using System;
using System.Text.RegularExpressions;

public partial class NewGameMenu : Control
{
	[Export] private AnimationPlayer anim_player;
	[Export] private BaseButton new_game_button;
	[Export] private TextEdit resort_name_input, player_name_input, pronouns_input;
	[Export] private Control popup_anchor;

	[GeneratedRegex(@"\[.+?\]")]
	private static partial Regex strip_bbcode();
	private const int resort_char_limit = 40, name_char_limit = 15, pronouns_char_limit = 25;
	private MenuInstance menu;

	/// <summary>
	/// Last valid new name set during game creation. All Player instances inherit this by default.
	/// </summary>
	public static string NewName { get; set; }
	/// <summary>
	/// Last valid new set of pronouns made during game creation. All Player instances inherit this by default.
	/// </summary>
	public static string[] NewPronouns { get; set; }

	public override void _EnterTree()
	{
		menu = new(Enum.GetName(StartMenu.WheelCategories.NewGame), anim_player, Open, null, Close, null, true);
		StartMenu.CreateWheelAction(StartMenu.WheelCategories.NewGame, menu);
		NewName = StringNames.DefaultPlayerName;
		NewPronouns = StringNames.DefaultPlayerPronouns;
	}
	public override void _Ready()
	{
		SetProcessInput(false);
		new_game_button.Pressed += CreateResort;
		player_name_input.TextChanged += () => SoundManager.CreateSound("ui/key_type", false);
		pronouns_input.TextChanged += () => SoundManager.CreateSound("ui/key_type", false);
		resort_name_input.TextChanged += () => SoundManager.CreateSound("ui/key_type", false);
		new_game_button.Pressed += () => SoundManager.CreateSound("ui/lock");
    }

	public void Open(MenuInstance _next)
	{
		SetProcessInput(true);
		player_name_input.Text = string.Empty;
		resort_name_input.Text = string.Empty;
		pronouns_input.Text = string.Empty;
		player_name_input.GrabFocus();
	}
	public void Close(MenuInstance _)
	{
		SetProcessInput(false);
	}

	public override void _Input(InputEvent @event)
	{
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
				AcceptEvent();
				SoundManager.CreateSound("ui/button_select");
				return;
			}

			// allow character deletion and moving through ui
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			if ((_resortIsFocused && resort_name_input.Text.Length >= resort_char_limit) || // Resort Name Char Limit
				(_playerIsFocused && player_name_input.Text.Length >= name_char_limit) || // Player Name Char Limit
				(_pronounsIsFocused && (pronouns_input.Text.Split('/').Length > 4 // Pronouns Max Elements
					|| pronouns_input.Text.Length >= pronouns_char_limit))) // Pronouns Char Limit
			{
				AcceptEvent();
				SoundManager.CreateSound("ui/button_fail");
			}
		}
	}
	private async void CreateResort()
	{
		if (GlobalManager.IsBusy)
			return;

		if (!SanitizeResortName(out string _resortName))
			return;

		// Start the game
		GameManager.IsANewSavefile = true;
		SaveSystem.SelectProfile(_resortName);
		NewName = SanitizePlayerName();
		NewPronouns = SanitizePronouns();

		await SaveSystem.CreateProfile();
		MainMenu.LoadResort();
	}
	private bool SanitizeResortName(out string _resortName)
	{
		_resortName = resort_name_input.Text;

		switch (_resortName)
		{
			// Its a secreeeeeet
			case "iblamemar":
				DebugConsole.IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt = true;
				SoundManager.CreateSound("aphid/hurt");
				return false;
			case "MEDIC!":
			case "medic!":
				SoundManager.CreateSound("misc/medic_prognosis", false);
				return false;
		}

		_resortName = strip_bbcode().Replace(_resortName, string.Empty);

		// Invalid resort names
		if (string.IsNullOrWhiteSpace(_resortName) || _resortName.EndsWith('.') || !_resortName.IsValidFileName())
		{
			GlobalManager.CREATE_POPUP("warning_invalid_name", popup_anchor);
			return false;
		}

		// Already Exists
		if (DirAccess.DirExistsAbsolute(SaveSystem.GetProfilePath(_resortName)))
		{
			GlobalManager.CREATE_POPUP("warning_already_exists", popup_anchor);
			return false;
		}

		return true;
	}
	private string SanitizePlayerName()
	{
		string _playerName = player_name_input.Text;

		_playerName = strip_bbcode().Replace(_playerName, string.Empty);

		if (string.IsNullOrWhiteSpace(_playerName))
			_playerName = StringNames.DefaultPlayerName;

		return _playerName;
	}
	private string[] SanitizePronouns()
	{
		string[] _pronouns;

		if (string.IsNullOrWhiteSpace(pronouns_input.Text))
			_pronouns = StringNames.DefaultPlayerPronouns;
		else
			_pronouns = pronouns_input.Text.Split("/");
		_pronouns[0].Capitalize();

		return _pronouns;
	}
}
