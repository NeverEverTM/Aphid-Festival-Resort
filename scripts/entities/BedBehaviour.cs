using System;
using Godot;

public partial class BedBehaviour : Sprite2D, SaveSystem.IGenericDataModule, IAphidAccess
{
    [Export] private float staminaRecoveryCooldown = -1;
    [ExportGroup("Essentials")]
    [Export] private Marker2D restingPosition;
    [Export] private AnimationPlayer animator;
    [Export] private InteractableArea2D interactArea;

    public Node2D AphidAccess_Owner => this;
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

        // animation
        MyAphid.GlobalPosition = restingPosition.GlobalPosition;

        //if (walking)
        //    MyAphid.skin.DoWalkAnim();

        // recovery
        if (staminaRecoveryCooldown < 0) // is disabled?
            return;

        if (MyAphid.Instance.Status.Tiredness > 50)
            MyAphid.skin.SetEyesSkin("sleep");
        else
            MyAphid.skin.SetEyesSkin("idle");

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
        MyAphid.SetState(Aphid.StateEnum.Play);
        MyAphid.skin.SetSkin(StringNames.IdleAnim);
        MyAphid.skin.SetFlipDirection(Vector2.Left);
        MyAphid.GlobalPosition = restingPosition.GlobalPosition;
        stamina_recovery_timer = staminaRecoveryCooldown;
        animator.Play("start");
    }
    public void Exit()
    {
        MyAphid.SetState(Aphid.StateEnum.Idle);
        MyAphid.skin.SetSkin(StringNames.IdleAnim);
        animator.Play("RESET");
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
