using Godot;

public partial class TorchBehaviour : AnimatedSprite2D
{
	private AnimatedSprite2D flame;
	private Light2D light;
	public override void _Ready()
	{
		flame = GetChild(0) as AnimatedSprite2D;
		light = GetChild(1) as Light2D;

		Play("default");
		Frame = new RandomNumberGenerator().RandiRange(0, 2);

		if (FieldManager.TimeOfDay == FieldManager.DayHours.Noon || FieldManager.TimeOfDay == FieldManager.DayHours.Sunset
			 || FieldManager.TimeOfDay == FieldManager.DayHours.Night)
		{
			light.Enabled = true;
			flame.Visible = true;
			flame.Play("lit");
		}

		RandomNumberGenerator _RNG = new();
		void CreateIn()
		{
			Tween _in = CreateTween();
			_in.TweenProperty(light, "energy", 2, _RNG.RandfRange(0.5f,2)).FromCurrent();
			_in.Finished += CreateOut;
		}
		void CreateOut()
		{
			Tween _out = CreateTween();
			_out.TweenProperty(light, "energy", 2.5, _RNG.RandfRange(0.5f,2)).FromCurrent();
			_out.Finished += CreateIn;
		}
		CreateIn();
	}

	public override void _Process(double delta)
	{
		if (flame.Visible)
			flame.Offset = new(Frame == 1 ? -1 : (Frame == 3 ? 1 : 0), 0);
	}
}
