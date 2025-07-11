using Godot;
using System;

public partial class BedBehaviour : Sprite2D, IFurnitureInteractable, Player.IInteractEvent
{
    [Export] private Marker2D restingPosition;
    [Export] private float staminaRecoveryCooldown = -1;

    public bool IsInterruptable { get; set; } = true;
    public Aphid SelectedAphid { get; set; }

    private float stamina_recovery_timer;

    public void Enter(EventArgs args)
    {
        SelectedAphid.skin.SetFlipDirection(Vector2.Left);
        SelectedAphid.GlobalPosition = restingPosition.GlobalPosition;
        stamina_recovery_timer = staminaRecoveryCooldown;
    }

    public void Exit(EventArgs args)
    {
        SelectedAphid.GlobalPosition = restingPosition.GlobalPosition + new Vector2(0, 20);
    }
    public void Process(EventArgs args, float delta)
    {
        if (staminaRecoveryCooldown < 0) // is disabled?
            return;

        if (stamina_recovery_timer > 0)
            stamina_recovery_timer -= delta;
        else
        {
            stamina_recovery_timer = staminaRecoveryCooldown;
            SelectedAphid.Instance.Status.AddTiredness(-1);
        }
    }

    public void Interact() =>
        (this as IFurnitureInteractable).TriggerPlayerInteraction();
}
