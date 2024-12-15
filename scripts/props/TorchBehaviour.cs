using Godot;

public partial class TorchBehaviour : AnimatedSprite2D
{
	private AnimatedSprite2D flame;
	private Light2D light;
	bool spawnCheck;
	RandomNumberGenerator _RNG = new();
	public override void _Ready()
	{
		flame = GetChild(0) as AnimatedSprite2D;
		light = GetChild(1) as Light2D;

		Play("default");
		Frame = new RandomNumberGenerator().RandiRange(0, 2);
		FrameChanged += () =>
		{
			flame.Offset = new(Frame == 1 ? -1 : (Frame == 3 ? 1 : 0), 0);
		};

		FieldManager.OnTimeChange += SetLight;
		SetLightInstant();
	}
	public void SetLight()
	{
		if (!spawnCheck)
		{
			spawnCheck = true;
			return;
		}

		if (FieldManager.TimeOfDay == FieldManager.DayHours.Noon)
			flame.Visible = false;
		else if (!flame.Visible)
		{
			light.Enabled = true;
			flame.Visible = true;
			flame.Play("lit");
			CreateFlameIn();
		}
	}
	public void SetLightInstant()
	{
		if (FieldManager.TimeOfDay == FieldManager.DayHours.Noon)
		{
			light.Enabled = false;
			flame.Visible = false;
		}
		else if (!flame.Visible)
		{
			light.Energy = 1;
			light.Enabled = true;
			flame.Visible = true;
			flame.Play("lit");
			CreateFlameIn();
		}
	}
	public void CreateFlameIn()
	{
		Tween _in = CreateTween();
		_in.TweenProperty(light, "energy", 1.1, _RNG.RandfRange(1, 2)).FromCurrent();
		_in.Finished += CreateFlameOut;
	}
	public void CreateFlameOut()
	{
		Tween _out = CreateTween();

		if (flame.Visible)
		{
			_out.TweenProperty(light, "energy", 1.2, _RNG.RandfRange(1, 2)).FromCurrent();
			_out.Finished += CreateFlameIn;
		}
		else
		{
			_out.TweenProperty(light, "energy", 0, _RNG.RandfRange(1, 2)).FromCurrent();
			_out.Finished += () => light.Enabled = false;
		}
	}
	public override void _ExitTree()
	{
		FieldManager.OnTimeChange -= SetLight;
	}
}
