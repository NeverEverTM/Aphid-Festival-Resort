using Godot;

/// <summary>
/// A structure that allows to train a specific skill.
/// </summary>
public partial class SkillStructure : InteractableStructure
{
    [Export] private AphidData.SkillEnum trainingSkill = AphidData.SkillEnum.Speed;
    [Export] private float trainingTime = 10;
    [Export] private int trainingPoints = 1;

    public override void Enter()
    {
        MyAphid.Instance.Status.LastTraining = new()
        {
            Skill = trainingSkill,
            RawPointGain = trainingPoints,
            RawBaseTime = trainingTime
        };
        base.Enter();
    }
    public override void Exit()
    {
        base.Exit();
        if (MyAphid != null)
        {
            MyAphid.Instance.Status.LastTraining = null;
        }
    }
    public override void Update(float _delta)
    {
        if (MyAphid.Instance.Status.Rest < AphidData.MIN_REST_FOR_SLEEP)
        {
            DropAphid();
            return;
        }
        base.Update(_delta);
    }
    public override bool Interact_PlayerCantGive()
    {
        return base.Interact_PlayerCantGive() || Player.Instance.HeldItem?.Entity_Aphid?.Instance.Status.Rest < AphidData.MIN_REST_FOR_SLEEP;
    }
}
