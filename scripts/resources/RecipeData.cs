using Godot;
using Godot.Collections;

[GlobalClass][Tool]
public partial class RecipeData : Resource
{
    [Export(PropertyHint.ResourceType, "ItemData")]
    public ItemData Owner { get; set; }

    [Export(PropertyHint.ArrayType, "FoodData")]
    public Array<Array<FoodData>> Combinations { get; set; } = [];

    public RecipeData() : this(null, []) { }

#pragma warning disable IDE0290 // Use primary constructor
    public RecipeData(ItemData Owner, Array<Array<FoodData>> Combinations)
#pragma warning restore IDE0290 // Use primary constructor
    {
        this.Owner = Owner;
        this.Combinations = Combinations;
    }
}
