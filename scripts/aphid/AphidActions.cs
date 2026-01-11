using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using static AphidData;

public partial class AphidActions : Aphid
{
	// CLASS DECLARATIONS
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

			Level = (RelationshipLevel)Mathf.Clamp(Total / 30, -2, 3);
		}

		public bool Equals(Aphid _aphid) => Aphid.Equals(_aphid.Instance.GUID);
	}
	public class Skill(string Name)
	{
		public string Name { get; set; } = Name;
		public int Points { get { return points; } set { points = Mathf.Clamp(value, 0, 10); } }
		private int points;
		public int Level { get; set; }

		public delegate void LevelUp(int _lastLevel, int _currentLevel);
		public event LevelUp OnLevelUp;
		public delegate void SkillChange();
		public event SkillChange OnSkillChange;

		public virtual void GivePoints(int _points, bool _noSignal = false)
		{
			points += _points;

			if (points > 10)
			{
				while (points > 10)
				{
					points -= 10;
					GiveLevel(1, _noSignal);
				}
			}
			else if (points < 0)
			{
				while (points < 0)
				{
					points += 10;
					GiveLevel(-1, _noSignal);
				}
			}
			OnSkillChange?.Invoke();
		}
		public virtual void GiveLevel(int _level, bool _noSignal = false)
		{
			if (!_noSignal)
				OnLevelUp?.Invoke(Level, Level + _level);
			Level += _level;
		}
	}

	/// <summary>
	/// An event interface that fires up when a node enters/exits an aphid's interaction area.
	/// </summary>
	public interface IAreaEvent
	{
		public StringNames.GlobalTags Tag { get; }
		public void OnNodeEntered(Aphid _aphid, Node2D _node);
		public void OnNodeExited(Aphid _aphid, Node2D _node);
	}

	// BASE GAMEPLAY DECAYS
	public class HungerDecay(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, OneShot, autostart)
	{
		public override void Finish(Aphid aphid)
		{
			aphid.Instance.AddHunger(-1);
		}
		public class Passive(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<AphidPassive>(BaseTime, TimeLeft, OneShot, autostart)
		{
			public override void Finish(AphidPassive aphid)
			{
				aphid.Instance.AddHunger(-1);
			}
		}
	}
	public class ThirstDecay(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, OneShot, autostart)
	{
		public override void Finish(Aphid aphid)
		{
			aphid.Instance.AddThirst(-1);
		}
		public class Passive(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<AphidPassive>(BaseTime, TimeLeft, OneShot, autostart)
		{

			public override void Finish(AphidPassive aphid)
			{
				aphid.Instance.AddThirst(-1);
			}
		}
	}
	public class RestDecay(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, OneShot, autostart)
	{
		public override void Process(Aphid entity, float _delta)
		{
			if (entity.State.Is(StateEnum.Sleep))
				return;
			base.Process(entity, _delta);
		}

		public override void Finish(Aphid aphid) => OnFinish(aphid.Instance);

		public static void OnFinish(AphidInstance aphid)
		{
			aphid.AddTiredness(1);
			// not tired enough
			float _currentTiredness = aphid.Status.Tiredness;
			if (_currentTiredness < MAX_TIREDNESS_SLEEP)
				return;

			// invalid state to transition from
			switch (aphid.Status.LastActiveState)
			{
				case StateEnum.Busy:
				case StateEnum.Train:
				case StateEnum.Breed:
					return;
				default:
					break;
			}

			// small chance to sleep every tick, chances increase the more sleepiness you have
			if (_currentTiredness == 100 || GlobalManager.Utils.GetRandomByWeight(CORE_RNG, [100 - _currentTiredness, _currentTiredness]) == 1)
				aphid.SetState(StateEnum.Sleep);
		}

		public class Passive(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<AphidPassive>(BaseTime, TimeLeft, OneShot, autostart)
		{
			public override void Finish(AphidPassive aphid) => OnFinish(aphid.Instance);
		}
	}
	public class RestGain(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, OneShot, autostart)
	{
		public float multiplier;

		public override void Process(Aphid aphid, float _delta)
		{
			if (!aphid.State.Is(StateEnum.Sleep))
				return;
			base.Process(aphid, _delta);
		}
		public override void Finish(Aphid aphid)
		{
			aphid.Instance.AddTiredness(-1);
			float _currentTiredness = aphid.Instance.Status.Tiredness;
			// bare minimum sleep is until it hits 85% energy
			if (_currentTiredness > MIN_TIREDNESS_WAKEUP)
				return;

			// chance to wake up after getting enough sleep
			if (_currentTiredness == 0 || GlobalManager.Utils.GetRandomByWeight
					([100 - _currentTiredness, _currentTiredness]) == 0)
				aphid.WakeUp(false, true);
		}

		public override float GetTimerTime()
		{
			return BaseTime * multiplier;
		}

		public class Passive(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<AphidPassive>(BaseTime, TimeLeft, OneShot, autostart)
		{
			public float multiplier;

			public override void Process(AphidPassive aphid, float _delta)
			{
				if (aphid.Instance.Status.LastActiveState != StateEnum.Sleep)
					return;
				base.Process(aphid, _delta);
			}
			public override void Finish(AphidPassive aphid)
			{
				aphid.Instance.AddTiredness(-1);
				float _currentTiredness = aphid.Instance.Status.Tiredness;
				// bare minimum sleep is until it hits 85% energy
				if (_currentTiredness > MIN_TIREDNESS_WAKEUP)
					return;

				// chance to wake up after getting enough sleep
				if (_currentTiredness == 0 || GlobalManager.Utils.GetRandomByWeight([100 - _currentTiredness, _currentTiredness]) == 0)
					aphid.Instance.SetState(StateEnum.Idle);
			}

			public override float GetTimerTime()
			{
				return BaseTime * multiplier;
			}
		}
	}
	public class AffectionDecay(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, OneShot, autostart)
	{
        public override void Process(Aphid entity, float _delta)
		{
			if (entity.State.Is(StateEnum.Pet))
				return;
			base.Process(entity, _delta);
		}

		public override void Finish(Aphid aphid)
		{
			aphid.Instance.AddAffection(-1);
		}
		public class Passive(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<AphidPassive>(BaseTime, TimeLeft, OneShot, autostart)
		{
			public override void Finish(AphidPassive aphid)
			{
				aphid.Instance.AddAffection(-1);
			}
		}
	}
	public class BondshipDecay(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, OneShot, autostart)
	{
        public override void Process(Aphid entity, float _delta)
		{
			if (entity.State.Is(StateEnum.Sleep) || entity.State.Is(StateEnum.Breed)) // dont go down while is asleep or mating
				return;

			if (entity.State.Is(StateEnum.Pet)) // reset after being pet
			{
				Start(BASE_BONDSHIP_GRACE);
				return;
			}

			base.Process(entity, _delta);
		}
		public override void Finish(Aphid aphid)
		{
			if (BaseTime != BASE_BONDSHIP_DECAY)
				Start(BASE_BONDSHIP_DECAY);
			aphid.Instance.AddBondship(-1);
		}
	}
	public class LifetimeDecay(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, TimeLeft, OneShot, autostart)
	{
        public override void Process(Aphid aphid, float _delta)
		{
			// All things have an end, specifically, a few hours after birth
			base.Process(aphid, _delta);
			aphid.Instance.Status.Age = BaseTime - TimeLeft;
			// Grow up into an adult
			if (!aphid.Instance.Status.IsAdult)
			{
				if (aphid.Instance.Status.Age > Age_Adulthood &&
					aphid.State.Is(StateEnum.Idle))
				{
					aphid.Instance.Status.IsAdult = true;
					aphid.skin.SetSkin("idle");
				}
			}
		}

		public override bool CanFinish(Aphid aphid)
		{
			return aphid.State.Type switch
			{
				StateEnum.Busy or StateEnum.Eat or StateEnum.Breed => false,
				_ => true,
			};
		}

		public override void Finish(Aphid aphid)
		{
			// Die at the old age of old years old
			aphid.PrepareToDie();
		}
	}
	public class BreedTimer(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, TimeLeft, OneShot, autostart)
	{
        public override void Process(Aphid entity, float _delta)
		{
			// Too tired/young for breeding
			if (!entity.Instance.Status.IsAdult || entity.State.Is(StateEnum.Breed) ||
					entity.Instance.Status.Hunger < 20 || entity.Instance.Status.Thirst < 20)
				return;

			base.Process(entity, _delta);
			entity.Instance.Status.BreedBuildup = TimeLeft;
		}
		public override bool CanFinish(Aphid aphid)
		{
			return aphid.State.Is(StateEnum.Idle);
		}

		public override void Finish(Aphid aphid)
		{
			aphid.Instance.Status.BreedMode = BreedMode.Inactive;
			aphid.SetState(StateEnum.Breed);
		}

		public class Passive(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<AphidPassive>(BaseTime, TimeLeft, OneShot, autostart)
		{
            public override void Process(AphidPassive aphid, float _delta)
			{
				// Too tired/young for breeding
				if (!aphid.Instance.Status.IsAdult || aphid.Instance.Status.LastActiveState == StateEnum.Breed ||
						aphid.Instance.Status.Hunger < 20 || aphid.Instance.Status.Thirst < 20)
					return;

				base.Process(aphid, _delta);
				aphid.Instance.Status.BreedBuildup = TimeLeft;
			}

			public override void Finish(AphidPassive aphid)
			{
				aphid.Instance.Status.BreedMode = BreedMode.Inactive;
				aphid.Instance.Status.LastActiveState = StateEnum.Breed;
			}
			public override bool CanFinish(AphidPassive aphid)
			{
				return aphid.Instance.Status.LastActiveState == StateEnum.Idle;
			}
		}
	}
	public class HarvestTimer(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, TimeLeft, OneShot, autostart)
	{
		public override void Process(Aphid aphid, float _delta)
		{
			base.Process(aphid, _delta);
			aphid.Instance.Status.HarvestBuildup = BaseTime - TimeLeft;
		}

		public override void Finish(Aphid entity)
		{
			if (!entity.IsReadyForHarvest)
				entity.AllowHarvest();
		}

		public class Passive(float BaseTime, float TimeLeft = -1, bool OneShot = false, bool autostart = true) : CustomBaseTimer<AphidPassive>(BaseTime, TimeLeft, OneShot, autostart)
		{
            public override void Process(AphidPassive entity, float _delta)
			{
				base.Process(entity, _delta);
				entity.Instance.Status.HarvestBuildup = BaseTime - TimeLeft;
			}

			public override void Finish(AphidPassive entity)
			{
				// TODO: Autocollector could be a nice upgrade but not sure about it
			}
		}
	}

	// AREA EVENTS
	public class AphidFriendhsip : IAreaEvent
	{
		public StringNames.GlobalTags Tag => StringNames.GlobalTags.Aphid;
		private readonly static float[] aphid_interaction_weights = [50, 50];

		public void OnNodeEntered(Aphid _aphid, Node2D _node)
		{
			if (!_aphid.State.Is(StateEnum.Idle))
				return;
			Aphid _otherAphid = _node as Aphid;
			if (!_otherAphid.State.Is(StateEnum.Idle))
				return;

			// met strangers you see for the first time
			if (!_aphid.Instance.Genes.Relationships.TryGetValue(_otherAphid.Instance.GUID, out Relationship current_relationship))
			{
				current_relationship = new(_otherAphid.Instance.GUID, Relationship.RelationshipLevel.Acquaintance);
				_aphid.Instance.Genes.Relationships.Add(_otherAphid.Instance.GUID, current_relationship);
				return;
			}

			if (MISC_RNG.Randf() < 0.67f) //flip a coin on wheter it happens
				return;

			// attempt social interaction
			_aphid.SetState(StateEnum.Social);
			_aphid.skin.SetFlipDirection(_otherAphid.GlobalPosition - _aphid.GlobalPosition);
			_aphid.CreateTimer(() => SoundManager.CreateSound2D(_aphid.AudioDynamic_Idle, _aphid.GlobalPosition), 1);

			switch (MISC_RNG.RandWeighted(aphid_interaction_weights))
			{
				case 0: // get angry at interaction
					_aphid.CreateTimer(() =>
					{
						current_relationship.AddToTotal(-3);
						GlobalManager.EmitParticles("anger", _aphid.GlobalPosition, false);
					}, 2);
					return;
				case 1: // get pleased at interaction
					_aphid.CreateTimer(() =>
					{
						current_relationship.AddToTotal(3);
						GlobalManager.EmitParticles("heart", _aphid.GlobalPosition, false);
						_aphid.skin.DoHop();
					}, 2);
					return;
			}
		}

		public void OnNodeExited(Aphid _aphid, Node2D _node)
		{
			return;
		}
	}

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
			DebugLogger.Print(DebugLogger.LogPriority.Debug, $"AphidActions: {aphid.Instance.ID} has been set to busy.");
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

		public void Awake(Aphid aphid)
		{
			idle_timer = new(MAX_IDLE_TIME, true, false);
			idle_timer.OnFinish.Add(GetNewIdlePoint);
			aphid.Timers.Add(idle_timer);

			timeout_timer = new(MAX_TIMEOUT_TIME, true, false);
			timeout_timer.OnFinish.Add(StandStill);
			aphid.Timers.Add(timeout_timer);
		}
		public void Enter(Aphid aphid, StateEnum _previous, EventArgs _args)
		{
			idle_timer.decay_rate = aphid.ValueFlags[ValueFlagsEnum.IdleTimeMultiplier];
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
			if (!idle_timer.IsFinished)
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
			aphid.SetMovementDirection(Vector2.Zero);
			// when finished, the idle timer will generate a new point and the cycle repeats
			idle_timer.Start();
		}
		public void GetNewIdlePoint(Aphid aphid)
		{
			target_position = new Vector2(MISC_RNG.RandfRange(-MAX_IDLE_RANGE, MAX_IDLE_RANGE),
					MISC_RNG.RandfRange(-MAX_IDLE_RANGE, MAX_IDLE_RANGE)) + aphid.GlobalPosition;
			aphid.SetMovementDirection(target_position, true);
			timeout_timer.Start();
		}

		public class IdleTimer(float BaseTime, bool OneShot = false, bool autostart = true) : CustomTimer<Aphid>(BaseTime, OneShot, autostart)
		{
			public float decay_rate = 1;

            public override float GetTimerTime()
			{
				return MISC_RNG.RandfRange(BaseTime, BaseTime * 2) * decay_rate;
			}
		}
	}
	public class HungryState : IState
	{
		public StateEnum Type => StateEnum.Hungry;
		public StateEnum[] TransitionList => null;
		public bool TransitionToAnything => true;
		public bool CanBeStartingState => false;

		// Eating Params
		public List<FoodArgs> nearby_food = [];
		public FoodArgs current_target;
		private CustomTimer<Aphid> food_timeout_timer;

		private const int MAX_FOOD_RANGE = 200 * 200, MIN_FOOD_RANGE = 40 * 40, MAX_TIMEOUT_BASE = 10;

		public class FoodArgs : EventArgs
		{
			public Node2D node;
			public bool is_favorite;
			public AphidData.FoodType flavor;
		}

		public void Awake(Aphid aphid)
		{
			food_timeout_timer = new(MAX_TIMEOUT_BASE, true, false);
			food_timeout_timer.OnFinish.Add((_aphid) =>
			{
				current_target = nearby_food[MISC_RNG.RandiRange(0, nearby_food.Count - 1)];
			});
			aphid.Timers.Add(food_timeout_timer);
		}
		public void Enter(Aphid aphid, StateEnum _previous, EventArgs _args)
		{
			current_target = nearby_food[0];
			food_timeout_timer.Start(MAX_TIMEOUT_BASE);

			List<FoodArgs> _validFood = [];
			for (int i = 0; i < nearby_food.Count; i++)
			{
				if (IsInstanceValid(nearby_food[i].node))
					_validFood.Add(nearby_food[i]);
			}
			nearby_food = [.. _validFood];
		}
		public void Exit(Aphid aphid, StateEnum _next)
		{
			current_target = null;
			food_timeout_timer.Stop();
		}
		public void Process(Aphid aphid, float delta)
		{
			// if is not valid, too far away, or claimed by someone, let go
			if (current_target == null 
				|| !IsInstanceValid(current_target.node) 
				|| aphid.GlobalPosition.DistanceSquaredTo(current_target.node.GlobalPosition) > MAX_FOOD_RANGE 
				|| !(bool)current_target.node.GetMeta(StringNames.PickupMeta))
			{
				aphid.SetState(StateEnum.Idle);
				return;
			}

			// Select the closest food item
			for (int i = 0; i < nearby_food.Count; i++)
			{
				// if they are both at the same level of priority(favorite) then choose the closest one
				if (nearby_food[i].is_favorite == current_target.is_favorite)
				{
					if (aphid.GlobalPosition.DistanceSquaredTo(nearby_food[i].node.GlobalPosition)
						< aphid.GlobalPosition.DistanceSquaredTo(current_target.node.GlobalPosition))
						continue;
				}
				else if (!nearby_food[i].is_favorite) // otherwise, if the new one isnt favorite, ignore it
					continue;

				current_target = nearby_food[i];
				food_timeout_timer.Start(10);
			}

			// Walk towards the food item, once close, begin the eating state
			if (aphid.GlobalPosition.DistanceSquaredTo(current_target.node.GlobalPosition) > MIN_FOOD_RANGE)
				aphid.SetMovementDirection(current_target.node.GlobalPosition - aphid.GlobalPosition);
			else
			{
				aphid.SetMovementDirection(Vector2.Zero);
				current_target.node.SetMeta(StringNames.PickupMeta, false); // Stops others from eating it
				current_target.node.GlobalPosition = aphid.GlobalPosition + (aphid.skin.IsFlipped ? new Vector2(-25, -10) : new Vector2(25, -10));
				aphid.SetState(StateEnum.Eat, current_target);
			}
		}

		public bool OnNodeExited(Node2D _node)
		{
			var _food = nearby_food.Find((f) => f.node.Equals(_node));

			if (_food != null)
			{
				nearby_food.Remove(_food);
				return true;
			}
			return false;
		}

		public class FoodTrigger : IAreaEvent
		{
			public StringNames.GlobalTags Tag => StringNames.GlobalTags.Food;
			public List<ulong> ignored = [];

			public void OnNodeEntered(Aphid aphid, Node2D _node)
			{
				if (ignored.Contains(_node.GetInstanceId()))
					return;

				// its marked to not be picked up or already in list
				if (!(bool)_node.GetMeta(StringNames.PickupMeta))
				{
					ignored.Add(_node.GetInstanceId());
					return;
				}

				FoodData _current_food = GlobalManager.G_FOOD[_node.GetMeta(StringNames.IdMeta).ToString()];
				var _flavor = _current_food.Type;
				bool _isfavorite = aphid.Instance.Genes.FoodPreference == _flavor;
				bool _isPickyEater = aphid.BoolFlags[BoolFlagsEnum.IsPicky];

				// if Vile, reject it cause yucky, unless you like it for some reason
				if (_flavor == AphidData.FoodType.Vile && _isfavorite)
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
				if (_isPickyEater || !_isfavorite && !aphid.BoolFlags[BoolFlagsEnum.CanOvereat])
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

				(aphid.ActiveStates[StateEnum.Hungry] as HungryState).nearby_food.Add(new()
				{
					node = _node,
					is_favorite = _isfavorite,
					flavor = _flavor
				});
				ignored.Add(_node.GetInstanceId());
				aphid.SetState(StateEnum.Hungry);
			}

			public void OnNodeExited(Aphid _aphid, Node2D _node)
			{
				(_aphid.ActiveStates[StateEnum.Hungry] as HungryState).OnNodeExited(_node);
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

		private HungryState.FoodArgs current_target;

		public void Awake(Aphid aphid)
		{
			gobble_timer = new(MAX_GOBBLE_TIME, true, false);
			gobble_timer.OnFinish.Add(FinishFood);
			aphid.Timers.Add(gobble_timer);
		}
		public void Enter(Aphid aphid, StateEnum _previous, EventArgs _specialArgs)
		{
			current_target = _specialArgs as HungryState.FoodArgs;
			gobble_timer.Start(MAX_GOBBLE_TIME);
		}

		public void Exit(Aphid aphid, StateEnum _next)
		{
			gobble_timer.Stop();
			current_target = null;
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
			float _multi = aphid.Instance.Genes.FoodMultipliers[(int)_food.Type];
			if (_food.FoodValue > 0)
				aphid.Instance.AddHunger(_food.FoodValue * _multi);

			if (_food.DrinkValue > 0)
				aphid.Instance.AddThirst(_food.DrinkValue * _multi);

			// set skill values, cannot gain skill if we are full
			if (_food.Skills.Count > 0 && (aphid.Instance.Status.Hunger <= 90 || aphid.Instance.Status.Thirst <= 90))
			{
				foreach (var _pair in _food.Skills)
					aphid.Instance.Genes.Skills[AphidData.SkillNames[(int)_pair.Key]].GivePoints(
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
		private RestGain rest_timer;

		public void Awake(Aphid _aphid)
		{
			rest_timer = new(BASE_SLEEP_GAIN, false, false);
			_aphid.Timers.Add(rest_timer);
		}

		public void Enter(Aphid aphid, StateEnum _previous, EventArgs _args)
		{
			rest_timer.multiplier = aphid.ValueFlags[ValueFlagsEnum.RestTimeMultitplier];
			rest_timer.Start(BASE_SLEEP_GAIN);

			aphid.skin.SetEyesSkin("sleep");
			aphid.skin.SetLegsSkin("sleep");
			sleep_effect = GlobalManager.EmitParticles("sleep", aphid.GlobalPosition);
			aphid.skin.Position = new(0, 2);
		}

		public void Exit(Aphid aphid, StateEnum _next)
		{
			aphid.skin.SetSkin("idle");
			aphid.skin.Position = new(0, 0);
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
			pet_timer = new(PET_DURATION, false, false);
			pet_timer.OnFinish.Add(FinishPet);
			pet_timer.Stop();
			aphid.Timers.Add(pet_timer);
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
			aphid.skin.SetFlipDirection(Player.Instance.GlobalPosition - aphid.GlobalPosition);
		}
		public void Exit(Aphid aphid, StateEnum _next)
		{
			pet_timer.Stop();
		}
		public void Process(Aphid aphid, float delta)
		{
			return;
		}

		public void FinishPet(Aphid aphid)
		{
			if (aphid.Instance.Status.Affection <= 90)
			{
				GlobalManager.EmitParticles("heart", aphid.GlobalPosition - new Vector2(0, 10));
				aphid.Instance.AddBondship(1);
			}
			aphid.Instance.AddAffection(10);
			aphid.SetState(StateEnum.Idle);
			aphid.skin.DoHop();
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
			if (aphid.Instance.Status.BreedMode == BreedMode.Inactive)
				GetRandBreedMode(aphid);
			StartBreedingBehaviour(aphid);

			breed_lookout = new();
			aphid.AreaEvents.Add(breed_lookout);

			if (_specialArgs != null)
				args = _specialArgs as BreedArgs;
		}
		public void Exit(Aphid aphid, StateEnum _next)
		{
			aphid.skin.OverrideMovementAnim = false;
			aphid.AreaEvents.Remove(breed_lookout);
			aphid.Instance.Status.BreedMode = BreedMode.Inactive;
			aphid.Timers.Find((t) => t is BreedTimer).Start();

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
				return;
			}

			// breed routine for non-breeding aphid
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
					if (!GodotObject.IsInstanceValid(breed_lookout.breed_partner) || !breed_lookout.breed_partner.State.Is(StateEnum.Breed))
					{
						aphid.SetState(StateEnum.Idle);
						return;
					}

					// wait for partner to arrive
					if (breed_lookout.breed_partner.GlobalPosition.DistanceSquaredTo(args.position) <= MIN_PARTNER_DISTANCE)
					{
						StartBreedingWithPartner(aphid);
						is_in_final_stage = true;
					}
					else
						aphid.skin.DoWalkAnim(); // waiting animation
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
					aphid.skin.OverrideMovementAnim = true;
					breed_effect = GlobalManager.EmitParticles("mating", aphid.GlobalPosition);
					break;
			}
		}
		public void StartBreedingWithPartner(Aphid aphid)
		{
			aphid.skin.OverrideMovementAnim = false; // deactivate waiting anim

			// face each other
			breed_lookout.breed_partner.skin.SetFlipDirection(breed_lookout.breed_partner.GlobalPosition - aphid.GlobalPosition);
			aphid.skin.SetFlipDirection(aphid.GlobalPosition - breed_lookout.breed_partner.GlobalPosition);

			// start laying egg
			_ = breed_lookout.breed_partner.skin.DoDance();
			StartLayingEgg(aphid);

			// BREED (the function automatically terminates breed state)
			aphid.LayAnEgg(breed_lookout.breed_partner.Instance);
		}
		public void StartLayingEgg(Aphid aphid)
		{
			Task _dance = aphid.skin.DoDance();
			_dance.ContinueWith((_task) =>
			{
				aphid.LayAnEgg(aphid.Instance, true);
			});
		}
		public void GetRandBreedMode(Aphid aphid)
		{
			if (GameManager.Aphids.Count == 1)
				aphid.Instance.Status.BreedMode = BreedMode.WithItself; // This is to make sure new games get a second aphid as soon as possible
			else
				aphid.Instance.Status.BreedMode = (BreedMode)GlobalManager.Utils.GetRandomByWeight(MISC_RNG, breeding_weights);
		}

		public class BreedTrigger : IAreaEvent
		{
			public StringNames.GlobalTags Tag => StringNames.GlobalTags.Aphid;
			public Aphid breed_partner;

			public void OnNodeEntered(Aphid _aphid, Node2D _node)
			{
				if (breed_partner != null)
					return;

				Aphid _partner = _node as Aphid;

				// only get a partner that is also an adult and its in the mood
				if (!_partner.Instance.Status.IsAdult ||
					_partner.Instance.Status.Hunger < 10 ||
					_partner.Instance.Status.Thirst < 10)
					return;

				if (!_aphid.Instance.Genes.Relationships.TryGetValue(_partner.Instance.GUID, out var _relation) ||
						_relation.Total < -20)
					return;

				if (!_partner.State.Is(StateEnum.Idle))
					return;

				breed_partner = _partner;
				_partner.Instance.Status.BreedMode = BreedMode.AsPartner;
				_partner.SetState(StateEnum.Breed);
				// _partner.GlobalPosition = _aphid.GlobalPosition + (_aphid.skin.IsFlipped ? new(-40, -5) : new(40, -5));
				_partner.skin.DoHop();
				GlobalManager.EmitParticles("heart", _partner.GlobalPosition, false);
			}

			public void OnNodeExited(Aphid _aphid, Node2D _node)
			{
				return;
			}
		}
	}
	public class TrainState : IState
	{
		public StateEnum Type => StateEnum.Train;
		public StateEnum[] TransitionList => [StateEnum.Idle];
		public bool TransitionToAnything => false;
		public bool CanBeStartingState => true;

		private TrainTimer train_timer;

		public void Awake(Aphid aphid)
		{
			train_timer = new(10, false, false);
			aphid.Timers.Add(train_timer);
		}
		public void Enter(Aphid aphid, StateEnum _previous, EventArgs args)
		{
			aphid.skin.SetSkin(StringNames.IdleAnim);

			train_timer.skill_name = aphid.Instance.Status.CurrentTraining.Skill.ToString().ToLower();
			train_timer.last_level = aphid.Instance.Genes.Skills[train_timer.skill_name].Level;

			train_timer.Start(aphid.Instance.Status.CurrentTraining.BaseTime);
		}
		public void Exit(Aphid aphid, StateEnum _next)
		{
			train_timer = null;
			aphid.skin.OverrideMovementAnim = false;
		}
		public void Process(Aphid aphid, float delta)
		{
			if (aphid.Instance.Status.Tiredness > AphidData.MAX_TIREDNESS_SLEEP)
			{
				aphid.SetState(StateEnum.Idle);
				return;
			}
			train_timer.Process(aphid, delta);
		}

		public class TrainTimer(float BaseTime, bool OneShot = false, bool autostart = true) : CustomBaseTimer<Aphid>(BaseTime, OneShot, autostart)
		{
			public int last_level;
			public string skill_name;

            public override void Finish(Aphid entity)
			{
				TrainData _data = entity.Instance.Status.CurrentTraining;
				entity.Instance.Genes.Skills[_data.Skill.ToString().ToLower()].GivePoints(
						_data.PointGain * (!entity.Instance.Status.IsAdult ? 2 : 1));
				if (entity.Instance.Genes.Skills[skill_name].Level != last_level) // give on levelup
				{
					last_level = entity.Instance.Genes.Skills[skill_name].Level;
					SoundManager.CreateSound2D("aphid/skill_gain", entity.GlobalPosition);
				}
			}
		}
	}

	// Unimplemented
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
			return;
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
			return;
		}
		public void Process(Aphid aphid, float delta)
		{
			return;
		}
	}
}
