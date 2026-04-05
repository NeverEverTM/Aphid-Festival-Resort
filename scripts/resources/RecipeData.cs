using Godot;
using Godot.Collections;

[GlobalClass][Tool]
public partial class RecipeData : Resource
{
    [Export(PropertyHint.ResourceType, "FoodData")]
    public FoodData Owner { get; set; }

    [Export(PropertyHint.ArrayType, "FoodData")]
    public Array<Array<FoodData>> Combinations { get; set; } = [];

    public RecipeData() : this(null, []) { }
    public RecipeData(FoodData Owner, Array<Array<FoodData>> Combinations)
    {
        this.Owner = Owner;
        this.Combinations = Combinations;
    }
}
