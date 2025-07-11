using Godot;

public partial class ChairBehaviour : Sprite2D, Player.IInteractEvent
{
    [Export] private Vector2 sitOffset;
    [Export] private CollisionShape2D playerCollider;

    bool enabled;
    Vector2 last_offset, last_position;

    public void Interact()
    {
        if (enabled)
        {
            playerCollider.Disabled = false;
            Player.Instance.LockMovement = false;

            Player.Instance.animatorNode.Offset = last_offset;
            Player.Instance.GlobalPosition = last_position;
            Player.Instance.SetPlayerAnim(StringNames.IdleAnim);
        }
        else
        {
            Player.Instance.SetMovementDirection(Vector2.Zero);
            Player.Instance.LockMovement = true;
            playerCollider.Disabled = true;

            last_offset = Player.Instance.animatorNode.Offset;
            last_position = Player.Instance.GlobalPosition;

            Player.Instance.animatorNode.Offset = last_offset + sitOffset;
            Player.Instance.GlobalPosition = GlobalPosition;
            Player.Instance.SetPlayerAnim(StringNames.SitAnim);
        }
        enabled = !enabled;
    }
}
