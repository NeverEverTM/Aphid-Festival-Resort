using System;
using Godot;

/// <summary>
/// AFR's own confirmation user display, it can contain one popup at a time.
/// </summary>
public partial class ConfirmationPopup : CanvasLayer
{
	private static ConfirmationPopup currentPopup;
	public static bool IsConfirming { get; private set; }

	/// <summary>
	/// The type of confirmation needed to approve the action.
	/// Safe: Asks the user to manually type a confirmation message. Used for major irreversible actions.
	/// Standard: Users needs to hold the interaction button to confirm the action.
	/// Fast: User simply needs to press Yes to confirm.
	/// </summary>
	public enum ConfirmationEnum { Safe, Standard, Fast }
	public ConfirmationEnum confirmationType;
	private Action confirmationAction, cancelAction;
	private float standardTimer;
	private string displayText;

	[Export] private AnimationPlayer player;
	[Export] private BaseButton cancel_button, yes_button, no_button;
	[Export] private TextEdit confirmation_edit;
	[Export] private RichTextLabel confirmation_label;
	[Export] private TextureProgressBar progress;
	[Export] private Curve progressCurve;

	public override void _Ready()
	{
		switch (confirmationType)
		{
			case ConfirmationEnum.Safe:
				yes_button.Hide();
				no_button.Hide();
				confirmation_edit.PlaceholderText = Tr("confirmation_yes");
				break;
			case ConfirmationEnum.Standard:
				progress.Show();
				confirmation_edit.Hide();
				standardTimer = 1;
				no_button.Pressed += Cancel;
				break;
			case ConfirmationEnum.Fast:
				confirmation_edit.Hide();
				yes_button.Pressed += Accept;
				no_button.Pressed += Cancel;
				break;
		}

		cancel_button.Pressed += Cancel;
		confirmation_label.Text = displayText;
		confirmation_edit.GrabFocus();
		// This is to prevent typing a key bind char if that was used to open the window
		GetViewport().SetInputAsHandled();
		player.Play(StringNames.OpenAnim);
	}
	public override void _ExitTree()
	{
		currentPopup = null;
		IsConfirming = false;
	}

	public override void _Input(InputEvent _event)
	{
		if (_event.IsActionPressed(InputNames.Escape) || _event.IsActionPressed(InputNames.Cancel))
		{
			GlobalManager.Instance.GetViewport().SetInputAsHandled();
			Cancel();
			return;
		}

		if (confirmationType != ConfirmationEnum.Safe || _event is not InputEventKey)
			return;

		InputEventKey _input = _event as InputEventKey;

		if (_input.KeyLabel == Key.Enter)
		{
			CheckConfirm();
			GlobalManager.Instance.GetViewport().SetInputAsHandled();
		}
	}

	public override void _Process(double delta)
	{
		if (confirmationType == ConfirmationEnum.Standard)
		{
			if (!yes_button.ButtonPressed)
			{
				if (progress.Visible)
					progress.Hide();
				standardTimer = 1;
			}
			else
			{
				if (!progress.Visible)
					progress.Show();
				standardTimer -= (float)delta;
				if (standardTimer < 0)
				{
					progress.Value = 100;
					Accept();
					return;
				}
				progress.Value = 101 - 100 * progressCurve.Sample(standardTimer);
			}
		}
	}

	private void CheckConfirm()
	{
		if (confirmation_edit.Text == Tr("confirmation_yes"))
		{
			confirmation_edit.ReleaseFocus();
			Accept();
		}
		else
		{
			confirmation_edit.Text = string.Empty;
			confirmation_edit.GrabFocus();
			SoundManager.CreateSound("ui/button_fail");
		}
	}

	/// <summary>
	/// Create a new confirmation popup.
	/// </summary>
	/// <param name="_onConfirm">Action to take if confirmed</param>
	/// <param name="_onCancel">Action to take if canceled</param>
	/// <param name="_type">The type of confirmation security needed</param>
	/// <param name="_custom_tr_key">Custom translation key to display</param>
	/// <returns></returns>
	public static ConfirmationPopup Create(Action _onConfirm, Action _onCancel = null, ConfirmationEnum _type = ConfirmationEnum.Standard, string _custom_tr_key = null)
	{
		IsConfirming = true;
		if (IsInstanceValid(currentPopup))
			currentPopup.QueueFree();
		currentPopup = ResourceLoader.Load<PackedScene>(GlobalManager.CONFIRM_WINDOW_SCENE).Instantiate() as ConfirmationPopup;

		currentPopup.confirmationType = _type;
		currentPopup.confirmationAction = _onConfirm;
		currentPopup.cancelAction = _onCancel;
		if (_custom_tr_key == null)
			currentPopup.displayText = currentPopup.Tr("confirmation_" + (int)currentPopup.confirmationType);
		else
			currentPopup.displayText = currentPopup.Tr(_custom_tr_key);

		GlobalManager.Instance.GetViewport().SetInputAsHandled();
		GlobalManager.Instance.GetTree().Root.CallDeferred(Node.MethodName.AddChild, currentPopup);
		GlobalManager.Instance.GetTree().Root.ProcessMode = ProcessModeEnum.Disabled;
		return currentPopup;
	}
	public static void Accept()
	{
		try { currentPopup.confirmationAction?.Invoke(); }
		catch (Exception e)
		{
			GlobalManager.CreatePopup(e.ToString(), GlobalManager.Instance);
			Logger.Print(Logger.LogPriority.Error, "ConfirmationPopup: Error on confirm:\n", e); Cancel();
		}
		currentPopup.GetTree().Root.ProcessMode = ProcessModeEnum.Pausable;
		currentPopup.CallDeferred(Node.MethodName.QueueFree);
		IsConfirming = false;
	}
	public static void Cancel()
	{
		currentPopup.cancelAction?.Invoke();
		currentPopup.GetTree().Root.ProcessMode = ProcessModeEnum.Pausable;
		currentPopup.QueueFree();
		IsConfirming = false;
	}
}
