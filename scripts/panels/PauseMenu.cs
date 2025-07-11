using Godot;
using System;
using System.Threading.Tasks;

public partial class PauseMenu : Control
{
	public static PauseMenu Instance { get; private set; }
	private MenuInstance menu;

	[Export] private AnimationPlayer menu_player;
	[Export] private Control panel, options_panel, controls_panel, help_panel;
	[Export] private AudioStream select_sound, switch_sound;
	[Export] private BaseButton[] buttons;
	private Control current_menu;
	private int lastButtonIndex;

	public override void _EnterTree()
	{
		Instance = this;

		Action[] actions =
		[
			ResumeButton,
			OptionsButton,
			ControlsButton,
			HelpButton,
			BackToMenuButton,
			ExitButton
		];

		for (int i = 0; i < actions.Length; i++)
		{
			int new_index = i;
			buttons[i].Pressed += () =>
			{
				lastButtonIndex = new_index;
				OnButtonPress(actions[new_index]);
			};
			buttons[i].FocusEntered += () => SoundManager.CreateSound(switch_sound);
			(buttons[i].GetChild(0) as Label).Text = $"pause_{buttons[i].Name}";
		}

		menu = new("pause", menu_player);
	}
	public override void _ExitTree()
	{
		GetTree().Paused = false;
	}

	public async Task SetPauseMenu(bool _state)
	{
		if (_state)
		{
			if (FreeCameraManager.Enabled || CanvasManager.Menus.IsActive || DialogManager.IsActive)
				return;

			if (await CanvasManager.Menus.SetTo(menu))
			{
				buttons[0].GrabFocus();
				GetTree().Paused = true;
				SoundManager.PauseSong();
				SoundManager.CreateSound(switch_sound);
			}
		}
		else
		{
			GetTree().Paused = false;
			SoundManager.ResumeSong();
			await CanvasManager.Menus.GoBack();
		}
	}
	public void SetSubMenu(Control _menu)
	{
		panel.Hide();
		current_menu = _menu;
		current_menu.Show();
	}
	public void ExitSubMenu()
	{
		current_menu.Hide();
		current_menu = null;
		panel.Show();
		buttons[lastButtonIndex].GrabFocus();
	}

	public override void _Input(InputEvent @event)
	{
		// closing pause menu or its submenus
		if (Visible)
		{
			if (@event.IsActionPressed(InputNames.Escape) || @event.IsActionPressed(InputNames.Cancel))
			{
				if (current_menu != null)
					ExitSubMenu();
				else
					_ = Instance.SetPauseMenu(false);
			}
		} // opening pause menu
		else if (@event.IsActionPressed(InputNames.Escape) && !CanvasManager.Menus.IsActive)
			_ = Instance.SetPauseMenu(true);
	}

	private void OnButtonPress(Action _action)
	{
		if (GlobalManager.IsBusy)
			return;

		_action();
		SoundManager.CreateSound(select_sound);
	}
	private void ResumeButton() =>
		_ = Instance.SetPauseMenu(false);
	private void OptionsButton() =>
		SetSubMenu(options_panel);
	private void ControlsButton() =>
		SetSubMenu(controls_panel);
	private void HelpButton() =>
		SetSubMenu(help_panel);
	private async void BackToMenuButton()
	{
		await SaveSystem.SaveProfile();
		await SceneManager.Switch("menu", false, false);
	}
	private void ExitButton()
	{
		ConfirmationPopup.Create(ExitGame, null,
			ConfirmationPopup.ConfirmationEnum.Fast, "confirmation_exit");

	}
	private async void ExitGame()
	{
		GlobalManager.IsBusy = true;
		await SaveSystem.SaveProfile();
		GetTree().Quit();
	}
}
