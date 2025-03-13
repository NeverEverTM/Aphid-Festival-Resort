using Godot;
using System;

public partial class test_camera : Camera2D
{
    public override async void _EnterTree()
    {
        GlobalManager.GlobalCamera = this;
        await GlobalManager.INTIALIZE_GAME_PROCESS(false);
    }
}
