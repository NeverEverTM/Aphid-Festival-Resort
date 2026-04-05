using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using static AphidActions;
using static AphidData;

public partial class AphidTraits : Aphid
{
    public interface ITrait
    {
        public string ID { get; }
        public string[] IncompatibleTraits { get; }

        /// <summary>
        /// Called when an active aphid is instantiated.
        /// </summary>
		public virtual void OnEnter(Aphid _aphid)
        {
            return;
        }
        /// <summary>
        /// Called when an aphid changes state.
        /// </summary>
		public virtual void OnStateChange(Aphid _aphid, StateEnum _previousState)
        {
            return;
        }
        /// <summary>
        /// Process method. Must be called manually by aphid.
        /// </summary>
		public virtual void OnProcess(Aphid _aphid, float _delta)
        {
            return;
        }

        public bool IsIncompatibleWith(string _ID)
        {
            if (IncompatibleTraits == null)
                return false;
            return IncompatibleTraits.Contains(_ID);
        }
    }
    /// <summary>
    /// An optional version of ITrait that allows changing behaviours on passive aphids.
    /// </summary>
    public interface ITraitPassive
    {
        /// <summary>
        /// Called when an active aphid is instantiated.
        /// </summary>
		public virtual void OnEnter(AphidPassive _aphid)
        {
            return;
        }
        /// <summary>
        /// Called when an aphid changes state.
        /// </summary>
		public virtual void OnStateChange(AphidPassive _aphid, StateEnum _previousState)
        {
            return;
        }
        /// <summary>
        /// Process method. Must be called manually by aphid.
        /// </summary>
		public virtual void OnProcess(AphidPassive _aphid, float _delta)
        {
            return;
        }
    }

    internal static readonly Dictionary<string, Type> G_TRAITS = [];
    public static readonly List<ITrait> TRAITS =
    [
        new HeavySleeper(),
        new HyperActive(),
        new Affectionate(),
        new Lazy(),
        new PickyEater(),
        new Glutton(),
        new Loyal(),
        new Fertile(),
        new MoneyMaker(),
        new FastLearner()
    ];

    public static ITrait GetTraitByName(string _name)
    {
        Type _type = G_TRAITS[_name];
        return (ITrait)Activator.CreateInstance(_type);
    }
    public static ITrait GetRandomTrait(out string _name)
    {
        KeyValuePair<string, Type> _type = G_TRAITS.ElementAt(GlobalManager.RNG.RandiRange(0, G_TRAITS.Count - 1));
        _name = _type.Key;
        return (ITrait)Activator.CreateInstance(_type.Value);
    }

    public class HeavySleeper : ITrait
    {
        public string ID => "heavysleeper";
        public string[] IncompatibleTraits => null;

        public void OnEnter(Aphid aphid)
        {
            if (!aphid.State.Is(StateEnum.Sleep))
                return;

            aphid.BoolFlags[BoolFlagsEnum.IsHeavySleeper] = true;
            aphid.ValueFlags[ValueFlagsEnum.RestTimeMultitplier] += 1;
        }
    }
    public class HyperActive : ITrait
    {
        public string[] IncompatibleTraits => null;
        public string ID => "hyperactive";

        private LittleWiddleJumpTimer little_widdle_jump_timer;

        public void OnEnter(Aphid aphid)
        {
            little_widdle_jump_timer = new(2);
            aphid.Timers.Add(little_widdle_jump_timer);

            aphid.ValueFlags[ValueFlagsEnum.IdleTimeMultiplier] -= 0.5f;
        }

        public void OnProcess(Aphid aphid, float _delta)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;
        }

        public class LittleWiddleJumpTimer(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, TimeLeft, OneShot, autostart)
        {
            public override void Finish(Aphid aphid)
            {
                if (aphid.State.Type == StateEnum.Idle)
                    aphid.skin.DoHop(false);
            }
            public override float GetTimerTime()
            {
                return MISC_RNG.RandfRange(BaseTime, BaseTime * 1.5f);
            }
        }
    }
    public class Affectionate : ITrait
    {
        public string[] IncompatibleTraits => null;
        public string ID => "affectionate";

        public void OnEnter(Aphid aphid)
        {
            aphid.AreaEvents.Add(new PlayerInteractionTrigger());
        }
        public void OnStateChange(Aphid aphid, StateEnum _previousState)
        {
            if (_previousState == StateEnum.Pet && aphid.Instance.Status.Affection < 90)
                aphid.Instance.AddBondship(1);
        }

        public class PlayerInteractionTrigger : IAreaEvent
        {
            public StringNames.GlobalTags Tag => StringNames.GlobalTags.Player;
            private const float interaction_cd = 6.35f;
            private Timer interaction_timer;

            public void OnNodeEntered(Aphid _aphid, Node2D _node)
            {
                if (!IsInstanceValid(interaction_timer))
                {
                    interaction_timer = new();
                    _aphid.AddChild(interaction_timer);
                }

                if (!_aphid.State.Is(StateEnum.Idle) || !interaction_timer.IsStopped() && interaction_timer.TimeLeft > 0.01)
                    return;

                interaction_timer.Start(interaction_cd);
                switch (MISC_RNG.RandiRange(0, 1))
                {
                    case 0:
                        _aphid.CallTowards(Player.Instance.GlobalPosition);
                        GlobalManager.EmitParticles("heart", _aphid.GlobalPosition - new Vector2(0, 10), false);
                        break;
                    case 1:
                        _aphid.skin.SetFlipDirection(_node.GlobalPosition - _aphid.GlobalPosition);
                        SoundManager.CreateSound2D(_aphid.AudioDynamic_Idle, _aphid.GlobalPosition, true);
                        break;
                }
            }
            public void OnNodeStay(Aphid _aphid, Node2D _node)
            {
                return;
            }
            public void OnNodeExited(Aphid _aphid, Node2D _node)
            {
                return;
            }
        }
    }
    public class Lazy : ITrait
    {
        public string[] IncompatibleTraits => ["hyperactive"];
        public string ID => "lazy";

        bool lazy_emote_active = false;

        public void OnEnter(Aphid aphid)
        {
            aphid.ValueFlags[ValueFlagsEnum.IdleTimeMultiplier] += 0.5f;
        }

        public void OnProcess(Aphid aphid, float delta)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;

            if (aphid.MovementDirection.IsEqualApprox(Vector2.Zero))
            {
                if (!lazy_emote_active)
                {
                    lazy_emote_active = true;
                    aphid.skin.SetLegsSkin("sleep");
                    aphid.skin.Position = new(0, 2);
                }
            }
            else
            {
                if (lazy_emote_active)
                {
                    lazy_emote_active = false;
                    aphid.skin.SetLegsSkin("idle");
                    aphid.skin.Position = new(0, 0);
                }
            }
        }
    }
    public class PickyEater : ITrait
    {
        public string ID => "pickyeater";
        public string[] IncompatibleTraits => null;

        public void OnEnter(Aphid aphid)
        {
            aphid.BoolFlags[BoolFlagsEnum.IsPicky] = true;
        }
    }
    public class Glutton : ITrait
    {
        public string ID => "glutton";
        public string[] IncompatibleTraits => null;

        public void OnEnter(Aphid aphid)
        {
            aphid.BoolFlags[BoolFlagsEnum.CanOvereat] = true;
        }
    }
    public class Loyal : ITrait
    {
        public string ID => "loyal";
        public string[] IncompatibleTraits => null;

        public void OnEnter(Aphid aphid)
        {
            aphid.Timers.Remove(aphid.Timers.Find((d) => d is BondshipDecay));
        }
    }
    public class Fertile : ITrait
    {
        public string ID => "fertile";
        public string[] IncompatibleTraits => null;

        public void OnEnter(Aphid aphid)
        {
            aphid.ValueFlags[ValueFlagsEnum.BreedTimeMultiplier] -= 0.1f;
        }
    }
    public class MoneyMaker : ITrait
    {
        public string ID => "moneymaker";
        public string[] IncompatibleTraits => null;

        public void OnEnter(Aphid _aphid)
        {
            return;
        }
    }
    public class FastLearner : ITrait
    {
        public string ID => "fastlearner";
        public string[] IncompatibleTraits => null;

        public void OnEnter(Aphid _aphid)
        {
            return;
        }
    }

    // trait idea list quick sketch final_v2.0
    // bioluminiscence
    // Shy
    // Weird
    // lightsleeper
    // mad/Grumpy

}
