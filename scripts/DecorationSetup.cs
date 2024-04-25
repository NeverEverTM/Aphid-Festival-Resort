using Godot;

public partial class DecorationSetup : Node2D
{
	public override void _Ready()
	{
		for (int i = 0; i < GetChildCount(); i++)
		{
			Sprite2D _node = GetChild(i) as Sprite2D;
			_node.ZIndex = (int)_node.GlobalPosition.Y - 10 + _node.Texture.GetHeight() / 2; 
		}
	}
}
