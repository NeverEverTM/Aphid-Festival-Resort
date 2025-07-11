using Godot;
using System;

public partial class SkillInteractable : Node2D, IFurnitureInteractable, Player.IInteractEvent
{
    [Export] private AphidData.SkillEnum skill = AphidData.SkillEnum.Speed;
    [Export] private float timer = 1;
    [Export] private int points_given = 1;
    [Export] private Marker2D resting_position;
    [Export] private AnimationPlayer anim_player;

    public bool IsInterruptable { get; set; } = true;
    public Aphid SelectedAphid { get; set; }

    private float train_timer;
    private string skill_name;

    public void Enter(EventArgs args)
    {
        SelectedAphid.skin.SetFlipDirection(Vector2.Left);
        SelectedAphid.GlobalPosition = resting_position.GlobalPosition;
        skill_name = skill.ToString().ToLower();
        train_timer = timer;
        //anim_player.Play("start");
    }

    public void Exit(EventArgs args)
    {
        SelectedAphid.GlobalPosition = resting_position.GlobalPosition + new Vector2(0, 20);
        SelectedAphid = null;
    }
    public void Process(EventArgs args, float delta)
    {
        SelectedAphid.GlobalPosition = resting_position.GlobalPosition + new Vector2(0, 20);

        if (SelectedAphid.Instance.Status.Tiredness > 80)
        {
            SelectedAphid.SetState(Aphid.StateEnum.Idle);
            return;
        }

        if (train_timer > 0)
            train_timer -= delta;
        else
        {
            train_timer = timer;
            SelectedAphid.Instance.Genes.Skills[skill_name].GivePoints(points_given);
            SoundManager.CreateSound2D("aphid/skill_gain", GlobalPosition);
            SelectedAphid.skin.DoJumpAnim();
        }
    }

    public void Interact() =>
        (this as IFurnitureInteractable).TriggerPlayerInteraction();
}
