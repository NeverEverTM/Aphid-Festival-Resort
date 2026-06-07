using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static AphidData;
// chance 1-((0.95)^(3))

public partial class Aphid : CharacterBody2D
{
    public class Relationship(Guid Aphid, Relationship.RelationshipLevel Level = Relationship.RelationshipLevel.Stranger, sbyte Total = 0, bool Locked = false)
    {
        public enum RelationshipLevel
        {
            Parent = -8, Stranger = -7,
            Enemy = -2, Hated, Acquaintance, Friend, BestFriend, Lover
        }

        /// <summary>
        /// GUID key of the aphid in this relationship.
        /// </summary>
        public Guid Aphid { get; set; } = Aphid;
        public RelationshipLevel Level { get; set; } = Level;
        public sbyte Total { get; set; } = Total;
        /// <summary>
        /// Locks current relationship level so it cannot change even when the relationship goes up or down.
        /// </summary>
        public bool Locked { get; set; } = Locked;

        public virtual void AddToTotal(sbyte _points)
        {
            Total += _points;

            if (Locked)
                return;

            Level = (RelationshipLevel)Mathf.Clamp(Total / 30, (int)RelationshipLevel.Enemy, (int)RelationshipLevel.Lover);
        }

        public bool Equals(Aphid _aphid) => Aphid.Equals(_aphid.Instance.GUID);
    }
    public class Skill(string Name)
    {
        public string Name { get; set; } = Name;
        public int Points { get { return points; } set { points = Mathf.Clamp(value, 0, 10); } }
        private int points;
        public int Level { get; set; }

        public class SkillArgs
        {
            public int CurrentLevel;
            public int NextLevel;
            public int LevelGain;
        }
        protected Dictionary<SkillEvents, List<Action<SkillArgs>>> SkillEventsList = new()
        {
            { SkillEvents.OnLevelUp, new() },
            { SkillEvents.OnLevelDown, new() },
            { SkillEvents.OnLevelChange, new() },
            { SkillEvents.OnPointChange, new() },
        };
        public enum SkillEvents { OnLevelUp, OnLevelDown, OnLevelChange, OnPointChange }
        public void AddEventListener(Action<SkillArgs> _action, SkillEvents _event) =>
            SkillEventsList[_event].Add(_action);
        public void RemoveEventListener(Action<SkillArgs> _action, SkillEvents _event) =>
            SkillEventsList[_event].Remove(_action);

        public virtual void GivePoints(int _points)
        {
            points += _points;
            int _lastLevel = Level;

            if (points > 10)
            {
                if (Level >= SKILL_LEVEL_CAP)
                    points = 10;
                else while (points > 10)
                {
                    points -= 10;
                    GiveLevel(1);
                }
            }
            else if (points < 0)
            {
                if (Level <= 0)
                {
                    Level = 0;
                    points = 0;
                }
                else while (points < 0)
                {
                    points += 10;
                    GiveLevel(-1);
                }
            }
            SkillArgs _args = new()
            {
                CurrentLevel = _lastLevel,
                NextLevel = Level,
                LevelGain = Level - _lastLevel
            };

            GlobalManager.Utils.InvokeEventListeners(SkillEventsList[SkillEvents.OnPointChange], _args);
        }
        public virtual void GiveLevel(int _level)
        {
            int _nextLevel = Level + _level;

            SkillArgs _args = new()
            {
                CurrentLevel = Level,
                NextLevel = _nextLevel,
                LevelGain = _level
            };

            if (_nextLevel < Level)
                GlobalManager.Utils.InvokeEventListeners(SkillEventsList[SkillEvents.OnLevelDown], _args);

            if (_nextLevel > Level)
                GlobalManager.Utils.InvokeEventListeners(SkillEventsList[SkillEvents.OnLevelUp], _args);

            GlobalManager.Utils.InvokeEventListeners(SkillEventsList[SkillEvents.OnLevelChange], _args);
            Level = _nextLevel;
        }
    }
    public class Flag
    {

    }
    public class FloatFlag(float Value = 1) : Flag
    {
        public float Value { get; set; } = Value;
    }
    public class BoolFlag(bool Value = false) : Flag
    {
        public bool Value { get; set; } = Value;
    }

    public interface IState
    {
        public StateEnum Type { get; }
        /// <summary>
        /// States that can/cannot transition into it. Dictated by TransitionToAnything.
        /// </summary>
        public StateEnum[] TransitionList { get; }
        /// <summary>
        /// Makes the TransitionList into either a blacklist(true) or a whitelist(false).
        /// </summary>
        public bool TransitionToAnything { get; }
        /// <summary>
        /// Dictates if this can be the starting state of an aphid (ex. to resume a previous action). If not, this state will be discarded and we will default to idle instead.
        /// </summary>
        public bool CanBeStartingState { get; }

        /// <summary>
        /// Initialies a state during startup.
        /// </summary>
        public void Awake(Aphid aphid);

        /// <param name="_previous">Last state we were in</param>
        /// <param name="args">Special args that the aphid can pass when setting the state. Empty EventArgs by default.</param>
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _specialArgs);
        public void Exit(Aphid aphid, StateEnum _next);
        public void Process(Aphid aphid, float delta);

        /// <summary>
        /// Checks if the type of this state equals the given one.
        /// </summary>
        public bool Is(StateEnum _state) => Type.Equals(_state);
        public bool CanTransitionInto(StateEnum _state)
        {
            if (TransitionList != null)
            {
                if (TransitionList.Contains(_state))
                    return !TransitionToAnything;
                else
                    return TransitionToAnything;
            }

            return TransitionToAnything;
        }
    }
    public abstract class ITrait
    {
        public abstract string ID { get; }
        public abstract string[] IncompatibleTraits { get; }

        /// <summary>
        /// Call that initializes a trait on first load. It will always be called before OnSpawn.
        /// </summary>
        public virtual void OnStart(AphidInstance _aphid)
        {
            return;
        }
        /// <summary>
        /// Called when an aphid entity is spawned.
        /// </summary>
		public virtual void OnSpawn(Aphid _aphid)
        {
            return;
        }

        /// <summary>
        /// Called when an aphid changes state, before the state is initialized.
        /// </summary>
		public virtual void OnPreStateChange(Aphid _aphid, StateEnum _previousState)
        {
            return;
        }
        /// <summary>
        /// Called when an aphid changes state, after the state has been initialized.
        /// </summary>
		public virtual void OnPostStateChange(Aphid _aphid, StateEnum _previousState)
        {
            return;
        }
        /// <summary>
        /// Process method called every frame. Only runs when the aphid entity exists.
        /// </summary>
		public virtual void OnProcess(Aphid _aphid, float _delta)
        {
            return;
        }
        /// <summary>
        /// Process method called every frame. Only runs when the aphid entity does not exist.
        /// </summary>
        public virtual void OnBackgroundProcess(AphidInstance _aphid, float _delta)
        {
            return;
        }

        /// <summary>
        /// Checks if the given trait is incompatible with this one.
        /// </summary>
        public virtual bool IsIncompatibleWith(ITrait _trait)
        {
            if (IncompatibleTraits == null)
                return false;
            return IncompatibleTraits.Contains(_trait.ID);
        }
        /// <summary>
        /// Checks if the given trait is incompatible with this one.
        /// </summary>
        public virtual bool IsIncompatibleWith(string _ID)
        {
            if (IncompatibleTraits == null)
                return false;
            return IncompatibleTraits.Contains(_ID);
        }
        /// <summary>
        /// Checks if the trait is incompatible with another trait in a list and visceversa.
        /// </summary>
        /// <param name="_list">The list of traits to check</param>
        public virtual bool IsIncompatibleWith(ITrait[] _list)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                if (IsIncompatibleWith(_list[i].ID) || _list[i].IsIncompatibleWith(ID))
                    return true;    
            }
            return false;
        }
        /// <summary>
        /// Checks if the trait is incompatible with another trait in a list and visceversa, based on ID.
        /// </summary>
        /// <param name="_list">The list of traits to check</param>
        public virtual bool IsIncompatibleWith(string[] _list)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                if (IsIncompatibleWith(_list[i]) || AphidTraits.CreateTraitByID(_list[i]).IsIncompatibleWith(ID))
                    return true;    
            }
            return false;
        }
    }
    /// <summary>
    /// An event interface that fires up when a node enters/exits an aphid's interaction area.
    /// </summary>
    public interface IInteractionEvent
    {
        public void OnTrigger(Aphid _myAphid, Node2D _incomingNode, StringNames.GlobalTags _nodeTag);
    }
}
