using Godot;
using System;

public partial class SunshadeBehaviour : Sprite2D
{
	[Export] private Area2D area;

    public override void _Ready()
    {
        area.BodyEntered += (Node2D _body) =>
        {
			_body.Modulate = new(0.5f,0.5f,0.5f);
        };
		area.BodyExited += (Node2D _body) =>
        {
			_body.Modulate = new(1,1,1);
        };
    }
}
