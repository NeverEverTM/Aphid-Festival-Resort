using Godot;
using System;

public partial class LobbyDoorDetail : Sprite2D
{
    [Export] private Light2D light;
    private Color transparency_color;

    public override void _Ready()
    {
        transparency_color = SelfModulate;
        FieldManager.Instance.OnTimeChange.Add(OnTimeChange);
    }

    public void OnTimeChange(FieldManager.DayHourMode _hour)
    {
        if (_hour == FieldManager.DayHourMode.Noon || _hour == FieldManager.DayHourMode.Afternoon)
        {
            light.Enabled = false;
            SelfModulate = transparency_color;
        }
        else
        {
            light.Enabled = true;
            SelfModulate = new Color(0,0,0,0);
        }
    }
}
