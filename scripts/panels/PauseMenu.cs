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

		Action[] actions = new Action[]
		{
			ResumeButton,
			OptionsButton,
			ControlsButton,
			HelpButton,
			BackToMenuButton,
			ExitButton
		};

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
			if (ResortGUI.WasFreeCamera || CanvasManager.Menus.IsInMenu || DialogManager.IsActive)
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
			CanvasManager.Menus.GoBackInMenu();
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

	public override void _Process(double delta)
	{
		// closing pause menu or its submenus
		if (Visible)
		{
			if (Input.IsActionJustPressed("escape") || Input.IsActionJustPressed("cancel"))
			{
				if (current_menu != null)
					ExitSubMenu();
				else
					SetPauseMenu(false);
			}
		} // opening pause menu
		else if (Input.IsActionJustPressed("escape"))
			SetPauseMenu(true);
	}

	private void OnButtonPress(Action _action)
	{
		if (GameManager.IsBusy)
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
		await SaveSystem.SaveProfileData();
		await GameManager.LoadScene(GameManager.SceneName.Menu);
	}
	private async void ExitButton()
	{
		await SaveSystem.SaveProfileData();
		GetTree().Quit();
	}
}
