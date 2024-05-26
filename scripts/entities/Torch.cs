using Godot;

public partial class Torch : AnimatedSprite2D
{
	[Export] private Light2D light;

	public override void _Ready()
	{
		if (FieldManager.TimeOfDay == FieldManager.DayHours.Sunset || FieldManager.TimeOfDay == FieldManager.DayHours.Night)
		{
			RandomNumberGenerator _new = new();
			Frame = _new.RandiRange(0, 2);
			light.Enabled = true;
			Play("lit");
		}
		else
			Play("idle");
	}
}
