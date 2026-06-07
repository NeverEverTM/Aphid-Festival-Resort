using Godot;

public partial class SunshadeStructure : Sprite2D
{
	[Export] private Area2D area;

    public override void _Ready()
    {
        area.BodyEntered += _body =>
        {
			_body.Modulate = new(0.5f,0.5f,0.5f);
        };
		area.BodyExited += _body =>
        {
			_body.Modulate = new(1,1,1);
        };
    }
}
