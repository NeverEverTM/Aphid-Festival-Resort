using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using static AphidData;

public partial class Aphid : CharacterBody2D
{
    // GAMEPLAY STATES
    public class BusyState : IState
    {
        public StateEnum Type => StateEnum.Busy;
        public StateEnum[] TransitionList => null;
        public bool TransitionToAnything => true;
        public bool CanBeStartingState => false;

        public void Awake(Aphid aphid)
        {
            return;
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _args)
        {
#if DEBUG
            DebugLogger.Print(DebugLogger.LogPriority.Debug, $"AphidActions: {aphid.Instance.ID} has been set to busy.");
#endif
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            return;
        }
        public void Process(Aphid aphid, float delta)
        {
            return;
        }
    }
    public class IdleState : IState
    {
        public StateEnum Type => StateEnum.Idle;
        public StateEnum[] TransitionList => null;
        public bool TransitionToAnything => true;
        public bool CanBeStartingState => true;

        private const int MAX_IDLE_RANGE = 50, MIN_DISTANCE_SQUARED = 20 * 20;
        private const int MAX_IDLE_TIME = 2, MAX_TIMEOUT_TIME = 10;

        public Vector2 target_position;
        public IdleTimer idle_timer;
        public CustomTimer<Aphid> timeout_timer;
        /// <summary>
        /// If true, a third party has told it to go somewhere specific.
        /// </summary>
        private bool is_chasing = false;

        public void Awake(Aphid aphid)
        {
            idle_timer = new(aphid, MAX_IDLE_TIME, true, false);
            idle_timer.OnFinish.Add(CreateAndSetIdlePoint);
            aphid.ActiveTimers.Add(idle_timer);

            timeout_timer = new(aphid, MAX_TIMEOUT_TIME, true, false);
            timeout_timer.OnFinish.Add(StandStill);
            aphid.ActiveTimers.Add(timeout_timer);
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _args)
        {
            if (_args is IdleArgs)
                SetAndChaseIdlePoint(aphid, (_args as IdleArgs).IdleSpot);
            else
                StandStill(aphid);
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            idle_timer.Stop();
            timeout_timer.Stop();
            target_position = aphid.GlobalPosition;
        }
        public void Process(Aphid aphid, float delta)
        {
            if (!idle_timer.IsFinished && !is_chasing)
                return;

            // once we get close to our idle position, stand still
            if (aphid.GlobalPosition.DistanceSquaredTo(target_position) < MIN_DISTANCE_SQUARED)
            {
                timeout_timer.Stop();
                StandStill(aphid);
            }
        }

        public void StandStill(Aphid aphid)
        {
            is_chasing = false;
            aphid.SetMovementDirection(Vector2.Zero);
            // when finished, the idle timer will generate a new point and the cycle repeats
            idle_timer.Start();
        }
        /// <summary>
        /// Generates an idle point and sets it as a target.
        /// </summary>
        public void CreateAndSetIdlePoint(Aphid aphid)
        {
            is_chasing = false;
            idle_timer.Stop();
            target_position = new Vector2(MISC_RNG.RandfRange(-MAX_IDLE_RANGE, MAX_IDLE_RANGE),
                    MISC_RNG.RandfRange(-MAX_IDLE_RANGE, MAX_IDLE_RANGE)) + aphid.GlobalPosition;
            aphid.SetMovementDirection(target_position, true);
            timeout_timer.Start(MAX_TIMEOUT_TIME);
        }
        /// <summary>
        /// Sets the given idle point as target and chases it for a longer time.
        /// </summary>
        public void SetAndChaseIdlePoint(Aphid aphid, Vector2 _absoluteIdlePoint)
        {
            is_chasing = true;
            idle_timer.Stop();
            target_position = _absoluteIdlePoint;
            aphid.SetMovementDirection(target_position, true);
            timeout_timer.Start(MAX_TIMEOUT_TIME * 3);
        }
        /// <summary>
        /// Sets the given idle point as target.
        /// </summary>
        public void SetIdlePoint(Aphid aphid, Vector2 _relativeIdlePoint, float _timeoutMult = 1)
        {
            is_chasing = false;
            idle_timer.Stop();
            target_position = aphid.GlobalPosition + _relativeIdlePoint;
            aphid.SetMovementDirection(target_position, true);
            timeout_timer.Start(MAX_TIMEOUT_TIME * _timeoutMult);
        }

        public class IdleTimer(Aphid aphid, float BaseTime, bool OneShot = false, bool autostart = true) : CustomTimer<Aphid>(aphid, BaseTime, OneShot, autostart)
        {
            public override float GetTimerTime()
            {
                return MISC_RNG.RandfRange(BaseTime, BaseTime * 2) * TInstance.Instance.FloatFlags[AphidInstance.FlagsEnum.IdleTimeMultiplier].Value;
            }
        }
        public class IdleArgs(Vector2 IdleSpot) : EventArgs
        {
            public Vector2 IdleSpot = IdleSpot;
        }
    }
    public class HungryState : IState
    {
        public StateEnum Type => StateEnum.Hungry;
        public StateEnum[] TransitionList => null;
        public bool TransitionToAnything => true;
        public bool CanBeStartingState => false;

        // Eating Params
        public FoodArgs current_target;
        private CustomTimer<Aphid> food_timeout_timer;
        private readonly FoodTrigger food_lookout = new();

        private const int MAX_FOOD_RANGE = 200 * 200, MIN_FOOD_RANGE = 30 * 30, MAX_TIMEOUT_BASE = 10;

        public class FoodArgs : EventArgs
        {
            public Node2D node;
            public bool is_favorite;
            public FoodType flavor;
        }

        public void Awake(Aphid aphid)
        {
            food_timeout_timer = new(aphid, MAX_TIMEOUT_BASE, true, false);
            food_timeout_timer.OnFinish.Add((_aphid) =>
            {
                if (food_lookout.nearby_food.Count > 0)
                    current_target = food_lookout.nearby_food.Values.ToList()[MISC_RNG.RandiRange(0, food_lookout.nearby_food.Count - 1)];
                else
                    _aphid.SetState(StateEnum.Idle);
            });
            aphid.ActiveTimers.Add(food_timeout_timer);
            aphid.AreaEvents.Add(food_lookout);
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _args)
        {
            food_timeout_timer.Start(MAX_TIMEOUT_BASE);
            current_target = food_lookout.nearby_food.First().Value;
            ChooseClosest(aphid);
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            current_target = null;
            food_timeout_timer.Stop();
        }
        public void Process(Aphid aphid, float delta)
        {
            // if is not valid, too far away, or claimed by someone, let go
            if (current_target == null || !IsInstanceValid(current_target.node) || food_lookout.nearby_food.Count == 0
                || aphid.GlobalPosition.DistanceSquaredTo(current_target.node.GlobalPosition) > MAX_FOOD_RANGE
                || !(bool)current_target.node.GetMeta(StringNames.PickupMeta))
            {
                aphid.SetState(StateEnum.Idle);
                return;
            }

            ChooseClosest(aphid);

            // Walk towards the food item, once close, begin the eating state
            if (aphid.GlobalPosition.DistanceSquaredTo(current_target.node.GlobalPosition) > MIN_FOOD_RANGE)
                aphid.SetMovementDirection(current_target.node.GlobalPosition - aphid.GlobalPosition);
            else
            {
                aphid.SetMovementDirection(Vector2.Zero);
                current_target.node.SetMeta(StringNames.PickupMeta, false); // Stops others from eating it
                current_target.node.GlobalPosition = aphid.GlobalPosition + (aphid.Skin.IsFlipped ? new Vector2(-25, -10) : new Vector2(25, -10));
                aphid.SetState(StateEnum.Eat, current_target);
            }
        }
        public void ChooseClosest(Aphid aphid)
        {
            // Select the closest food item
            foreach (var _pair in food_lookout.nearby_food)
            {
                if (!IsInstanceValid(current_target.node))
                {
                    current_target = _pair.Value;
                    continue;
                }
                if (!IsInstanceValid(_pair.Value.node))
                {
                    food_lookout.nearby_food.Remove(_pair.Key);
                    continue;
                }
                // if they are both at the same level of priority(favorite) then choose the closest one
                if (_pair.Value.is_favorite == current_target.is_favorite)
                {
                    if (aphid.GlobalPosition.DistanceSquaredTo(_pair.Value.node.GlobalPosition)
                        >= aphid.GlobalPosition.DistanceSquaredTo(current_target.node.GlobalPosition))
                        continue;
                }
                else if (!_pair.Value.is_favorite) // otherwise, if the new one isnt favorite, ignore it
                    continue;

                current_target = _pair.Value;
                food_timeout_timer.Start(MAX_TIMEOUT_BASE);
            }
        }

        public class FoodTrigger : IInteractionEvent
        {
            public List<ulong> ignored = [];
            public Dictionary<ulong, FoodArgs> nearby_food = [];
            private readonly int max_distance = 400 * 400;

            public void OnTrigger(Aphid _myAphid, Node2D _incomingNode, StringNames.GlobalTags _nodeTag)
            {
                if (_nodeTag != StringNames.GlobalTags.Food)
                    return;

                // clean the ignored foods regularly
                for (int i = ignored.Count - 1; i >= 0; i--)
                {
                    if (!IsInstanceIdValid(ignored[i]))
                        ignored.RemoveAt(i);
                }

                foreach (var _pair in nearby_food)
                {
                    // remove food items that are now too far or are invalid
                    if (!IsInstanceValid(_pair.Value.node)
                        || _pair.Value.node.GlobalPosition.DistanceSquaredTo(_myAphid.GlobalPosition) > max_distance)
                        nearby_food.Remove(_pair.Key);
                }

                AnalyzeItem(_myAphid, _incomingNode);
            }
            public void AnalyzeItem(Aphid aphid, Node2D _node)
            {
                ulong _instanceID = _node.GetInstanceId();
                if (ignored.Contains(_instanceID) || nearby_food.ContainsKey(_instanceID)
                    || aphid.State.Is(StateEnum.Eat) || aphid.State.Is(StateEnum.Train)
                    || !(bool)_node.GetMeta(StringNames.PickupMeta))
                    return;

                FoodData _current_food = GlobalManager.G_FOOD[_node.GetMeta(StringNames.IdMeta).ToString()];
                var _flavor = _current_food.Flavor;
                bool _isfavorite = aphid.Instance.Genes.FoodPreference == _flavor;
                bool _isPickyEater = aphid.Instance.BoolFlags[AphidInstance.FlagsEnum.IsPicky].Value;

                // if Vile, reject it cause yucky, unless you like it for some reason
                if (_flavor == FoodType.Vile && _isfavorite)
                {
                    ignored.Add(_node.GetInstanceId());
                    return;
                }

                // picky eaters reject non-favorites except for neutral flavors
                if (_isPickyEater && !_isfavorite && _flavor != FoodType.Neutral)
                {
                    ignored.Add(_node.GetInstanceId());
                    return;
                }

                // Do not overeat/drink unless is your favorite or you are a glutton, picky eaters check regardless
                if (_isPickyEater || !_isfavorite && !aphid.Instance.BoolFlags[AphidInstance.FlagsEnum.CanOvereat].Value)
                {
                    bool _givesFood = _current_food.FoodValue > 0,
                        _givesDrink = _current_food.DrinkValue > 0;

                    // the threshold differs depending on if it gives both stats or just a single one
                    if (_givesDrink)
                    {
                        if (aphid.Instance.Status.Thirst >= (_givesFood ? 80 : 100))
                            return;
                    }
                    if (_givesFood)
                    {
                        if (aphid.Instance.Status.Hunger >= (_givesDrink ? 80 : 100))
                            return;
                    }
                }

                nearby_food.Add(_instanceID, new()
                {
                    node = _node,
                    is_favorite = _isfavorite,
                    flavor = _flavor
                });
                aphid.SetState(StateEnum.Hungry);
            }
        }
    }
    public class EatingState : IState
    {
        public StateEnum Type => StateEnum.Eat;
        public StateEnum[] TransitionList => [StateEnum.Idle];
        public bool TransitionToAnything => false;
        public bool CanBeStartingState => false;

        private const int anim_gobble_ticks = 8, MAX_GOBBLE_TIME = 2;
        private int gobble_ticks;
        private bool anim_direction;
        private CustomTimer<Aphid> gobble_timer;
        private GpuParticles2D gobble_particles;
        private HungryState.FoodArgs current_target;

        public void Awake(Aphid aphid)
        {
            gobble_timer = new(aphid, MAX_GOBBLE_TIME, true, false);
            gobble_timer.OnFinish.Add(FinishFood);
            aphid.ActiveTimers.Add(gobble_timer);
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _specialArgs)
        {
            if (_specialArgs == null || _specialArgs is not HungryState.FoodArgs)
                return;
            current_target = _specialArgs as HungryState.FoodArgs;

            gobble_timer.Start(MAX_GOBBLE_TIME);
            gobble_particles = GlobalManager.EmitParticles("food", current_target.node.GlobalPosition, false);
            gobble_particles.Texture = (current_target.node.GetChild(0) as Sprite2D).Texture;
            (gobble_particles as FoodParticleBehaviour).CreateCustomParticleTexture();
        }

        public void Exit(Aphid aphid, StateEnum _next)
        {
            gobble_timer.Stop();
            current_target = null;
            gobble_particles.OneShot = true;
            gobble_particles = null;
        }

        public void Process(Aphid aphid, float delta)
        {
            if (current_target == null || !IsInstanceValid(current_target.node))
            {
                DebugLogger.Print(DebugLogger.LogPriority.Error, "AphidActions: Food Item was taken away prematurely");
                aphid.SetState(StateEnum.Idle);
                return;
            }

            // eating motion
            if (gobble_ticks == 0)
            {
                current_target.node.GlobalPosition += anim_direction ? Vector2.Up : Vector2.Down;
                if (!current_target.node.Scale.IsEqualApprox(Vector2.Zero))
                    current_target.node.Scale -= new Vector2(0.05f, 0.05f);
                anim_direction = !anim_direction;
                gobble_ticks = anim_gobble_ticks;
                SoundManager.CreateSound2D("aphid/nom", aphid.GlobalPosition);
            }
            else
                gobble_ticks--;
        }

        public void FinishFood(Aphid aphid)
        {
            FoodData _food = GlobalManager.G_FOOD[current_target.node.GetMeta(StringNames.IdMeta).ToString()];

            // set food values
            float _multi = aphid.Instance.Genes.FoodMultipliers[(int)_food.Flavor];
            if (_food.FoodValue > 0)
                aphid.Instance.AddHunger(_food.FoodValue * _multi);

            if (_food.DrinkValue > 0)
                aphid.Instance.AddThirst(_food.DrinkValue * _multi);

            // set skill values, cannot gain skill if we are full
            if (_food.Skills.Count > 0 && (aphid.Instance.Status.Hunger <= 90 || aphid.Instance.Status.Thirst <= 90))
            {
                foreach (var _pair in _food.Skills)
                    aphid.Instance.Genes.Skills[SkillNames[(int)_pair.Key]].GivePoints(
                        _pair.Value * (current_target.is_favorite ? 2 : 1));
            }

            // Dispose of the food item now
            current_target.node.QueueFree();
            aphid.SetState(StateEnum.Idle);
        }
    }
    public class SleepState : IState
    {
        public StateEnum Type => StateEnum.Sleep;
        public StateEnum[] TransitionList => [StateEnum.Idle, StateEnum.Pet];
        public bool CanBeStartingState => true;
        public bool TransitionToAnything => false;

        private GpuParticles2D sleep_effect;

        public void Awake(Aphid _aphid)
        {
            return;
        }

        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _args)
        {
            aphid.Instance.Timers.Find(t => t is RestGain).Start();
            aphid.Skin.SetEyesSkin("sleep");
            aphid.Skin.SetLegsSkin("sleep");
            sleep_effect = GlobalManager.EmitParticles("sleep", aphid.GlobalPosition);
            aphid.Skin.Position = new(0, 2);
        }

        public void Exit(Aphid aphid, StateEnum _next)
        {
            aphid.Skin.SetTo("idle");
            aphid.Skin.Position = new(0, 0);
            sleep_effect.OneShot = true;
            sleep_effect = null;
        }

        public void Process(Aphid aphid, float delta)
        {
            // maybe make a random sleepy sound?
            sleep_effect.GlobalPosition = aphid.GlobalPosition; // if picked up, update the position of effect
        }
    }
    public class PetState : IState
    {
        public StateEnum Type => StateEnum.Pet;
        public StateEnum[] TransitionList => [StateEnum.Idle];
        public bool CanBeStartingState => false;
        public bool TransitionToAnything => false;

        private CustomTimer<Aphid> pet_timer;

        public void Awake(Aphid aphid)
        {
            pet_timer = new(aphid, PET_DURATION, false, false);
            pet_timer.OnFinish.Add(FinishPet);
            pet_timer.Stop();
            aphid.ActiveTimers.Add(pet_timer);
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _specialArgs)
        {
            pet_timer.Start(PET_DURATION);
            // we run the player timer separately so any error from this side doesnt affect it
            // aphids cannot be interacted while being in pet mode anyways
            Player.Instance.SetDisabled(true);
            Player.Instance.RunDisabledTimer(PET_DURATION, false, false);

            // visuals
            Player.Instance.SetPlayerAnim("pet");
            Player.Instance.SetFlipDirection(aphid.GlobalPosition - Player.Instance.GlobalPosition);
            aphid.Skin.SetFlipDirection(Player.Instance.GlobalPosition - aphid.GlobalPosition);
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            pet_timer.Stop();
        }
        public void Process(Aphid aphid, float delta)
        {
            return;
        }

        public static void FinishPet(Aphid aphid)
        {
            if (aphid.Instance.Status.Affection <= 90)
            {
                GlobalManager.EmitParticles("heart", aphid.GlobalPosition - new Vector2(0, 10));
                aphid.Instance.AddBondship(1);
            }
            aphid.Instance.AddAffection(10);
            aphid.SetState(StateEnum.Idle);
            aphid.Skin.DoHop();
        }
    }
    public class BreedState : IState
    {
        public StateEnum Type => StateEnum.Breed;
        public StateEnum[] TransitionList => [StateEnum.Idle];
        public bool CanBeStartingState => true;
        public bool TransitionToAnything => false;

        private readonly float[] breeding_weights = [70, 30];
        private const int MIN_PARTNER_DISTANCE = 20 * 20;

        private BreedTrigger breed_lookout = new();
        private GpuParticles2D breed_effect;
        private BreedArgs args;
        private bool is_in_final_stage;

        public class BreedArgs : EventArgs
        {
            public Vector2 position;
        }

        public void Awake(Aphid aphid)
        {
            if (aphid.Instance.Status.BreedMode == BreedMode.AsPartner)
                aphid.Instance.Status.BreedMode = BreedMode.Inactive;
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _specialArgs)
        {
            if (_specialArgs != null)
                args = _specialArgs as BreedArgs;

            if (aphid.Instance.Status.BreedMode == BreedMode.Inactive)
                GetRandBreedMode(aphid);
            StartBreedingBehaviour(aphid);

            breed_lookout = new();
            aphid.AreaEvents.Add(breed_lookout);
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            aphid.Skin.OverrideMovementAnim = false;
            aphid.AreaEvents.Remove(breed_lookout);
            aphid.Instance.Status.BreedMode = BreedMode.Inactive;
            aphid.Instance.Timers.Find((t) => t is BreedTimer).Start();

            // get rid of breed partner and effect
            breed_lookout.breed_partner?.SetState(StateEnum.Idle);
            if (breed_effect != null)
                breed_effect.OneShot = true;

            breed_effect = null;
            breed_lookout.breed_partner = null;
            is_in_final_stage = false;
            args = null;
        }
        public void Process(Aphid aphid, float delta)
        {
            // we are finalizing breeding, no need for more processing
            if (is_in_final_stage)
                return;

            if (aphid.Instance.Status.BreedMode == BreedMode.Inactive)
            {
                aphid.SetState(StateEnum.Idle);
                DebugLogger.Print(DebugLogger.LogPriority.Error, "BreedState: It was in an invalid breed mode");
                return;
            }

            switch (aphid.Instance.Status.BreedMode)
            {
                case BreedMode.AsPartner:
                    // walk towards it and stop once you are close
                    if (aphid.GlobalPosition.DistanceSquaredTo(args.position) <= MIN_PARTNER_DISTANCE)
                    {
                        aphid.SetMovementDirection(Vector2.Zero);
                        is_in_final_stage = true;
                    }
                    break;
                case BreedMode.WithPartner:
                    if (breed_lookout.breed_partner == null)
                        return;

                    if (!IsInstanceValid(breed_lookout.breed_partner) || !breed_lookout.breed_partner.State.Is(StateEnum.Breed))
                    {
                        breed_lookout.breed_partner = null;
                        return;
                    }

                    // wait for partner to arrive
                    if (breed_lookout.breed_partner.GlobalPosition.DistanceSquaredTo(args.position) <= MIN_PARTNER_DISTANCE)
                        StartBreedingWithPartner(aphid);
                    else
                        aphid.Skin.DoWalkAnim(); // waiting animation
                    break;
            }
        }

        public void StartBreedingBehaviour(Aphid aphid)
        {
            switch (aphid.Instance.Status.BreedMode)
            {
                case BreedMode.AsPartner:
                    aphid.SetMovementDirection(args.position - aphid.GlobalPosition);
                    break;
                case BreedMode.WithItself:
                    is_in_final_stage = true;
                    breed_effect = GlobalManager.EmitParticles("heart", aphid.GlobalPosition, false);
                    breed_effect.OneShot = false;
                    StartLayingEgg(aphid);
                    break;
                case BreedMode.WithPartner:
                    aphid.Skin.OverrideMovementAnim = true;
                    breed_effect = GlobalManager.EmitParticles("mating", aphid.GlobalPosition);
                    break;
            }
        }
        public void StartBreedingWithPartner(Aphid aphid)
        {
            is_in_final_stage = true;
            aphid.Skin.OverrideMovementAnim = false; // deactivate waiting anim

            // face each other
            breed_lookout.breed_partner.Skin.SetFlipDirection(breed_lookout.breed_partner.GlobalPosition - aphid.GlobalPosition);
            aphid.Skin.SetFlipDirection(aphid.GlobalPosition - breed_lookout.breed_partner.GlobalPosition);

            // BREED (the function automatically terminates breed state for both parties)
            StartLayingEgg(aphid, breed_lookout.breed_partner);
        }
        public static void StartLayingEgg(Aphid aphid, Aphid _father = null)
        {
            _father ??= aphid;
            _ = _father.Skin.DoDance();// let your partner do a lil dance
            Task _dance = aphid.Skin.DoDance();

            _dance.ContinueWith((_task) =>
            {
                Callable _layEgg = Callable.From(() =>
                {
                    aphid.LayAnEgg(aphid.Instance, true);
                });
                _layEgg.CallDeferred();
            });
        }
        public void GetRandBreedMode(Aphid aphid)
        {
            if (GameManager.Aphids.Count == 1)
                aphid.Instance.Status.BreedMode = BreedMode.WithItself; // This is to make sure new games get a second aphid as soon as possible
            else
                aphid.Instance.Status.BreedMode = (BreedMode)GlobalManager.Utils.GetRandomByWeight(breeding_weights);
        }

        public class BreedTrigger : IInteractionEvent
        {
            public Aphid breed_partner;
            private readonly List<Node2D> ignored = [], alreadyChecked = [];

            public void OnTrigger(Aphid _myAphid, Node2D _incomingNode, StringNames.GlobalTags _nodeTag)
            {
                if (_nodeTag != StringNames.GlobalTags.Aphid || breed_partner != null || ignored.Contains(_incomingNode))
                    return; // if not an aphid or breed partner is picked and present, return

                Aphid _partner = _incomingNode as Aphid;

                if (!_partner.State.Is(StateEnum.Idle))
                    return;

                // only adults can breed with each other
                if (!_partner.Instance.Status.IsAdult)
                {
                    ignored.Add(_incomingNode);
                    return;
                }

                // partner is too badly taken care of to mate
                if (_partner.Instance.Status.Hunger < 25 || _partner.Instance.Status.Thirst < 25)
                {
                    alreadyChecked.Add(_incomingNode);
                    if (!alreadyChecked.Contains(_incomingNode))
                        SoundManager.CreateSound2D("aphid/hurt", _partner.GlobalPosition);
                    return;
                }
                alreadyChecked.Remove(_incomingNode);// remove if they are in good condition again

                // do not mate with an enemy or a parent!
                if (!_myAphid.Instance.HasRelationship(_partner, out var _myRelationshipWithThem) ||
                        _myRelationshipWithThem.Level == Relationship.RelationshipLevel.Enemy ||
                        _myRelationshipWithThem.Level == Relationship.RelationshipLevel.Parent)
                {
                    ignored.Add(_incomingNode);
                    return;
                }

                // roll a chance to join in as a partner, chance is affected by total relationship, lovers will always mate no matter what
                if (_myRelationshipWithThem.Level != Relationship.RelationshipLevel.Lover
                    && GlobalManager.RNG.RandiRange(1, 1000) > 8 + ((int)_myRelationshipWithThem.Level) * 5)
                    return;

                breed_partner = _partner;
                _partner.Instance.Status.BreedMode = BreedMode.AsPartner;
                _partner.SetState(StateEnum.Breed, new BreedArgs()
                { position = _myAphid.GlobalPosition + (_myAphid.Skin.IsFlipped ? new(-40, -5) : new(40, -5)) });
                _partner.Skin.DoHop();
                GlobalManager.EmitParticles("heart", _partner.GlobalPosition, false);
            }
        }
    }
    public class TrainState : IState
    {
        public StateEnum Type => StateEnum.Train;
        public StateEnum[] TransitionList => [StateEnum.Idle];
        public bool TransitionToAnything => false;
        public bool CanBeStartingState => false;

        private TrainTimer train_timer;

        public void Awake(Aphid aphid)
        {
            return;
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs args)
        {
            train_timer = new TrainTimer(aphid.Instance);
            aphid.Instance.Timers.Add(train_timer);
            aphid.Skin.SetTo(StringNames.IdleAnim);
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            aphid.Instance.Timers.Remove(train_timer);
            aphid.Skin.OverrideMovementAnim = false;
            train_timer = null;
        }
        public void Process(Aphid aphid, float delta)
        {
            if (aphid.Instance.Status.Rest < MIN_REST_FOR_SLEEP)
            {
                aphid.SetState(StateEnum.Idle);
                return;
            }
        }

        public class TrainTimer : CustomBaseTimer<AphidInstance>
        {
            public int last_level;
            public string skill_name;

            public TrainTimer(AphidInstance aphid) : base(aphid)
            {
                BaseTime = aphid.Status.LastTraining.RawBaseTime;
                TimeLeft = GetTimerTime();
                skill_name = aphid.Status.LastTraining.Skill.ToString().ToLower();
                last_level = aphid.Genes.Skills[skill_name].Level;
            }
            public override void Update(float _timePassed)
            {
                if (TInstance.Status.Rest < MIN_REST_FOR_SLEEP)
                {
                    TInstance.SetState(StateEnum.Idle);
                    TInstance.Timers.Remove(this);
                    return;
                }
                if (!TInstance.StateIs(StateEnum.Train))
                {
                    TInstance.Timers.Remove(this);
                    return;
                }
                base.Update(_timePassed);
            }

            public override void Finish()
            {
                TrainData _data = TInstance.Status.LastTraining;
                TInstance.Genes.Skills[_data.Skill.ToString().ToLower()].GivePoints(
                        _data.GetPointGain(!TInstance.Status.IsAdult ? 2 : 1)); // gain double the points during childhood
                if (TInstance.Genes.Skills[skill_name].Level != last_level) // give on levelup
                {
                    last_level = TInstance.Genes.Skills[skill_name].Level;
                    if (IsInstanceValid(TInstance.Entity))
                        SoundManager.CreateSound2D("aphid/skill_gain", TInstance.Entity.GlobalPosition);
                }
            }
        }
    }
    public class PlayState : IState
    {
        public StateEnum Type => StateEnum.Play;
        public StateEnum[] TransitionList => null;
        public bool CanBeStartingState => true;
        public bool TransitionToAnything => true;

        public void Awake(Aphid aphid)
        {
            return;
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _specialArgs)
        {
            return;
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            aphid.Skin.OverrideMovementAnim = false;
        }
        public void Process(Aphid aphid, float delta)
        {
            return;
        }
    }
    public class SocialState : IState
    {
        public StateEnum Type => StateEnum.Social;
        public StateEnum[] TransitionList => null;
        public bool CanBeStartingState => false;
        public bool TransitionToAnything => true;

        private readonly static float[] aphid_interaction_weights = [50, 50];
        private SocialArgs args;

        public class SocialArgs : EventArgs
        {
            /// <summary>
            /// The other aphid who we are socializing with.
            /// </summary>
            public Aphid Friendphid;
            /// <summary>
            /// Relationship of the other aphid with this aphid
            /// </summary>
            public Relationship FriendphidRelationship;
            /// <summary>
            /// Relationship of this aphid with the other aphid.
            /// </summary>
            public Relationship MyRelationship;
            /// <summary>
            /// If we are the aphid controlling the social interaction or just the friendphid.
            /// </summary>
            public bool IsActive;
        }

        public void Awake(Aphid aphid)
        {
            aphid.AreaEvents.Add(new AphidFriendhsip());
        }
        public void Enter(Aphid aphid, StateEnum _previous, EventArgs _specialArgs)
        {
            if (_specialArgs == null)
                return;

            args = _specialArgs as SocialArgs;
            if (!args.IsActive)
                return;

            aphid.Skin.SetFlipDirection(args.Friendphid.GlobalPosition - aphid.GlobalPosition);
            args.Friendphid.Skin.SetFlipDirection(aphid.GlobalPosition - args.Friendphid.GlobalPosition);

            Interaction1(aphid);
        }
        public void Exit(Aphid aphid, StateEnum _next)
        {
            args = null;
        }
        public void Process(Aphid aphid, float delta)
        {
            if (args == null)
                aphid.SetState(StateEnum.Idle);
        }

        private void FinishInteraction(Aphid aphid)
        {
            if (args.Friendphid.State.Is(StateEnum.Social))
                args.Friendphid.SetState(StateEnum.Idle);
            if (aphid.State.Is(StateEnum.Social))
                aphid.SetState(StateEnum.Idle);
        }
        private void Interaction1(Aphid aphid)
        {
            aphid.CreateTimer(() =>
            {
                RandRelationship(aphid);
                FinishInteraction(aphid);
            }, 3);
            aphid.CreateTimer(() => SoundManager.CreateSound2D(aphid.AudioDynamic_Idle, aphid.GlobalPosition), MISC_RNG.RandfRange(0.5f, 2));
        }
        private void RandRelationship(Aphid aphid)
        {
            switch (MISC_RNG.RandWeighted(aphid_interaction_weights))
            {
                case 0: // get angry at interaction
                    args.MyRelationship.AddToTotal(-3);
                    args.FriendphidRelationship.AddToTotal(-3);
                    GlobalManager.EmitParticles("anger", aphid.GlobalPosition + (args.Friendphid.GlobalPosition - aphid.GlobalPosition) / 2, false);
                    return;
                case 1: // get pleased at interaction
                    args.MyRelationship.AddToTotal(3);
                    args.FriendphidRelationship.AddToTotal(3);
                    GlobalManager.EmitParticles("heart", aphid.GlobalPosition + (args.Friendphid.GlobalPosition - aphid.GlobalPosition) / 2, false);
                    return;
            }
        }

        public class AphidFriendhsip : IInteractionEvent
        {
            public void OnTrigger(Aphid _myAphid, Node2D _incomingNode, StringNames.GlobalTags _nodeTag)
            {
                if (_nodeTag != StringNames.GlobalTags.Aphid)
                    return;

                Aphid _otherAphid = _incomingNode as Aphid;
                if (_myAphid.Instance.GUID.Equals(_otherAphid.Instance.GUID))
                    return;

                // met strangers you see for the first time
                if (!_otherAphid.Instance.HasRelationship(_myAphid, out Relationship theirRelationWithMe))
                {
                    _otherAphid.Instance.AddRelationship(_myAphid);
                    theirRelationWithMe = _otherAphid.Instance.GetRelationship(_myAphid.Instance.GUID);
                }
                if (!_myAphid.Instance.HasRelationship(_otherAphid, out Relationship myRelationWithThem))
                {
                    _myAphid.Instance.AddRelationship(_otherAphid);
                    myRelationWithThem = _myAphid.Instance.GetRelationship(_otherAphid.Instance.GUID);
                }

                if (myRelationWithThem == null || theirRelationWithMe == null)
                {
                    GD.PrintErr($"Error on relationship of: {_myAphid.Instance.Genes.Name}", myRelationWithThem, "//", theirRelationWithMe);
                }

                if (!_myAphid.State.Is(StateEnum.Idle) || !_otherAphid.State.Is(StateEnum.Idle))
                    return;

                if (MISC_RNG.RandiRange(1, 1000) > 5 + (int)myRelationWithThem.Level * 2) // chance for an interaction to happen
                    return;

                _myAphid.SetState(StateEnum.Social, new SocialArgs()
                {
                    Friendphid = _otherAphid,
                    FriendphidRelationship = theirRelationWithMe,
                    MyRelationship = myRelationWithThem,
                    IsActive = true
                });
            }
        }
    }
}