using Godot;
using System;
using System.Collections.Generic;
using static Aphid;

public class AphidActions
{
	public class HungerDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }
		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (TimeLeft > 0)
				TimeLeft -= _delta;
			else
			{
				aphid.Instance.Status.AddHunger(-1);
				TimeLeft = AphidData.Food_Drain_Time;
			}
		}
	}
	public class ThirstDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }
		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (TimeLeft > 0)
				TimeLeft -= _delta;
			else
			{
				aphid.Instance.Status.AddThirst(-1);

				TimeLeft = aphid.State.Is(StateEnum.Sleep) ?
					AphidData.Water_Drain_Time * 2f :
					AphidData.Water_Drain_Time;
			}
		}
	}
	public class RestDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }
		private const float sleep_decay = 9.5f;
		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (aphid.State.Is(StateEnum.Sleep))
				return;

			if (TimeLeft > 0)
				TimeLeft -= _delta;
			else
			{
				aphid.Instance.Status.AddTiredness(aphid.rng.RandfRange(0.5f, 1.5f));
				TimeLeft = sleep_decay;

				// cannot sleep if is not tired enough and the state isnt idle
				if (aphid.Instance.Status.Tiredness < 75 || !aphid.State.Is(StateEnum.Idle))
					return;

				// small chance to sleep every tick, chances increase the more sleepiness you have
				if (aphid.Instance.Status.Tiredness == 100 || GlobalManager.Utils.GetRandomByWeight
						([100 - aphid.Instance.Status.Tiredness, aphid.Instance.Status.Tiredness]) == 1)
					aphid.SetState(StateEnum.Sleep);
			}
		}
	}
	public class AffectionDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }

		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (aphid.State.Is(StateEnum.Pet))
				return;

			if (TimeLeft > 0)
				TimeLeft -= _delta;
			else
			{
				aphid.Instance.Status.AddAffection(-1);
				TimeLeft = AphidData.Care_Drain_Time;
			}
		}
	}
	public class BondshipDecay : IDecayEvent
	{
		public const float bondship_cooldown_base = 700f;
		public const float bondship_decay_base = 50f;
		public float TimeLeft { get; set; }

		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (aphid.State.Is(StateEnum.Pet))
				TimeLeft = bondship_cooldown_base;
			else
			{
				TimeLeft -= _delta;
				if (TimeLeft <= 0)
				{
					TimeLeft = bondship_decay_base;
					aphid.Instance.Status.AddBondship(-1);
				}
			}
		}
		public void Start(Aphid aphid, EventArgs args)
		{
			TimeLeft = bondship_cooldown_base;
		}
	}
	public class LifetimeDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }

		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			// All things have an end, specifically, a few hours after birth
			aphid.Instance.Status.Age += _delta;

			// Grow up into an adult
			if (!aphid.Instance.Status.IsAdult)
			{
				if (aphid.State.Is(StateEnum.Idle) &&
						aphid.Instance.Status.Age > AphidData.Age_Adulthood)
				{
					aphid.Instance.Status.IsAdult = true;
					aphid.skin.SetSkin("idle");
				}
			}
			// Die at the old age of old years old
			else if (aphid.Instance.Status.Age > AphidData.Age_Death)
				aphid.PrepareToDie();
		}
	}

	/* NOT IMPLEMENTED */
	public class HealthDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }

		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{

		}
	}

	public class AphidInteraction : ITriggerEvent
	{
		public string TriggerID => StringNames.InteractableTag;

		public void OnTrigger(Aphid _aphid, Node2D _node, EventArgs _args)
		{
			// Attempts interacting with the CollisionObject itself
			if (_node is IInteractEvent)
				_node.CallDeferred(StringNames.InteractFunc, _aphid);
			// Otherwise, attempts to interact with its parent instead
			else if (_node.GetParent() is IInteractEvent)
				_node.GetParent().CallDeferred(StringNames.InteractFunc, _aphid);
		}
	}
	public class AphidFriendhsip : ITriggerEvent
	{
		public string TriggerID => "aphid";
		private readonly static float[] aphid_interaction_weights = [50, 50];

		public void OnTrigger(Aphid _aphid, Node2D _node, EventArgs _args)
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
			if (_aphid.rng.Randf() < 0.67f) //flip a coin on wheter it happens
				return;

			// attempt social interaction
			(_aphid.StateArgs as IdleState.IdleArgs).stand_time = (_otherAphid.StateArgs as IdleState.IdleArgs).stand_time = 5;
			_aphid.skin.SetFlipDirection(_otherAphid.GlobalPosition - _aphid.GlobalPosition);
			_aphid.CreateTimer(() => SoundManager.CreateSound2D(_aphid.AudioDynamic_Idle, _aphid.GlobalPosition), 1);

			switch (_aphid.rng.RandWeighted(aphid_interaction_weights))
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
						_aphid.skin.DoJumpAnim();
					}, 2);
					return;
			}
		}
	}

	public class BusyState : IState
	{
		public StateEnum Type => StateEnum.Busy;
		public StateEnum[] TransitionList => null;
		public bool TransitionToAnything = true;
		public bool Locked { get; set; }

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			Logger.Print(Logger.LogPriority.Debug, $"AphidActions: {aphid.Instance.ID} has been set to busy.");
		}
		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			return;
		}
		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			return;
		}
	}
	public class IdleState : IState
	{
		public StateEnum Type => StateEnum.Idle;
		public StateEnum[] TransitionList => null;
		public bool TransitionToAnything => true;
		public bool Locked { get; set; }

		private const int idle_rand_range = 50;
		private const float idle_timer_range = 1.15f, idle_timeout = 5f;

		public class IdleArgs(Vector2 target_position, float stand_time, float timeout = 0) : EventArgs
		{
			public Vector2 target_position = target_position;
			public float stand_time = stand_time;
			public float timeout = timeout;
			public float decay_rate = 1;
		}

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			if (args is IdleArgs)
				aphid.StateArgs = args as IdleArgs;
			else
				aphid.StateArgs = new IdleArgs(
					aphid.GlobalPosition,
					aphid.rng.RandfRange(idle_timer_range, idle_timer_range * 2)
				);
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			return;
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			IdleArgs _args = args as IdleArgs;
			// standing still wait time
			if (_args.stand_time > 0)
			{
				aphid.SetMovementDirection(Vector2.Zero);
				_args.timeout = 0;
				_args.stand_time -= delta * _args.decay_rate;
				return;
			}

			// we are close to idle pos, generate a new one and stand still for a few seconds
			if (aphid.GlobalPosition.DistanceSquaredTo(_args.target_position) < 400)
			{
				_args.target_position = new Vector2(aphid.rng.RandfRange(-idle_rand_range, idle_rand_range),
					aphid.rng.RandfRange(-idle_rand_range, idle_rand_range)) + aphid.GlobalPosition;
				aphid.SetMovementDirection(Vector2.Zero);
				_args.stand_time = aphid.rng.RandfRange(idle_timer_range, idle_timer_range * 2);
				_args.timeout = 0;
				return;
			}

			// move to idle pos, timeout if you cant
			aphid.SetMovementDirection(_args.target_position - aphid.GlobalPosition);
			_args.timeout += delta;
			if (_args.timeout > idle_timeout)
				_args.target_position = aphid.GlobalPosition;
		}
	}
	public class HungryState : IState, ITriggerEvent
	{
		public StateEnum Type => StateEnum.Hungry;
		public StateEnum[] TransitionList => [StateEnum.Idle, StateEnum.Hungry, StateEnum.Eat, StateEnum.Pet];
		public bool Locked { get; set; }
		public string TriggerID => "food";

		// Eating Params
		private const float food_pursue_duration = 5f;
		private Timer food_pursue_timer, food_gc_timer;
		public readonly List<Node2D> food_ignore_list = new();
		public bool only_favorites, allow_overconsume;

		public class HungryArgs : EventArgs
		{
			public Node2D food_item;
			public bool is_favorite;
			public AphidData.FoodType flavor;
		}

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			aphid.StateArgs = args;
			if (food_gc_timer != null)
				return;
			food_gc_timer = new();
			aphid.AddChild(food_gc_timer);
			food_gc_timer.Timeout += () => food_ignore_list.Clear();
			food_gc_timer.Start(30);
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			food_pursue_timer.Stop();
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			ref Node2D _food_item = ref (aphid.StateArgs as HungryArgs).food_item;

			// if is not valid, too far away, or claimed by someone, let go
			if (!GameManager.IsInstanceValid(_food_item) || aphid.GlobalPosition.DistanceTo(_food_item.GlobalPosition) > 200
				|| !(bool)_food_item.GetMeta(StringNames.PickupMeta) || !_food_item.HasMeta(StringNames.TagMeta))
			{
				aphid.SetState(StateEnum.Idle);
				return;
			}

			// If close, eat it, else try walk to it
			if (aphid.GlobalPosition.DistanceTo(_food_item.GlobalPosition) < 40)
			{
				aphid.SetMovementDirection(Vector2.Zero);
				_food_item.RemoveMeta(StringNames.TagMeta); // Stops others from eating it
				_food_item.SetMeta(StringNames.PickupMeta, false);
				_food_item.GlobalPosition = aphid.GlobalPosition + (aphid.skin.IsFlipped ? new Vector2(-25, -10) : new Vector2(25, -10));

				for (int i = 0; i < _food_item.GetChildCount(); i++)
				{
					if (_food_item.GetChild(i).IsClass("CollisionShape2D"))
					{
						(_food_item.GetChild(i) as CollisionShape2D).Disabled = true;
						break;
					}
				}
				food_pursue_timer.Stop();
				aphid.SetState(StateEnum.Eat, args);
			}
			else if (food_pursue_timer.TimeLeft > 0)
				aphid.SetMovementDirection(_food_item.GlobalPosition - aphid.GlobalPosition);
		}

		public void OnTrigger(Aphid aphid, Node2D _node, EventArgs args)
		{
			HungryArgs _args = args is HungryArgs ? args as HungryArgs : null;
			// ignore if:
			// its currently consuming
			// its not in a proper state
			// its already pursuing this item
			// its marked to not be picked up
			// or its marked for ignore
			if (aphid.State.Is(StateEnum.Eat))
				return;

			if (!aphid.State.CanTransitionInto(StateEnum.Hungry))
				return;

			if (_args?.food_item != null && _node.Equals(_args.food_item))
				return;

			if (!(bool)_node.GetMeta(StringNames.PickupMeta))
				return;

			if (food_ignore_list.Exists(_node.Equals))
				return;

			// TODO: food item should simply be able to be replaced by another once the food chase timeouts
			// also do a quick raycast check to see if the food item is in direct path

			var _current_food = GlobalManager.G_FOOD[_node.GetMeta(StringNames.IdMeta).ToString()];
			var _flavor = _current_food.type;
			// if Vile, reject it cause yucky, unless you like it for some reason
			if (_flavor == AphidData.FoodType.Vile && _flavor != aphid.Instance.Genes.FoodPreference)
				return;

			// If not hungy/thirsty, cancel, items that give both, wait until you are full to return
			// favorites and certain traits ignore how full you are
			var _isfavorite = aphid.Instance.Genes.FoodPreference == _flavor;
			if (!_isfavorite && !allow_overconsume)
			{
				bool _givesFood = _current_food.food_value > 0, _givesDrink = _current_food.drink_value > 0;

				if (_givesDrink && aphid.Instance.Status.Thirst >= (_givesFood ? 100 : 80)
						|| _givesFood && aphid.Instance.Status.Hunger >= (_givesDrink ? 100 : 80))
				{
					food_ignore_list.Add(_node);
					return;
				}
			}

			// this checks our current food item to the new one
			if (_args != null)
			{
				// if they are both at the same level of priority(favorite) then choose the closest one 
				if (_isfavorite == _args.is_favorite)
				{
					if (aphid.GlobalPosition.DistanceTo(_node.GlobalPosition) > aphid.GlobalPosition.DistanceTo(_args.food_item.GlobalPosition))
						return;
				}
				else if (!_isfavorite) // otherwise, if the new one isnt favorite, ignore it
					return;
			}
			// if we dont have one, and we are picky, and this isnt a favorite(or a neutral food), ignore it
			else if (only_favorites && !_isfavorite && _flavor != AphidData.FoodType.Neutral)
			{
				food_ignore_list.Add(_node);
				return;
			}

			SetFoodItem(aphid, _node, _flavor, _isfavorite);
		}
		private void SetFoodItem(Aphid aphid, Node2D _food_item, AphidData.FoodType _flavor, bool _isfavorite)
		{
			// Set current food item to pursue
			food_pursue_timer = new()
			{
				OneShot = true
			};
			food_pursue_timer.Timeout += () =>
			{
				if (aphid.State.Is(StateEnum.Hungry))
				{
					food_ignore_list.Add(_food_item);
					aphid.SetState(StateEnum.Idle);
				}
			};
			aphid.AddChild(food_pursue_timer);
			food_pursue_timer.Start(food_pursue_duration);
			aphid.skin.SetFlipDirection(_food_item.GlobalPosition - aphid.GlobalPosition);
			aphid.SetState(StateEnum.Hungry, new HungryArgs()
			{
				food_item = _food_item,
				is_favorite = _isfavorite,
				flavor = _flavor
			});
		}
	}
	public class EatingState : IState
	{
		public StateEnum Type => StateEnum.Eat;
		public StateEnum[] TransitionList => [StateEnum.Idle];
		public bool Locked { get; set; }

		private int foodgobble_shutter_speed;
		private bool food_item_switch;
		protected float gobble_timer;
		private const float gobble_duration = 2f;

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			aphid.StateArgs = args;
			gobble_timer = gobble_duration;
			Locked = true;
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			return;
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			// Gobbling it up, yum yum
			gobble_timer -= delta;
			ref Node2D _food_item = ref (aphid.StateArgs as HungryState.HungryArgs).food_item;
			if (!GameManager.IsInstanceValid(_food_item))
			{
				Locked = false;
				aphid.SetState(StateEnum.Idle);
				Logger.Print(Logger.LogPriority.Error, "AphidActions: Food Item was taken away prematurely");
				return;
			}

			// eating motion
			if (foodgobble_shutter_speed == 0)
			{
				_food_item.GlobalPosition += food_item_switch ? Vector2.Up : Vector2.Down;
				if (!_food_item.Scale.IsEqualApprox(Vector2.Zero))
					_food_item.Scale -= new Vector2(0.05f, 0.05f);
				food_item_switch = !food_item_switch;
				foodgobble_shutter_speed = 8;
				SoundManager.CreateSound2D(Audio_Nom, aphid.GlobalPosition);
			}
			else
				foodgobble_shutter_speed--;

			// finished meal
			if (gobble_timer <= 0)
			{
				// set food values
				GlobalManager.Food _food = GlobalManager.G_FOOD[_food_item.GetMeta(StringNames.IdMeta).ToString()];
				float _multi = aphid.Instance.Genes.FoodMultipliers[(int)_food.type];
				if (_food.food_value > 0)
					aphid.Instance.Status.AddHunger(_food.food_value * _multi);

				if (_food.drink_value > 0)
					aphid.Instance.Status.AddThirst(_food.drink_value * _multi);

				// set skill values
				if (_food.skill_list != null && _food.skill_list.Length > 0)
				{
					for (int i = 0; i < _food.skill_list.Length; i++)
						aphid.Instance.Genes.Skills[_food.skill_list[i]].GivePoints(_food.skill_values[i]);
				}

				// Dispose of the food item now
				_food_item.QueueFree();
				Locked = false;
				aphid.SetState(StateEnum.Idle);
			}
		}
	}
	public class SleepState : IState
	{
		public StateEnum Type => StateEnum.Sleep;
		public StateEnum[] TransitionList => [StateEnum.Idle, StateEnum.Pet];
		public bool Locked { get; set; }

		private float sleep_gain_timer;
		private const float sleep_gain = 3.5f;
		private GpuParticles2D sleep_effect;

		public class SleepArgs : EventArgs
		{
			public float gain_rate = 1;
			public bool heavysleeper = false;
		}

		public void Awake(Aphid aphid, EventArgs args)
		{
			aphid.SetState(StateEnum.Sleep);
		}

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			aphid.skin.SetEyesSkin("sleep");
			aphid.skin.SetLegsSkin("sleep");
			sleep_effect = GlobalManager.EmitParticles("sleep", aphid.GlobalPosition);
			aphid.skin.Position = new(0, 2);
			aphid.StateArgs = new SleepArgs();
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			aphid.skin.SetSkin("idle");
			aphid.skin.Position = new(0, 0);
			sleep_effect.OneShot = true;
			sleep_effect = null;
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			SleepArgs _args = args as SleepArgs;
			sleep_effect.GlobalPosition = aphid.GlobalPosition; // if picked up, update the position of effect
			if (sleep_gain_timer > 0)
				sleep_gain_timer -= delta;
			else
			{
				aphid.Instance.Status.AddTiredness(-1 * _args.gain_rate);
				sleep_gain_timer = sleep_gain;
				// bare minimum sleep
				if (aphid.Instance.Status.Tiredness > 15)
					return;

				// chance to wake up after getting enough sleep
				if (aphid.Instance.Status.Tiredness == 0 || GlobalManager.Utils.GetRandomByWeight
						([100 - aphid.Instance.Status.Tiredness, aphid.Instance.Status.Tiredness]) == 0)
					aphid.WakeUp(false, true);
			}
		}
	}
	public class PetState : IState
	{
		public StateEnum Type => StateEnum.Pet;
		public StateEnum[] TransitionList => [StateEnum.Idle];
		public bool Locked { get; set; }

		private float pet_timer;

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			pet_timer = AphidData.PET_DURATION;
			Player.Instance.SetDisabled(true);
			Player.Instance.RunDisabledTimer(AphidData.PET_DURATION, false, false);

			// visuals
			Player.Instance.SetPlayerAnim("pet");
			Player.Instance.SetFlipDirection(aphid.GlobalPosition - Player.Instance.GlobalPosition);
			aphid.skin.SetFlipDirection(Player.Instance.GlobalPosition - aphid.GlobalPosition);
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			return;
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			if (pet_timer > 0)
				pet_timer -= delta;
			else
			{
				// If low on affection, raise bondship too
				if (aphid.Instance.Status.Affection <= 90)
				{
					GlobalManager.EmitParticles("heart", aphid.GlobalPosition - new Vector2(0, 10));
					aphid.Instance.Status.AddBondship(1);
				}
				aphid.Instance.Status.AddAffection(10);
				aphid.SetState(StateEnum.Idle);
				aphid.skin.DoJumpAnim();
			}
		}
	}
	public class BreedState : IState, ITriggerEvent, IDecayEvent
	{
		public StateEnum Type => StateEnum.Breed;
		public StateEnum[] TransitionList => [StateEnum.Idle];
		public bool Locked { get; set; }

		public string TriggerID => Tag;
		public float TimeLeft { get; set; }

		private readonly float[] breeding_weights = [70, 30];
		private const int BREED_TIMEOUT_BASE = 80, PARTNER_TIMEOUT_BASE = 8;
		public enum BreedEnum { Inactive = -1, WithItself = 0, WithPartner = 1, Starting = 2 }

		private Aphid breed_partner;
		private float breed_timeout_timer, partner_timeout;
		private GpuParticles2D breed_effect;

		public class BreedArgs : EventArgs
		{
			public Vector2 position;
			public bool is_in_final_stage;
		}

		public void Awake(Aphid aphid, EventArgs args)
		{
			SetBreed(aphid, aphid.Instance.Status.BreedMode);
		}

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			aphid.StateArgs = args is BreedArgs ? args : new BreedArgs();
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			aphid.skin.OverrideMovementAnim = false;
			aphid.TriggerActions.Remove(this);
			aphid.Instance.Status.BreedMode = BreedEnum.Inactive;

			breed_partner?.SetState(StateEnum.Idle);
			if (breed_effect != null)
				breed_effect.OneShot = true;
			breed_effect = null;

			if (breed_partner != null)
				breed_partner.Instance.Status.BreedMode = BreedEnum.Inactive;
			breed_partner = null;
		}

		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (!aphid.Instance.Status.IsAdult || aphid.State.Is(Type) ||
					aphid.Instance.Status.Hunger < 10 || aphid.Instance.Status.Thirst < 10)
				return;

			if (aphid.Instance.Status.BreedBuildup < AphidData.Breed_Cooldown)
				aphid.Instance.Status.BreedBuildup += _delta;
			else if (aphid.State.Is(StateEnum.Idle))
				SetBreed(aphid);
		}
		public async void SetBreed(Aphid aphid, BreedEnum _mode = BreedEnum.Starting)
		{
			if (_mode == BreedEnum.Inactive)
				return;

			// Set breed state
			aphid.SetState(StateEnum.Breed);
			GlobalManager.EmitParticles("mating", aphid.GlobalPosition).OneShot = true;

			// Set a new breed mode, setting to 0 or 1 means we are the mother
			// otherwise -1 means we are just a partner, thus do nothing
			if (_mode == BreedEnum.Starting)
			{
				if (GameManager.Aphids.Count == 1)
					_mode = BreedEnum.WithItself; // This is to make sure new games get a second aphid as soon as possible
				else
					_mode = (BreedEnum)GlobalManager.Utils.GetRandomByWeight(aphid.rng, breeding_weights);
				aphid.Instance.Status.BreedMode = _mode;
			}

			aphid.skin.OverrideMovementAnim = true;
			if (_mode == BreedEnum.WithPartner) // Try finding a pardner around to mate
			{
				aphid.TriggerActions.Add(this);
				breed_timeout_timer = BREED_TIMEOUT_BASE;
				breed_effect = GlobalManager.EmitParticles("mating", aphid.GlobalPosition);
			}
			else if (_mode == BreedEnum.WithItself) // Mate with yourself
			{
				breed_effect = GlobalManager.EmitParticles("heart", aphid.GlobalPosition, false);
				breed_effect.OneShot = false;
				await aphid.skin.DoDanceAnim();
				aphid.LayAnEgg(aphid.Instance, true);
			}
		}
		// constantly calls aphids around to complete the breeding process
		public void OnTrigger(Aphid _aphid, Node2D _node, EventArgs _args)
		{
			Aphid _partner = _node as Aphid;
			if (breed_partner != null)
				return;

			// only get a partner that is also an adult and its in the mood
			if (!_partner.Instance.Status.IsAdult ||
				_partner.Instance.Status.Hunger < 10 ||
				_partner.Instance.Status.Thirst < 10)
				return;

			if (_aphid.Instance.Genes.Relationships.TryGetValue(_partner.Instance.GUID, out var _relation) &&
					_relation.Total < -10)
			return;

			if (!_partner.State.Is(StateEnum.Idle))
				return;

			breed_partner = _partner;
			_partner.skin.DoJumpAnim();
			GlobalManager.EmitParticles("heart", _partner.GlobalPosition, false);
			_partner.SetState(Type, new BreedArgs()
			{
				position = _aphid.GlobalPosition + (_aphid.skin.IsFlipped ? new(-40, -5) : new(40, -5)),
			});
			partner_timeout = PARTNER_TIMEOUT_BASE;
		}
		public async void Process(Aphid aphid, EventArgs args, float delta)
		{
			BreedArgs breed_args = args as BreedArgs;

			if (breed_args.is_in_final_stage)
			{
				aphid.SetMovementDirection(Vector2.Zero);
				return;
			}

			// breed routine for non-breeding aphid
			if (aphid.Instance.Status.BreedMode == BreedEnum.Inactive)
			{
				if (aphid.skin.OverrideMovementAnim)
					aphid.skin.OverrideMovementAnim = false;
				if (aphid.GlobalPosition.DistanceTo(breed_args.position) > 20)
					aphid.SetMovementDirection(breed_args.position, true);
			}
			else if (aphid.Instance.Status.BreedMode == BreedEnum.WithPartner)
			{
				// timeout
				if (breed_timeout_timer > 0)
					breed_timeout_timer -= delta;
				else
				{
					aphid.Instance.Status.BreedBuildup = AphidData.Breed_Cooldown / 5;
					aphid.SetState(StateEnum.Idle);
				}

				// partner validity
				if (!GameManager.IsInstanceValid(breed_partner) || !breed_partner.State.Is(Type))
					breed_partner = null;

				// wait for partner
				if (breed_partner != null)
				{
					// partner timeout
					if (partner_timeout > 0)
						partner_timeout -= delta;
					else
					{
						breed_partner.SetState(StateEnum.Idle);
						breed_partner = null;
						return;
					}

					Vector2 _magnitude = breed_partner.GlobalPosition - aphid.GlobalPosition;
					// Checks for distance, done this way because aphids have a larger horizontal hitbox
					// so 20 X units may not be close enough, but 20 Y units is
					if (_magnitude.X < 50 && _magnitude.X > -50 && _magnitude.Y < 20 && _magnitude.Y > -20)
					{
						// Dance
						(aphid.StateArgs as BreedArgs).is_in_final_stage = (breed_partner.StateArgs as BreedArgs).is_in_final_stage = true;
						breed_partner.skin.SetFlipDirection(_magnitude);
						aphid.skin.SetFlipDirection(aphid.GlobalPosition - breed_partner.GlobalPosition);
						breed_partner.skin.OverrideMovementAnim = true;
						_ = breed_partner.skin.DoDanceAnim();
						await aphid.skin.DoDanceAnim();

						// BREED (the function automatically terminates breed state)
						aphid.LayAnEgg(breed_partner.Instance);
					}
				}
				else
					aphid.skin.DoWalkAnim();
			}
		}
	}
	
	public class TrainState : IState
	{
		public StateEnum Type => StateEnum.Train;
		public StateEnum[] TransitionList => [StateEnum.Idle];
		public bool Locked { get; set; }

		public class TrainArgs : EventArgs
		{
			public int point_gain;
			public float gain_rate;
		}

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			aphid.StateArgs = new();
			// start chase
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{

		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			// if far from it
			// chase
			// else
			// snap to it and start anim & training

			// if training
			// gain points at rate
			// check for exhaustion, if so, cancel
		}
	}
	public class PlayState : IState
	{
		public StateEnum Type => throw new NotImplementedException();
		public StateEnum[] TransitionList => throw new NotImplementedException();
		public bool Locked { get; set; }

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			throw new NotImplementedException();
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			throw new NotImplementedException();
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			throw new NotImplementedException();
		}
	}
}
