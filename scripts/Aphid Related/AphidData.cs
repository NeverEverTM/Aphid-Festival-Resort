using Godot;
using System.Collections.Generic;

public partial class AphidData : Node
{
	public enum FoodType { Sweet, Sour, Salty, Bitter, Vile, Bland, Neutral }
	private readonly static float[] flavor_weights = new float[]{
		0.2f,
		0.2f,
		0.2f,
		0.2f,
		0.05f,
		0.15f
	};
	public static string[] NameArchive = new string[]
	{
		"Apuff", "Kiwi", "Brassmo",
		"Alphred", "Sumi", "Summer",
		"Apa", "Miriam", "Cascade",
		"Artyom", "Leif", "Sushi",
		"Apy", "Alpha", "Crow",
		"Azure", "Arty", "A.P.I",
		"Atlas", "Amelia", "Audrey",
		"Abraham", "Charlie", "Madeline",
		"Ape", "Axye", "Theo", "Jeb",
		"Apu", "Arthur", "MissingNo",
		"Ace", "Amber", "Atheleya",
		"Arial", "Aphid", "Amor",
		"Ariel", "Armando", "Omega",
		"Afty", "Buggy", "Toffee",
		"Axel", "Lea", "Sky",
		"Alicia", "Mr Von Aphid", "Apartment Complex",
	};
	public const int adulthoodAge = 600, breedTimer = 600, deathAge = 3600;

	public class Status
	{
		// Basic stats
		public string Name { get; set; }
		public float Hunger { get; set; }
		public float Thirst { get; set; }
		public float Sleepiness { get; set; }
		public float Affection { get; set; }
		public float Bondship { get; set; }

		// Production & Breeding
		public float EggBuildup { get; set; }
		public float MilkBuildup { get; set; }

		// Lifetime
		public float Health { get; set; }
		public float Age { get; set; }
		public bool IsAdult { get; set; }

		// State
		public float PositionX { get; set; }
		public float PositionY { get; set; }

		// Gen Data
		public Status()
		{
			Name = NameArchive[GameManager.RNG.RandiRange(0, NameArchive.Length - 1)];
			Health = 100;
			Hunger = 50;
			Thirst = 50;
			Sleepiness = 50;
			Affection = 50;

			strength = new Strength();
			intelligence = new Intelligence();
			speed = new Speed();
			stamina = new Stamina();
		}

		// Abilities
		public IAphidAbility strength;
		public IAphidAbility intelligence;
		public IAphidAbility speed;
		public IAphidAbility stamina;
    }
	public record Genes
	{
		public Color AntennaColor {get; set; }
		public Color BodyColor {get; set; }
		public Color LegColor {get; set; }
		public Color EyeColor {get; set; }

		public int LegType {get; set; }
		public int BodyType {get; set; }
		public int AntennaType {get; set; }
		public int EyeType {get; set; }

		public FoodType FoodPreference {get; set; }
		public float[] FoodMultipliers {get; set; }

		public List<IAphidSkill> Skills  {get; set; }

		public Genes()
		{
			Skills = new();
			SetFoodPreferences();
		}
		public void RandomizeColors()
		{
			AntennaColor = GameManager.Utils.GetRandomColor();
			BodyColor = GameManager.Utils.GetRandomColor();
			LegColor = GameManager.Utils.GetRandomColor();
			EyeColor = GameManager.Utils.GetRandomColor();
		}
		public void SetFoodPreferences()
		{
			FoodPreference = (FoodType)GameManager.GetRandomByWeight(flavor_weights);
			FoodMultipliers = new float[]{ 
				GetMultiplier(FoodType.Sweet),
				GetMultiplier(FoodType.Sour),
				GetMultiplier(FoodType.Salty),
				GetMultiplier(FoodType.Bitter),
				FoodPreference == FoodType.Vile ? GetMultiplier(FoodType.Vile) - 0.65f : -0.5f, // the rare Vile preference
				GetMultiplier(FoodType.Bland) - 0.4f,
				1
			};
		}
		public float GetMultiplier(FoodType _type) =>
			0.5f + (_type == FoodPreference ? 0.5f : 0) + GameManager.RNG.Randf();
	}

	public abstract class IAphidAbility
	{
		public abstract string Id { get; }
		public int Points { get; set; }
		public int Level { get; set; }

		public delegate void LevelUpEventHandler();
		public event LevelUpEventHandler OnLevelUp;

		public virtual void GivePoints(int _points)
		{
			if (_points > 10)
				return;
			Points += _points;
			if (_points > 10)
			{
				Level++;
				Points -= 10;
			}
			OnLevelUp?.Invoke(); 
		}
		public virtual void GiveLevel(int _level)
		{
			Level += _level;
			OnLevelUp?.Invoke(); 
		}
	}
    public partial class Stamina : IAphidAbility
    {
        public override string Id => "Stamina";
    }
    public partial class Intelligence : IAphidAbility
    {
        public override string Id => "Intelligence";
    }
	public partial class Strength : IAphidAbility
    {
        public override string Id => "Strength";
    }
	public partial class Speed : IAphidAbility
    {
        public override string Id => "Speed";
    }

    public interface IAphidSkill 
	{
		public string Id { get; }
	}
	public class Production : IAphidSkill
	{
		public string Id => "Production";

		public float Quality;
		public int Produce;
	}
	public class Combat : IAphidSkill
	{
		public string Id => "Combat";
	}
	public class Finder : IAphidSkill
	{
		public string Id => "Finder";
	}
}
