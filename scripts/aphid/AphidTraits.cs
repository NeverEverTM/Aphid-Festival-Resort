using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using static AphidActions;
using static Aphid;

public static class AphidTraits
{
    public static readonly Dictionary<string, Type> G_TRAITS = new()
    {
        { "affectionate", typeof(Affectionate) },
        { "fertile", typeof(Fertile) },
        { "glutton", typeof(Glutton) },
        { "heavysleeper", typeof(HeavySleeper) },
        { "hyperactive", typeof(HyperActive) },
        { "lazy", typeof(Lazy) },
        { "loyal", typeof(Loyal) },
        { "pickyeater", typeof(PickyEater) },
    };
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
        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            if (!aphid.State.Is(StateEnum.Sleep))
                return;

            (aphid.StateArgs as SleepState.SleepArgs).gain_rate++;
            (aphid.StateArgs as SleepState.SleepArgs).heavysleeper = true;
        }
        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }
        public void OnStateChange(Aphid aphid, EventArgs args, StateEnum _previous)
        {
            if (!aphid.State.Is(StateEnum.Sleep))
                return;

            (aphid.StateArgs as SleepState.SleepArgs).gain_rate++;
            (aphid.StateArgs as SleepState.SleepArgs).heavysleeper = true;
        }
        
    }
    public class HyperActive : ITrait
    {
        public string[] IncompatibleTraits => null;
        private float timer;

        public void Activate(Aphid aphid, EventArgs args)
        {
            timer = aphid.rng.RandfRange(2f, 4f);

            if (!aphid.State.Is(StateEnum.Idle))
                return;
            (aphid.StateArgs as IdleState.IdleArgs).decay_rate++;
        }
        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }
        public void OnStateChange(Aphid aphid, EventArgs args, StateEnum _previousState)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;
            (aphid.StateArgs as IdleState.IdleArgs).decay_rate++;
        }
        public void OnProcess(Aphid aphid, EventArgs args, float delta)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;

            if (timer > 0)
                timer -= delta;
            else
            {
                aphid.skin.DoJumpAnim(false);
                timer = aphid.rng.RandfRange(2f, 4f);
            }
        }
    }
    public class Affectionate : ITrait
    {
        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            aphid.TriggerActions.Add(new PlayerInteractionTrigger());
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void OnStateChange(Aphid aphid, EventArgs args, StateEnum _previousState)
        {
            if (_previousState == StateEnum.Pet && aphid.Instance.Status.Affection < 90)
                aphid.Instance.Status.AddBondship(1);
        }

        public class PlayerInteractionTrigger : ITriggerEvent
        {
            public string TriggerID => "player";
            private const float interaction_cd = 6.35f;
            private Timer interaction_timer;

            public void OnTrigger(Aphid _aphid, Node2D _node, EventArgs _args)
            {
                if (!GameManager.IsInstanceValid(interaction_timer))
                {
                    interaction_timer = new();
                    _aphid.AddChild(interaction_timer);
                    interaction_timer.Start();
                }
                if (!_aphid.State.Is(StateEnum.Idle) || interaction_timer.TimeLeft > 0.1f)
                    return;

                interaction_timer.Start(interaction_cd);
                switch (_aphid.rng.RandiRange(0, 1))
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
        }
    }
    public class Lazy : ITrait
    {
        public string[] IncompatibleTraits => null;
        bool lazy_emote_active = false;

        public void Activate(Aphid aphid, EventArgs args)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;
            (aphid.StateArgs as IdleState.IdleArgs).decay_rate -= 0.5f;
        }
        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }
        public void OnStateChange(Aphid aphid, EventArgs args, StateEnum _previousState)
        {
            if (!aphid.State.Is(StateEnum.Idle))
            {
                if (lazy_emote_active)
                {
                    lazy_emote_active = false;
                    aphid.skin.SetLegsSkin("idle");
                    aphid.skin.Position = new(0, 0);
                }
                return;
            }
            (aphid.StateArgs as IdleState.IdleArgs).decay_rate -= 0.5f;
        }

        public void OnProcess(Aphid aphid, EventArgs args, float delta)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;

            IdleState.IdleArgs _args = args as IdleState.IdleArgs;

            if (_args.stand_time > 0)
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
        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            (aphid.ActiveStates[StateEnum.Hungry] as HungryState).only_favorites = true;
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }
    }
    public class Glutton : ITrait
    {
        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            (aphid.ActiveStates[StateEnum.Hungry] as HungryState).allow_overconsume = true;
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }
    }
    public class Loyal : ITrait
    {
        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            aphid.DecayActions.Remove(aphid.DecayActions.Find((d) => d is BondshipDecay));
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }
    }
    public class Fertile : ITrait
    {
        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void OnProcess(Aphid aphid, EventArgs _args, float _delta)
        {
            if (!aphid.Instance.Status.IsAdult)
                return;
            
            aphid.Instance.Status.BreedBuildup += _delta;
        }
    }
}
