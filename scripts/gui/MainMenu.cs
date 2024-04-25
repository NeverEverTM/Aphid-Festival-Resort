using System;
using System.Collections.Generic;
using Godot;

public partial class MainMenu : Node2D
{
	[Export] private Label BOOT_LOADING_LABEL;
	[Export] private Camera2D cameraMenu;
	[Export] private RichTextLabel button_wheel, hover_start;
	[Export] private AnimationPlayer sweep_animator, logo_animator;
	[Export] private PackedScene aphidPrefab;
	[Export] private TextureButton githubButton, itchioButton;
	[ExportCategory("Menu Panels")]
	[Export] private Control start_panel;
	[Export] private Control new_game_panel, load_game_panel, credits_panel, options_panel;
	[ExportCategory("New Game Behaviour")]
	[Export] private BaseButton new_game_button;
	[Export] private TextEdit resort_input_name, player_input_name;
	[ExportCategory("Load Game Behaviour")]
	[Export] private RichTextLabel savefile_name;

	private static bool HasBeenIntialized;
	private const int resort_charLimit = 40, name_charLimit = 15;
	private const string defaultName = "Mello";

	[ExportCategory("Button Wheel Behaviour")]
	[Export] private AudioStream switchSound;
	[Export] private AudioStream selectSound;
	private bool UsingButtonWheel = false;
	private Action Back, Forward;
	private Action Interact;
	private int maxLength, wheePointer;

	// Categories for button wheel
	private const string newGameCategory = "new_game", loadGameCategory = "load_game", optionsCategory = "options", continueCategory = "continue",
	creditsCategory = "credits", exitCategory = "exit";
	private string category = newGameCategory;
	private readonly List<string> MenuCategories = new();

	// Menu stuff
	private Control currentMenu;
	private readonly Dictionary<string, Action> MenuActions = new();

	public async override void _Ready()
	{
		if (!HasBeenIntialized)
			await GameManager.PREPARE_GAME_PROCESS();
			
		// instance variables
		sweep_animator.Play("loading");
		player_input_name.Text = defaultName;
		currentMenu = start_panel;
		new_game_button.Pressed += CreateResort;
		githubButton.Pressed += () =>
			OS.ShellOpen("https://github.com/NeverEverTM/Aphid-Festival");
		itchioButton.Pressed += () =>
			OS.ShellOpen("https://neverevertm.itch.io/aphid-festival-resort");

		SetCategory(newGameCategory);
		// create menu button wheel
		CreateMenuAction(newGameCategory, OnNewGameButton);
		if (DirAccess.GetDirectoriesAt(SaveSystem.ProfilesDirectory).Length > 0)
		{
			// create load game button
			CreateMenuAction(loadGameCategory, OnLoadGameButton);
			// create continue button
			if (!string.IsNullOrEmpty(OptionsManager.Data.LastPlayedResort))
			{
				CreateMenuAction(continueCategory, OnContinueButton);
				SetCategory(continueCategory);
				wheePointer = MenuActions.Count - 1;
			}
		}
		CreateMenuAction(optionsCategory, OnOptionsButton);
		CreateMenuAction(creditsCategory, OnCreditsButton);
		CreateMenuAction(exitCategory, OnExitButton);

		// Start game processes
		GameManager.CleanSaveData();
		if (!HasBeenIntialized)
		{
			GameManager.BOOT_LOADING_LABEL = BOOT_LOADING_LABEL;
			await GameManager.INTIALIZE_GAME_PROCESS();
			SpawnBunchaOfAphidsForTheFunnies();
			logo_animator.Play("slide_down");
			sweep_animator.Play("slide_up");
		}
		else
		{
			SpawnBunchaOfAphidsForTheFunnies();
			logo_animator.Play("slide_down");
			sweep_animator.Play("RESET");
		}

		hover_start.Text = $"[wave amp=50.0 freq=5.0 connected=1][center]{Tr("press_start")}[/center][/wave]";
		HasBeenIntialized = true;
	}
	public override void _Input(InputEvent _event)
	{
		if (GameManager.IsBusy)
			return;

		bool _resortIsFocused = resort_input_name.HasFocus(), _playerIsFocused = player_input_name.HasFocus();
		if (!start_panel.Visible && ((Input.IsActionJustPressed("cancel") && !_resortIsFocused && !_playerIsFocused) 
		|| Input.IsActionJustPressed("escape")))
		{
			SetMenu();
			return;
		}

		if ((_resortIsFocused || _playerIsFocused) && _event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			// move through inputs
			if (_input.KeyLabel == Key.Up)
			{
				if (_resortIsFocused)
					new_game_button.GrabFocus();
				else  if (_playerIsFocused)
					resort_input_name.GrabFocus();
				else
					player_input_name.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}
			if (_input.KeyLabel == Key.Down)
			{
				if (_resortIsFocused)
					player_input_name.GrabFocus();
				else  if (_playerIsFocused)
					new_game_button.GrabFocus();
				else
					resort_input_name.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}

			// confirm input
			if (_input.KeyLabel == Key.Enter)
			{
				if (_resortIsFocused)
					player_input_name.GrabFocus();
				else
					new_game_button.GrabFocus();
				GetViewport().SetInputAsHandled();
				return;
			}

			// allow character deletion and moving through the text
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			if ((_resortIsFocused && resort_input_name.Text.Length >= resort_charLimit) ||
			(_playerIsFocused && player_input_name.Text.Length >= name_charLimit))
				GetViewport().SetInputAsHandled();
			return;
		}
	}
	public override void _Process(double delta)
	{
		DoBounceAnim();

		if (GameManager.IsBusy)
			return;

		if (UsingButtonWheel)
			ProcessButtonWheel();

		// Delete command
		if (load_game_panel.Visible && Input.IsActionJustPressed("open_inventory"))
			ConfirmationPopup.CreateConfirmation(DeleteResort);

		// "Press to Start" prompt
		if (hover_start.Visible && Input.IsActionJustPressed("interact"))
		{
			hover_start.Hide();
			SetButtonWheel(MenuActions.Count, () => MenuActions[category](), SwitchCategories);
			SoundManager.CreateSound(selectSound);
		}
	}

	private void ProcessButtonWheel()
	{
		if (Input.IsActionJustPressed("interact"))
		{
			Interact();
			SoundManager.CreateSound(selectSound);
			return;
		}

		if (Input.IsActionJustPressed("left"))
		{
			wheePointer--;
			if (wheePointer == -1)
				wheePointer = maxLength - 1;
			Back();
			SoundManager.CreateSound(switchSound);
		}

		if (Input.IsActionJustPressed("right"))
		{
			wheePointer++;
			if (wheePointer == maxLength)
				wheePointer = 0;
			Forward();
			SoundManager.CreateSound(switchSound);
		}
	}
	public void SetButtonWheel(int _max_length, Action _interact, Action _back, Action _forward)
	{
		Back = _back;
		Forward = _forward;
		Interact = _interact;
		maxLength = _max_length;
		button_wheel.Show();
		UsingButtonWheel = true;
	}
	public void SetButtonWheel(int _max_length, Action _interact, Action _switch) =>
		SetButtonWheel(_max_length, _interact, _switch, _switch);
	public void CloseButtonWheel()
	{
		button_wheel.Hide();
		UsingButtonWheel = false;
	}

	private async void CreateResort()
	{
		string _resort = resort_input_name.Text;

		// Invalid resort names
		if (string.IsNullOrWhiteSpace(_resort) || !_resort.IsValidFileName() || _resort.Equals("default"))
		{
			GD.Print("Not a valid resort name.");
			return;
		}

		// Already Exists
		SaveSystem.SetProfile(_resort);
		if (DirAccess.DirExistsAbsolute(SaveSystem.CurrentProfilePath))
		{
			GD.Print("This resort already exists! Move it away from the profiles folder or delete it.");
			return;
		}

		// Start the game
		new_game_panel.Hide();
		ResortManager.IsNewGame = true;
		Player.Data.Name = !string.IsNullOrWhiteSpace(player_input_name.Text) ? player_input_name.Text : defaultName;
		await SaveSystem.CreateProfile();
		LoadResort();
	}
	private void DeleteResort()
	{
		string _profile = savefile_name.Text;
		if (string.IsNullOrWhiteSpace(_profile))
		{
			GD.Print("Not a valid resort");
			return;
		}

		SaveSystem.SetProfile(_profile);
		if (!DirAccess.DirExistsAbsolute(SaveSystem.CurrentProfilePath))
		{
			GD.Print("This resort doesnt exist!");
			return;
		}

		SaveSystem.DeleteProfile(_profile);
		if (OptionsManager.Data.LastPlayedResort == SaveSystem.Profile)
			OptionsManager.Data.LastPlayedResort = string.Empty;
		SaveSystem.SetProfile(SaveSystem.defaultProfile);

		// Set the current wheel of filenames
		fileNames = DirAccess.Open(SaveSystem.ProfilesDirectory).GetDirectories();
		if (fileNames.Length == 0) // if no more savefiles, quit and delete access buttons
		{
			RemoveMenuAction(continueCategory);
			RemoveMenuAction(loadGameCategory);
			SetMenu();
		}
		else
		{
			if (wheePointer >= fileNames.Length)
				wheePointer = fileNames.Length - 1;
			SetFile();
			SetButtonWheel(fileNames.Length, PlayFile, SetFile);
		}
	}
	private static async void LoadResort()
	{
		OptionsManager.Data.LastPlayedResort = SaveSystem.Profile;
		SaveSystem.SaveGlobalData();
		await GameManager.LoadScene(GameManager.ResortScenePath);
	}

	// =======| Menu Managment |=========
	public void CreateMenuAction(string _key, Action _action)
	{
		MenuActions.Add(_key, _action);
		MenuCategories.Add(_key);
	}
	public void RemoveMenuAction(string _key)
	{
		MenuCategories.Remove(_key);
		SetCategory(newGameCategory);
		wheePointer = 0;
		maxLength--;
	}
	public void SetMenu(Control _menu)
	{
		currentMenu.Hide();
		_menu.Show();

		if (UsingButtonWheel)
			CloseButtonWheel();

		currentMenu = _menu;
	}
	public void SetMenu()
	{
		currentMenu.Hide();

		// Start Panel behaviour
		button_wheel.Text = $"[center][tornado radius=2.0 freq=6.0 connected=1]{Tr(category)}[/tornado][/center]";
		SetButtonWheel(MenuActions.Count, () => MenuActions[category](), SwitchCategories);
		wheePointer = lastCategoryIndex;

		start_panel.Show();
		currentMenu = start_panel;
	}

	// Aesthetic Menu Functions
	private bool DirectionForX, DirectionForY;
	private void DoBounceAnim()
	{
		Vector2 _newDirectionVector = new();

		if (cameraMenu.Position.X > 500)
			DirectionForX = true;
		else if (cameraMenu.Position.X < -500)
			DirectionForX = false;
		if (cameraMenu.Position.Y > 500)
			DirectionForY = true;
		else if (cameraMenu.Position.Y < -500)
			DirectionForY = false;

		if (!DirectionForX) // Going Right
			_newDirectionVector.X = 1;
		else // Going Left
			_newDirectionVector.X = -1;

		if (!DirectionForY) // Going Down
			_newDirectionVector.Y = 1;
		else // Going Up
			_newDirectionVector.Y = -1;

		cameraMenu.Position += _newDirectionVector;
	}
	private float[] babyWeight = new float[] { 90, 10 };
	private void SpawnBunchaOfAphidsForTheFunnies()
	{
		for (int i = 0; i < GameManager.RNG.RandiRange(12, 24); i++)
		{
			var _aphid = aphidPrefab.Instantiate() as FakeAphid;
			_aphid.Instance = new();
			_aphid.Instance.Genes.RandomizeColors();
			_aphid.Instance.Status.IsAdult = GameManager.GetRandomByWeight(babyWeight) == 0;
			_aphid.GlobalPosition = GameManager.Utils.GetRandomVector(-300, 300);
			AddChild(_aphid);
		}
	}

	// =======| Categories Managment |========
	private int lastCategoryIndex;
	private void SetCategory(string _text)
	{
		category = _text;
		button_wheel.Text = $"[center][tornado radius=2.0 freq=6.0 connected=1]{Tr(category)}[/tornado][/center]";
	}
	private void SwitchCategories()
	{
		lastCategoryIndex = wheePointer;
		SetCategory(MenuCategories[wheePointer]);
	}

	private void OnNewGameButton()
	{
		resort_input_name.Text = "";
		player_input_name.Text = "";
		resort_input_name.GrabFocus();
		SetMenu(new_game_panel);
	}
	private void OnLoadGameButton()
	{
		fileNames = DirAccess.Open(SaveSystem.ProfilesDirectory).GetDirectories();

		// Set the presentation
		savefile_name.Text = fileNames[0];

		if (lastFileNameIndex < fileNames.Length)
			wheePointer = lastFileNameIndex;
		SetMenu(load_game_panel);
		SetButtonWheel(fileNames.Length, PlayFile, SetFile);
	}
	private void OnContinueButton()
	{
		if (string.IsNullOrWhiteSpace(OptionsManager.Data.LastPlayedResort) || !DirAccess.DirExistsAbsolute(SaveSystem.CurrentProfilePath))
		{
			RemoveMenuAction(continueCategory);
			GD.Print("MenuManager: Could not find valid profile to continue.");
			return;
		}
		SaveSystem.SetProfile(OptionsManager.Data.LastPlayedResort);

		LoadResort();
	}
	private void OnOptionsButton()
	{
		SetMenu(options_panel);
	}
	private void OnCreditsButton() =>
		SetMenu(credits_panel);
	private void OnExitButton() =>
		GetTree().Quit();

	// Load Game behaviour
	private string[] fileNames;
	private int lastFileNameIndex;
	private void SetFile()
	{
		lastFileNameIndex = wheePointer;
		savefile_name.Text = fileNames[wheePointer];
	}
	private void PlayFile()
	{
		SaveSystem.SetProfile(fileNames[wheePointer]);
		LoadResort();
	}
}
