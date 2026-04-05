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
    public void AddRelationship(Aphid _aphid, AphidActions.Relationship _relationship = null)
    {
        if (_aphid.Instance.GUID.Equals(GUID))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidRelationship: This is our aphid!");
            return;
        }
        if (Genes.Relationships.ContainsKey(_aphid.Instance.GUID))
            return;
        _relationship ??= new(_aphid.Instance.GUID, AphidActions.Relationship.RelationshipLevel.Acquaintance);
        Genes.Relationships.Add(_aphid.Instance.GUID, _relationship);
    }
    public void AddRelationship(AphidInstance _aphid, AphidActions.Relationship _relationship = null)
    {
        if (_aphid.GUID.Equals(GUID))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidRelationship: This is our aphid!");
            return;
        }
        if (Genes.Relationships.ContainsKey(_aphid.GUID))
            return;
        _relationship ??= new(_aphid.GUID, AphidActions.Relationship.RelationshipLevel.Acquaintance);
        Genes.Relationships.Add(_aphid.GUID, _relationship);
    }
    public bool HasRelationship(Aphid _aphid, out AphidActions.Relationship _relationship)
    {
        if (_aphid.Instance.GUID.Equals(GUID))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidRelationship: This is our aphid!");
            _relationship = null;
            return false;
        }
        bool _hasValue = Genes.Relationships.TryGetValue(_aphid.Instance.GUID, out _relationship);
        return _hasValue;
    }
    public bool HasRelationship(AphidInstance _aphid, out AphidActions.Relationship _relationship)
    {
        if (_aphid.GUID.Equals(GUID))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidRelationship: This is our aphid!");
            _relationship = null;
            return false;
        }
        bool _hasValue = Genes.Relationships.TryGetValue(_aphid.GUID, out _relationship);
        return _hasValue;
    }
}