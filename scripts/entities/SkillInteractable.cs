using Godot;
using System;

public partial class SkillInteractable : Sprite2D, IStructureAphid, SaveSystem.IDataModule
{
    [Export] private AphidData.SkillEnum trainingSkill = AphidData.SkillEnum.Speed;
    [Export] private float trainingTime = 10;
    [Export] private int trainingPoints = 1;
    [Export] private Marker2D restingPosition;
    [Export] private AnimationPlayer animator;

    public bool IsInterruptable { get; set; } = true;
    public Aphid SelectedAphid { get; set; }
    public Guid SelectedAphidID { get; set; } = Guid.Empty;

    private bool walking;

    // Animation Methods
    public void FlipAphidToTheLeft()
    {
        SelectedAphid.skin.SetFlipDirection(Vector2.Left);
    }
    public void FlipAphidToTheRight()
    {
        SelectedAphid.skin.SetFlipDirection(Vector2.Right);
    }
    public void JumpAphid() =>
        SelectedAphid.skin.DoJump();
    public void HopAphid() =>
        SelectedAphid.skin.DoHop(false);
    public void SetWalk()
    {
        walking = !walking;
        SelectedAphid.skin.OverrideMovementAnim = walking;
    }

    public override void _Process(double delta)
    {
        if (SelectedAphid != null)
            Process((float)delta);
    }

    public void Enter()
    {
        SelectedAphid.Instance.Status.CurrentTraining = new()
        {
            Skill = trainingSkill,
            PointGain = trainingPoints,
            BaseTime = trainingTime
        };
        SelectedAphid.SetState(Aphid.StateEnum.Train);

        SelectedAphid.GlobalPosition = restingPosition.GlobalPosition;
        animator.Play("start");
    }
    public void Exit()
    {
        SelectedAphid.GlobalPosition = GlobalPosition + new Vector2(0, 20);
        SelectedAphid.skin.DoSquish();
        SelectedAphid = null;

        walking = false;
        animator.Play("RESET");
    }
    public void Process(float delta)
    {
        if (!SelectedAphid.State.Is(Aphid.StateEnum.Train))
        {
            Exit();
            return;
        }

        SelectedAphid.GlobalPosition = restingPosition.GlobalPosition;

        if (walking)
            SelectedAphid.skin.DoWalkAnim();
    }
    
    public void Interact()
    {
        if (Player.Instance.HeldPickup.Entity != null && Player.Instance.HeldPickup.Entity_Aphid.Instance.Status.Tiredness > AphidData.MAX_TIREDNESS_SLEEP)
        {
            SoundManager.CreateSound("ui/button_fail");
            return;
        }
        (this as IStructureAphid).InteractWithAnAphid();
    }

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
