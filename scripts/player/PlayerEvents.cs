using System;
using System.Collections.Generic;
using Godot;

public partial class Player : CharacterBody2D
{
    // MARK: Currency Events
    public enum CurrencyEvents { OnCurrencyGain, OnCurrencyLose, OnCurrencyChange }
    public class CurrencyArgs : EventArgs
    {
        public CurrencySource Source;
        public static int Amount { get => VAR_AMOUNT_CURRENCY; set => VAR_AMOUNT_CURRENCY = value; }
        public int Current;
    }
    protected Dictionary<CurrencyEvents, List<Action<CurrencyArgs>>> CurrencyEventsList = new()
    {
        { CurrencyEvents.OnCurrencyGain, new() },
        { CurrencyEvents.OnCurrencyLose, new() },
        { CurrencyEvents.OnCurrencyChange, new() },
    };

    public void AddEventListener(Action<CurrencyArgs> _action, CurrencyEvents _event) =>
        CurrencyEventsList[_event].Add(_action);
    public void RemoveEventListener(Action<CurrencyArgs> _action, CurrencyEvents _event) =>
        CurrencyEventsList[_event].Remove(_action);

    // MARK: Interactable Events
    public enum InteractableEvents
    {
        /// <summary>
        /// Triggered when a node enters the area and/or is detected inside of the same. This event can be triggered multiple times, make sure to check for repeated results.
        /// </summary>
        OnInteractableEnter,
        /// <summary>
        /// Triggered once when node leaves the area.
        /// </summary>
        OnInteractableExit
    }
    public class InteractableArgs : EventArgs
    {
        public StringNames.GlobalTags Tag;
        public Node2D Entity;
    }
    protected Dictionary<InteractableEvents, List<Action<InteractableArgs>>> InteractableEventsList = new()
    {
        { InteractableEvents.OnInteractableEnter, new() },
        { InteractableEvents.OnInteractableExit, new() },
    };

    public void AddEventListener(Action<InteractableArgs> _action, InteractableEvents _event) =>
        InteractableEventsList[_event].Add(_action);
    public void RemoveEventListener(Action<InteractableArgs> _action, InteractableEvents _event) =>
        InteractableEventsList[_event].Remove(_action);

    // MARK: Pickup Events
    public enum PickupEvents { OnPickup, OnDrop }
    public class PickupArgs : EventArgs
    {
        public Node2D Entity = null;
        public Aphid Entity_Aphid = null;
        public StringNames.GlobalTags Tag;
        public bool IsAphid = false;
        public Sprite2D Sprite = null;
        public Vector2 InitialOffset = new();
        public Vector2 LastValidPosition = new();

        public bool IsNoAnim = false;

        public PickupArgs(Node2D Entity)
        {
            this.Entity = Entity;
            Tag = (StringNames.GlobalTags)(int)Entity.GetMeta(StringNames.TagMeta);
            LastValidPosition = Entity.GlobalPosition;
        }
    }
    protected Dictionary<PickupEvents, List<Action<PickupArgs>>> PickupEventsList = new()
    {
        { PickupEvents.OnPickup, new() },
        { PickupEvents.OnDrop, new() },
    };

    public void AddEventListener(Action<PickupArgs> _action, PickupEvents _event) =>
        PickupEventsList[_event].Add(_action);
    public void RemoveEventListener(Action<PickupArgs> _action, PickupEvents _event) =>
        PickupEventsList[_event].Remove(_action);
}
