using Godot;
public partial class test_camera : CameraManager
{
    public async override void _Ready()
    {
        await GlobalManager.INTIALIZE_GAME_PROCESS(false);
        EnableFreeRoam = true;
        int e = 0;
		for (int i = 0; i < 1800; i++, e++)
		{
			if (i % 10 == 0)
				i++;
		}
		GD.Print(e);
    }
}
