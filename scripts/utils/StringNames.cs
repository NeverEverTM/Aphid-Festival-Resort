using Godot;
/// <summary>
/// Inline InputNames this script is NOT automatically generated, it contains all global StringName and string references.
/// </summary>
public static class StringNames
{
    /// <summary>
    /// Universal tag system
    /// </summary>
    public enum GlobalTags {
        None = -1,
        /// <summary>
        /// Marks object as a pickup
        /// </summary>
        Item,
        /// <summary>
        /// Allows aphids to consume it, plus marks it as an Item too
        /// </summary>
        Food,
        /// <summary>
        /// Used for invisible triggers that can activate menus via user input
        /// </summary>
        MenuTrigger,
        /// <summary>
        /// Used by structures or items that can be interacted via user input
        /// </summary>
        Interactable,
        /// <summary>
        /// Marks this as an aphid and thus must be handled diffrently, essentially a unique handling Item.
        /// </summary>
        Aphid,
        /// <summary>
        /// Used by triggers that activate NPC dialog via user input.
        /// </summary>
        NPC,
        /// <summary>
        /// Self explanatory.
        /// </summary>
        Player
    }
    public readonly static string[] GlobalTagsNames =
    [
        "item",
        "food",
        "menu",
        "interactable",
        "aphid",
        "npc",
        "player"
    ];
    
    /// <summary>
    /// The Tag flag define the type of the entity. Normally managed by middleman modules.
    /// </summary>
    public readonly static StringName TagMeta = new("tag");
    /// <summary>
    /// The Pickup flag allows the entity to be picked up by entities, it can also control whenever pickup is allowed by its bool value.
    /// </summary>
    public readonly static StringName PickupMeta = new("pickup");
    /// <summary>
    /// The ID flag marks the unique global identifier for this object. Normally reserved for items or structures, and modified only by their spawner.
    /// </summary>
    public readonly static StringName IdMeta = new("id");
    /// <summary>
    /// Structures can have an 'offset' and a 'size' meta for custom build mode rects
    /// </summary>
    public readonly static StringName OffsetMeta = new("offset"), SizeMeta = new("size");
    /// <summary>
    /// Dictates if a tilemaps physics interaction with buildings.
    /// </summary>
    public readonly static StringName PlaceableAreaMeta = new("placeable_area");

    // Misc. Anims
    public readonly static StringName DefaultAnim = new("default");
    public readonly static StringName OpenAnim = new("open");
    public readonly static StringName CloseAnim = new("close");
    // Player Anims
    public readonly static StringName IdleAnim = new("idle");
    public readonly static StringName SitAnim = new("sit");
    public readonly static StringName SitIdleAnim = new("sit_idle");
    public readonly static StringName WalkAnim = new("walk");
    public readonly static StringName RunAnim = new("run");
    public readonly static StringName WhistleAnim = new("whistle");
    public readonly static StringName PickupAnim = new("pickup");

    public const string DefaultPlayerName = "Mello";
    public static string[] DefaultPlayerPronouns { get => GlobalManager.Instance.Tr("pronouns_nonbinary").Split("/"); }
    public const string EnglishLocale = "en_US";
    public const string SpanishLocale = "es_ES";

    /// <summary>
    /// BBCode to display the corresponding icon in a richtextlabel.
    /// </summary>
    public const string BerryIcon = "[img height=50]res://sprites/ui/berries.tres[/img]",
        StarIcon = "[img height=50]res://sprites/icons/star.tres[/img]",
        EmptyStarIcon = "[img height=50]res://sprites/icons/empty_star.tres[/img]",
        StrengthIcon = "[img height=50]res://sprites/icons/strength.tres[/img]",
        UnknownIcon = "[img height=50]res://sprites/icons/unknown.tres[/img]";
}