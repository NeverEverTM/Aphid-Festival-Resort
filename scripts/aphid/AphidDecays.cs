using Godot;
using static AphidData;

public partial class Aphid : CharacterBody2D
{
    // BASE GAMEPLAY DECAYS
    public class HungerDecay : CustomBaseTimer<AphidInstance>
    {
        public HungerDecay(AphidInstance Entity) : base(Entity)
        {
            BaseTime = BASE_HUNGER_DECAY;
            TimeLeft = GetTimerTime();
        }
        public override void Finish()
        {
            TInstance.AddHunger(-1);
        }
    }
    public class ThirstDecay : CustomBaseTimer<AphidInstance>
    {
        public ThirstDecay(AphidInstance Entity) : base(Entity)
        {
            BaseTime = BASE_THIRST_DECAY;
            TimeLeft = GetTimerTime();
        }
        public override void Finish()
        {
            TInstance.AddThirst(-1);
        }
    }
    public class RestDecay : CustomBaseTimer<AphidInstance>
    {
        public RestDecay(AphidInstance Entity) : base(Entity)
        {
            BaseTime = BASE_REST_DECAY;
            TimeLeft = GetTimerTime();
        }
        public override void Update(float _delta)
        {
            if (TInstance.StateIs(StateEnum.Sleep))
                return;
            base.Update(_delta);
        }
        public override void Finish()
        {
            TInstance.AddRest(-1);
            // not tired enough
            if (TInstance.Status.Rest > MIN_REST_FOR_SLEEP)
                return;

            // invalid state to transition from
            switch (IsInstanceValid(TInstance.Entity) ? TInstance.Entity.State.Type : TInstance.Status.LastActiveState)
            {
                case StateEnum.Busy:
                case StateEnum.Train:
                case StateEnum.Breed:
                    return;
                default:
                    break;
            }

            // small chance to sleep every tick, chances increase the more sleepiness you have
            if (TInstance.Status.Rest == 0 || GlobalManager.RNG.RandiRange(1,100) <= 1 + (MIN_REST_FOR_SLEEP - TInstance.Status.Rest))
            {
                TInstance.SetState(StateEnum.Sleep);
                TInstance.Timers.Find(t => t is RestGain).Start();
            }
        }
    }
    public class RestGain : CustomBaseTimer<AphidInstance>
    {
        public RestGain(AphidInstance Instance) : base(Instance)
        {
            BaseTime = BASE_REST_GAIN;
            TimeLeft = GetTimerTime();
            IsStopped = true;
        }

        public override void Update(float _delta)
        {
            if (!TInstance.StateIs(StateEnum.Sleep))
            {
                Stop();
                return;
            }
            base.Update(_delta);
        }
        public override void Finish()
        {
            TInstance.AddRest(1);

            if (TInstance.Status.Rest < MIN_REST_TO_WAKEUP)
                return;

            // chance to wake up after getting enough sleep
            if (TInstance.Status.Rest == 100 || !(TInstance.BoolFlags[AphidInstance.FlagsEnum.IsHeavySleeper].Value &&
                GlobalManager.RNG.RandiRange(1,100) <= 1 + (TInstance.Status.Rest - MIN_REST_TO_WAKEUP)))
            {
                if (IsInstanceValid(TInstance.Entity))
                    TInstance.Entity.WakeUp(false, true);
                else
                    TInstance.Status.LastActiveState = StateEnum.Idle;
            }
        }

        public override float GetTimerTime()
        {
            return BaseTime * TInstance.FloatFlags[AphidInstance.FlagsEnum.RestTimeMultiplier].Value;
        }
    }
    public class AffectionDecay : CustomBaseTimer<AphidInstance>
    {
        public AffectionDecay(AphidInstance Instance) : base(Instance)
        {
            BaseTime = BASE_AFFECTION_DECAY;
            TimeLeft = GetTimerTime();
        }

        public override void Update(float _delta)
        {
            if (TInstance.StateIs(StateEnum.Pet))
                return;
            base.Update(_delta);
        }

        public override void Finish()
        {
            TInstance.AddAffection(-1);
        }
    }
    public class BondshipDecay : CustomBaseTimer<AphidInstance>
    {
        public BondshipDecay(AphidInstance Instance) : base(Instance)
        {
            BaseTime = BASE_BONDSHIP_DECAY;
            TimeLeft = GetTimerTime();
        }

        public override void Update(float _delta)
        {
            if (TInstance.StateIs(StateEnum.Sleep) || TInstance.StateIs(StateEnum.Breed)) // dont go down while is asleep or mating
                return;

            if (TInstance.StateIs(StateEnum.Pet)) // reset after being pet
            {
                Start(BASE_BONDSHIP_GRACE);
                return;
            }

            if (TInstance.Status.Affection > 25)
                return;

            base.Update(_delta);
        }
        public override void Finish()
        {
            if (BaseTime != BASE_BONDSHIP_DECAY)
                Start(BASE_BONDSHIP_DECAY);

            TInstance.AddBondship(-1);
        }
    }
    public class LifetimeDecay : CustomBaseTimer<AphidInstance>
    {
        public LifetimeDecay(AphidInstance Instance) : base(Instance)
        {
            BaseTime = Age_Lifetime;
            TimeLeft = Age_Lifetime - Instance.Status.Age;
            OneShot = true;
        }
        public override void Update(float _delta)
        {
            // All things have an end, specifically, a few hours after birth
            base.Update(_delta);
            TInstance.Status.Age = Age_Lifetime - TimeLeft;
            // Grow up into an adult
            if (!TInstance.Status.IsAdult)
            {
                if (TInstance.Status.Age > Age_Adulthood && TInstance.StateIs(StateEnum.Idle))
                {
                    TInstance.Status.IsAdult = true;
                    if (IsInstanceValid(TInstance.Entity))
                        TInstance.Entity.Skin.SetTo("idle");
                }
            }
        }

        public override bool CanFinish()
        {
            return IsInstanceValid(TInstance.Entity) && !(TInstance.StateIs(StateEnum.Busy) || TInstance.StateIs(StateEnum.Eat) 
                    || TInstance.StateIs(StateEnum.Breed) || TInstance.StateIs(StateEnum.Sleep));
        }

        public override void Finish()
        {
            // Die at the old age of old years old
            TInstance.Entity.PrepareToDie();
            TInstance.Status.IsDead = true;
        }
    }
    public class BreedTimer: CustomBaseTimer<AphidInstance>
    {
        public BreedTimer(AphidInstance Instance) : base(Instance)
        {
            BaseTime = Breed_Cooldown;
            TimeLeft = Breed_Cooldown - Instance.Status.BreedBuildup;
            OneShot = true;
            IsStopped = Instance.Status.BreedMode != BreedMode.Inactive;
        }

        public override void Update(float _delta)
        {
            if (!CanUpdate())
                return;

            base.Update(_delta);
            TInstance.Status.BreedBuildup = Breed_Cooldown - TimeLeft;
        }
        public override bool CanFinish()
        {
            // dont breed if you arent idle or too badly taken care of
            return TInstance.StateIs(StateEnum.Idle) && TInstance.Status.Hunger > 20 && TInstance.Status.Thirst > 20;
        }

        public override bool CanUpdate()
        {
            // pause timer during breeding ritual or if it is too young
            return base.CanUpdate() && !TInstance.StateIs(StateEnum.Breed) && TInstance.Status.IsAdult;
        }

        public override void Finish()
        {
            TInstance.Status.BreedMode = BreedMode.Inactive;
            TInstance.SetState(StateEnum.Breed);
        }

        public override float GetTimerTime()
        {
            return base.GetTimerTime() * TInstance.FloatFlags[AphidInstance.FlagsEnum.BreedTimeMultiplier].Value;
        }
    }
    public class HarvestTimer: CustomBaseTimer<AphidInstance>
    {
        public HarvestTimer(AphidInstance Instance) : base(Instance)
        {
            BaseTime = Harvest_Cooldown;
            TimeLeft = Harvest_Cooldown - Instance.Status.HarvestBuildup;
            OneShot = true;
            IsStopped = Instance.Status.IsReadyForHarvest;
        }
        public override void Update(float _delta)
        {
            if (!CanUpdate())
                return;
            base.Update(_delta);
            TInstance.Status.HarvestBuildup = Harvest_Cooldown - TimeLeft;
        }

        public override void Finish()
        {
            TInstance.Status.IsReadyForHarvest = true;
            if (IsInstanceValid(TInstance.Entity))
                TInstance.Entity.AllowHarvest();
        }
    }
}
