using Godot;

public partial class AphidFake : CharacterBody2D
{
    [Export] private AphidSkin skin;

    private bool IsAdult;
    private Vector2 MovementDirection;
    private readonly static RandomNumberGenerator rng = new();

    private Vector2 target_position;
    private float stand_time;
    private float timeout;
    private const int idle_rand_range = 50;
    private const float idle_timer_range = 1.15f, idle_timeout = 5f;

    public AudioStream AudioDynamic_Idle
    {
        get
        {
            if (IsAdult)
                return SoundManager.GetAudioStream("aphid/idle");
            else
                return SoundManager.GetAudioStream("aphid/baby_idle");
        }
    }

    public void SetReady(AphidInstance Instance)
    {
        skin.SetInstance(Instance);
        IsAdult = Instance.Status.IsAdult;
        skin.SetSkin("idle");
        SetTimers();
    }

    public override void _PhysicsProcess(double delta)
    {
        float _delta = (float)delta;
        // standing still wait time
        if (stand_time > 0)
        {
            MovementDirection = Vector2.Zero;
            timeout = 0;
            stand_time -= _delta;
            return;
        }

        // we are close to idle pos, generate a new one and stand still for a few seconds
        if (GlobalPosition.DistanceSquaredTo(target_position) < 400)
        {
            target_position = new Vector2(Aphid.MISC_RNG.RandfRange(-idle_rand_range, idle_rand_range),
                Aphid.MISC_RNG.RandfRange(-idle_rand_range, idle_rand_range)) + GlobalPosition;
            MovementDirection = Vector2.Zero;
            stand_time = Aphid.MISC_RNG.RandfRange(idle_timer_range, idle_timer_range * 2);
            timeout = 0;
            return;
        }

        // move to idle pos, timeout if you cant
        MovementDirection = (target_position - GlobalPosition).Normalized();
        timeout += _delta;
        if (timeout > idle_timeout)
            target_position = GlobalPosition;

        // Set default movement state
        if (!MovementDirection.IsEqualApprox(Vector2.Zero))
            skin.SetFlipDirection(MovementDirection);
        skin.StartWalk(MovementDirection);
        Velocity = MovementDirection * 20;
        MoveAndSlide();
    }

    private void SetTimers()
    {
        Timer blink_timer = new(), blink_duration_timer = new(), squeak_timer = new();

        AddChild(squeak_timer);
        squeak_timer.Timeout += () =>
        {
            SoundManager.CreateSound2D(AudioDynamic_Idle, GlobalPosition, true);
            squeak_timer.Start(rng.RandiRange(5, 15));
        };
        squeak_timer.Start(rng.RandiRange(5, 15));

        AddChild(blink_timer);
        blink_timer.OneShot = true;
        blink_timer.Timeout += () =>
        {
            skin.SetEyesSkin("blink");
            blink_duration_timer.Start(0.1f);
        };

        AddChild(blink_duration_timer);
        blink_duration_timer.OneShot = true;
        blink_duration_timer.Timeout += () =>
        {
            if (skin.currentEyeExpression == "blink")
                skin.SetEyesSkin(skin.lastEyeExpression);
            blink_timer.Start(rng.RandfRange(4.5f, 6.7f));
        };
        blink_timer.Start(rng.RandfRange(4.5f, 6.7f));

        // this makes them able to blink and sqeak while being grabbed
        blink_duration_timer.ProcessMode = ProcessModeEnum.Pausable;
        blink_timer.ProcessMode = ProcessModeEnum.Pausable;
        squeak_timer.ProcessMode = ProcessModeEnum.Pausable;
    }
}
