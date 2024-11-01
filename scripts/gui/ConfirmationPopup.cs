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
	private Timer standardTimer = new();

	[Export] private AnimationPlayer player;
	[Export] private BaseButton cancel_button, yes_button, no_button;
	[Export] private TextEdit confirmation_edit;
	[Export] private RichTextLabel confirmation_label;
	[Export] private TextureProgressBar progress;

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
				standardTimer.Timeout += Accept;
				yes_button.Pressed += () => standardTimer.Start(3);
				no_button.Pressed += Cancel;
				break;
			case ConfirmationEnum.Fast:
				yes_button.Pressed += Accept;
				no_button.Pressed += Cancel;
				break;
		}

		cancel_button.Pressed += Cancel;
		confirmation_label.Text = "[center]" + Tr("confirmation_" + (int)confirmationType) + "[/center]";
		confirmation_edit.GrabFocus();
		// This is to prevent typing a key bind char if that was used to open the window
		GetViewport().SetInputAsHandled();
		player.Play("open");
	}
	public override void _ExitTree()
	{
		currentPopup = null;
		IsConfirming = false;
	}

	public override void _Input(InputEvent _event)
	{
		if (Input.IsActionJustPressed("cancel") || Input.IsActionJustPressed("escape"))
		{
			Cancel();
			return;
		}

		if (confirmationType != ConfirmationEnum.Safe || _event is not InputEventKey)
			return;

		InputEventKey _input = _event as InputEventKey;

		if (_input.KeyLabel == Key.Enter)
		{
			confirmation_edit.ReleaseFocus();
			CheckConfirm();
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
				standardTimer.Stop();
			}
			else
			{
				if (!progress.Visible)
					progress.Show();
				progress.Value = standardTimer.TimeLeft / 3;
			}
		}
	}

	private void CheckConfirm()
	{
		if (confirmation_edit.Text == Tr("confirmation_yes"))
		{
			try { confirmationAction(); }
			catch (Exception e) { GameManager.CreatePopup(e.ToString(), GameManager.Instance); GD.PrintErr(e); Cancel(); }
			Accept();
		}
		else
			confirmation_edit.GrabFocus();
	}

	public static ConfirmationPopup Create(Action _onConfirm, Action _onCancel = null, ConfirmationEnum _type = ConfirmationEnum.Standard)
	{
		IsConfirming = true;
		if (IsInstanceValid(currentPopup))
			currentPopup.QueueFree();
		currentPopup = ResourceLoader.Load<PackedScene>(GameManager.ConfirmationWindowPath).Instantiate() as ConfirmationPopup;

		currentPopup.confirmationType = _type;
		currentPopup.confirmationAction = _onConfirm;
		currentPopup.cancelAction = _onCancel;
		GameManager.Instance.GetViewport().SetInputAsHandled();
		GameManager.Instance.GetTree().Root.AddChild(currentPopup);
		GameManager.Instance.GetTree().Root.ProcessMode = ProcessModeEnum.Disabled;
		return currentPopup;
	}
	public static void Accept()
	{
		currentPopup.GetTree().Root.ProcessMode = ProcessModeEnum.Pausable;
		currentPopup.QueueFree();
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
