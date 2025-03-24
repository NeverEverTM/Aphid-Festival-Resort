using Godot;
using System;

public partial class PauseMenu : Control
{
	public static PauseMenu Instance { get; private set; }
	private MenuUtil.MenuInstance menu;

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
			// dumb C# behaviour references the same index for all actions for some UNKNOWN REASON
			var new_index = i; // <- just do this
			buttons[i].Pressed += () =>
			{
				lastButtonIndex = new_index;
				OnButtonPress(actions[new_index]);
			};
			buttons[i].FocusEntered += () => SoundManager.CreateSound(switch_sound);
		}

		menu = new("pause", menu_player, null, null, false);
	}
	public override void _ExitTree()
	{
		GetTree().Paused = false;
	}

	public void SetPauseMenu(bool _state)
	{
		if (_state)
		{
			if (FreeCameraManager.Instance.FreeCameraWasActive || CanvasManager.Instance.IsInFocus 
					|| CanvasManager.Menus.IsBusy || DialogManager.IsActive)
				return;
				
			SoundManager.PauseSong();
			CanvasManager.Menus.OpenMenu(menu);
			CanvasManager.RemoveFocus();
			buttons[0].GrabFocus();
			GetTree().Paused = true;
			SoundManager.CreateSound(switch_sound);
		}
		else
		{
			SoundManager.ResumeSong();
			CanvasManager.Menus.GoBack();
			GetTree().Paused = false;
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
					SetPauseMenu(false);
			}
		} // opening pause menu
		else if (@event.IsActionPressed(InputNames.Escape) && !CanvasManager.Menus.IsBusy)
			SetPauseMenu(true);
	}

	private void OnButtonPress(Action _action)
	{
		if (GlobalManager.IsBusy)
			return;

		_action();
		SoundManager.CreateSound(select_sound);
	}
	private void ResumeButton() =>
		SetPauseMenu(false);
	private void OptionsButton() =>
		SetSubMenu(options_panel);
	private void ControlsButton() =>
		SetSubMenu(controls_panel);
	private void HelpButton() =>
		SetSubMenu(help_panel);
	private async void BackToMenuButton()
	{
		await SaveSystem.SaveProfile();
		await GlobalManager.LoadScene(GlobalManager.SceneName.Menu);
	}
	private async void ExitButton()
	{
		await SaveSystem.SaveProfile();
		GetTree().Quit();
	}
}
