using System;
using Godot;

public partial class ConfirmationPopup : CanvasLayer
{
	private static ConfirmationPopup currentPopup;
	private static Action givenAction;

	[Export] private AnimationPlayer popupPlayer;
	[Export] private TextureButton cancel_button;
	[Export] private TextEdit confirmation_edit;

    public override void _Ready()
    {
        cancel_button.Pressed += CancelConfirmation;
		confirmation_edit.PlaceholderText = Tr("confirmation_yes");
		confirmation_edit.GrabFocus();
		// This is to prevent typing a key bind char if that was used to open the window
		GetViewport().SetInputAsHandled(); 
		popupPlayer.Play("open");
    }

    public override void _Input(InputEvent _event)
    {
        if (_event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			if (Input.IsActionJustPressed("cancel") || Input.IsActionJustPressed("escape"))
			{
				GetViewport().SetInputAsHandled();
				CancelConfirmation();
				return;
			}

			if (_input.KeyLabel == Key.Enter)
			{
				GetViewport().SetInputAsHandled();
				confirmation_edit.ReleaseFocus();
				CheckConfirm();
				return;
			}
		}
    }

    private void CheckConfirm()
	{
		if (confirmation_edit.Text == Tr("confirmation_yes"))
		{
			try { givenAction(); }
			catch(Exception e) { GD.PrintErr(e); }
			CancelConfirmation();
		}
		else
		{
			confirmation_edit.GrabFocus();
		}
	}

	public static void CreateConfirmation(Action _event)
    {
		if (IsInstanceValid(currentPopup))
			currentPopup.QueueFree();

		currentPopup = ResourceLoader.Load<PackedScene>(GameManager.ConfirmationWindowPath).Instantiate() as ConfirmationPopup;
        GameManager.Instance.GetTree().Root.AddChild(currentPopup);
        givenAction = _event;
    }

    public static void CancelConfirmation()
    {
        givenAction = null;
        currentPopup.QueueFree();
		currentPopup = null;
    }
}
