using Godot;
/// <summary>
/// Inline InputNames this script is NOT automatically generated, it contains all global StringName and string references.
/// </summary>
public static class StringNames
{
    public enum GlobalTags { 
        None = -1, 
        Item, 
        Food, 
        Interactable, 
        Decoration, 
        Equipment, 
        Playground, 
        Aphid, 
        NPC, 
        Menu, 
        Player
    }
    public readonly static string[] GlobalTagsNames =
    [
        "item",
        "food",
        "interactable",
        "decoration",
        "equipment",
        "playground",
        "aphid",
        "npc",
        "menu",
        "player"
    ];

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
    public const string EnglishLocale = "en_US";
    public const string SpanishLocale = "es_ES";
}