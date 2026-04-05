using Godot;

[GlobalClass][Tool]
public partial class ItemData : Resource
{
    public enum ShopOwner { None, Item, Furniture, Hats }

    [Export]
    public string ID { get; set; }
    [Export(PropertyHint.Range, "0,9999")]
    public int Cost { get; set; } = 0;
    [Export(PropertyHint.Range, "0,3")]
    public int TierRequirement { get; set; } = 0;
    [Export(PropertyHint.Enum)]
    public StringNames.CategoryTags Category { get; set; } = StringNames.CategoryTags.Item;

    [ExportCategory("Shop Metadata")]
    [Export(PropertyHint.Enum)]
    public ShopOwner Shop { get; set; } = ShopOwner.None;
    [Export(PropertyHint.Range, "-9999,9999")]
    public int ShopOrderPriority { get; set; } = 0;

    public readonly static ItemData Empty = new();
}
