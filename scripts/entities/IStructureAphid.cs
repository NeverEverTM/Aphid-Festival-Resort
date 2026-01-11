using System;

public interface IStructureAphid
{
    public void Enter();
    public void Process(float delta);
    public void Exit();

    public Aphid SelectedAphid { get; set; }
    public Guid SelectedAphidID { get; set; }

    public virtual void InteractWithAnAphid()
    {
        if (SelectedAphid == null)
        {
            if (Player.Instance.HeldPickup.Entity == null || !Player.Instance.HeldPickup.IsAphid
                || !Player.Instance.HeldPickup.Entity_Aphid.State.Is(Aphid.StateEnum.Idle))
            {
                // Particle Effect
                SoundManager.CreateSound("ui/button_fail");
                return;
            }

            SelectedAphid = Player.Instance.HeldPickup.Entity_Aphid;
            SelectedAphidID = Player.Instance.HeldPickup.Entity_Aphid.Instance.GUID;
            Player.Instance.DropNoAnim(false);
            Enter();
        }
        else
            Exit();
    }
}