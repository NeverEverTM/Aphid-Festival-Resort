using Godot;

public partial class RoomDoor : Area2D
{
    [Export] public string roomToGo = "test";
    [Export] public int entryToGo = 0;
    [Export] public Vector2 entryDirection = Vector2.Left;
    [Export] public CollisionShape2D collider;

    /// <summary>
    /// If player is coming through this entry, do not trigger room transition.
    /// </summary>
    internal bool comingThrough;

    public override void _EnterTree()
    {
        BodyEntered += TriggerRoomTransition;
    }
    private void TriggerRoomTransition(Node2D _node)
    {
        // we are entering from this door
        if (comingThrough)
        {
            comingThrough = false;
            return;
        }

        if (SceneManager.IsBusy)
            return;

        // deactivate the door barrier to cross to the next scene
        if (_node.HasMeta(StringNames.TagMeta) && _node.GetMeta(StringNames.TagMeta).ToString() == "player")
        {
            collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            _ = SceneManager.Load(roomToGo, new(roomToGo, entryToGo, entryDirection, GlobalPosition));
        }
    }
}
