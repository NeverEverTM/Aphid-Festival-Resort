using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class StartMenu : Control
{
	public static StartMenu Instance { get; private set; }

	[Export] private AnimationPlayer anim_player;
	[Export] private RichTextLabel wheel_label, start_label;
	[Export] private Control options_panel, controls_panel;
	[Export] private AnimationPlayer credits_player;
	[ExportGroup("Cosmetics")]
	[Export] private TextureRect titleAphid, titleFestival;
	[Export] private TextureButton aphidButton, secretButton, githubButton, itchioButton;
	[Export] private Material trans_rights, aro_flag; // the woke left? without saying goodbye?

	public bool IsReady;
	private bool is_busy;

	private int isASecreeeeet;
	private MenuInstance menu, credits_menu;

	public enum WheelCategories
	{
		NewGame,
		LoadGame,
		Continue,
		Controls,
		Options,
		Credits,
		Exit
	}
	public enum WheelDirection { Left, Right }
	public readonly MenuHandler Menus = new();
	private Dictionary<WheelCategories, Action> wheel_actions = [];
	private WheelCategories current_category, last_category;
	private int wheel_index;

	public override void _EnterTree()
	{
		Instance = this;

		// event listeners
		if (OptionsManager.SaveModule.Loaded)
			E_CreateContinueButton();
		else
			OptionsManager.SaveModule.AddEventListener((_) => E_CreateContinueButton(), SaveSystem.SaveEventsEnum.OnLoadFinish);

		if (ControlsManager.SaveModule.Loaded)
			ReadyUp();
		else
			ControlsManager.SaveModule.AddEventListener((_) => E_SetStartButton(), SaveSystem.SaveEventsEnum.OnLoadFinish);

		// set menu interface
		menu = new("start", anim_player);
		_ = Menus.SetTo(menu);
		credits_menu = new MenuInstance("credits", credits_player, null, null, null, true);

		// miscellaneous
		aphidButton.Pressed += () =>
		{
			anim_player.Play("squish_aphid");
			SoundManager.CreateSound("aphid/baby_idle");
		};
		secretButton.Pressed += () =>
		{
			anim_player.Play("boing_festival");
			if (DebugConsole.IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt)
				DebugConsole.LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair = true;
			SoundManager.CreateSound("aphid/boing");

			if (isASecreeeeet < 7)
				isASecreeeeet++;
			else
			{
				if (GD.Randf() > 0.5f)
					titleFestival.Material = trans_rights;
				else
					titleFestival.Material = aro_flag;
			}
		};

		if (DateTime.Now.Month == 6) // pride month!!!
		{
			if (GD.Randf() > 0.5f)
				titleFestival.Material = trans_rights;
			else
				titleFestival.Material = aro_flag;
		}

		githubButton.Pressed += () =>
			OS.ShellOpen("https://github.com/NeverEverTM/Aphid-Festival");
		itchioButton.Pressed += () =>
			OS.ShellOpen("https://neverevertm.itch.io/aphid-festival-resort");
	}
	public override void _Ready()
	{
		CreateWheelAction(WheelCategories.Options, (options_panel as IMenuInstance).Create());
		CreateWheelAction(WheelCategories.Controls, (controls_panel as IMenuInstance).Create());
		CreateWheelAction(WheelCategories.Credits, credits_menu);
		CreateWheelAction(WheelCategories.Exit, MainMenu.Instance.ExitGame);
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (GlobalManager.IsBusy || Menus.Processing || !MainMenu.IsReady || is_busy)
			return;

		// Press To Start - Pressed
		if (!IsReady)
		{
			if (Input.IsActionJustPressed(InputNames.Interact))
				ReadyUp();
			return;
		}

		// Exit current menu
		if (Menus.Current != null && Menus.Current.Name != "start")
		{
			if (@event.IsActionPressed(InputNames.Escape) || @event.IsActionPressed(InputNames.Cancel))
				GoBack();
			return;
		}

		// wheel interactions
		if (@event is InputEventMouseButton && (@event as InputEventMouseButton).Pressed)
		{
			InputEventMouseButton _mouse = @event as InputEventMouseButton;
			if (_mouse.ButtonIndex == MouseButton.WheelUp)
				ScrollThroughWheel(WheelDirection.Left);
			else if (_mouse.ButtonIndex == MouseButton.WheelDown)
				ScrollThroughWheel(WheelDirection.Right);
		}
		else
		{
			if (@event.IsActionPressed(InputNames.Left) || @event.IsActionPressed("ui_left"))
				ScrollThroughWheel(WheelDirection.Left);
			else if (@event.IsActionPressed(InputNames.Right) || @event.IsActionPressed("ui_right"))
				ScrollThroughWheel(WheelDirection.Right);
		}

		if (@event.IsActionPressed(InputNames.Interact))
		{
			wheel_actions[current_category]();
			SoundManager.CreateSound("ui/button_select");
		}
	}

	/// <summary>
	/// Exits intro text and shows the title screen after user input, intro only appears at the start of the program so this will not be called again later.
	/// </summary>
	public void ReadyUp()
	{
		IsReady = true;
		start_label.Hide();
		wheel_label.Show();
		SetWheelText();
		SoundManager.CreateSound("aphid/idle");
	}
	private void E_CreateContinueButton()
	{
		if (!string.IsNullOrEmpty(OptionsManager.Settings.LastPlayedResort))
		{
			SaveSystem.SelectProfile(OptionsManager.Settings.LastPlayedResort);
			if (DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
			{
				CreateWheelAction(WheelCategories.Continue, MainMenu.Instance.ContinueGame);
				SetWheelCategory(WheelCategories.Continue, true);
			}
		}
	}
	private void E_SetStartButton()
	{
		string _translation = string.Format(Tr("press_start"),
			ControlsManager.GetLocalizedActionName(InputNames.Interact));
		start_label.Text = $"[wave][center]{_translation}[/center][/wave]";
	}

	// MARK: UI Handling
	public static void CreateWheelAction(WheelCategories _key, MenuInstance _menu) =>
		CreateWheelAction(_key, () => _ = Instance.Menus.SetTo(_menu));		
	public static void CreateWheelAction(WheelCategories _key, Action _action)
	{
		Instance.wheel_actions.Add(_key, _action);
		Instance.wheel_actions = Instance.wheel_actions.OrderBy(a => a.Key).ToDictionary();
	}
	public static void RemoveWheelAction(WheelCategories _key)
	{
		Instance.wheel_actions.Remove(_key);
		if (_key == Instance.current_category)
			Instance.SetWheelCategory(WheelCategories.NewGame, true);
	}
	public void GoBack()
	{
		_ = Menus.GoBack();
		SetWheelCategory(last_category);
		SoundManager.CreateSound("ui/button_select");
	}
	public void ScrollThroughWheel(WheelDirection _direction)
	{
		if (is_busy)
			return;
		is_busy = true;

		if (_direction == WheelDirection.Left)
			wheel_index--;
		else
			wheel_index++;

		if (wheel_index < 0)
			wheel_index = wheel_actions.Count - 1;
		else if (wheel_index >= wheel_actions.Count)
			wheel_index = 0;
		current_category = wheel_actions.Keys.ToList()[wheel_index];

		SetWheelCategory(current_category);
		SoundManager.CreateSound("ui/button_switch");
		is_busy = false;
	}
	public void SetWheelCategory(WheelCategories _category, bool _setIndex = false)
	{
		last_category = current_category;
		current_category = _category;
		SetWheelText();

		if (_setIndex)
			wheel_index = wheel_actions.Keys.ToList().IndexOf(current_category);
	}
	public void SetWheelText() =>
		wheel_label.Text = $"[center]<| [wave]{Tr("start_" + current_category.ToString().ToLower())}[/wave] |>[/center]";
}
