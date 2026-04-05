using Godot;
using Godot.Collections;

[GlobalClass][Tool]
public partial class FoodData : Resource
{
    [Export] public ItemData Item { get; set; }
    [Export] public AphidData.FoodType Flavor { get; set; } = AphidData.FoodType.Neutral;
    [Export] public int FoodValue { get; set; }
    [Export] public int DrinkValue { get; set; }
    [Export(PropertyHint.Enum)]
    public Dictionary<AphidData.SkillEnum, int> Skills { get; set; } = [];

    public FoodData() : this(AphidData.FoodType.Neutral, 0, 0, []) {}

#pragma warning disable IDE0290 // Use primary constructor
    public FoodData(AphidData.FoodType Flavor, int FoodValue, int DrinkValue, Dictionary<AphidData.SkillEnum, int> Skills)
    {
        this.Flavor = Flavor;
        this.FoodValue = FoodValue;
        this.DrinkValue = DrinkValue;
        this.Skills = Skills;
    }
#pragma warning restore IDE0290 // Use primary constructor
}
