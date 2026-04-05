using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Middleware component that manages interactions for an owner node and the tag it identifies itself with.
/// </summary>
public partial class InteractableArea2D : Area2D
{
    public bool IsInteractable { get; set; } = true;
    /// <summary>
    /// List of events to trigger if this object is interacted with. Includes the entity reference and entity tag of whomst it was interacted by.
    /// </summary>
    public readonly List<Action<Node2D, StringNames.GlobalTags>> OnInteract = [];
    /// <summary>
    /// List of events to trigger if this object is interacted with. Does not include information by the entity who interacted.
    /// </summary>
    public readonly List<Action> OnInteractOnly = [];
    [Export(PropertyHint.Enum)] public StringNames.GlobalTags Tag;

    public override void _EnterTree()
    {
        SetMeta(StringNames.TagMeta, (int)Tag);
    }

    public void Interact(Node2D _entity, StringNames.GlobalTags _tag)
    {
        try
        {
            for (int i = 0; i < OnInteractOnly.Count; i++)
                OnInteractOnly[i].Invoke();
            for (int i = 0; i < OnInteract.Count; i++)
                OnInteract[i].Invoke(_entity, _tag);
        }
        catch (Exception _err)
        {
            DebugLogger.Print(DebugLogger.LogPriority.Error, "InteractableArea: Error when invoking event.", _err);
        }
    }
}

public interface IInteractableArea
{
    /// <summary>
    /// List of events to trigger if this object is interacted with. Includes the entity reference and entity tag of whomst it was interacted by.
    /// </summary>
    public List<Action<Node2D, StringNames.GlobalTags>> OnInteract { get; set; }
    /// <summary>
    /// List of events to trigger if this object is interacted with. Does not include information by the entity who interacted.
    /// </summary>
    public List<Action> OnInteractOnly { get; set; }
    [Export(PropertyHint.Enum)] public StringNames.GlobalTags Tag { get; set; }

    public void Interact(Node2D _entity, StringNames.GlobalTags _tag)
    {
        try
        {
            for (int i = 0; i < OnInteractOnly.Count; i++)
                OnInteractOnly[i].Invoke();
            for (int i = 0; i < OnInteract.Count; i++)
                OnInteract[i].Invoke(_entity, _tag);
        }
        catch (Exception _err)
        {
            GD.Print(_err);
        }
    }
}