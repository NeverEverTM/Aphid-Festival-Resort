using Godot;
using System;

public partial class LumiButton : NinePatchRect
{
    public override void _EnterTree()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseEntered += () => Modulate = new("green");
        MouseExited += () => Modulate = new("white");
    }
}