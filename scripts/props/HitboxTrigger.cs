using Godot;

public partial class HitboxTrigger : RigidBody2D
{
	[Export]private Node script;

	private void Interact() =>
		script.Call("Interact");
}
