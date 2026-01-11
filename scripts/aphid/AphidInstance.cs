using System;
using Godot;

public class AphidInstance
{
    /// <summary>
    /// The status data for the aphid. Reflects current mood.
    /// </summary>
    public AphidData.Status Status { get; set; } = new();
    /// <summary>
    /// The genes data for the aphid. Contains important genetical information.
    /// </summary>
    public AphidData.Genes Genes { get; set; } = new();

    /// <summary>
    /// The current active entity in the scene. (May not exist at all)
    /// </summary>
    public Aphid Entity;
    /// <summary>
    /// The passive entity, acts as acting aphid while off-screen.
    /// </summary>
    public AphidPassive PassiveEntity;

    /// <summary>
    /// Unique Key of the aphid.
    /// </summary>
    public Guid GUID { get; set; }
    /// <summary>
    /// The GUID, as a string variable.
    /// </summary>
    public string ID { get; set; }

    public AphidInstance(Guid GUID)
    {
        this.GUID = GUID;
        ID = GUID.ToString();
    }

    public virtual void AddHunger(float _amount) =>
            Status.Hunger = Math.Clamp(Status.Hunger + _amount, 0, 100);
    public virtual void AddThirst(float _amount) =>
            Status.Thirst = Math.Clamp(Status.Thirst + _amount, 0, 100);
    public virtual void AddTiredness(float _amount) =>
        Status.Tiredness = Math.Clamp(Status.Tiredness + _amount, 0, 100);
    public virtual void AddBondship(int _amount) =>
        Status.Bondship = Math.Clamp(Status.Bondship + _amount, 0, 100);
    public virtual void AddAffection(int _amount) =>
        Status.Affection = Math.Clamp(Status.Affection + _amount, 0, 100);

    public virtual void SetState(Aphid.StateEnum _state)
    {
        if (GodotObject.IsInstanceValid(Entity))
            Entity.SetState(_state);
        else
            Status.LastActiveState = _state;
    }
}