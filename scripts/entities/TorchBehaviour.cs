using Godot;

public partial class TorchBehaviour : AnimatedSprite2D
{
	[Export] private Light2D light;
	[Export] private AnimatedSprite2D flame;
	bool is_activated, firstload;

	private static readonly RandomNumberGenerator _RNG = new();
	public override void _EnterTree()
	{
		Play(StringNames.DefaultAnim);
		Frame = new RandomNumberGenerator().RandiRange(0, 2);
		if (flame != null)
		{
			FrameChanged += () =>
			{
				flame.Offset = new(Frame == 1 ? -1 : (Frame == 3 ? 1 : 0), 0);
			};
		}

		FieldManager.OnTimeChange += LIGHT_EVENT_FN;
		LIGHT_EVENT_FN();
	}
	public override void _Process(double delta)
	{
		if (CameraManager.GetSquaredDistanceTo(GlobalPosition) < CameraManager.SCREEN_RENDER_DISTANCE_SQR
		&& !(FieldManager.TimeOfDay == FieldManager.DayHours.Noon || FieldManager.TimeOfDay == FieldManager.DayHours.Morning))
			light.Enabled = true;
		else
			light.Enabled = false;
    }

	public override void _ExitTree()
	{
		FieldManager.OnTimeChange -= LIGHT_EVENT_FN;
	}
	public void LIGHT_EVENT_FN()
	{
		if (firstload)
			SwitchLightState();
		else
			SwitchLightState(_instant: true);
		firstload = true;
	}
	public void SwitchLightState(bool _instant = false)
	{
		if (FieldManager.TimeOfDay == FieldManager.DayHours.Noon || FieldManager.TimeOfDay == FieldManager.DayHours.Morning)
		{
			if (_instant)
			{
				if (flame != null)
					flame.Visible = false;
			}
			is_activated = false;
		}
		else if (!is_activated)
		{
			if (_instant)
				light.Energy = 1;
			if (flame != null)
			{
				flame.Visible = true;
				flame.Play("lit");
			}
			is_activated = true;
			LightIn();
		}
	}
	public void LightIn()
	{
		Tween _in = CreateTween();
		_in.TweenProperty(light, "energy", 1.1, _RNG.RandfRange(1, 2)).FromCurrent();
		_in.Finished += LightOut;
	}
	public void LightOut()
	{
		Tween _out = CreateTween();

		if (is_activated)
		{
			_out.TweenProperty(light, "energy", 1.2, _RNG.RandfRange(1, 2)).FromCurrent();
			_out.Finished += LightIn;
		}
		else
		{
			_out.TweenProperty(light, "energy", 0, _RNG.RandfRange(1, 2)).FromCurrent();
			_out.Finished += () =>
			{
				light.Enabled = false;
				flame?.Hide();
			};
		}
	}
}
