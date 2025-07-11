using System;
using Godot;

public partial class TestSkill : Sprite2D, IFurnitureInteractable, Player.IInteractEvent
{
    private Aphid aphid;
    
    [Export] private AphidData.SkillEnum skill = AphidData.SkillEnum.Speed;
    [Export] private float timer = 1;
    [Export] private int points_given = 1;
    [Export] private Marker2D resting_position;

    public bool IsInterruptable { get; set; } = true;
    public Aphid SelectedAphid { get; set; }

    private float train_timer;

    public void Enter(Aphid aphid, EventArgs args)
    {
        aphid.skin.SetFlipDirection(Vector2.Left);
        aphid.GlobalPosition = resting_position.GlobalPosition;
        train_timer = timer;
    }

    public void Exit(Aphid aphid, EventArgs args)
    {
        aphid.GlobalPosition = resting_position.GlobalPosition + new Vector2(0, 20);
    }
    public void Process(Aphid aphid, EventArgs args, float delta)
    {
        if (aphid.Instance.Status.Tiredness > 90)
        {
            aphid.SetState(Aphid.StateEnum.Idle);
            return;
        }

        if (train_timer > 0)
            train_timer -= delta;
        else
        {
            train_timer = timer;
            aphid.Instance.Genes.Skills[skill.ToString().ToLower()].GivePoints(points_given);
            SoundManager.CreateSound2D("aphid/skill_gain", GlobalPosition);
            aphid.skin.DoJumpAnim();
        }
    }

    public void Interact() =>
        (this as IFurnitureInteractable).TriggerPlayerInteraction();

    public void Enter(EventArgs args)
    {
        throw new NotImplementedException();
    }

    public void Process(EventArgs args, float delta)
    {
        throw new NotImplementedException();
    }

    public void Exit(EventArgs args)
    {
        throw new NotImplementedException();
    }

}
