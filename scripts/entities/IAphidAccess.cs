using System;
using Godot;

/// <summary>
/// Allows a structure to select an aphid as part of their interaction, and hold it between rooms.
/// </summary>
public interface IAphidAccess
{
    public Node2D AphidAccess_Owner { get; }
    public bool IsAphidAvailable { get; set; }
    public Aphid MyAphid { get; set; }
    public Guid MyAphidID { get; set; }

    public void OnInteractOnly()
    {
        if (!IsAphidAvailable)
        {
            if (Player.Instance.HeldItem == null || !Player.Instance.HeldItem.IsAphid
                || !Player.Instance.HeldItem.Entity_Aphid.State.Is(Aphid.StateEnum.Idle))
            {
                GlobalManager.EmitParticles("need_aphid", AphidAccess_Owner.GlobalPosition);
                SoundManager.CreateSound("ui/button_fail");
            }
            else
            {
                Aphid _aphid = Player.Instance.HeldItem.Entity_Aphid;
                Player.Instance.DropNoAnim(false, false);
                SelectAphid(_aphid);
                Enter();
            }
        }
        else
        {
            if (MyAphid == null || MyAphidID == Guid.Empty)
                SoundManager.CreateSound("ui/button_fail");
            else
            {
                Aphid _aphid = MyAphid;
                Exit();
                DropAphid();
                Player.Instance.PickupNoAnim(_aphid);
            }
        }
    }
    public void SelectAphid(Guid _guid, Aphid _aphid)
    {
        MyAphidID = _guid;
        MyAphid = _aphid;
        IsAphidAvailable = true;

        // prevent further interaction with the aphid
        MyAphid.RemoveMeta(StringNames.TagMeta);
        MyAphid.SetMeta(StringNames.PickupMeta, false);
    }
    public void SelectAphid(Guid _guid) =>
        SelectAphid(_guid, GameManager.Aphids[_guid].Entity);
    public void SelectAphid(Aphid _aphid) =>
        SelectAphid(_aphid.Instance.GUID, _aphid);
    public void DropAphid()
    {
        IsAphidAvailable = false;
        if (MyAphid != null)
        {
            MyAphid.SetMeta(StringNames.TagMeta, (int)MyAphid.Tag);
            MyAphid.SetMeta(StringNames.PickupMeta, true);
        }
        MyAphidID = Guid.Empty;
        MyAphid = null;
    }

    public void SetAphid(string _data)
    {
        if (string.IsNullOrWhiteSpace(_data))
        {
            DropAphid();
            return;
        }

        Guid _guid = new(_data);
        if (_guid == Guid.Empty)
        {
            DropAphid();
            return;
        }

        if (!GameManager.Aphids.TryGetValue(_guid, out AphidInstance value) || value.Status.Mode != AphidData.EntityStatusType.Active)
        {
            DropAphid();
            return;
        }

        SelectAphid(_guid);
        Enter();
    }
    public string GetAphid()
    {
        return MyAphidID.ToString();
    }

    public void Enter();
    public void Exit();
}