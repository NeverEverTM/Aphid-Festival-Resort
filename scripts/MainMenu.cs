using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using System.Threading.Tasks;

public partial class MainMenu : Node2D
{
	[Export] private Label BOOT_LOADING_LABEL;
	[Export] private Camera2D cameraMenu;
	[Export] private RichTextLabel button_wheel;
	[Export] private AnimationPlayer sweep_animator, logo_animator;
	[Export] private PackedScene aphidPrefab;
	[ExportCategory("Menu Panels")]
	[Export] private Control start_panel;
	[Export] private Control new_game_panel, load_game_panel, credits_panel, options_panel;
	[ExportCategory("New Game Behaviour")]
	[Export] private BaseButton new_game_button;
	[Export] private TextEdit resort_name, player_name;
	[ExportCategory("Load Game Behaviour")]
	[Export] private RichTextLabel savefile_name;

	private static bool HasBeenIntialized;

	// Button Wheel behaviour
	private bool UsingButtonWheel = false;
	private Action Back, Forward;
	private Action Interact;
	private int maxLength, wheePointer;

	// Categories for button wheel
	private const string newGameCategory = "new_game", loadGameCategory = "load_game",optionsCategory = "options", continueCategory = "continue",
	creditsCategory = "credits", exitCategory = "exit";
	private string category = newGameCategory;
	private readonly List<string> MenuCategories = new();

	// Menu stuff
	private Control currentMenu;
	private readonly Dictionary<string, Action> MenuActions = new();

    public async override void _Ready()
    {
		sweep_animator.Play("loading");
		currentMenu = start_panel;
		new_game_button.Pressed += CreateResort;
		SetCategory(newGameCategory);

		CreateMenuAction(newGameCategory, OnNewGameButton);
		if (DirAccess.GetDirectoriesAt(SaveSystem.ProfilesDirectory).Length > 2)
		{
			CreateMenuAction(loadGameCategory, OnLoadGameButton);

			if (FileAccess.FileExists(SaveSystem.GlobalDataPath))
			{
				CreateMenuAction(continueCategory, OnContinueButton);
				SetCategory(continueCategory);
				wheePointer = MenuActions.Count - 1;
			}
		}
		CreateMenuAction(optionsCategory, OnOptionsButton);
		CreateMenuAction(creditsCategory, OnCreditsButton);
		CreateMenuAction(exitCategory, OnExitButton);

		SpawnBunchaOfAphidsForTheFunnies();

		if (!HasBeenIntialized)
		{
			GameManager.BOOT_LOADING_LABEL = BOOT_LOADING_LABEL;
			await GameManager.INTIALIZE_GAME_PROCESS();
			logo_animator.Play("slide_down");
			sweep_animator.Play("slide_up");
		}
		else
		{
			logo_animator.Play("slide_down");
			sweep_animator.Play("RESET");
			while (GameManager.IsBusy)
				await Task.Delay(1);
		}
		SetButtonWheel(MenuActions.Count, () => MenuActions[category](), SwitchCategories);
		HasBeenIntialized = true;
    }
    public override void _Input(InputEvent _event)
    {
		bool _resortIsFocused = resort_name.HasFocus(), _playerIsFocused = player_name.HasFocus();
		if (_resortIsFocused == _playerIsFocused) // If either are on focus, proceed
			return;

		if (_event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			if (_input.KeyLabel == Key.Enter)
			{
				GetViewport().SetInputAsHandled();
				return;
			}

			// allow character deletion and moving through the text
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			if ((_resortIsFocused && resort_name.Text.Length >= 40) ||
			(_playerIsFocused && player_name.Text.Length >= 30))
				GetViewport().SetInputAsHandled();
		}
    }
    public override void _Process(double delta)
    {
		DoBounceAnim();

		if (GameManager.IsBusy)
			return;

		// Input
		if (!start_panel.Visible && Input.IsActionJustPressed("cancel"))
			SetMenu();

		if (load_game_panel.Visible && Input.IsActionJustPressed("open_inventory"))
		{
			
		}

		if (Input.IsActionJustPressed("escape"))
			OnExitButton();

		if (UsingButtonWheel)
			ProcessButtonWheel();
    }
	
	private void ProcessButtonWheel()
	{
        if(Input.IsActionJustPressed("interact"))
			Interact();

		if(Input.IsActionJustPressed("left"))
		{
			wheePointer--;
			if (wheePointer == -1)
				wheePointer = maxLength - 1;
			Back();
		}

		if(Input.IsActionJustPressed("right"))
		{
			wheePointer++;
			if (wheePointer == maxLength)
				wheePointer = 0;
			Forward();
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
		string _resort = resort_name.Text;
		if (!string.IsNullOrWhiteSpace(player_name.Text))
			Player.Data.Name = player_name.Text;
		else
			Player.Data.Name = "Matty";

		if (string.IsNullOrWhiteSpace(_resort) || _resort.Length > 20 || _resort.Equals("default") || _resort.Equals("[backup]"))
		{
			GD.Print("Not a valid resort name. Resort names can only be 20 characters long.");
			return;
		}
		SaveSystem.SetProfile(_resort);
		if (DirAccess.DirExistsAbsolute(SaveSystem.CurrentProfilePath))
		{
			GD.Print("This resort already exists! Move it away from the profiles folder or delete it.");
			return;
		}

		new_game_panel.Hide();
		ResortManager.IsNewGame = true;

		await SaveSystem.CreateProfile();
		LoadResort();
	}
	private static void LoadResort()
	{
		SaveSystem.SaveGlobalData();
		_ = GameManager.LoadScene(GameManager.ResortScenePath);
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
		SetButtonWheel(MenuActions.Count, () => MenuActions[category](), SwitchCategories);
		wheePointer = lastCategoryIndex;

		start_panel.Show();
		currentMenu = start_panel;
	}

	// Aesthetic Menu Functions
	private bool DirectionForX, DirectionForY;
	private void DoBounceAnim()
	{
		// Bounce on the X axis
		if (cameraMenu.Position.X > 500)
			DirectionForX = true;
		else if (cameraMenu.Position.X < -500)
			DirectionForX = false;
		if (!DirectionForX)
			cameraMenu.Position = cameraMenu.GlobalPosition + Vector2.Right;
		else
			cameraMenu.Position = cameraMenu.GlobalPosition + Vector2.Left;

		// Bounce o the Y axis
		if (cameraMenu.Position.Y > 500)
			DirectionForY = true;
		else if (cameraMenu.Position.Y < -500)
			DirectionForY = false;

		if (!DirectionForY)
			cameraMenu.Position = cameraMenu.GlobalPosition + Vector2.Down;
		else
			cameraMenu.Position = cameraMenu.GlobalPosition + Vector2.Up;
	}
	private void SpawnBunchaOfAphidsForTheFunnies()
	{
		RandomNumberGenerator _rng = new();
		for (int i = 0; i < 12; i++)
		{
			var _aphid = aphidPrefab.Instantiate() as Aphid;
			_aphid.Instance = new();
			_aphid.Instance.Genes.RandomizeColors();
			_aphid.GlobalPosition = new Vector2(_rng.RandiRange(-300, 300), _rng.RandiRange(-300, 300));
			AddChild(_aphid);
		}
	}

	// =======| Categories Managment |========
	private int lastCategoryIndex;
	private void SetCategory(string _text)
	{
		category = _text;
		button_wheel.Text = $"[center]< {Tr(category)} >[/center]";
	}
	private void SwitchCategories()
	{
		lastCategoryIndex = wheePointer;
		SetCategory(MenuCategories[wheePointer]);
	}

	private void OnNewGameButton() =>
		SetMenu(new_game_panel);
	private void OnLoadGameButton()
	{
		// Get the files and exclude default and [backup] from being showed
		var _files = DirAccess.Open(SaveSystem.ProfilesDirectory).GetDirectories().ToList();
		
		if (!(_files.Count > 2))
		{
			RemoveMenuAction(loadGameCategory);
			return;
		}

		_files.Remove("default");
		_files.Remove("[backup]");
		fileNames = _files.ToArray();

		// Set the presentation
		savefile_name.Text = fileNames[0];
		wheePointer = lastFileNameIndex;
		SetMenu(load_game_panel);
		SetButtonWheel(fileNames.Length, SetFileName, SwitchFileName);
	}
	private void OnContinueButton()
	{
		var _profile = FileAccess.Open(SaveSystem.GlobalDataPath, FileAccess.ModeFlags.Read).GetPascalString();
		SaveSystem.SetProfile(_profile);
		
		if (string.IsNullOrWhiteSpace(_profile) || !DirAccess.DirExistsAbsolute(SaveSystem.CurrentProfilePath))
		{
			RemoveMenuAction(continueCategory);
			GD.Print("MenuManager: Could not find user in profiles folder.");
			return;
		}

		LoadResort();
	}
	private void OnOptionsButton() =>
		SetMenu(options_panel);
	private void OnCreditsButton() =>
		SetMenu(credits_panel);
	private void OnExitButton() =>
		GetTree().Quit();

	// Load Game behaviour
	private string[] fileNames;
	private int lastFileNameIndex;
	private void SwitchFileName()
	{
		lastFileNameIndex = wheePointer;
		savefile_name.Text = fileNames[wheePointer];
	}
	private void SetFileName()
	{
		SaveSystem.SetProfile(fileNames[wheePointer]);
		LoadResort();
	}
}
