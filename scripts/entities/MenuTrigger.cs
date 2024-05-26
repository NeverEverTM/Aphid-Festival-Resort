using Godot;

// Used as the entity that triggers their respective shop interface
public partial class MenuTrigger : Node2D
{
	[Export] private Area2D triggerArea;
	[Export] private Control menuInterface;
	private IMenuInterface Interface { get; set; }

	private bool IsNearby;

	public override void _Ready()
	{
		triggerArea.AreaEntered += (Area2D _node) => IsNearby = true;
		triggerArea.AreaExited += (Area2D _node) => IsNearby = false;

		Interface = menuInterface as IMenuInterface;
	}

	public override void _Process(double delta)
	{
		if (!IsNearby)
			return;

		if (Interface.IsActive)
		{
			if (!CanvasManager.IsInMenu)
				Interface.IsActive = false;
			return;
		}

		if (Input.IsActionJustPressed("interact"))
			Interface.OpenMenu();
	}
}

public interface IMenuInterface
{
	public bool IsActive { get; set; }
	public void OpenMenu();
}