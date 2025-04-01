using Godot;
using System;

public partial class test_camera : CameraManager
{
    public override async void _EnterTree()
    {
        await GlobalManager.INTIALIZE_GAME_PROCESS(false);
    }
}
