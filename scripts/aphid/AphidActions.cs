using Godot;
using System;
using System.Collections.Generic;

public partial class AphidActions : Aphid
{
	public class HungerDecay : IDecayEvent
	{
		public const float BaseTime = 14.5f;
		public float TimeLeft { get; set; }
		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (TimeLeft > 0)
				TimeLeft -= _delta;
			else
			{
				aphid.Instance.Status.AddHunger(-1);

				TimeLeft = aphid.State.Is(StateEnum.Sleep) ?
					BaseTime * 2f :
					BaseTime;
			}
		}
	}
	public class ThirstDecay : IDecayEvent
	{
		public const float BaseTime = 8.8f;
		public float TimeLeft { get; set; }
		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (TimeLeft > 0)
				TimeLeft -= _delta;
			else
			{
				aphid.Instance.Status.AddThirst(-1);

				TimeLeft = aphid.State.Is(StateEnum.Sleep) ?
					BaseTime * 2f :
					BaseTime;
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
			}

			// Only if we are tired or are in idle (dont sleep while other action is up)
			if (aphid.Instance.Status.Tiredness < 75 || aphid.State.Is(StateEnum.Idle))
				return;

			// 1 in 12500 chance to sleep, chances increase the more sleepiness you have
			if (aphid.rng.RandiRange(0, Mathf.FloorToInt(500 * 100 - aphid.Instance.Status.Tiredness)) == 0)
				aphid.SetState(StateEnum.Sleep);
		}
	}
	public class AffectionDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }
		private const float affection_decay = 13.2f;

		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			if (aphid.State.Is(StateEnum.Pet))
				return;

			if (TimeLeft > 0)
				TimeLeft -= _delta;
			else
			{
				aphid.Instance.Status.AddAffection(-1);
				TimeLeft = aphid.State.Is(StateEnum.Sleep) ?
					affection_decay * 2f :
					affection_decay;
			}
		}
	}
	public class BondshipDecay : IDecayEvent
	{
		public const float bondship_cooldown_base = 600f;
		public const float bondship_decay_base = 20f;
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
					GD.Print("We are losing friendhship");
				}
			}
		}
		public void Start(Aphid aphid, EventArgs args)
		{
			TimeLeft = bondship_cooldown_base;
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
	public class LifetimeDecay : IDecayEvent
	{
		public float TimeLeft { get; set; }

		public void Tick(Aphid aphid, EventArgs args, float _delta)
		{
			// All things have an end, specifically, a few hours after birth
			aphid.Instance.Status.Age += _delta;

			// Grow up into an adult
			if (!aphid.Instance.Status.IsAdult && aphid.Instance.Status.Age > AphidData.adulthoodAge)
			{
				aphid.Instance.Status.IsAdult = true;
				aphid.SetState(StateEnum.Idle);
				aphid.skin.SetSkin("idle");
			}

			// Die at the old age of old years old
			if (aphid.Instance.Status.Age > AphidData.deathAge)
				aphid.Die();
		}
	}

	public class BusyState : IState
	{
		public StateEnum Type => StateEnum.Busy;

		public StateEnum[] Whitelist => null;

		public StateEnum[] Blacklist => null;

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			return;
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
		private const int idle_rand_range = 100;
		private const float idle_timer_range = 1.15f, idle_timeout = 4f;

		public class IdleArgs : EventArgs
		{
			public Vector2 target_position;
			public float timeleft, decay_rate = 1;
		}
		private float timeout;

		public StateEnum[] Whitelist { get => null; }
		public StateEnum[] Blacklist { get => null; }

		public StateEnum Type => StateEnum.Idle;

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			aphid.StateArgs = new IdleArgs()
			{
				timeleft = aphid.rng.RandfRange(idle_timer_range, idle_timer_range * 2),
				target_position = aphid.GlobalPosition
			};
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			return;
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			IdleArgs _args = args as IdleArgs;
			// standing still wait time
			if (_args.timeleft > 0)
			{
				(aphid.StateArgs as IdleArgs).timeleft -= delta * _args.decay_rate;
				return;
			}

			// we are close to idle pos, generate a new one and stand still for a few seconds
			if (aphid.GlobalPosition.DistanceTo(_args.target_position) < 20)
			{
				(aphid.StateArgs as IdleArgs).target_position = new Vector2(aphid.rng.RandfRange(-idle_rand_range, idle_rand_range),
					aphid.rng.RandfRange(-idle_rand_range, idle_rand_range)) + aphid.GlobalPosition;
				aphid.SetMovementDirection(Vector2.Zero);
				(aphid.StateArgs as IdleArgs).timeleft = aphid.rng.RandfRange(idle_timer_range, idle_timer_range * 2);
				return;
			}

			// move to idle pos, timeout if you cant
			aphid.SetMovementDirection(_args.target_position - aphid.GlobalPosition);
			timeout += delta;
			if (timeout > idle_timeout)
			{
				_args.target_position = aphid.GlobalPosition;
				timeout = 0;
			}
		}
	}
	public class HungryState : IState, ITriggerEvent
	{
		public StateEnum Type => StateEnum.Eat;
		public StateEnum[] Whitelist => new StateEnum[] { StateEnum.Idle, StateEnum.Eat };
		public StateEnum[] Blacklist => null;
		public string TriggerID => "food";

		// Eating Params
		private Node2D food_item;
		private int foodgobble_shutter_speed;
		private bool is_food_favorite, food_item_switch;
		protected float gobble_timer;
		private const float gobble_duration = 2f, food_pursue_duration = 5f;
		private Timer food_pursue_timer, food_gc_timer;
		private readonly List<Node2D> food_ignore_list = new();

		public class HungryArgs : EventArgs
		{
			public Node2D food_item;
			public AphidData.FoodType flavor;
			public bool is_food_favorite;
		} 

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			if (food_gc_timer != null)
				return;
			food_gc_timer = new();
			aphid.AddChild(food_gc_timer);
			food_gc_timer.Timeout += () => food_ignore_list.Clear();
			food_gc_timer.Start(30);
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			food_item = null;
			is_food_favorite = false;
		}

		public void Process(Aphid aphid, EventArgs args, float delta)
		{
			if (aphid.IsEating)
			{
				TickFoodGobble(aphid, args, delta);
				return;
			}

			// if is not valid, too far away, or claimed by someone, let go
			if (!IsInstanceValid(food_item) || aphid.GlobalPosition.DistanceTo(food_item.GlobalPosition) > 200
				|| !(bool)food_item.GetMeta("pickup") || !food_item.HasMeta("tag"))
			{
				aphid.SetState(StateEnum.Idle);
				return;
			}

			// If close, eat it, else try walk to it
			if (aphid.GlobalPosition.DistanceTo(food_item.GlobalPosition) < 30)
			{
				aphid.SetMovementDirection(Vector2.Zero);
				food_item.RemoveMeta("tag"); // Stops others from eating it
				food_item.SetMeta("pickup", false);
				aphid.IsEating = true;

				food_item.GlobalPosition = aphid.GlobalPosition +
					new Vector2(aphid.skin.IsFlipped ? -25 : 25, aphid.GlobalPosition.Y);

				for (int i = 0; i < food_item.GetChildCount(); i++)
				{
					if (food_item.GetChild(i).IsClass("CollisionShape2D"))
					{
						(food_item.GetChild(i) as CollisionShape2D).Disabled = true;
						break;
					}
				}

				food_pursue_timer.Stop();
				gobble_timer = gobble_duration;
			}
			else if (food_pursue_timer.TimeLeft > 0)
				aphid.SetMovementDirection(food_item.GlobalPosition - aphid.GlobalPosition);
		}

		protected void TickFoodGobble(Aphid aphid, EventArgs args, float _delta)
		{
			// Gobbling it up, yum yum
			gobble_timer -= _delta;

			// eating motion
			if (foodgobble_shutter_speed == 0)
			{
				food_item.GlobalPosition += food_item_switch ? Vector2.Up : Vector2.Down;
				food_item_switch = !food_item_switch;
				foodgobble_shutter_speed = 8;
				aphid.PlaySound(Audio_Nom, true);
			}
			else
				foodgobble_shutter_speed--;

			// finished meal
			if (gobble_timer <= 0)
			{
				// Dispose of the food item now
				if (IsInstanceValid(food_item))
				{
					string _id = food_item.GetMeta("id").ToString();
					GameManager.Food _food = GameManager.G_FOOD[_id];
					float _multi = aphid.Instance.Genes.FoodMultipliers[(int)_food.type];
					if (_food.food_value > 0)
						aphid.Instance.Status.AddHunger(_food.food_value * _multi);

					if (_food.drink_value > 0)
						aphid.Instance.Status.AddThirst(_food.drink_value * _multi);

					food_item.QueueFree();
				}
				aphid.IsEating = false;
				aphid.SetState(StateEnum.Idle);
			}
		}

		public void OnTrigger(Aphid aphid, Node2D _food_node, EventArgs args)
		{
			// if is currently consuming,
			//  its marked to not be picked up
			// already pursuing said item, or its marked for ignore, dont bother
			if (aphid.IsEating || (food_item != null && food_item.Equals(_food_node)) || !(bool)_food_node.GetMeta("pickup") ||
			(food_ignore_list.Count > 0 && food_ignore_list.Exists((Node2D _food_node_list) => _food_node.Equals(_food_node_list))))
				return;

			var _foodItem = GameManager.G_FOOD[_food_node.GetMeta("id").ToString()];
			var _flavor = _foodItem.type;
			// if Vile, reject it cause yucky, unless you like it for some reason
			if (_flavor == AphidData.FoodType.Vile && _flavor != aphid.Instance.Genes.FoodPreference)
				return;

			// If not hungy/thirsty, cancel, items that give both, wait until you are full to return
			// favorites ignore how full you are
			var _isfavorite = aphid.Instance.Genes.FoodPreference == _flavor;
			if (!_isfavorite)
			{
				bool _givesFood = _foodItem.food_value > 0, _givesDrink = _foodItem.drink_value > 0;

				if (_givesDrink && aphid.Instance.Status.Thirst >= (_givesFood ? 100 : 80))
					return;

				if (_givesFood && aphid.Instance.Status.Hunger >= (_givesDrink ? 100 : 80))
					return;
			}

			// if you are pursuing a food already
			// and the current one is valid
			if (IsInstanceValid(food_item))
			{
				// if either are a favorite or neither, check which one is closer
				// else if this one isnt a favorite, ignore it, this means if it IS, just grab it
				if (_isfavorite == is_food_favorite)
				{
					if (aphid.GlobalPosition.DistanceTo(_food_node.GlobalPosition) > aphid.GlobalPosition.DistanceTo(food_item.GlobalPosition))
						return;
				}
				else if (!_isfavorite)
					return;
			}

			// Set current food item to pursue
			food_item = _food_node;
			food_item.GlobalPosition = aphid.GlobalPosition + (aphid.skin.IsFlipped ? new Vector2(-8, -25) : new Vector2(-8, 25));
			food_pursue_timer = new()
			{
				OneShot = true
			};
			food_pursue_timer.Timeout += () =>
			{
				food_ignore_list.Add(food_item);
				food_item = null;
				aphid.SetState(StateEnum.Idle);
			};
			aphid.AddChild(food_pursue_timer);
			food_pursue_timer.Start(food_pursue_duration);
			is_food_favorite = _isfavorite;
			aphid.skin.SetFlipDirection(_food_node.GlobalPosition - aphid.GlobalPosition);
			aphid.SetState(StateEnum.Eat);
		}
	}
	public class SleepState : IState
	{
		private float sleep_gain_timer;
		private const float sleep_gain = 4.5f;
		private GpuParticles2D sleep_effect;

		public StateEnum Type => StateEnum.Sleep;

		public StateEnum[] Whitelist => new StateEnum[] { StateEnum.Idle };
		public StateEnum[] Blacklist => null;

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
			sleep_effect = GameManager.EmitParticles("sleep", aphid.GlobalPosition);
			aphid.skin.Position = new(0, 2);
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
			if (sleep_gain_timer > 0)
				sleep_gain_timer -= delta;
			else
			{
				aphid.Instance.Status.AddTiredness(aphid.rng.RandfRange(0.75f, 1.95f) * _args.gain_rate);
				sleep_gain_timer = sleep_gain;
			}

			// bare minimum sleep
			if (aphid.Instance.Status.Tiredness > 20)
				return;

			// 1 in 10000 chance to wake up, increased by the less sleepiness you have
			if (aphid.rng.RandiRange(0, (int)(500 * aphid.Instance.Status.Tiredness)) == 0)
				aphid.WakeUp(false, true);
		}
	}
	public class PetState : IState
	{
		// Petting Params And Affection too
		private float pet_timer;
		public StateEnum[] Whitelist => null;
		public StateEnum[] Blacklist => new StateEnum[] { StateEnum.Eat, StateEnum.Breed };
		public StateEnum Type => StateEnum.Pet;

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			pet_timer = AphidData.PET_DURATION;
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
				if (aphid.Instance.Status.Affection < 80)
				{
					GameManager.EmitParticles("heart", aphid.GlobalPosition - new Vector2(0, 10));
					aphid.Instance.Status.AddBondship(1);
				}
				aphid.Instance.Status.AddAffection(10);
				aphid.SetState(StateEnum.Idle);
				aphid.skin.DoJumpAnim();
			}
		}
	}
	public class BreedState : IState, ITriggerEvent
	{
		public StateEnum[] Whitelist => new StateEnum[] { StateEnum.Idle };
		public StateEnum[] Blacklist => throw new NotImplementedException();
		public StateEnum Type => StateEnum.Breed;

		private float[] breeding_weights = new float[] { 65, 35 };
		private Aphid breed_partner;
		private const int breed_timeout = 60, breed_partner_timeout = 10;
		private float breed_timeout_timer, breed_partner_timeout_timer;
		private GpuParticles2D breed_effect;

		public string TriggerID => "aphid";

		public void Awake(Aphid aphid, EventArgs args)
		{
			if (aphid.Instance.Status.BreedMode == -1)
				return;
			aphid.SetBreed(aphid.Instance.Status.BreedMode);
		}

		public void Enter(Aphid aphid, EventArgs args, StateEnum _previous)
		{
			aphid.skin.OverrideMovementAnim = true;
		}

		public void Exit(Aphid aphid, EventArgs args, StateEnum _next)
		{
			aphid.IsBreeding = false;
			aphid.skin.OverrideMovementAnim = false;
			aphid.TriggerActions.Remove(this);
			breed_partner = null;
			if (breed_effect != null)
			{
				breed_effect.OneShot = true;
				breed_effect = null;
			}
		}

		public async void Process(Aphid aphid, EventArgs args, float delta)
		{
			if (aphid.IsBreeding) // If already on it, dont bother
				return;

			if (breed_timeout_timer > 0)
				breed_timeout_timer -= delta;
			else
				aphid.SetState(StateEnum.Idle);

			// as long as we have a valid partner, wait for them
			if (!IsInstanceValid(breed_partner))
				breed_partner = null;

			if (breed_partner != null)
			{
				if (breed_partner_timeout_timer > 0)
					breed_partner_timeout_timer -= delta;
				else
				{
					breed_partner = null;
					return;
				}

				Vector2 _magnitude = breed_partner.GlobalPosition - aphid.GlobalPosition;
				// Checks for distance, done this way because aphids have a larger horizontal hitbox
				// so 20 X units may not be close enough, but 20 Y units is
				if (_magnitude.X < 50 && _magnitude.X > -50 && _magnitude.Y < 20 && _magnitude.Y > -20)
				{
					// Set both to breed
					aphid.IsBreeding = breed_partner.IsBreeding = true;
					breed_partner.SetState(StateEnum.Breed);
					// Dance
					breed_partner.skin.SetFlipDirection(_magnitude);
					aphid.skin.SetFlipDirection(aphid.GlobalPosition - breed_partner.GlobalPosition);
					_ = breed_partner.skin.DoDanceAnim();
					await aphid.skin.DoDanceAnim();

					// BREED
					aphid.Breed(breed_partner.Instance);
					return;
				}

				if (aphid.rng.RandiRange(0, 1000) == 0)
					SoundManager.CreateSound2D(aphid.AudioDynamic_Idle, aphid.GlobalPosition, true);
			}
			else // else do your mating dance
				aphid.skin.DoWalkAnim();
		}

		// constantly calls aphids around to complete the breeding process
		public void OnTrigger(Aphid _aphid, Node2D _node, EventArgs _args)
		{
			if (breed_partner != null || !_aphid.Instance.Status.IsAdult || _aphid.Instance.Status.Hunger == 0 || _aphid.Instance.Status.Thirst == 0)
				return;

			if (_aphid.State.Is(StateEnum.Idle))
			{
				breed_partner = _aphid;
				breed_partner_timeout_timer = breed_partner_timeout;
				_aphid.CallTowards(_aphid.GlobalPosition + (_aphid.skin.IsFlipped ? Vector2.Left : Vector2.Right) * 5);
			}
		}
	}
	/* NOT IMPLEMENTED */
	public class TrainState : IState
	{
		public StateEnum Type => throw new NotImplementedException();

		public StateEnum[] Whitelist => throw new NotImplementedException();

		public StateEnum[] Blacklist => throw new NotImplementedException();
		public void Awake(Aphid aphid, EventArgs args)
		{
			throw new NotImplementedException();
		}

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
	/* NOT IMPLEMENTED */
	public class PlayState : IState
	{
		public StateEnum Type => throw new NotImplementedException();

		public StateEnum[] Whitelist => throw new NotImplementedException();

		public StateEnum[] Blacklist => throw new NotImplementedException();
		public void Awake(Aphid aphid, EventArgs args)
		{
			throw new NotImplementedException();
		}

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
	/* NOT IMPLEMENTED */
	public class ChaseState : IState
	{
		public StateEnum Type => throw new NotImplementedException();

		public StateEnum[] Whitelist => throw new NotImplementedException();

		public StateEnum[] Blacklist => throw new NotImplementedException();
		public void Awake(Aphid aphid, EventArgs args)
		{
			throw new NotImplementedException();
		}

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
