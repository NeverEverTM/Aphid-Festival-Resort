using Godot;

public partial class LightEntity : Node2D
{
    protected static readonly RandomNumberGenerator RNG = new();
    
    [Export] protected PointLight2D light;
    protected bool is_light_active;

    public override void _EnterTree()
    {
        RoomInstance.Instance.AddEventListener(OnHourChange, RoomInstance.TimeEvents.OnHourChange);
    }
    public override void _ExitTree()
    {
        RoomInstance.Instance.RemoveEventListener(OnHourChange, RoomInstance.TimeEvents.OnHourChange);
    }

    // Events
    protected void OnHourChange(RoomInstance.TimeArgs _args)
    {
        is_light_active = (RoomInstance.TimeOfDay != RoomInstance.DayHourMode.Noon) == (RoomInstance.TimeOfDay != RoomInstance.DayHourMode.Morning);
        
        if (is_light_active)
            LightIn(_args);
        else
            LightOut(_args);
    }

    public virtual void LightIn(RoomInstance.TimeArgs _args)
    {
        light.Enabled = true;
    }
    public virtual void LightOut(RoomInstance.TimeArgs _args)
    {
        light.Enabled = false;
    }
}
