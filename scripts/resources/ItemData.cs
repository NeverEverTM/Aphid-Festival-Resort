using Godot;

[GlobalClass][Tool]
public partial class ItemData : Resource
{
    public enum ShopOwner { None, Item, Furniture, Hats }
    public enum ItemType { Item, Structure, Food }
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

    [Export]
    public string ID { get; set; }
    [Export]
    public ItemType Type { get; set; } = ItemType.Item;
    [Export(PropertyHint.Range, "0,9999")]
    public int Cost { get; set; } = 0;
    [Export(PropertyHint.Range, "0,3")]
    public int TierRequirement { get; set; } = 0;
    [Export(PropertyHint.Enum)]
    public CategoryTags Category { get; set; } = CategoryTags.Item;

    [ExportCategory("Shop Metadata")]
    [Export(PropertyHint.Enum)]
    public ShopOwner Shop { get; set; } = ShopOwner.None;
    [Export(PropertyHint.Range, "-9999,9999")]
    public int ShopOrderPriority { get; set; } = 0;

    public readonly static ItemData Empty = new();
}
