using Godot;

public partial class LobbyDoorDetail : Node2D
{
    [Export] private Light2D light;
    [Export] private Sprite2D shadow;

    public override void _Ready()
    {
        RoomInstance.Instance.AddEventListener(OnTimeChange, RoomInstance.TimeEvents.OnHourChange);
    }

    public void OnTimeChange(RoomInstance.TimeArgs _args)
    {
        if (RoomInstance.TimeArgs.TimeOfDay == RoomInstance.DayHourMode.Noon || RoomInstance.TimeArgs.TimeOfDay == RoomInstance.DayHourMode.Afternoon)
        {
            light.Enabled = false;
            shadow.Show();
        }
        else
        {
            light.Enabled = true;
            shadow.Hide();
        }
    }
}
