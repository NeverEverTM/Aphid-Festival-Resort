using System.Collections.Generic;
using Godot;

// Props to this tutorial video for an easy control binding display, truly lifesaving
// https://www.youtube.com/watch?v=ZDPM45cHHlI

/// <summary>
/// Panel for the controls menu user interface.
/// </summary>
public partial class ControlsMenu : Control
{
	[Export] private Control[] controls;
	[Export] private TextureProgressBar reset_bar;
	[Export] private AudioStream select_sound, fail_sound, reset_sound;
	[Export] private Curve interaction_curve;

	private double interactionTimer;
	private readonly List<string> validActions = new();
	private bool is_remapping, was_modified, was_restarted;
	private Control current_action;
	private string current_keybind;

	public override void _Ready()
	{
		SetControlBinds();
		VisibilityChanged += () =>
		{
			if (Visible)
				return;

			if (was_modified)
			{
				SaveSystem.SaveGlobalData();
				// GameManager.IsBusy = true;
				// ConfirmationPopup.Create(
				// 	_onConfirm: () => {
				// 		SaveSystem.SaveGlobalData();
				// 		GameManager.IsBusy = false;
				// 		},
				// 	_onCancel: () => {
				// 		InputMap.LoadFromProjectSettings();
				// 		GameManager.IsBusy = false;
				// 	});
			}

			was_modified = was_restarted = false;
		};
	}

	private void SetControlBinds()
	{
		foreach (Control _inputButton in controls)
		{
			// get the corresponding action
			if (!OptionsManager.Data.InputBinds.ContainsKey(_inputButton.Name))
				continue;
			validActions.Add(_inputButton.Name);
			string _displayAction = ControlsManager.GetUserReadableText(OptionsManager.Data.InputBinds[_inputButton.Name]);

			// Set text and functinality of control binders
			(_inputButton.FindChild("action") as RichTextLabel).Text = _displayAction;
			(_inputButton.FindChild("name") as Label).Text = "controls_" + _inputButton.Name;
			(_inputButton.FindChild("action_bg") as BaseButton).Pressed += () => OnControlBindChange(_inputButton);
		}
	}
	private void RefreshBinds()
	{
		foreach (Control _inputButton in controls)
		{
			string _displayAction = ControlsManager.GetUserReadableText(OptionsManager.Data.InputBinds[_inputButton.Name]);

			// Set text and functinality of control binders
			(_inputButton.FindChild("action") as RichTextLabel).Text = _displayAction;
			(_inputButton.FindChild("name") as Label).Text = "controls_" + _inputButton.Name;
		}
	}
	private void OnControlBindChange(Control _control)
	{
		if (is_remapping)
			return;

		is_remapping = true;
		current_action = _control;
		current_keybind = OptionsManager.Data.InputBinds[current_action.Name];
		(_control.FindChild("action") as RichTextLabel).Text = "...";
	}
	private bool IsKeyDuplicated(string _keybind)
	{
		if (current_keybind == _keybind)
			return false;

		// it only checks for duplicate values for available binds in the menu
		for (int i = 0; i < validActions.Count; i++)
		{
			if (OptionsManager.Data.InputBinds[validActions[i]] == _keybind)
			{
				SoundManager.CreateSound(fail_sound);
				GameManager.CreatePopup("warning_key_duplicated", GetParent());
				return true;
			}
		}
		// at this point, we assigned a key that wasnt just the same we had, or duplicated in another action 
		was_modified = true;
		return false;
	}

	public override void _Input(InputEvent @event)
	{
		if (!is_remapping)
			return;
		InputEventMouseButton _mouseEvent = @event is InputEventMouseButton ? @event as InputEventMouseButton : null;

		// Non-valid keybinds
		if (@event.IsAction("escape") || @event.IsAction("debug_0") || @event.IsAction("debug_1") ||
			@event.IsAction("debug_2") || @event.IsAction("take_screenshot") ||
			_mouseEvent?.ButtonIndex == MouseButton.WheelDown ||
			_mouseEvent?.ButtonIndex == MouseButton.WheelUp)
		{
			AcceptEvent();
			return;
		}

		// allow cancel button to rebind
		if (@event.IsAction("cancel"))
			AcceptEvent();

		// disallow double clicks
		if (_mouseEvent != null && _mouseEvent.DoubleClick)
			(@event as InputEventMouseButton).DoubleClick = false;

		// assign key if possible
		if (@event is InputEventKey || _mouseEvent != null && _mouseEvent.Pressed)
		{
			if (IsKeyDuplicated(@event.AsText()))
				return;

			ControlsManager.BindAction(@event, current_action.Name);
			(current_action.FindChild("action") as RichTextLabel).Text = ControlsManager.GetUserReadableText(@event.AsText());

			// reset state
			is_remapping = false;
			current_action = null;
			current_keybind = string.Empty;
			SoundManager.CreateSound(select_sound);
			AcceptEvent();
		}
	}
	public override void _Process(double delta)
	{	
		if (is_remapping)
			return;

		if (Input.IsKeyPressed(Key.F1))
		{
			// prevents it from repeatdly doing it if held down
			if (was_restarted)
				return;

			if (interactionTimer < 1)
			{
				interactionTimer += delta;
				reset_bar.Value = 100 * interaction_curve.Sample((float)interactionTimer / 1);
			}
			else
			{
				was_restarted = true;
				interactionTimer = 0;
				reset_bar.Value = 0;

				ControlsManager.ResetToDefault();
				RefreshBinds();
				SoundManager.CreateSound(reset_sound);
			}
		}
		else
		{
			was_restarted = false;
			interactionTimer = 0;
			reset_bar.Value = 0;
		}
	}
}
