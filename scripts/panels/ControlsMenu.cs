using System.Collections.Generic;
using System.Linq;
using Godot;

// Props to this tutorial video for an easy control binding display, truly lifesaving
// https://www.youtube.com/watch?v=ZDPM45cHHlI

/// <summary>
/// Panel for the controls menu user interface.
/// </summary>
public partial class ControlsMenu : Control
{
	[Export] private Control[] controls;
	[Export] private ScrollContainer scrollContainer;
	[Export] private TextureProgressBar reset_bar;
	[Export] private AudioStream select_sound, fail_sound, reset_sound;
	[Export] private Curve interaction_curve;

	private double interactionTimer;
	private readonly List<string> validActions = [];
	private bool is_remapping, was_modified, was_restarted;
	private Control current_action;
	private InputEvent current_keybind;

	public override void _Ready()
	{
		SetControlBinds();
		VisibilityChanged += () =>
		{
			if (Visible)
			{
				scrollContainer.ScrollVertical = 0;
				return;
			}

			is_remapping = false;

			if (IsInstanceValid(current_action))
			{
				(current_action.FindChild("action") as RichTextLabel).Text =
					ControlsManager.GetActionName(current_action.Name);
			}

			if (was_modified && !ConfirmationPopup.IsConfirming)
			{
				ConfirmationPopup.Create(
					_onConfirm: () => { ControlsManager.InputBinds.Save(); },
					_onCancel: () => { ControlsManager.InputBinds.Load(); RefreshBinds(); });
			}

			current_action = null;
			current_keybind = null;
			was_modified = was_restarted = false;
		};
	}

	private void SetControlBinds()
	{
		foreach (Control _inputButton in controls)
		{
			// get the corresponding action
			if (!ControlsManager.Binds.ContainsKey(_inputButton.Name))
				continue;
			validActions.Add(_inputButton.Name);
			string _displayAction = ControlsManager.GetActionName(_inputButton.Name);

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
			string _displayAction = ControlsManager.GetActionName(_inputButton.Name);

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
		current_keybind = ControlsManager.Binds[current_action.Name];
		(_control.FindChild("action") as RichTextLabel).Text = "...";
	}
	private bool IsKeyDuplicated(InputEvent _keybind)
	{
		string _inputedKey = ControlsManager.CleanActionName(_keybind);
		bool _isBuildMode = controls.First((c) => c.Name == current_action.Name).GetParent().HasMeta("BuildMode");

		// check for duplicate values within the available binds
		for (int i = 0; i < validActions.Count; i++)
		{
			// make sure to not disallow yourself from just canceling out by setting the same key
			if (validActions[i] == current_action.Name)
				continue;

			string _storedKey = ControlsManager.CleanActionName(ControlsManager.Binds[validActions[i]]);

			// this key is duplicated
			if (_inputedKey == _storedKey)
			{
				// binds can be duplicated if they arent from the same group
				// this is cause build mode binds dont affect overworld actions and viceversa
				if (_isBuildMode == controls.First((c) => c.Name == validActions[i]).GetParent().HasMeta("BuildMode"))
				{
					SoundManager.CreateSound(fail_sound);
					GlobalManager.CreatePopup("warning_key_duplicated", GetParent());
					return true;
				}
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

		// cancel the rebind
		if (@event.IsActionPressed(InputNames.Escape))
		{
			SetBind(ControlsManager.Binds[current_action.Name], false);
			AcceptEvent();
			return;
		}

		InputEventMouseButton _mouseEvent = @event is InputEventMouseButton ? @event as InputEventMouseButton : null;
		// Non-valid keybinds
		if (@event.IsAction(InputNames.Debug0) || @event.IsAction(InputNames.Debug1) ||
			@event.IsAction(InputNames.Debug2) || @event.IsAction(InputNames.TakeScreenshot) ||
			_mouseEvent?.ButtonIndex == MouseButton.WheelDown || _mouseEvent?.ButtonIndex == MouseButton.WheelUp)
		{
			AcceptEvent();
			return;
		}

		// disallow double clicks
		if (_mouseEvent != null && _mouseEvent.DoubleClick)
			(@event as InputEventMouseButton).DoubleClick = false;

		// assign key if possible
		if (@event is InputEventKey || _mouseEvent != null && _mouseEvent.Pressed)
		{
			if (IsKeyDuplicated(@event))
				return;

			SetBind(@event);
		}

		AcceptEvent();
	}
	public void SetBind(InputEvent @event, bool _success = true)
	{
		is_remapping = false;

		ControlsManager.BindAction(@event, current_action.Name);
		(current_action.FindChild("action") as RichTextLabel).Text = ControlsManager.GetActionName(@event);
		current_action = null;
		current_keybind = null;
		if (_success)
			SoundManager.CreateSound(select_sound);
		else
			SoundManager.CreateSound(fail_sound);
	}
	public override void _Process(double delta)
	{
		if (!Visible || is_remapping)
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
				ControlsManager.InputBinds.Save();
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