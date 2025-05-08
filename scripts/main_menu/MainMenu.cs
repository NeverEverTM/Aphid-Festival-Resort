using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Central processing script for main menu operations, including boot up and user interface.
/// </summary>
public partial class MainMenu : Node2D
{
	public static MainMenu Instance { get; private set; }

	[Export] private CanvasLayer canvas;
	[Export] private Label BOOT_LOADING_LABEL;
	[Export] private RichTextLabel button_wheel, hover_start;
	[Export] private AnimationPlayer sweep_animator, logo_animator, title_animator;
	[Export] private PackedScene aphidPrefab;
	[Export] private Node2D entityRoot;
	[ExportCategory("Menu Panels")]
	[Export] private StartMenu start_panel;
	[Export] private NewGameMenu new_game_panel;
	[Export] private LoadGameMenu load_game_panel;
	[Export] private Control credits_panel, options_panel, controls_panel;

	private static bool HasBeenIntialized;

	[ExportCategory("Button Wheel Behaviour")]
	[Export] private AudioStream switchSound;
	[Export] private AudioStream selectSound;
	private bool UsingButtonWheel = false;
	private Action OnMenuSwitch, OnMenuInteract;
	public int MenuWheelIndex { get; set; }

	// Categories for button wheel
	public string currentCategory = NewGameMenu.newGameCategory;
	private Control currentMenu;

	private readonly List<string> MenuCategories = new();
	public readonly Dictionary<string, Action> MenuActions = new();

	public override void _EnterTree()
	{
		Instance = this;
		(sweep_animator.GetParent() as Control).Visible = true;
	}
	public async override void _Ready()
	{
		currentMenu = start_panel;
		CameraManager.Instance.Position = GlobalManager.Utils.GetRandomVector(-300, 300);

		// create menu button wheel
		new_game_panel.AddMenuAction();
		load_game_panel.AddMenuAction();
		CreateMenuAction("controls", () => SetMenu(controls_panel));
		CreateMenuAction("options", () => SetMenu(options_panel));
		CreateMenuAction("credits", () => SetMenu(credits_panel));
		CreateMenuAction("exit", ExitGame);

		if (!HasBeenIntialized)
		{
			GlobalManager.BOOT_LOADING_LABEL = BOOT_LOADING_LABEL;
			logo_animator.Play("start");
			while (logo_animator.IsPlaying())
			{
				if (Input.IsAnythingPressed())
					logo_animator.Play("RESET");
				await Task.Delay(1);
			}
			await GlobalManager.INTIALIZE_GAME_PROCESS();
			sweep_animator.Play("slide_up");
		}
		else
		{
			(sweep_animator.GetParent() as Control).Visible = false;
			start_panel.ReadyUp();
		}
		SoundManager.PlaySong("misc/title.wav");
		title_animator.Play("slide_down");
		SpawnBunchaOfAphidsForTheFunnies();
		start_panel.SetPanel();
		HasBeenIntialized = true;
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (GlobalManager.IsBusy)
			return;

		// Press To Start - Pressed
		if (!start_panel.IsReady)
		{
			if (Input.IsActionJustPressed(InputNames.Interact))
				start_panel.ReadyUp();
			return;
		}

		// Exit current menu
		if (!start_panel.Visible)
		{
			if (@event.IsActionPressed(InputNames.Escape) || @event.IsActionPressed(InputNames.Cancel))
				CloseMenu();
			return;
		}

		// wheel interactions
		if (@event is InputEventMouseButton && (@event as InputEventMouseButton).Pressed)
		{
			InputEventMouseButton _mouse = @event as InputEventMouseButton;
			if (_mouse.ButtonIndex == MouseButton.WheelUp)
				GoLeftInWheel();
			else if (_mouse.ButtonIndex == MouseButton.WheelDown)
				GoRightInWheel();
		}
		else
		{
			if (@event.IsActionPressed(InputNames.Left) || @event.IsActionPressed("ui_left"))
				GoLeftInWheel();
			else if (@event.IsActionPressed(InputNames.Right) || @event.IsActionPressed("ui_right"))
				GoRightInWheel();
		}

		if (@event.IsActionPressed(InputNames.Interact))
		{
			OnMenuInteract?.Invoke();
			SoundManager.CreateSound(selectSound);
		}
	}
	public override void _Process(double delta)
	{
		DoBounceAnim();
	}

	public void GoLeftInWheel()
	{
		MenuWheelIndex--;
		if (MenuWheelIndex < 0)
			MenuWheelIndex = MenuActions.Count - 1;
		OnMenuSwitch();
		SoundManager.CreateSound(switchSound);
	}
	public void GoRightInWheel()
	{
		MenuWheelIndex++;
		if (MenuWheelIndex >= MenuActions.Count)
			MenuWheelIndex = 0;
		OnMenuSwitch?.Invoke();
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

	public static void DeleteResort(string _profile)
	{
		SaveSystem.SelectProfile(_profile);
		if (!DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			GlobalManager.CreatePopup("warning_invalid_resort", Instance.canvas);
			return;
		}

		SaveSystem.DeleteProfile(_profile);
		SoundManager.CreateSound(Aphid.Audio_Hurt);
	}
	public static async void LoadResort()
	{
		OptionsManager.Settings.LastPlayedResort = SaveSystem.Profile;
		Logger.Print(Logger.LogPriority.Info, $"MainMenu: Loading the profile <{SaveSystem.Profile}>.");
		await OptionsManager.Module.Save();
		await GlobalManager.LoadScene(GlobalManager.SceneName.Resort);
	}
	public void ExitGame()
	{
		ConfirmationPopup.Create(() => GetTree().Quit(), null,
			ConfirmationPopup.ConfirmationEnum.Fast, "confirmation_exit");
	}

	// MARK: Menu Managment
	public void CreateMenuAction(string _key, Action _action)
	{
		MenuActions.Add(_key, _action);
		MenuCategories.Add(_key);
	}
	public void RemoveMenuAction(string _key)
	{
		if (_key == NewGameMenu.newGameCategory)
		{
			Logger.Print(Logger.LogPriority.Warning, "Main Menu: Cannot remove default category");
			return;
		}
		MenuCategories.Remove(_key);
		SetCategory(NewGameMenu.newGameCategory);
	}
	public void SetMenu(Control _menu)
	{
		currentMenu.Hide();
		_menu.Show();

		if (UsingButtonWheel)
			CloseButtonWheel();

		currentMenu = _menu;
	}
	public void CloseMenu()
	{
		currentMenu.Hide();
		SoundManager.CreateSound(selectSound);

		// Start Panel behaviour
		button_wheel.Text = $"[center]<| [tornado radius=2.0 freq=6.0 connected=1]{Tr(currentCategory)}[/tornado] |>[/center]";
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
		button_wheel.Text = $"[center]<| [tornado radius=2.0 freq=6.0 connected=1]{Tr(currentCategory)}[/tornado] |>[/center]";
		MenuWheelIndex = MenuCategories.IndexOf(currentCategory);
	}
	public void SwitchCategories()
	{
		lastCategoryIndex = MenuWheelIndex;
		SetCategory(MenuCategories[MenuWheelIndex]);
	}

	// MARK: Cosmetics
	private bool DirectionForX, DirectionForY;
	private float MaxWanderDistanceX = 800, MaxWanderDistanceY = 300;
	private void DoBounceAnim()
	{
		if (CameraManager.Instance.Position.X > MaxWanderDistanceX)
			DirectionForX = true;
		else if (CameraManager.Instance.Position.X < -MaxWanderDistanceX)
			DirectionForX = false;

		if (CameraManager.Instance.Position.Y > MaxWanderDistanceY)
			DirectionForY = true;
		else if (CameraManager.Instance.Position.Y < -MaxWanderDistanceY)
			DirectionForY = false;

		CameraManager.Instance.Position += new Vector2(DirectionForX ? -1 : 1, DirectionForY ? -1 : 1);
	}
	private float[] babyWeight = [70, 30];
	private void SpawnBunchaOfAphidsForTheFunnies()
	{
		for (int i = 0; i < 15; i++)
		{
			var _aphid = aphidPrefab.Instantiate() as Aphid;
			_aphid.IS_FAKE = true;
			_aphid.Instance = new(Guid.Empty);
			_aphid.Instance.Genes.DEBUG_Randomize(false);
			_aphid.Instance.Status.IsAdult = GlobalManager.Utils.GetRandomByWeight(babyWeight) == 0;
			_aphid.GlobalPosition = GlobalManager.Utils.GetRandomVector(-300, 300);
			entityRoot.AddChild(_aphid);
			_aphid.SetReady();
			_aphid.State.Enter(_aphid, new AphidActions.IdleState.IdleArgs(
				GlobalManager.Utils.GetRandomVector(-300, 300),
				GlobalManager.RNG.RandiRange(0, 3)
			), Aphid.StateEnum.Idle);
		}
	}
}