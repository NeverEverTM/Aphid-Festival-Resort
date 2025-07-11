using Godot;

public partial class SpinAphid : Node3D
{
    private float speed;
    public override void _Ready()
    {
        RandomNumberGenerator _new = new();
        speed = _new.RandiRange(3,8) * 0.5f + _new.Randf();
    }
    public override void _PhysicsProcess(double delta)
    {
        RotateY((float)(delta * speed));
    }
}
