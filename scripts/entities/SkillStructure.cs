using System;
using Godot;

/// <summary>
/// A structure that allows to train a specific skill.
/// </summary>
public partial class SkillStructure : Sprite2D, SaveSystem.IGenericDataModule, IAphidAccess
{
    [Export] private AphidData.SkillEnum trainingSkill = AphidData.SkillEnum.Speed;
    [Export] private float trainingTime = 10;
    [Export] private int trainingPoints = 1;
    [ExportGroup("Essentials")]
    [Export] private Marker2D restingPosition;
    [Export] private AnimationPlayer animator;
    [Export] private InteractableArea2D interactArea;

    public Node2D AphidAccess_Owner => this;
    public bool IsAphidAvailable { get; set; }
    public Aphid MyAphid { get; set; }
    public Guid MyAphidID { get; set; }
    private bool walking;

    // MARK: Animation Event Methods
    public void FlipAphidToTheLeft() =>
        MyAphid.skin.SetFlipDirection(Vector2.Left);
    public void FlipAphidToTheRight() =>
        MyAphid.skin.SetFlipDirection(Vector2.Right);
    public void JumpAphid() =>
        MyAphid.skin.DoJump();
    public void HopAphid() =>
        MyAphid.skin.DoHop(false);
    public void SetWalk()
    {
        walking = !walking;
        MyAphid.skin.OverrideMovementAnim = walking;
    }

    // MARK: Processing
    public override void _EnterTree()
    {
        interactArea.OnInteractOnly.Add((this as IAphidAccess).OnInteractOnly);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!IsAphidAvailable)
            return;

        MyAphid.GlobalPosition = restingPosition.GlobalPosition;

        if (walking)
            MyAphid.skin.DoWalkAnim();
    }

    public void Enter()
    {
        MyAphid.Instance.Status.CurrentTraining = new()
        {
            Skill = trainingSkill,
            PointGain = trainingPoints,
            BaseTime = trainingTime
        };
        MyAphid.SetState(Aphid.StateEnum.Train);
        MyAphid.GlobalPosition = restingPosition.GlobalPosition;
        animator.Play("start");
    }
    public void Exit()
    {
        MyAphid.SetState(Aphid.StateEnum.Idle);
        walking = false;
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
