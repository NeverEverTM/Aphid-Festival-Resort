using System;
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
    public void TriggerRoomTransition(Node2D _node)
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
        if (_node.HasMeta(StringNames.TagMeta) && (StringNames.GlobalTags)(int)_node.GetMeta(StringNames.TagMeta) == StringNames.GlobalTags.Player)
        {
            collider.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            LoadRoom();
        }
    }

    public async void LoadRoom()
    {
        try
        {
            await SceneManager.Load(roomToGo, new(roomToGo, entryToGo, entryDirection, GlobalPosition));
        }
        catch(Exception _error)
        {
            DebugLogger.Print(DebugLogger.LogPriority.Error, _error);
        }
    }
}
