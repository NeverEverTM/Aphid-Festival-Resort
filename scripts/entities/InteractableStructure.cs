using Godot;
using System;

public abstract partial class InteractableStructure : Sprite2D, SaveSystem.IGenericDataModule
{
    [ExportGroup("Essentials")]
    [Export(PropertyHint.Enum)] private Aphid.StateEnum StateToGo;
    [Export] private Marker2D restingPosition;
    [Export] private AnimationPlayer animator;
    [Export] private InteractableArea2D interactArea;

    public bool IsAphidAvailable { get; protected set; }
    public Aphid MyAphid { get; protected set; }
    public Guid MyAphidID { get; protected set; }
    private bool walking;

    public void OnInteract()
    {
        if (!IsAphidAvailable)
        {
            if (Interact_PlayerCantGive())
                SoundManager.CreateSound("ui/button_fail");
            else
            {
                Aphid _aphid = Player.Instance.HeldItem.Entity_Aphid;
                Player.Instance.DropNoAnim(false, false);
                SelectAphid(_aphid);
            }
        }
        else
        {
            if (Interact_PlayerCantTake())
                SoundManager.CreateSound("ui/button_fail");
            else
            {
                Aphid _aphid = MyAphid;
                DropAphid();
                Player.Instance.PickupNoAnim(_aphid);
            }
        }
    }
    public virtual bool Interact_PlayerCantTake()
    {
        return MyAphid == null || MyAphidID == Guid.Empty;
    }
    public virtual bool Interact_PlayerCantGive()
    {
        bool _noAphidInHand = Player.Instance.HeldItem == null || !Player.Instance.HeldItem.IsAphid;

        if (_noAphidInHand)
            GlobalManager.EmitParticles("need_aphid", GlobalPosition);

        return _noAphidInHand;
    }
    
    public void SelectAphid(Guid _guid, Aphid _aphid)
    {
        MyAphidID = _guid;
        MyAphid = _aphid;
        IsAphidAvailable = true;

        // prevent further interaction with the aphid
        MyAphid.RemoveMeta(StringNames.TagMeta);
        MyAphid.SetMeta(StringNames.PickupMeta, false);

        Enter();
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
            Exit();
        }

        MyAphidID = Guid.Empty;
        MyAphid = null;
    }

    // MARK: Animation Event Methods
    public void FlipAphidToTheLeft() =>
            MyAphid.Skin.SetFlipDirection(Vector2.Left);
    public void FlipAphidToTheRight() =>
        MyAphid.Skin.SetFlipDirection(Vector2.Right);
    public void JumpAphid() =>
        MyAphid.Skin.DoJump();
    public void HopAphid() =>
        MyAphid.Skin.DoHop(false);
    public void ToggleWalk()
    {
        walking = !walking;
        MyAphid.Skin.OverrideMovementAnim = walking;
    }
    public void ToggleWalkOn()
    {
        walking = true;
        MyAphid.Skin.OverrideMovementAnim = walking;
    }
    public void ToggleWalkOff()
    {
        walking = false;
        MyAphid.Skin.OverrideMovementAnim = walking;
    }

    // MARK: Processing
    public override void _EnterTree()
    {
        interactArea.OnInteractOnly.Add(OnInteract);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!IsAphidAvailable)
            return;

        if (!IsInstanceValid(MyAphid))
        {
            MyAphid = null;
            DropAphid();
            return;
        }

        if (!MyAphid.State.Is(StateToGo))
        {
            DropAphid();
            return;
        }

        Update((float)delta);
    }

    /// <summary>
    /// Called every physics process tick as long as there is an aphid available.
    /// </summary>
    public virtual void Update(float _delta)
    {
        MyAphid.GlobalPosition = restingPosition.GlobalPosition;

        if (walking)
            MyAphid.Skin.DoWalkAnim();
    }
    /// <summary>
    /// Enters and prepares the aphid for the structure.
    /// </summary>
    public virtual void Enter()
    {
        MyAphid.SetState(StateToGo);
        MyAphid.Skin.SetTo(StringNames.IdleAnim);
        MyAphid.GlobalPosition = restingPosition.GlobalPosition;
        animator.Play("start");
    }
    /// <summary>
    /// Disposes of references and sets the aphid free.
    /// </summary>
    public virtual void Exit()
    {
        walking = false;
        if(MyAphid != null)
        {
            MyAphid.Skin.Position = new();
            MyAphid.Skin.OverrideMovementAnim = false;
            if (MyAphid.Instance.Status.LastActiveState == StateToGo)
                MyAphid.SetState(Aphid.StateEnum.Idle);
        }
        animator.Play("RESET");
    }

    // MARK: Data related
    public virtual void Set(string _data)
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

        if (!CanSet(_guid))
        {
            DropAphid();
            return;
        }

        SelectAphid(_guid);
    }
    public virtual bool CanSet(Guid _guid)
    {
        return GameManager.Aphids.TryGetValue(_guid, out AphidInstance value) && value.Status.Mode == AphidData.EntityStatusType.Active
            && value.Status.LastActiveState == StateToGo;
    }
    public virtual string Get()
    {
        return MyAphidID.ToString();
    }
}
