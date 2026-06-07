using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AphidTraits : Aphid
{
    public static readonly List<ITrait> G_TRAITS =
    [
        new HeavySleeper(),
        new HyperActive(),
        new Affectionate(),
        new Lazy(),
        new PickyEater(),
        new Glutton(),
        new Loyal(),
        new Fertile(),
        new Producer(),
        new FastLearner(),
        new Bioluminescent(),
        new Shy()
    ];

    /// <summary>
    /// Fetches and creates an instance of a trait.
    /// </summary>
    /// <returns>The trait by given ID. Null if no such trait exists.</returns>
    public static ITrait CreateTraitByID(string _id)
    {
        if (!G_TRAITS.Exists(t => t.ID.Equals(_id)))
            return null;
        
        return (ITrait)Activator.CreateInstance(G_TRAITS.Find(t => t.ID.Equals(_id)).GetType());
    }
    /// <summary>
    /// Returns a reference to a trait in the global list.
    /// </summary>
    /// <param name="_id"></param>
    /// <returns></returns>
    public static void GetTraitByID(string _id, out ITrait _trait)
    {
        _trait = null;
        if (!G_TRAITS.Exists(t => t.ID.Equals(_id)))
            return;
        _trait = G_TRAITS.Find(t => t.ID.Equals(_id));
    }
    /// <summary>
    /// Gets a random trait from the global trait list.
    /// </summary>
    /// <returns></returns>
    public static ITrait GetRandomTrait()
    {
        var _trait = G_TRAITS[GlobalManager.RNG.RandiRange(0, G_TRAITS.Count - 1)];
        return (ITrait)Activator.CreateInstance(_trait.GetType());
    }
    /// <summary>
    /// Gets a random trait from the global trait list.
    /// </summary>
    /// <param name="_excludeList">List of traits to exclude from search.</param>
    /// <returns></returns>
    public static ITrait GetRandomTrait(ITrait[] _excludeList)
    {
        var _list = G_TRAITS.Where(t => !_excludeList.Contains(t)).ToArray();
        if (_list.Length == 0)
            return null;

        var _trait = _list[GlobalManager.RNG.RandiRange(0, _list.Length - 1)];
        return (ITrait)Activator.CreateInstance(_trait.GetType());
    }
    /// <summary>
    /// Gets a random trait from the global trait list.
    /// </summary>
    /// <param name="_excludeList">List of traits to exclude from search, based on ID.</param>
    /// <returns></returns>
    public static ITrait GetRandomTrait(List<string> _excludeList)
    {
        var _list = G_TRAITS.Where(t => !_excludeList.Contains(t.ID)).ToArray();
        if (_list.Length == 0)
            return null;

        var _trait = _list[GlobalManager.RNG.RandiRange(0, _list.Length - 1)];
        return (ITrait)Activator.CreateInstance(_trait.GetType());
    }

    public class HeavySleeper : ITrait
    {
        public override string ID => "heavysleeper";
        public override string[] IncompatibleTraits => null;

        public override void OnStart(AphidInstance aphid)
        {
            aphid.BoolFlags[AphidInstance.FlagsEnum.IsHeavySleeper].Value = true;
            aphid.FloatFlags[AphidInstance.FlagsEnum.RestTimeMultiplier].Value += 1;
        }
    }
    public class HyperActive : ITrait
    {
        public override string[] IncompatibleTraits => null;
        public override string ID => "hyperactive";

        public override void OnStart(AphidInstance _aphid)
        {
            _aphid.FloatFlags[AphidInstance.FlagsEnum.IdleTimeMultiplier].Value -= 0.5f;
        }
        public override void OnSpawn(Aphid aphid)
        {
            aphid.ActiveTimers.Add(new LittleWiddleJumpTimer(aphid));
        }

        public class LittleWiddleJumpTimer : CustomBaseTimer<Aphid>
        {
            public LittleWiddleJumpTimer(Aphid Entity) : base(Entity)
            {
                BaseTime = 2;
                TimeLeft = GetTimerTime();
            }
            public override void Finish()
            {
                if (TInstance.State.Type == StateEnum.Idle)
                    TInstance.Skin.DoHop(false);
            }
            public override float GetTimerTime()
            {
                return MISC_RNG.RandfRange(BaseTime, BaseTime * 1.5f);
            }
        }
    }
    public class Affectionate : ITrait
    {
        public override string[] IncompatibleTraits => null;
        public override string ID => "affectionate";

        public override void OnSpawn(Aphid aphid)
        {
            aphid.AreaEvents.Add(new PlayerInteractionTrigger());
        }
        public override void OnPostStateChange(Aphid aphid, StateEnum _previousState)
        {
            if (_previousState == StateEnum.Pet && aphid.Instance.Status.Affection < 90)
                aphid.Instance.AddBondship(1);
        }

        public class PlayerInteractionTrigger : IInteractionEvent
        {
            private double interaction_timer;
            private double last_tick_time = Time.GetUnixTimeFromSystem();

            public void OnTrigger(Aphid _myAphid, Node2D _incomingNode, StringNames.GlobalTags _nodeTag)
            {
                if (_nodeTag != StringNames.GlobalTags.Player)
                    return;

                if (!_myAphid.State.Is(StateEnum.Idle))
                    return;

                // weird timer shenanigans
                interaction_timer -= Time.GetUnixTimeFromSystem() - last_tick_time;
                last_tick_time = Time.GetUnixTimeFromSystem();
                if (interaction_timer > 0)
                    return;
                interaction_timer = 10;

                switch (MISC_RNG.RandiRange(0, 1))
                {
                    case 0:
                        _myAphid.CallTowards(Player.Instance.GlobalPosition);
                        GlobalManager.EmitParticles("heart", _myAphid.GlobalPosition - new Vector2(0, 10), false);
                        break;
                    case 1:
                        _myAphid.Skin.SetFlipDirection(_incomingNode.GlobalPosition - _myAphid.GlobalPosition);
                        SoundManager.CreateSound2D(_myAphid.AudioDynamic_Idle, _myAphid.GlobalPosition, true);
                        break;
                }
            }
        }
    }
    public class Lazy : ITrait
    {
        public override string[] IncompatibleTraits => ["hyperactive"];
        public override string ID => "lazy";

        bool lazy_emote_active = false;

        public override void OnStart(AphidInstance aphid)
        {
            aphid.FloatFlags[AphidInstance.FlagsEnum.IdleTimeMultiplier].Value += 0.5f;
        }

        public override void OnProcess(Aphid aphid, float delta)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;

            if (aphid.MovementDirection.IsEqualApprox(Vector2.Zero))
            {
                if (!lazy_emote_active)
                    GetLazy(aphid);
            }
            else if (lazy_emote_active)
                GetFunky(aphid);

        }
        public override void OnPreStateChange(Aphid _aphid, StateEnum _previousState)
        {
            if (_previousState == StateEnum.Idle && lazy_emote_active)
                GetFunky(_aphid);
        }
        private void GetLazy(Aphid aphid)
        {
            lazy_emote_active = true;
            aphid.Skin.SetLegsSkin("sleep");
            aphid.Skin.Position = new(0, 2);
        }
        private void GetFunky(Aphid aphid)
        {
            lazy_emote_active = false;
            aphid.Skin.SetLegsSkin("idle");
            aphid.Skin.Position = new(0, 0);
        }
    }
    public class PickyEater : ITrait
    {
        public override string ID => "pickyeater";
        public override string[] IncompatibleTraits => null;

        public override void OnStart(AphidInstance aphid)
        {
            aphid.BoolFlags[AphidInstance.FlagsEnum.IsPicky].Value = true;
        }
    }
    public class Glutton : ITrait
    {
        public override string ID => "glutton";
        public override string[] IncompatibleTraits => null;

        public override void OnStart(AphidInstance aphid)
        {
            aphid.BoolFlags[AphidInstance.FlagsEnum.CanOvereat].Value = true;
        }
    }
    public class Loyal : ITrait
    {
        public override string ID => "loyal";
        public override string[] IncompatibleTraits => null;

        public override void OnStart(AphidInstance aphid)
        {
            aphid.Timers.Remove(aphid.Timers.Find((d) => d is BondshipDecay));
        }
    }
    public class Fertile : ITrait
    {
        public override string ID => "fertile";
        public override string[] IncompatibleTraits => null;

        public override void OnStart(AphidInstance aphid)
        {
            aphid.FloatFlags[AphidInstance.FlagsEnum.BreedTimeMultiplier].Value -= 0.1f;
        }
    }
    public class Producer : ITrait
    {
        public override string ID => "producer";
        public override string[] IncompatibleTraits => null;

        public override void OnStart(AphidInstance _aphid)
        {
            _aphid.FloatFlags[AphidInstance.FlagsEnum.HarvestMultiplier].Value += 0.5f;
        }
    }
    public class FastLearner : ITrait
    {
        public override string ID => "fastlearner";
        public static string STATIC_ID => "fastlearner";
        public override string[] IncompatibleTraits => null;

        public override void OnPostStateChange(Aphid _aphid, StateEnum _previousState)
        {
            if (_aphid.State.Is(StateEnum.Train))
                _aphid.Instance.Status.LastTraining.PointBuffs.Add("fastlearner", 1);
        }
    }
    public class Bioluminescent : ITrait
    {
        public override string ID => "bioluminescent";
        public override string[] IncompatibleTraits => null;

        public override void OnSpawn(Aphid _aphid)
        {
            PointLight2D _light = new()
            {
                Texture = ResourceLoader.Load<Texture2D>("uid://b7528xex67ab2"),
                Energy = 0.5f,
                Color = _aphid.Instance.Genes.BodyColor
            };
            _aphid.AddChild(_light);
        }
    }
    public class Shy : ITrait
    {
        public override string ID => "shy";

        public override string[] IncompatibleTraits => null;

        public override void OnSpawn(Aphid _aphid)
        {
            _aphid.AreaEvents.Add(new ShyEvent());
        }

        public class ShyEvent : IInteractionEvent
        {
            public void OnTrigger(Aphid _myAphid, Node2D _incomingNode, StringNames.GlobalTags _nodeTag)
            {
                if (_nodeTag != StringNames.GlobalTags.Aphid || !_myAphid.State.Is(StateEnum.Idle))
                    return;

                Aphid _otherAphid = _incomingNode as Aphid;

                if (_myAphid.Instance.HasRelationship(_otherAphid, out var _relation) && _relation.Level > Relationship.RelationshipLevel.Friend)
                    return;
    
                (_myAphid.ActiveStates.Find(s => s.Type == StateEnum.Idle) as IdleState)
                    .SetIdlePoint(_myAphid, (-(_otherAphid.GlobalPosition - _myAphid.GlobalPosition)) * 100, 0.5f);
            }
        }
    }
    
    public class Unpredictable : ITrait
    {
        public override string ID => "unpredictable";

        public override string[] IncompatibleTraits => null;

    }
    public class LightSleeper : ITrait
    {
        public override string ID => throw new NotImplementedException();

        public override string[] IncompatibleTraits => throw new NotImplementedException();

    }
    public class Clumsy : ITrait
    {
        public override string ID => throw new NotImplementedException();

        public override string[] IncompatibleTraits => throw new NotImplementedException();

    }
    public class Grumpy : ITrait
    {
        public override string ID => throw new NotImplementedException();

        public override string[] IncompatibleTraits => throw new NotImplementedException();

    }
}
