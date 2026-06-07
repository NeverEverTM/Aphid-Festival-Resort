using Godot;

public partial class RestStructure : InteractableStructure
{
    [Export] private float staminaRecoveryCooldown = -1;
    [Export] private Vector2 flipDirection = Vector2.Left;
    private float stamina_recovery_timer;

    public override void Enter()
    {
        base.Enter();
        MyAphid.Skin.SetFlipDirection(flipDirection);
        stamina_recovery_timer = staminaRecoveryCooldown;
        if (MyAphid.Instance.Status.Rest < 20)
            MyAphid.Skin.SetEyesSkin("sleep");
    }
    public override void Update(float _delta)
    {
        base.Update(_delta);

        // recovery
        if (staminaRecoveryCooldown < 0) // is disabled?
            return;

        if (MyAphid.Instance.Status.Rest >= AphidData.MIN_REST_TO_WAKEUP)
            MyAphid.Skin.SetEyesSkin("idle");

        if (stamina_recovery_timer > 0)
            stamina_recovery_timer -= _delta;
        else
        {
            stamina_recovery_timer = staminaRecoveryCooldown;
            MyAphid.Instance.AddRest(1);
        }
    }
}
