using System;
using Godot;

public partial class BedBehaviour : Sprite2D, SaveSystem.IGenericDataModule, IAphidAccess
{
    [Export] private Marker2D restingPosition;
    [Export] private float staminaRecoveryCooldown = -1;
    [ExportGroup("Inmutables")]
    [Export] private InteractableArea2D interactArea;

    public bool IsAphidAvailable { get; set; }
    public Aphid MyAphid { get; set; }
    public Guid MyAphidID { get; set; }

    private float stamina_recovery_timer;

    public override void _EnterTree()
    {
        interactArea.OnInteractOnly.Add((this as IAphidAccess).OnInteractOnly);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!IsAphidAvailable)
            return;

        if (staminaRecoveryCooldown < 0) // is disabled?
            return;

        if (stamina_recovery_timer > 0)
            stamina_recovery_timer -= (float)delta;
        else
        {
            stamina_recovery_timer = staminaRecoveryCooldown;
            MyAphid.Instance.AddTiredness(-1);
        }
    }

    public void Enter()
    {
        MyAphid.skin.SetFlipDirection(Vector2.Left);
        MyAphid.GlobalPosition = restingPosition.GlobalPosition;
        stamina_recovery_timer = staminaRecoveryCooldown;
    }
    public void Exit()
    {
        return;
    }

    public void Set(string _data)
    {
        (this as IAphidAccess).SetAphid(_data);
    }
    public string Get()
    {
        return (this as IAphidAccess).GetAphid();
    }
}
