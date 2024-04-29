using Godot;

// Used as the entity that triggers their respective shop interface
public partial class ShopEntity : Node2D
{
	[Export] private Area2D triggerArea;
	[Export] private ShopInterface shopInterface;

	public bool IsActive;
	private bool IsNearby;

	public override void _Ready()
	{
		triggerArea.AreaEntered += (Area2D _node) => IsNearby = true;
		triggerArea.AreaExited += (Area2D _node) => IsNearby = false;
	}

	public override void _Process(double delta)
	{
		if (!IsNearby)
			return;

		if (IsActive)
		{
			if (!CanvasManager.IsInMenu)
				IsActive = false;
			return;
		}

		if (Input.IsActionJustPressed("interact"))
			shopInterface.OpenStore();
	}
}