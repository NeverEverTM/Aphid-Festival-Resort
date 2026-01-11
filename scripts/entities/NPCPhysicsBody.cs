using Godot;

public partial class NPCPhysicsBody : CharacterBody2D
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
		npcBody.GetChild<StaticBody2D>(1).ProcessMode = ProcessModeEnum.Disabled;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (npcBody.isBusy)
			return;

		float _delta = (float)delta;
		// standing still wait time
		if (idle_time_left > 0)
		{
			idle_time_left -= (float)delta;
			if (!npcBody.Animation.Equals(StringNames.DefaultAnim))
				npcBody.Play(StringNames.DefaultAnim);
			return;
		}

		WanderAround(_delta);

		// if we are close to idle position, generate a new position and stand still for a few seconds
		if (GlobalPosition.DistanceSquaredTo(target_position) < 100)
			GenerateWanderPosition();
	}

	private void GenerateWanderPosition()
	{
		target_position = new Vector2(NPCBehaviour.RNG.RandfRange(-RANDOM_RANGE_CAP, RANDOM_RANGE_CAP),
				NPCBehaviour.RNG.RandfRange(-RANDOM_RANGE_CAP, RANDOM_RANGE_CAP)) + GlobalPosition;

		// we check if we are straying away from our origin, if so, steer towards the center
		if (target_position.DistanceTo(origin) > maxWanderRange)
			target_position = GlobalPosition + (origin - GlobalPosition);

		idle_time_left = NPCBehaviour.RNG.RandfRange(TIMER_RANGE, TIMER_RANGE * 2);
		timeout = 0;
	}

	private void WanderAround(float delta)
	{
		// start walking anim
		if (!npcBody.Animation.Equals(StringNames.WalkAnim))
			npcBody.Play(StringNames.WalkAnim);

		// move to idle pos, timeout if you cant
		movement_direction = (target_position - GlobalPosition).Normalized();
		timeout += delta;
		if (timeout > TIMEOUT_BASE)
		{
			target_position = GlobalPosition;
			timeout = 0;
		}

		Velocity = movement_direction * 20;
		MoveAndSlide();
		npcBody.SetFlipDirection(target_position - GlobalPosition);
	}
}
