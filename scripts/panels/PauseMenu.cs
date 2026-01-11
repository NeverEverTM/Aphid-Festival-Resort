using System;
using System.Threading.Tasks;
using Godot;

public partial class PauseMenu : Control
{
	public static PauseMenu Instance { get; private set; }

	[Export] private TextureButton menu_button;
	[Export] private AnimationPlayer menu_player, bg_player;
	[Export] private BaseButton[] buttons;
	[Export] private Control options_panel, controls_panel, help_panel;

	private MenuInstance menu;
	private const string PAUSE_MENU_NAME = "pause";
	private MenuInstance[] menu_panels;

	public override void _EnterTree()
	{
		Instance = this;

		// hardcoded menu values
		menu_panels = [
			(options_panel as IMenuInstance).Create(),
			(controls_panel as IMenuInstance).Create(),
			(help_panel as IMenuInstance).Create()
		];

		// hardcoded button values
		Action[] _actions = [
			() => _ = CanvasManager.Menus.SetTo(null),
			() => ConfirmationPopup.Create(SaveButton, null, ConfirmationPopup.ConfirmationEnum.Fast),
			() => _ = CanvasManager.Menus.SetTo(menu_panels[0]),
			() => _ = CanvasManager.Menus.SetTo(menu_panels[1]),
			() => _ = CanvasManager.Menus.SetTo(menu_panels[2]),
			() => _ = ConfirmationPopup.Create(BackToMenuButton, null,
					ConfirmationPopup.ConfirmationEnum.Fast, "confirmation_exit_nosave"),
			() => ConfirmationPopup.Create(() => GetTree().Quit(), null,
					ConfirmationPopup.ConfirmationEnum.Fast, "confirmation_exit_nosave")
		];
		// setup button text and sounds
		for (int i = 0; i < buttons.Length; i++)
		{
			int _index = i;
			buttons[i].Pressed += () =>
			{
				if (!CanvasManager.Menus.Processing)
					_actions[_index]();
				SoundManager.CreateSound("ui/button_select");
			};
			buttons[i].GetChild<Label>(0).Text = $"{PAUSE_MENU_NAME}_{buttons[i].Name}";
		}

		menu = new(PAUSE_MENU_NAME, menu_player,
		Open: (_) =>
		{
			if (!CanvasManager.Menus.IsActive)
			{
				Show();
				GetTree().Paused = true;
				bg_player.Play(StringNames.OpenAnim); // sets the permanent pause bg
				SoundManager.PauseSong();
				SoundManager.CreateSound("ui/button_switch");
			}
			buttons[0].GrabFocus();
		}
		, null,
		Close: (_nextMenu) =>
		{
			if (_nextMenu == null)
			{
				GetTree().Paused = false;
				bg_player.Play(StringNames.CloseAnim); // sets the permanent pause bg
				SoundManager.ResumeSong();
				Player.Instance.SetMovementDirection(Vector2.Zero);
			}
		},
		() =>
		{
			if (CanvasManager.Menus.Pending == null)
				Hide();
		});

		menu_button.Pressed += () => _ = CanvasManager.Menus.SetTo(menu);
	}
	public override void _ExitTree()
	{
		GetTree().Paused = false;
	}
	public override void _Input(InputEvent @event)
	{
		if (GlobalManager.IsBusy || SceneManager.IsBusy || FreeCameraManager.Enabled || DialogManager.IsActive)
			return;

		if (CanvasManager.Menus.Current == null)
		{
			if (@event.IsActionPressed(InputNames.Escape))
				_ = CanvasManager.Menus.SetTo(menu);
		}
		else if (CanvasManager.Menus.Available[0].Name.Equals(PAUSE_MENU_NAME))
		{
			if (@event.IsActionPressed(InputNames.Escape) || @event.IsActionPressed(InputNames.Cancel))
				_ = CanvasManager.Menus.GoBack();
		}
	}

	private async void SaveButton()
	{
		if (await SaveSystem.SaveProfile())
			SoundManager.CreateSound("ui/kitchen_success");
		else
			SoundManager.CreateSound("ui/kitchen_fail");
	}
	private async void BackToMenuButton()
	{
		await SceneManager.Switch("menu", false);
	}
}