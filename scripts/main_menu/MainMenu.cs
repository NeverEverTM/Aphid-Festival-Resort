using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class MainMenu : Node2D
{
	public static MainMenu Instance { get; private set; }

	[Export] private CanvasLayer canvas;
	[Export] private Label BOOT_LOADING_LABEL;
	[Export] private Camera2D cameraMenu;
	[Export] private RichTextLabel button_wheel, hover_start;
	[Export] private AnimationPlayer sweep_animator, logo_animator, title_animator;
	[Export] private PackedScene aphidPrefab;
	[Export] private Node2D entityRoot;
	[ExportCategory("Menu Panels")]
	[Export] private StartMenu start_panel;
	[Export] private LoadGameMenu loadPanel;
	[Export] private Control new_game_panel, credits_panel, options_panel;
	[ExportCategory("New Game Behaviour")]
	[Export] private BaseButton new_game_button;
	[Export] private TextEdit resort_input_name, player_input_name;

	private static bool HasBeenIntialized;
	private const int resort_charLimit = 40, name_charLimit = 15;
	private const string defaultName = "Mello";

	[ExportCategory("Button Wheel Behaviour")]
	[Export] private AudioStream switchSound;
	[Export] private AudioStream selectSound;
	private bool UsingButtonWheel = false;
	private Action OnMenuSwitch, OnMenuInteract;
	public int MenuWheelIndex { get; set; }

	// Categories for button wheel
	public string currentCategory = newGameCategory;
	private Control currentMenu;
	private const string newGameCategory = "new_game", optionsCategory = "options",
	creditsCategory = "credits", exitCategory = "exit";
	private readonly List<string> MenuCategories = new();
	public readonly Dictionary<string, Action> MenuActions = new();


	public override void _EnterTree()
	{
		Instance = this;
		cameraMenu.Position = GameManager.Utils.GetRandomVector(-300, 300);
		(sweep_animator.GetParent() as Control).Visible = true;
	}
	public async override void _Ready()
	{
		if (!HasBeenIntialized)
			await GameManager.PREPARE_GAME_PROCESS();

		// instance variables
		player_input_name.Text = defaultName;
		currentMenu = start_panel;
		new_game_button.Pressed += CreateResort;

		SetCategory(newGameCategory);
		// create menu button wheel
		CreateMenuAction(newGameCategory, OnNewGameButton);
		loadPanel.AddMenuActions();
		CreateMenuAction(optionsCategory, OnOptionsButton);
		CreateMenuAction(creditsCategory, OnCreditsButton);
		CreateMenuAction(exitCategory, OnExitButton);

		// Start game processes
		GameManager.CleanSaveData();
		if (!HasBeenIntialized)
		{
			GameManager.BOOT_LOADING_LABEL = BOOT_LOADING_LABEL;
			logo_animator.Play("start");
			while (logo_animator.IsPlaying())
			{
				if (Input.IsAnythingPressed())
					logo_animator.Play("RESET");
				await Task.Delay(1);
			}
			await GameManager.INTIALIZE_GAME_PROCESS();
			sweep_animator.Play("slide_up");
		}
		else
		{
			(sweep_animator.GetParent() as Control).Visible = false;
			start_panel.ReadyUp();
		}
		title_animator.Play("slide_down");

		SpawnBunchaOfAphidsForTheFunnies();
		start_panel.SetPanel();
		HasBeenIntialized = true;
	}
	public override void _Input(InputEvent _event)
	{
		if (GameManager.IsBusy)
			return;

		if (UsingButtonWheel)
		{
			if (_event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
			{
				if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
					GoLeftInWheel();
				else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
					GoRightInWheel();
			}
		}

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
				else if (_playerIsFocused)
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
				else if (_playerIsFocused)
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

		if (!start_panel.IsReady && Input.IsActionJustPressed("interact"))
		{
			start_panel.ReadyUp();
			return;
		}

		if (UsingButtonWheel)
			ProcessButtonWheel();
	}
	
	private void ProcessButtonWheel()
	{
		if (Input.IsActionJustPressed("interact"))
		{
			OnMenuInteract();
			SoundManager.CreateSound(selectSound);
			return;
		}

		if (Input.IsActionJustPressed("left"))
			GoLeftInWheel();

		if (Input.IsActionJustPressed("right"))
			GoRightInWheel();
	}
	public void GoLeftInWheel()
	{
		MenuWheelIndex--;
		if (MenuWheelIndex == -1)
			MenuWheelIndex = MenuActions.Count - 1;
		OnMenuSwitch();
		SoundManager.CreateSound(switchSound);

	}
	public void GoRightInWheel()
	{
		MenuWheelIndex++;
		if (MenuWheelIndex == MenuActions.Count)
			MenuWheelIndex = 0;
		OnMenuSwitch();
		SoundManager.CreateSound(switchSound);
	}
	public void SetButtonWheel(Action _interact, Action _switch)
	{
		OnMenuSwitch = _switch;
		OnMenuInteract = _interact;
		button_wheel.Show();
		UsingButtonWheel = true;
	}	
	public void CloseButtonWheel()
	{
		button_wheel.Hide();
		UsingButtonWheel = false;
	}

	private async void CreateResort()
	{
		string _resortName = resort_input_name.Text;

		// Its a secreeeeeet
		if (_resortName == "iblamemar")
		{
			Cheats.IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt = true;
			SoundManager.CreateSound(Player.Audio_Whistle);
			return;
		}

		// Invalid resort names
		if (string.IsNullOrWhiteSpace(_resortName) || !_resortName.IsValidFileName())
		{
			GameManager.CreatePopup("warning_invalid_name", canvas);
			return;
		}

		// Already Exists
		SaveSystem.SetProfile(_resortName);
		if (DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			GameManager.CreatePopup("warning_already_exists", canvas);
			return;
		}

		// Start the game
		new_game_panel.Hide();
		ResortManager.IsNewGame = true;
		Player.NewName = !string.IsNullOrWhiteSpace(player_input_name.Text) ? player_input_name.Text : defaultName;
		await SaveSystem.CreateProfile();
		LoadResort();
	}
	public static void DeleteResort(string _profile)
	{
		SaveSystem.SetProfile(_profile);
		if (!DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			GameManager.CreatePopup("warning_invalid_resort", Instance.canvas);
			return;
		}

		SaveSystem.DeleteProfile(_profile);
		SoundManager.CreateSound(Aphid.Audio_Hurt);
	}
	public static async void LoadResort()
	{
		OptionsManager.Data.LastPlayedResort = SaveSystem.Profile;
		SaveSystem.SaveGlobalData();
		await GameManager.LoadScene(GameManager.SceneName.Resort);
	}

	// MARK: Menu Managment
	public void CreateMenuAction(string _key, Action _action)
	{
		MenuActions.Add(_key, _action);
		MenuCategories.Add(_key);
	}
	public void RemoveMenuAction(string _key)
	{
		MenuCategories.Remove(_key);
		SetCategory(newGameCategory);
		MenuWheelIndex = 0;
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
		SoundManager.CreateSound(selectSound);

		// Start Panel behaviour
		button_wheel.Text = $"[center][tornado radius=2.0 freq=6.0 connected=1]{Tr(currentCategory)}[/tornado][/center]";
		SetButtonWheel(() => MenuActions[currentCategory](), SwitchCategories);
		MenuWheelIndex = lastCategoryIndex;

		start_panel.Show();
		currentMenu = start_panel;
	}

	// MARK:  Category Managment
	private int lastCategoryIndex;
	public void SetCategory(string _text)
	{
		currentCategory = _text;
		button_wheel.Text = $"[center][tornado radius=2.0 freq=6.0 connected=1]{Tr(currentCategory)}[/tornado][/center]";
		MenuWheelIndex = MenuCategories.IndexOf(currentCategory);
	}
	public void SwitchCategories()
	{
		lastCategoryIndex = MenuWheelIndex;
		SetCategory(MenuCategories[MenuWheelIndex]);
	}

	private void OnNewGameButton()
	{
		resort_input_name.Text = "";
		player_input_name.Text = "";
		resort_input_name.GrabFocus();
		SetMenu(new_game_panel);
	}

	private void OnOptionsButton()
	{
		SetMenu(options_panel);
	}
	private void OnCreditsButton() =>
		SetMenu(credits_panel);
	private void OnExitButton() =>
		GetTree().Quit();

	// MARK: Cosmetics
	private bool DirectionForX, DirectionForY;
	private float MaxWanderDistance = 800;
	private void DoBounceAnim()
	{
		if (cameraMenu.Position.X > MaxWanderDistance)
			DirectionForX = true;
		else if (cameraMenu.Position.X < -MaxWanderDistance)
			DirectionForX = false;

		if (cameraMenu.Position.Y > MaxWanderDistance)
			DirectionForY = true;
		else if (cameraMenu.Position.Y < -MaxWanderDistance)
			DirectionForY = false;

		cameraMenu.Position += new Vector2(DirectionForX ? -1 : 1, DirectionForY ? -1 : 1);
	}
	private float[] babyWeight = new float[] { 90, 10 };
	private void SpawnBunchaOfAphidsForTheFunnies()
	{
		for (int i = 0; i < GameManager.RNG.RandiRange(6, 12); i++)
		{
			var _aphid = aphidPrefab.Instantiate() as Aphid;
			_aphid.IS_FAKE = true;
			_aphid.Instance = new();
			_aphid.Instance.Genes.RandomizeColors();
			_aphid.Instance.Status.IsAdult = GameManager.GetRandomByWeight(babyWeight) == 0;
			_aphid.GlobalPosition = GameManager.Utils.GetRandomVector(-300, 300);
			entityRoot.AddChild(_aphid);
			_aphid.skin.SetSkin("idle");
		}
	}
}
