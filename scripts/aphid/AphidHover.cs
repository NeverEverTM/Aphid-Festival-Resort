using Godot;

public partial class AphidHover : InteractableArea2D
{
    [Export] private Aphid aphid;
    private bool mouseIshovering;

    public override void _Process(double delta)
    {
        QueueRedraw();
    }
    public override void _MouseEnter()
    {
        mouseIshovering = true;
    }
    public override void _MouseExit()
    {
        mouseIshovering = false;
    }
    public override void _Draw()
    {
        if (FreeCameraManager.Enabled)
        {
            if (aphid.Equals(CameraManager.FocusedAphid))
                DrawCircle(aphid.skin.Position, 40, new Color("green"), false, 2);
            else if (mouseIshovering)
                DrawCircle(aphid.skin.Position, 60, new Color("white"), false, 1);
        }
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!mouseIshovering || !FreeCameraManager.Enabled)
            return;

        if (@event.IsActionPressed(InputNames.Interact))
        {
            if (!aphid.Equals(CameraManager.FocusedAphid))
                FreeCameraManager.Instance.FocusAphid(aphid);
            else
                FreeCameraManager.StopFocus();
            GetViewport().SetInputAsHandled();
        }
    }
}
