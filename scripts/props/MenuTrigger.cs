using Godot;

// Used as the entity that triggers their respective shop interface
public partial class MenuTrigger : Node2D, Player.IInteractEvent
{
	[Export] private Control triggerObject;

	public void Interact() =>
		(triggerObject as ITrigger).SetMenu();

	public interface ITrigger{
		public void SetMenu();
	}
}