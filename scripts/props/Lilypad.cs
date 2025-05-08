using System;
using Godot;

public partial class Lilypad : Sprite2D
{
    private float orig;

    public override void _EnterTree()
    {
        orig = Offset.Y;
    }

    public override void _Process(double delta)
    {
        Offset = new(0, -(float)Math.Sin(Time.GetUnixTimeFromSystem()) * orig);
    }
}
