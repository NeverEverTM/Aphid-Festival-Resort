using Godot;

public partial class AphidHover : Area2D
{
    [Export] private Aphid aphid;
    private bool mouseIshovering;
    public override void _Ready()
    {
        if (aphid.IS_FAKE)
            QueueFree();
    }
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
        if (FreeCameraManager.Enabled && FreeCameraManager.Instance.Visible)
        {
            if (aphid.Equals(CameraManager.FocusedAphid))
                DrawCircle(aphid.skin.Position, 40, new Color("green"), false, 2);
            else if (mouseIshovering)
                DrawCircle(aphid.skin.Position, 60, new Color("white"), false, 1);
        }
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!mouseIshovering || !FreeCameraManager.Enabled || !FreeCameraManager.Instance.Visible)
            return;

        if (@event.IsActionPressed(InputNames.Interact))
        {
            if (!aphid.Equals(CameraManager.FocusedAphid))
            {
                AphidInfo.SetAphid(aphid);
                CameraManager.Focus(aphid);
            }
            else
            {
                AphidInfo.SetAphid(null);
                CameraManager.UnFocus();
            }
            GetViewport().SetInputAsHandled();
        }
    }
}
