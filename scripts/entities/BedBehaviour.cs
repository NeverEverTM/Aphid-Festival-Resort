using Godot;
using System;

public partial class BedBehaviour : Sprite2D, IStructureAphid
{
    [Export] private Marker2D restingPosition;
    [Export] private float staminaRecoveryCooldown = -1;

    public bool IsInterruptable { get; set; } = true;
    public Aphid SelectedAphid { get; set; }
    public Guid SelectedAphidID { get; set; }

    private float stamina_recovery_timer;

    public void Enter()
    {
        SelectedAphid.skin.SetFlipDirection(Vector2.Left);
        SelectedAphid.GlobalPosition = restingPosition.GlobalPosition;
        stamina_recovery_timer = staminaRecoveryCooldown;
    }

    public void Exit()
    {
        SelectedAphid.GlobalPosition = restingPosition.GlobalPosition + new Vector2(0, 20);
    }
    public void Process(float delta)
    {
        if (staminaRecoveryCooldown < 0) // is disabled?
            return;

        if (stamina_recovery_timer > 0)
            stamina_recovery_timer -= delta;
        else
        {
            stamina_recovery_timer = staminaRecoveryCooldown;
            SelectedAphid.Instance.AddTiredness(-1);
        }
    }

    public void Interact() =>
        (this as IStructureAphid).InteractWithAnAphid();

    public void Set(string _data)
    {
        SelectedAphidID = new Guid(_data);
        if (SelectedAphidID != Guid.Empty && GameManager.Aphids[SelectedAphidID].Status.Mode == AphidData.EntityStatusType.Active)
        {
            SelectedAphid = GameManager.Aphids[SelectedAphidID].Entity; 
            Enter();
        }
    }

    public string Get()
    {
        return SelectedAphidID.ToString();
    }
}
