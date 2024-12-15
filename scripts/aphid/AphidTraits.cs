using Godot;
using System;
using System.Collections.Generic;

public partial class AphidTraits : Aphid
{
    public static readonly Dictionary<string, ITrait> TRAITS = new()
    {
        { HeavySleeper.ID, new HeavySleeper() },
        { HyperActive.ID, new HyperActive() },
        { Affectionate.ID, new Affectionate() },
        { Lazy.ID, new Lazy() },
        { PickyEater.ID, new PickyEater() },
        { Fertile.ID, new Fertile() },
    };
    
    public class HeavySleeper : ITrait
    {
        public const string ID = "heavysleeper";
        public string Name => ID;

        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            return;
        }
        public void OnStateChange(Aphid aphid, StateEnum newState, EventArgs args)
        {
            if (newState != StateEnum.Sleep)
                return;

            (aphid.StateArgs as AphidActions.SleepState.SleepArgs).gain_rate++;
            (aphid.StateArgs as AphidActions.SleepState.SleepArgs).heavysleeper = true;
        }
        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void OnProcess(Aphid aphid, EventArgs args, float delta)
        {
            return;
        }

        public ITrait GetTrait() => new HeavySleeper();
    }
    public class HyperActive : ITrait
    {
        public const string ID = "hyperactive";
        public string Name => ID;
        public string[] IncompatibleTraits => new string[] { Lazy.ID };

        private float timer;

        public void Activate(Aphid aphid, EventArgs args)
        {
            timer = aphid.rng.RandfRange(2f, 4f);
            (aphid.StateArgs as AphidActions.IdleState.IdleArgs).decay_rate++;
        }
        public void OnStateChange(Aphid aphid, StateEnum newState, EventArgs args)
        {
            return;
        }
        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
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
        public ITrait GetTrait() => new HyperActive();
    }
    public class Affectionate : ITrait
    {
        public const string ID = "affectionate";
        public string Name => ID;

        public string[] IncompatibleTraits => null;
        private StateEnum lastState;

        public void Activate(Aphid aphid, EventArgs args)
        {
            aphid.TriggerActions.Add(new PlayerInteractionTrigger());
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void OnProcess(Aphid aphid, EventArgs args, float delta)
        {
            return;
        }

        public void OnStateChange(Aphid aphid, StateEnum newState, EventArgs args)
        {
            if (lastState == StateEnum.Pet || aphid.Instance.Status.Affection < 90)
                aphid.Instance.Status.AddBondship(1);
            lastState = newState;
        }

        public ITrait GetTrait() => new Affectionate();
        public class PlayerInteractionTrigger : AphidActions.ITriggerEvent
        {
            public string TriggerID => "player";
            private const float interaction_cd = 1.35f;
            private Timer interaction_timer;

            public void OnTrigger(Aphid _aphid, Node2D _node, EventArgs _args)
            {
                if (!IsInstanceValid(interaction_timer))
                {
                    interaction_timer = new();
                    _aphid.AddChild(interaction_timer);
                }

                if (!_aphid.State.Is(StateEnum.Idle) || _aphid.Instance.Status.Bondship < 25 || interaction_timer.TimeLeft > 0.1f)
                    return;

                interaction_timer.Start(interaction_cd);

                switch (_aphid.rng.RandiRange(0, 1))
                {
                    case 0:
                        _aphid.CallTowards(Player.Instance.GlobalPosition);
                        GameManager.EmitParticles("heart", _aphid.GlobalPosition - new Vector2(0, 10));
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
        // Should add a delegate for aphid actions
        public const string ID = "lazy";
        public string Name => ID;
        public string[] IncompatibleTraits => new string[] { HyperActive.ID };

        public void Activate(Aphid aphid, EventArgs args)
        {
            (aphid.StateArgs as AphidActions.IdleState.IdleArgs).decay_rate -= 0.5f;
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void OnStateChange(Aphid aphid, StateEnum newState, EventArgs args)
        {
            return;
        }

        public void OnProcess(Aphid aphid, EventArgs args, float delta)
        {
            if (!aphid.State.Is(StateEnum.Idle))
                return;

            AphidActions.IdleState.IdleArgs _args = args as AphidActions.IdleState.IdleArgs;

            if (_args.timeleft > 0)
                aphid.skin.SetLegsSkin("sleep");
            else 
                aphid.skin.SetLegsSkin("idle");
        }

        public ITrait GetTrait() => new Lazy();
    }
    public class PickyEater : ITrait
    {
        public const string ID = "pickyeater";
        public string Name => ID;
        public string[] IncompatibleTraits => null;

        public void Activate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void OnStateChange(Aphid aphid, StateEnum newState, EventArgs args)
        {
            return;
        }

        public void OnProcess(Aphid aphid, EventArgs args, float delta)
        {
            return;
        }

        public ITrait GetTrait() => new PickyEater();
    }
    public class Fertile : ITrait
    {
        public const string ID = "fertile";
        public string Name => ID;

        public string[] IncompatibleTraits => null;


        public void Activate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void Deactivate(Aphid aphid, EventArgs args)
        {
            return;
        }

        public void OnStateChange(Aphid aphid, StateEnum newState, EventArgs args)
        {
            return;
        }

        public void OnProcess(Aphid aphid, EventArgs args, float delta)
        {
            return;
        }

        public ITrait GetTrait() => new Fertile();
    }
}
