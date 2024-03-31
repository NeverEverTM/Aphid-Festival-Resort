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
	public readonly static string[] NameArchive = new string[]
	{
		"Apuff", "Kiwi", "Brassmo",
		"Alphred", "Sumi", "Summer",
		"Apa", "Miriam", "Cascade",
		"Artyom", "Leif", "Sushi",
		"Apy", "Alpha", "Crow",
		"Azure", "Arty", "A.P.I",
		"Atlas", "Amelia", "Audrey",
		"Alex", "Charlie", "Madeline",
		"Ape", "Axye", "Theo", "Jeb",
		"Apu", "Arthur", "MissingNo",
		"Ace", "Amber", "Atheleya",
		"Arial", "Aphid", "Amor",
		"Ariel", "Armando", "Omega",
		"Afty", "Buggy", "Toffee",
		"Axel", "Lea", "Sky",
		"Alicia", "Mr Von Aphid", "Apartment Complex",
	};
	public const int adulthoodAge = 1200, breedTimer = 600, deathAge = 7200;

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
		public float BreedBuildup { get; set; }
		/// <summary>
		/// 0 : None, 1 : With Itself, 2 : With Partner
		/// </summary>
		public int BreedMode { get; set; }
		public float MilkBuildup { get; set; }

		// Lifetime
		public float Health { get; set; }
		public float Age { get; set; }
		public bool IsAdult { get; set; }

		// State
		public float PositionX { get; set; }
		public float PositionY { get; set; }
		public Aphid.AphidState LastActiveState { set; get; }

		// Gen Data
		public Status()
		{
			Name = NameArchive[GameManager.RNG.RandiRange(0, NameArchive.Length - 1)];
			Health = 100;
			Hunger = 50;
			Thirst = 50;
			Sleepiness = 50;
			Affection = 50;
		}
	}
	public record Genes
	{
		public Color AntennaColor { get; set; }
		public Color BodyColor { get; set; }
		public Color LegColor { get; set; }
		public Color EyeColor { get; set; }

		public int LegType { get; set; }
		public int BodyType { get; set; }
		public int AntennaType { get; set; }
		public int EyeType { get; set; }

		public FoodType FoodPreference { get; set; }
		public float[] FoodMultipliers { get; set; }

		public Dictionary<string, IAphidAbility> Abilities { get; set; }

		public Genes()
		{
			Abilities = new()
			{
				//"stamina", new IAphidSkill()
			};
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

	public interface IAphidAbility
	{
		public int Points { get; set; }
		public int Level { get; set; }

		public virtual void SetPoints(int _points)
		{
			if (_points > 9)
				return;
			Points = _points;
			if (Points < 0) // Clamp at zero, cant level down
				Points = 0;
		}
		public virtual void GivePoints(int _points)
		{
			if (_points > 10)
				return;
			Points += _points;
			if (Points > 9) // Level up once you reach 10 points
			{
				Level++;
				Points -= 10;
			}
			else if (Points < 0) // Clamp at zero, cant level down
				Points = 0;
			OnLevelUp();
		}
		public virtual void GiveLevel(int _level)
		{
			Level += _level;
			OnLevelUp();
		}
		public void OnLevelUp();
	}
}
