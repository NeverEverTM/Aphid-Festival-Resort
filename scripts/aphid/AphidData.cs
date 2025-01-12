using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class AphidData
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
	public const int adulthoodAge = 1200, breedTimer = 1200, deathAge = 7200, productionCooldown = 120,
		HarvestValue_Baby = 4, HarvestValue_Adult = 8;
	public const float PET_DURATION = 1;

	/// <summary>
	/// Current living status of the aphid and its needs
	/// </summary>
	public class Status
	{
		// Basic stats
		public float Hunger { get; set; }
		public float Thirst { get; set; }
		public float Tiredness { get; set; }
		public float Affection { get; set; }
		public float Bondship { get; set; }

		// Production & Breeding
		public float BreedBuildup { get; set; }
		/// <summary>
		/// -1 : None, 0 : With Itself, 1 : With Partner
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
		public Aphid.StateEnum LastActiveState { set; get; }

		// Gen Data
		public Status()
		{
			Health = 100;
			Hunger = 50;
			Thirst = 50;
			Tiredness = 50;
			Affection = 50;
			BreedMode = -1;
		}

		public virtual void AddHunger(float _amount) =>
			Hunger = Math.Clamp(Hunger + _amount, 0, 100);
		public virtual void AddThirst(float _amount) =>
			Thirst = Math.Clamp(Thirst + _amount, 0, 100);
		public virtual void AddTiredness(float _amount) =>
			Tiredness = Math.Clamp(Tiredness + _amount, 0, 100);
		public virtual void AddBondship(int _amount) =>
			Bondship = Math.Clamp(Bondship + _amount, 0, 100);
		public virtual void AddAffection(int _amount) =>
			Affection = Math.Clamp(Affection + _amount, 0, 100);
	}
	/// <summary>
	/// Genetic Information about the aphid's preferences and personality
	/// </summary>
	public record Genes
	{
		public string Name { get; set; }
		public string Owner { get; set; }
		public string Father { get; set; }
		public string Mother { get; set; }

		public Color AntennaColor { get; set; }
		public Color EyeColor { get; set; }
		public Color BodyColor { get; set; }
		public Color LegColor { get; set; }

		public int AntennaType { get; set; }
		public int EyeType { get; set; }
		public int BodyType { get; set; }
		public int LegType { get; set; }

		public FoodType FoodPreference { get; set; }
		public float[] FoodMultipliers { get; set; }

		public List<Aphid.Skill> Skills { get; set; }
		public List<string> Traits { get; set; }

		/// <summary>
		/// This function generates new info completely from scratch without taking inheritance into account.
		/// Used exclusively for new aphids.
		/// </summary>
		public void GenerateNewAphid()
		{
			Name = NameArchive[GlobalManager.RNG.RandiRange(0, NameArchive.Length - 1)];
			Mother = Father = "Joy";
			Owner = Player.Data?.Name;
			GenerateSkills();
			GenerateFoodPreferences();
			GenerateTraits();
		}
		// TODO: variation in color and limbs depends on combined skill points and levels from both parents 
		public void BreedNewAphid(AphidInstance _father, AphidInstance _mother)
		{
			AphidInstance[] _parents = new AphidInstance[] { _mother, _father };
			Father = _father.Genes.Name;
			Mother = _mother.Genes.Name;

			AntennaType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.AntennaType;
			EyeType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.EyeType;
			BodyType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.BodyType;
			LegType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.LegType;
			AntennaColor = GlobalManager.Utils.LerpColor(_mother.Genes.AntennaColor, _father.Genes.AntennaColor);
			EyeColor = GlobalManager.Utils.LerpColor(_mother.Genes.EyeColor, _father.Genes.EyeColor);
			BodyColor = GlobalManager.Utils.LerpColor(_mother.Genes.BodyColor, _father.Genes.BodyColor);
			LegColor = GlobalManager.Utils.LerpColor(_mother.Genes.LegColor, _father.Genes.LegColor);
		}
		public virtual void GenerateSkills()
		{
			Skills = new()
			{
				new Aphid.Skill("stamina"),
				new Aphid.Skill("strength"),
				new Aphid.Skill("intelligence"),
				new Aphid.Skill("speed"),
			};
		}
		public virtual void GenerateTraits()
		{
			Traits = new();
			List<string> registered_traits = AphidTraits.TRAITS.Keys.ToList();
			int TIMEOUT = 0;

			while (Traits.Count < 3)
			{
				if (TIMEOUT > 500)
					break;
				TIMEOUT++;
				// we draw a trait from the global pool using a local list
				var _trait = registered_traits[GlobalManager.RNG.RandiRange(0, registered_traits.Count - 1)];

				// check if this trait is compatible with all others
				// otherwise ignore it now and in all follwing checks by removing it from the list 
				Aphid.ITrait _traitToCheck = AphidTraits.TRAITS[_trait];
				bool _isIncompatible = false;

				for (int i = 0; i < Traits.Count; i++)
				{
					if (_traitToCheck.IsIncompatibleWith(_trait))
					{
						registered_traits.Remove(_trait);
						_isIncompatible = true;
						break;
					}
				}
				if (_isIncompatible)
					continue;
				else
					Traits.Add(_trait);
			}
		}
		public virtual void GenerateFoodPreferences()
		{
			FoodPreference = (FoodType)GlobalManager.GetRandomByWeight(flavor_weights);
			FoodMultipliers = new float[]{
				GetMultiplier(FoodType.Sweet),
				GetMultiplier(FoodType.Sour),
				GetMultiplier(FoodType.Salty),
				GetMultiplier(FoodType.Bitter),
				FoodPreference == FoodType.Vile ? GetMultiplier(FoodType.Vile) : -1, // the rare Vile preference
				GetMultiplier(FoodType.Bland) - 0.4f,
				1
			};
		}
		public float GetMultiplier(FoodType _type) =>
			0.5f + (_type == FoodPreference ? 0.5f : 0) + GlobalManager.RNG.Randf();

		/// <summary>
		/// FOR DEBUG PURPOSES
		/// </summary>
		public void DEBUG_Randomize()
		{
			RandomNumberGenerator _gen = new();
			AntennaColor = GlobalManager.Utils.GetRandomColor();
			BodyColor = GlobalManager.Utils.GetRandomColor();
			LegColor = GlobalManager.Utils.GetRandomColor();
			EyeColor = GlobalManager.Utils.GetRandomColor();
			AntennaType = _gen.RandiRange(0, 1);
			EyeType = _gen.RandiRange(0, 1);
			BodyType = _gen.RandiRange(0, 1);
			LegType = _gen.RandiRange(0, 1);
			GenerateNewAphid();
		}
	}

	public class AphidRelationship
	{
		public Guid aphid;
		public sbyte relationship;
	}
}
