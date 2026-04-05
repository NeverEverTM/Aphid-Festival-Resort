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
    /// Category tags for Items, used only for cosmetic ordering.
    /// </summary>
    public enum CategoryTags
    {
        None = -1,
        Item,
        Food,
        Toy,
        Decoration,
        Equipment, 
        Playground
    }
    
    /// <summary>
    /// The Tags flag define the type of the entity. Entities with the tag "Aphid" will be managed as such.
    /// </summary>
    public readonly static StringName TagMeta = new("tag");
    /// <summary>
    /// The pickup flag makes the entity available to pickup (or to not).
    /// </summary>
    public readonly static StringName PickupMeta = new("pickup");
    /// <summary>
    /// The ID flag marks the unique global identifier for this object. Normally for items or structures.
    /// </summary>
    public readonly static StringName IdMeta = new("id");
    public readonly static StringName OffsetMeta = new("offset");
    public readonly static StringName SizeMeta = new("size");
    public readonly static StringName DefaultAnim = new("default");
    public readonly static StringName OpenAnim = new("open");
    public readonly static StringName CloseAnim = new("close");
    public readonly static StringName IdleAnim = new("idle");
    public readonly static StringName SitAnim = new("sit");
    public readonly static StringName WalkAnim = new("walk");
    public readonly static StringName RunAnim = new("run");
    public readonly static StringName WhistleAnim = new("whistle");
    public readonly static StringName PickupAnim = new("pickup");

    public const string DefaultPlayerName = "Mello";
    public static string[] DefaultPlayerPronouns { get => GlobalManager.Instance.Tr("pronouns_nonbinary").Split("/"); }
    public const string EnglishLocale = "en_US";
    public const string SpanishLocale = "es_ES";

    public const string BerryTextIcon = "[img height=50]res://sprites/ui/berries.tres[/img]";
    public const string StrengthTextIcon = "[img height=50]res://sprites/icons/strength.tres[/img]";
    public const string UnknownTextIcon = "[img height=50]res://sprites/icons/unknown.tres[/img]";
}