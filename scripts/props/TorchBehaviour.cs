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
		Frame = new RandomNumberGenerator().RandiRange(0,2);

		if (FieldManager.TimeOfDay == FieldManager.DayHours.Noon || FieldManager.TimeOfDay == FieldManager.DayHours.Sunset
			 || FieldManager.TimeOfDay == FieldManager.DayHours.Night)
		{
			light.Enabled = true;
			flame.Visible = true;
			flame.Play("lit");
		}
		Tween _tween = CreateTween();
		float _cap = 0.8f;
		_tween.SetLoops();
		PropertyTweener _property = _tween.TweenProperty(light, "energy", _cap, 1).FromCurrent();
		//_tween.LoopFinished += (_) => _property. = light.Energy == 0.8f ? 1 : 0.8f;
	}

    public override void _Process(double delta)
    {
		if (flame.Visible)
        	flame.Offset = new(Frame == 1 ? -1 : (Frame == 3 ? 1 : 0), 0);
    }
}
