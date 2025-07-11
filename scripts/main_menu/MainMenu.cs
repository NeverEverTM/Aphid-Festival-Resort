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
	[Export] private RichTextLabel button_wheel, hover_start;
	[Export] private AnimatedSprite2D load_sprite;
	[Export] private AnimationPlayer intro_animator, title_animator;
	[Export] private PackedScene aphidPrefab;
	[Export] private Node2D entity_root;
	[ExportCategory("Menu Panels")]
	[Export] private StartMenu start_panel;
	[Export] private NewGameMenu new_game_panel;
	[Export] private LoadGameMenu load_game_panel;
	[Export] private Control credits_panel, options_panel, controls_panel;

	private static bool hasBeenInitialized;
	private bool usingButtonWheel = false;
	private Action onMenuSwitch, onMenuInteract;

	// Categories for button wheel
	public int menuWheelIndex;
	public string currentCategory = NewGameMenu.newGameCategory;
	private Control currentMenu;

	private readonly List<string> menuCategories = [];
	public readonly Dictionary<string, Action> menuActions = [];

	public override void _EnterTree()
	{
		Instance = this;
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

		if (!hasBeenInitialized)
		{
			intro_animator.Play("start");
			while (intro_animator.IsPlaying())
			{
				if (Input.IsAnythingPressed())
				{
					intro_animator.Play("sweep");
					await Task.Delay(1);
					intro_animator.Pause();
					break;
				}
				await Task.Delay(1);
			}
			load_sprite.Visible = true;
			await GlobalManager.INTIALIZE_GAME_PROCESS();
			load_sprite.Visible = false;
			intro_animator.Play("sweep");
		}
		else
			start_panel.ReadyUp();
		
		CameraManager.ForceCameraPosition(new());
		SpawnBunchaOfAphidsForTheFunnies();
		start_panel.SetPanel();

		SoundManager.PlaySong("misc/title");
		title_animator.Play("slide_down");

		while (title_animator.IsPlaying())
			await Task.Delay(1);
		hasBeenInitialized = true;
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (GlobalManager.IsBusy || !hasBeenInitialized)
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
			onMenuInteract?.Invoke();
			SoundManager.CreateSound("ui/button_select");
		}
	}
	public override void _Process(double delta)
	{
		DoBounceAnim();
	}

	public void GoLeftInWheel()
	{
		menuWheelIndex--;
		if (menuWheelIndex < 0)
			menuWheelIndex = menuActions.Count - 1;
		onMenuSwitch();
		SoundManager.CreateSound("ui/button_switch");
	}
	public void GoRightInWheel()
	{
		menuWheelIndex++;
		if (menuWheelIndex >= menuActions.Count)
			menuWheelIndex = 0;
		onMenuSwitch?.Invoke();
		SoundManager.CreateSound("ui/button_switch");
	}
	public void SetButtonWheel(Action _interact, Action _switch)
	{
		onMenuSwitch = _switch;
		onMenuInteract = _interact;
		button_wheel.Show();
		usingButtonWheel = true;
	}
	public void CloseButtonWheel()
	{
		button_wheel.Hide();
		usingButtonWheel = false;
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
	public static async void LoadResort(string _room = "")
	{
		if (string.IsNullOrEmpty(_room))
			_room = "golden_resort";

		OptionsManager.Settings.LastPlayedResort = SaveSystem.Profile;
		Logger.Print(Logger.LogPriority.Info, $"MainMenu: Loading the profile <{SaveSystem.Profile}>.");
		await OptionsManager.Module.Save();
		await SceneManager.Switch(_room, true, true);
	}
	public void ExitGame()
	{
		ConfirmationPopup.Create(() => GetTree().Quit(), null,
			ConfirmationPopup.ConfirmationEnum.Fast, "confirmation_exit");
	}

	// MARK: Menu Managment
	public void CreateMenuAction(string _key, Action _action)
	{
		menuActions.Add(_key, _action);
		menuCategories.Add(_key);
	}
	public void RemoveMenuAction(string _key)
	{
		if (_key == NewGameMenu.newGameCategory)
		{
			Logger.Print(Logger.LogPriority.Warning, "Main Menu: Cannot remove default category");
			return;
		}
		menuCategories.Remove(_key);
		SetCategory(NewGameMenu.newGameCategory);
	}
	public void SetMenu(Control _menu)
	{
		currentMenu.Hide();
		_menu.Show();

		if (usingButtonWheel)
			CloseButtonWheel();

		currentMenu = _menu;
	}
	public void CloseMenu()
	{
		currentMenu.Hide();
		SoundManager.CreateSound("ui/button_select");

		// Start Panel behaviour
		button_wheel.Text = $"[center]<| [tornado radius=2.0 freq=6.0 connected=1]{Tr(currentCategory)}[/tornado] |>[/center]";
		SetButtonWheel(() => menuActions[currentCategory](), SwitchCategories);
		menuWheelIndex = lastCategoryIndex;

		start_panel.Show();
		currentMenu = start_panel;
	}

	// MARK:  Category Managment
	private int lastCategoryIndex;
	public void SetCategory(string _text)
	{
		currentCategory = _text;
		button_wheel.Text = $"[center]<| [tornado radius=2.0 freq=6.0 connected=1]{Tr(currentCategory)}[/tornado] |>[/center]";
		menuWheelIndex = menuCategories.IndexOf(currentCategory);
	}
	public void SwitchCategories()
	{
		lastCategoryIndex = menuWheelIndex;
		SetCategory(menuCategories[menuWheelIndex]);
	}

	// MARK: Cosmetics
	private bool DirectionForX, DirectionForY;
	private float MaxWanderDistanceX = 800, MaxWanderDistanceY = 400;
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
		for (int i = 0; i < 8; i++)
		{
			var _aphid = aphidPrefab.Instantiate() as Aphid;
			_aphid.IS_FAKE = true;
			_aphid.Instance = new(Guid.Empty);
			_aphid.Instance.Genes.DEBUG_Randomize(false);
			_aphid.Instance.Status.IsAdult = GlobalManager.Utils.GetRandomByWeight(babyWeight) == 0;
			_aphid.GlobalPosition = GlobalManager.Utils.GetRandomVector(-300, 300);
			entity_root.AddChild(_aphid);
			_aphid.SetReady();
			_aphid.State.Enter(_aphid, new AphidActions.IdleState.IdleArgs(
				GlobalManager.Utils.GetRandomVector(-300, 300),
				GlobalManager.RNG.RandiRange(0, 3)
			), Aphid.StateEnum.Idle);
		}
	}
}