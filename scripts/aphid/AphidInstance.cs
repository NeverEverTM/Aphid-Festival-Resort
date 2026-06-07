using System;
using System.Collections.Generic;
using Godot;
using static Aphid;
using static AphidData;

/// <summary>
/// Contains all data related to a single aphid, plus some functions to control its behaviour during active/pasive time.
/// </summary>
public class AphidInstance
{
    // MARK: SAVEDATA VARIABLES
    /// <summary>
    /// The status data for the aphid. Reflects current mood.
    /// </summary>
    public Status Status { get; set; } = new();
    /// <summary>
    /// The genes data for the aphid. Contains important genetical information.
    /// </summary>
    public Genes Genes { get; set; } = new();
    /// <summary>
    /// ID of the aphid
    /// </summary>
    public Guid GUID { get; set; }
    /// <summary>
    /// ID of the aphid
    /// </summary>
    public string ID { get => GUID.ToString(); }

    public AphidInstance(Guid GUID)
    {
        this.GUID = GUID;
        Start();
    }

    // RUNTIME VARIABLES
    /// <summary>
    /// The current active entity in the scene, if the scene doesn't spawn them then it will return null/invalid.
    /// </summary>
    public Aphid Entity;
    /// <summary>
	/// This is the list for active traits during runtime, to add/remove a trait permanently use the Genes.Traits list instead
	/// </summary>
	public readonly List<ITrait> Traits = [];

    /// <summary>
    /// List of timers that run on the background at all times, including when the aphid is not loaded in as an entity.
    /// </summary>
    public readonly List<CustomBaseTimer<AphidInstance>> Timers = [];
    public enum FlagsEnum { IdleTimeMultiplier, RestTimeMultiplier, BreedTimeMultiplier, HarvestMultiplier, IsHeavySleeper, IsPicky, CanOvereat }
    public readonly Dictionary<FlagsEnum, FloatFlag> FloatFlags = new()
    {
        { FlagsEnum.IdleTimeMultiplier, new() },
        { FlagsEnum.RestTimeMultiplier, new() },
        { FlagsEnum.BreedTimeMultiplier, new() },
        { FlagsEnum.HarvestMultiplier, new() },
    };
    public readonly Dictionary<FlagsEnum, BoolFlag> BoolFlags = new()
    {
        { FlagsEnum.IsHeavySleeper, new() },
        { FlagsEnum.IsPicky, new() },
        { FlagsEnum.CanOvereat, new() }
    };

    public void Start()
    {
        Timers.Add(new HungerDecay(this));
        Timers.Add(new ThirstDecay(this));
        Timers.Add(new RestDecay(this));
        Timers.Add(new RestGain(this));
        Timers.Add(new AffectionDecay(this));
        Timers.Add(new LifetimeDecay(this));
        Timers.Add(new BreedTimer(this));
        Timers.Add(new HarvestTimer(this));

        for (int i = 0; i < Genes.Traits.Count; i++)
        {
            Traits.Add(AphidTraits.CreateTraitByID(Genes.Traits[i]));
            Traits[i].OnStart(this);
        }

        switch (Status.LastActiveState) // todo, add a way for states to implement passive initialization
        {
            case StateEnum.Sleep:
                Timers.Find(t => t is RestGain).Start();
            break;
            case StateEnum.Train:
                Timers.Add(new TrainState.TrainTimer(this));
            break;
        }
    }

    public void Update(float _timeDifference)
    {
        for (int i = 0; i < Timers.Count; i++)
            Timers[i].Update(_timeDifference);

        for (int i = 0; i < Traits.Count; i++)
            Traits[i].OnBackgroundProcess(this, _timeDifference);
    }

    // MARK: Instance Interaction
    public virtual void AddHunger(float _amount) => Status.AddHunger(_amount);
    public virtual void AddThirst(float _amount) => Status.AddThirst(_amount);
    public virtual void AddRest(float _amount) => Status.AddRest(_amount);
    public virtual void AddAffection(int _amount) => Status.AddAffection(_amount);
    public virtual void AddBondship(int _amount) => Status.AddBondship(_amount);

    /// <summary>
    /// Sets the aphid entity state, disposes the reference to its entity if is not active.
    /// </summary>
    /// <param name="_nextMode"></param>
    public void EnterMode(EntityStatusType _nextMode)
    {
        if (Status.Mode == _nextMode)
            return;

        Status.Mode = _nextMode;
        if (Status.Mode != EntityStatusType.Active)
            Entity = null;
    }
    public void SetState(StateEnum _state)
    {
        if (GodotObject.IsInstanceValid(Entity))
            Entity.SetState(_state);
        else
        {
            if (Status.LastActiveState != StateEnum.Breed) // avoid denying breed oportunities when passive
                Status.LastActiveState = _state;
        }
    }
    public bool StateIs(StateEnum _state)
    {
        if (GodotObject.IsInstanceValid(Entity))
            return Entity.State.Is(_state);
        else
            return Status.LastActiveState == _state;
    }
    public void AddRelationship(Aphid _aphid, Relationship _relationship = null)
    {
        if (_aphid.Instance.GUID.Equals(GUID))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidRelationship: This is our aphid!");
            return;
        }
        if (Genes.Relationships.ContainsKey(_aphid.Instance.GUID))
            return;
        _relationship ??= new(_aphid.Instance.GUID, Relationship.RelationshipLevel.Acquaintance);
        Genes.Relationships.Add(_aphid.Instance.GUID, _relationship);
    }
    public void AddRelationship(AphidInstance _aphid, Relationship _relationship = null)
    {
        if (_aphid.GUID.Equals(GUID))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidRelationship: This is our aphid!");
            return;
        }
        if (Genes.Relationships.ContainsKey(_aphid.GUID))
            return;
        _relationship ??= new(_aphid.GUID, Aphid.Relationship.RelationshipLevel.Acquaintance);
        Genes.Relationships.Add(_aphid.GUID, _relationship);
    }
    public bool HasRelationship(Aphid _aphid, out Relationship _relationship)
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
    public bool HasRelationship(AphidInstance _aphid, out Relationship _relationship)
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
    public Relationship GetRelationship(Guid _guid) =>
        Genes.Relationships[_guid];
}