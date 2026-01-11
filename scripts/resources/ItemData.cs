using Godot;

[GlobalClass]
public partial class ItemData : Resource
{
    public enum ShopOwner { NoShop, Item, Furniture, Hats }

    [Export]
    public string ID { get; set; }
    [Export(PropertyHint.Range, "0,9999")]
    public int Cost { get; set; } = 0;
    [Export(PropertyHint.Range, "0,3")]
    public int LevelRequirement { get; set; } = 0;
    [Export(PropertyHint.Enum)]
    public StringNames.GlobalTags Tag { get; set; } = StringNames.GlobalTags.Item;

    [ExportCategory("Shop Metadata")]
    [Export(PropertyHint.Enum)]
    public ShopOwner Shop { get; set; } = ShopOwner.NoShop;
    [Export(PropertyHint.Range, "-9999,9999")]
    public int ShopOrderPriority { get; set; } = 0;

    public readonly static ItemData Empty = new();
}
