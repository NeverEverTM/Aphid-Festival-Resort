using Godot;

public partial class ZoomCamera : Camera3D
{
	private float currentY, time;

    public override void _Ready()
    {
        currentY = GlobalPosition.Y;
    }

    public override void _Process(double delta)
	{
		time += (float)delta;
		if (time > 999)
			time = 0;
		Fov = 90 + 30 * (float)Mathf.Sin(time);
		GlobalPosition = new (GlobalPosition.X, currentY + 2 * (float)Mathf.Cos(time)
		, GlobalPosition.Z);
		RotateZ((float)delta);
	}
}
