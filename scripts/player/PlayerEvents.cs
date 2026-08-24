using System;
using System.Collections.Generic;
using Godot;

public partial class Player : CharacterBody2D
{
    // MARK: Currency Events
    public enum CurrencyEvents { OnPreCalculation, OnPostCalculation }
    public class CurrencyArgs : EventArgs
    {
        /// <summary>
        /// Source of the income
        /// </summary>
        public CurrencySource Source;
        /// <summary>
        /// Current amount being charged, directly modify it to apply effects to the final amount.
        /// </summary>
        public int Amount { get => VAR_AMOUNT_CURRENCY; set => VAR_AMOUNT_CURRENCY = value; }
        /// <summary>
        /// Current amount of money.
        /// </summary>
        public int Currency;
    }
    protected Dictionary<CurrencyEvents, List<Action<CurrencyArgs>>> CurrencyEventsList = new()
    {
        { CurrencyEvents.OnPreCalculation, new() },
        { CurrencyEvents.OnPostCalculation, new() },
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
