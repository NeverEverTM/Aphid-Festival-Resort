using System;

public interface IFurnitureInteractable
{
    public void Enter(EventArgs args);
    public void Process(EventArgs args, float delta);
    public void Exit(EventArgs args);

    public bool IsInterruptable { get; set; }
    public Aphid SelectedAphid { get; set; }

    public void TriggerPlayerInteraction()
    {
        if (SelectedAphid == null)
        {
            if (Player.Instance.HeldPickup.Item == null || !Player.Instance.HeldPickup.IsAphid
                || !Player.Instance.HeldPickup.AphidEntity.State.Is(Aphid.StateEnum.Idle))
            {
                SoundManager.CreateSound("ui/button_fail");
                return;
            }

            SelectedAphid = Player.Instance.HeldPickup.AphidEntity;
            Player.Instance.DropNoAnim(false);
            SelectedAphid.SetState(Aphid.StateEnum.Play, new AphidActions.PlayState.PlayArgs(this, IsInterruptable));
        }
        else if (IsInterruptable)
            SelectedAphid.SetState(Aphid.StateEnum.Idle);
    }
}
