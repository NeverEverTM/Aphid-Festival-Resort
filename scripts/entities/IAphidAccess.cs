using System;

/// <summary>
/// Allows a structure to select an aphid as part of their interaction, and hold it between rooms.
/// </summary>
public interface IAphidAccess
{
    public bool IsAphidAvailable { get; set; }
    public Aphid MyAphid { get; set; }
    public Guid MyAphidID { get; set; }

    public void OnInteractOnly()
    {
        if (!IsAphidAvailable)
        {
            if (Player.Instance.HeldItem.Entity == null || !Player.Instance.HeldItem.IsAphid
                || !Player.Instance.HeldItem.Entity_Aphid.State.Is(Aphid.StateEnum.Idle))
            {
                // Particle Effect
                SoundManager.CreateSound("ui/button_fail");
            }
            else
                TakeAphidFromPlayer();
        }
        else
        {
            if (MyAphid == null || MyAphidID == Guid.Empty)
                SoundManager.CreateSound("ui/button_fail");
            else
                GiveAphidToPlayer();
        }
    }
    public void TakeAphidFromPlayer()
    {
        MyAphid = Player.Instance.HeldItem.Entity_Aphid;
        MyAphidID = Player.Instance.HeldItem.Entity_Aphid.Instance.GUID;
        Player.Instance.DropNoAnim(false, false);
        IsAphidAvailable = true;
        Enter();
    }
    public void GiveAphidToPlayer()
    {
        IsAphidAvailable = false;
        Exit();
        Player.Instance.PickupNoAnim(MyAphid);

        MyAphid = null;
        MyAphidID = Guid.Empty;
    }

    public void SetAphid(string _data)
    {
        MyAphidID = new Guid(_data);
        if (MyAphidID != Guid.Empty && GameManager.Aphids[MyAphidID].Status.Mode == AphidData.EntityStatusType.Active)
        {
            MyAphid = GameManager.Aphids[MyAphidID].Entity; 
            Enter();
        }
    }
    public string GetAphid()
    {
        return MyAphidID.ToString();
    }

    public void Enter();
    public void Exit();
}