using Godot;

public partial class NPCPhysicsBody : CharacterBody2D, Player.IInteractEvent
{
	[Export] private NPCBehaviour npcBody;
	[Export(PropertyHint.Range, "10,1000,10")] private float maxWanderRange;
	private Vector2 target_position, movement_direction;
	private const int RANDOM_RANGE_CAP = 100;
	private const float TIMER_RANGE = 1.15f, TIMEOUT_BASE = 4f;
	private float idle_time_left, timeout;
	private Vector2 origin;

	public override void _EnterTree()
	{
		target_position = origin = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		float _delta = (float)delta;
		if (npcBody.isBusy)
		{
			npcBody.TickFlip(_delta);
			return;
		}
		// standing still wait time
		if (idle_time_left > 0)
		{
			idle_time_left -= (float)delta;
			if (npcBody.Animation.Equals("walk"))
				npcBody.Play("default");
			return;
		}
		else if (GlobalPosition.DistanceTo(target_position) < 10)
		{
			// we are close to idle pos, generate a new one and stand still for a few seconds
			RandomNumberGenerator rng = new();
			target_position = new Vector2(rng.RandfRange(-RANDOM_RANGE_CAP, RANDOM_RANGE_CAP),
				rng.RandfRange(-RANDOM_RANGE_CAP, RANDOM_RANGE_CAP)) + GlobalPosition;
				
			// we check if we are straying away from our origin, if so, steer towards the center
			if (target_position.DistanceTo(origin) > maxWanderRange)
				target_position = GlobalPosition + (origin - GlobalPosition);

			idle_time_left = rng.RandfRange(TIMER_RANGE, TIMER_RANGE * 2);
			timeout = 0;
			return;
		}
		if (npcBody.Animation.Equals("default"))
			npcBody.Play("walk");
		// move to idle pos, timeout if you cant
		movement_direction = (target_position - GlobalPosition).Normalized();
		timeout += _delta;
		if (timeout > TIMEOUT_BASE)
		{
			target_position = GlobalPosition;
			timeout = 0;
		}

		Velocity = movement_direction * 20;
		MoveAndSlide();
		npcBody.SetFlipDirection(target_position - GlobalPosition);
		npcBody.TickFlip(_delta);
	}

	public void Interact() =>
		npcBody.Interact();
}
