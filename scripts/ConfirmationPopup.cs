using System;
using Godot;

public partial class ConfirmationPopup : CanvasLayer
{
	public static bool IsConfirming;
	private static ConfirmationPopup currentPopup;
	private static Action givenAction;

	[Export] private TextureButton cancel_button;
	[Export] private TextEdit confirmation_edit;

    public override void _Ready()
    {
        cancel_button.Pressed += CancelConfirmation;
		confirmation_edit.PlaceholderText = Tr("confirmation_yes");
		confirmation_edit.GrabFocus();
		// This is to prevent typing a key bind char if that was used to open the window
		GetViewport().SetInputAsHandled(); 
    }

    public override void _Input(InputEvent _event)
    {
        if (_event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			if (Input.IsActionJustPressed("cancel"))
			{
				CancelConfirmation();
				GetViewport().SetInputAsHandled();
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
	}

	public static void CreateConfirmation(Action _event)
    {
		currentPopup = ResourceLoader.Load<PackedScene>(GameManager.ConfirmationWindowPath).Instantiate() as ConfirmationPopup;
        GameManager.Instance.GetTree().Root.AddChild(currentPopup);
		IsConfirming = true;
        givenAction = _event;
    }

    public static void CancelConfirmation()
    {
        IsConfirming = false;
        givenAction = null;
        currentPopup.QueueFree();
    }
}
