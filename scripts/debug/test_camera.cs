
public partial class test_camera : CameraManager
{
    public async override void _Ready()
    {
        await GlobalManager.INTIALIZE_GAME_PROCESS(false);
        EnableFreeRoam = true;
    }
}
