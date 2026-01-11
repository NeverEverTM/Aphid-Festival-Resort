using Godot;
using Godot.Collections;

[GlobalClass]
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
    public RecipeData(string _id, string _ingredient1, string _ingredient2)
    {
        Owner = GlobalManager.G_FOOD[_id];
        Array<FoodData> _list = [];
        if (!string.IsNullOrWhiteSpace(_ingredient1))
            _list.Add(GlobalManager.G_FOOD[_ingredient1]);
        if (!string.IsNullOrWhiteSpace(_ingredient2))
            _list.Add(GlobalManager.G_FOOD[_ingredient2]);

        Combinations = [_list];
    }
}
