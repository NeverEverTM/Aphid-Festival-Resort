using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class NewGameMenu : Control
{
	[Export] private AnimationPlayer anim_player;
	[Export] private BaseButton new_game_button;
	[Export] private TextEdit resort_name_input, player_name_input, pronouns_input;
	[Export] private Control popup_anchor;

	[GeneratedRegex(@"[\(\)\[\]]")]
	private static partial Regex remove_invalid_characters();
	private const int resort_char_limit = 40, name_char_limit = 15, pronouns_char_limit = 25;
	private MenuInstance menu;
	private Dictionary<Control, Vector2> cached_positions = [];

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
		player_name_input.TextChanged += () => TextTween(player_name_input);
		pronouns_input.TextChanged += () => TextTween(pronouns_input);
		resort_name_input.TextChanged += () => TextTween(resort_name_input);
	}
	private void TextTween(Control _node)
	{
		if (!cached_positions.ContainsKey(_node))
			cached_positions.Add(_node, _node.Position);
		_node.Position = cached_positions[_node];
		SoundManager.CreateSound("ui/key_type", false);

		Tween _tween = _node.CreateTween();
		_tween.TweenProperty(_node, "position", _node.Position + new Vector2(0, 5), 0);
		_tween.TweenProperty(_node, "position", _node.Position, 0.05f);
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
		if (@event is not InputEventKey || !@event.IsPressed() || @event.IsEcho())
			return;

		bool _resortIsFocused = resort_name_input.HasFocus(), _playerIsFocused = player_name_input.HasFocus(), _pronounsIsFocused = pronouns_input.HasFocus();
		if (!_resortIsFocused && !_playerIsFocused && !_pronounsIsFocused)
			return;

		InputEventKey _input = @event as InputEventKey;
		Key _inputKey = (Key)_input.Unicode;

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

		// invalid keys
		if (_inputKey == Key.Parenleft || _inputKey == Key.Parenright ||
			_inputKey == Key.Bracketleft || _inputKey == Key.Bracketright ||
			_inputKey == Key.Braceleft || _inputKey == Key.Braceright)
		{
			AcceptEvent();
			SoundManager.CreateSound("ui/button_fail");
		}

		// char limits
		if ((_resortIsFocused && resort_name_input.Text.Length >= resort_char_limit) || // Resort Name Char Limit
			(_playerIsFocused && player_name_input.Text.Length >= name_char_limit) || // Player Name Char Limit
			(_pronounsIsFocused && (pronouns_input.Text.Split('/').Length > 4 // Pronouns Max Elements
				|| pronouns_input.Text.Length >= pronouns_char_limit))) // Pronouns Char Limit
		{
			AcceptEvent();
			SoundManager.CreateSound("ui/button_fail");
		}

	}
	private async void CreateResort()
	{
		if (GlobalManager.IsBusy)
		{
			SoundManager.CreateSound("ui/button_fail");
			return;
		}

		if (!SanitizeResortName(out string _resortName))
		{
			SoundManager.CreateSound("ui/button_fail");
			return;
		}

		// Start the game
		GameManager.IsANewSavefile = true;
		SaveSystem.SelectProfile(_resortName);
		NewName = SanitizePlayerName();
		NewPronouns = SanitizePronouns();

		await SaveSystem.CreateProfile();
		SoundManager.CreateSound("ui/lock");
		MainMenu.LoadResort();
	}
	private bool SanitizeResortName(out string _resortName)
	{
		_resortName = remove_invalid_characters().Replace(resort_name_input.Text, string.Empty);

		switch (_resortName)
		{
#if DEBUG
			// Its a secreeeeeet
			case "iblamemar":
				DebugConsole.IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt = true;
				SoundManager.CreateSound("aphid/hurt");
				return false;
#endif
			case "MEDIC!":
			case "medic!":
				SoundManager.CreateSound("misc/medic_prognosis", false).VolumeDb = -20;
				return false;
		}

		// Invalid resort names
		if (string.IsNullOrWhiteSpace(_resortName) || _resortName.EndsWith('.') || !_resortName.IsValidFileName())
		{
			GlobalManager.CREATE_POPUP("warning_invalid_name", popup_anchor);
			return false;
		}

		// Already Exists
		if (DirAccess.DirExistsAbsolute(SaveSystem.CreatePathFromProfile(_resortName)))
		{
			GlobalManager.CREATE_POPUP("warning_already_exists", popup_anchor);
			return false;
		}

		return true;
	}
	private string SanitizePlayerName()
	{
		string _playerName = remove_invalid_characters().Replace(player_name_input.Text, string.Empty);

		if (string.IsNullOrWhiteSpace(_playerName))
			_playerName = StringNames.DefaultPlayerName;

		return _playerName;
	}
	private string[] SanitizePronouns()
	{
		string _pronounInput = remove_invalid_characters().Replace(pronouns_input.Text, string.Empty);
		string[] _pronouns = StringNames.DefaultPlayerPronouns;

		if (!string.IsNullOrWhiteSpace(_pronounInput))
			_pronouns = pronouns_input.Text.Split("/");
		_pronouns[0].Capitalize();

		return _pronouns;
	}
}
