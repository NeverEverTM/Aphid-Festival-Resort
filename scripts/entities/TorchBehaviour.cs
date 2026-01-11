using Godot;

public partial class TorchBehaviour : AnimatedSprite2D
{
	[Export] private PointLight2D light;
	[Export] private AnimatedSprite2D flame;

	bool is_active;
	float default_scale;

	private static readonly RandomNumberGenerator TORCH_RNG = new();
	public override void _Ready()
	{
		Play(StringNames.DefaultAnim);
		Frame = TORCH_RNG.RandiRange(0, 2);
		if (flame != null)
		{
			FrameChanged += () =>
			{
				flame.Offset = new(Frame == 1 ? -1 : (Frame == 3 ? 1 : 0), 0);
			};
		}

		FieldManager.Instance.OnTimeChange.Add((_) => { SwitchLightState(); });
		SwitchLightState(_instant: true);
	}

	public void SwitchLightState(bool _instant = false)
	{
		is_active = true;
		if (FieldManager.TimeOfDay == FieldManager.DayHourMode.Noon || FieldManager.TimeOfDay == FieldManager.DayHourMode.Morning)
			is_active = false;

		if (is_active) // turn on
		{
			flame?.Show();
			light.Enabled = true;

			if (_instant)
				light.Energy = 1;
			else
				LightIn();
		}
		else // turn off
		{
			if (_instant)
			{
				light.Enabled = false;
				light.Energy = 0;
				flame?.Hide();
			}
			else
				LightOut();
		}
	}
	public void LightIn()
	{
		Tween _in = CreateTween();
		_in.TweenProperty(light, "energy", 1, TORCH_RNG.RandfRange(3, 5)).FromCurrent();
	}
	public void LightOut()
	{
		Tween _out = CreateTween();

		_out.TweenProperty(light, "energy", 0, TORCH_RNG.RandfRange(1, 2)).FromCurrent();
		_out.Finished += () =>
		{
			light.Enabled = false;
			flame?.Hide();
		};
	}
}
