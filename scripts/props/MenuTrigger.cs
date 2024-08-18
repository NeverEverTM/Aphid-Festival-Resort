using Godot;

// Used as the entity that triggers their respective shop interface
public partial class MenuTrigger : Node2D
{
	[Export] private Area2D triggerArea;
	[Export] private Node triggerObject;
	private ITriggerInteract triggerInterface;

	private bool IsActive, IsNearby;

	public override void _Ready()
	{
		triggerInterface = triggerObject as ITriggerInteract;
		triggerArea.AreaEntered += (Area2D _node) => CheckNearbyPlayer(true, _node);
		triggerArea.AreaExited += (Area2D _node) => CheckNearbyPlayer(false, _node);
	}

	private void CheckNearbyPlayer(bool _setTo, Area2D _node)
	{
		if (_node.HasMeta("tag") && _node.GetMeta("tag").ToString() == "player")
			IsNearby = _setTo;
	}

	public override void _Process(double delta)
	{
		if (!IsNearby)
			return;

		if (!IsActive)
		{
			if (Input.IsActionJustPressed("interact"))
			{
				IsActive = true;
				triggerInterface.TriggerInteract();
			}
		}
		else if (!triggerInterface.GetState())
			IsActive = false;
	}

	public interface ITriggerInteract
	{
		public void TriggerInteract();
		public bool GetState();
	}
}