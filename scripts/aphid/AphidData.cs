using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class AphidData
{
	public enum FoodType { Sweet, Sour, Salty, Bitter, Vile, Bland, Neutral }
	private readonly static float[] flavor_weights = [
		25,
		25,
		30,
		15,
		5
	];
	public readonly static string[] NameArchive =
    [
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
		"Axel", "Lea", "Sky", "April",
		"Alicia", "Mr Von Aphid", "Apartment Complex",
	];
	internal static int Age_Adulthood = 1200, Age_Death = 7200,
		Breed_Cooldown = 1200, Harvest_Cooldown = 120, Food_Drain_Time = 16, Water_Drain_Time = 10;

	internal const int HARVEST_VALUE_BABY = 5, HARVEST_VALUE_ADULT = 9, PET_DURATION = 1;

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
		public AphidActions.BreedState.BreedEnum BreedMode { get; set; }
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
			BreedMode = AphidActions.BreedState.BreedEnum.Inactive;
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
		/// Used exclusively for new aphids. Does not change their color and skin.
		/// </summary>
		public void GenerateNewAphid()
		{
			Name = NameArchive[GlobalManager.RNG.RandiRange(0, NameArchive.Length - 1)];
			Mother = Father = "Crystal";
			Owner = Player.Data?.Name;
			GenerateSkills();
			GenerateFoodPreferences();
			GenerateTraits();
		}
		public void BreedNewAphid(AphidInstance _father, AphidInstance _mother)
		{
			AphidInstance[] _parents = [_mother, _father];
			Name = NameArchive[GlobalManager.RNG.RandiRange(0, NameArchive.Length - 1)];
			Father = _father.Genes.Name;
			Mother = _mother.Genes.Name;
			Owner = Player.Data?.Name;

			// inherit skills
			GenerateSkills();
			for (int i = 0; i < Skills.Count; i++)
			{
				int _total = Mathf.CeilToInt((_father.Genes.Skills[i].Level + _mother.Genes.Skills[i].Level) / 4);
				Skills[i].Level = _total;
			}

			GenerateTraits(2); // randomize two traits
			// inherit two random traits from the father and mother
			Traits.Add(_father.Genes.Traits[GlobalManager.RNG.RandiRange(0, _father.Genes.Traits.Count - 1)]);
			Traits.Add(_mother.Genes.Traits[GlobalManager.RNG.RandiRange(0, _mother.Genes.Traits.Count - 1)]);
			
			GenerateFoodPreferences();

			AntennaType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.AntennaType;
			EyeType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.EyeType;
			BodyType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.BodyType;
			LegType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.LegType;
			AntennaColor = LerpColor(_mother.Genes.AntennaColor, _father.Genes.AntennaColor);
			EyeColor = LerpColor(_mother.Genes.EyeColor, _father.Genes.EyeColor);
			BodyColor = LerpColor(_mother.Genes.BodyColor, _father.Genes.BodyColor);
			LegColor = LerpColor(_mother.Genes.LegColor, _father.Genes.LegColor);
		}
		
		public virtual void GenerateSkills()
		{
			Skills =
            [
                new Aphid.Skill("stamina"),
				new Aphid.Skill("strength"),
				new Aphid.Skill("intelligence"),
				new Aphid.Skill("speed"),
			];
		}
		public virtual void GenerateTraits(int _amount = 3)
		{
			Traits = [];

			for (int timeout = 0; timeout < 500; timeout++)
			{
				Aphid.ITrait _trait = AphidTraits.GetRandomTrait(out string _trait_name);

				if (Traits.Contains(_trait_name))
					continue;

				for (int i = 0; i < Traits.Count; i++)
				{
					Aphid.ITrait _existingTrait = AphidTraits.GetTraitByName(Traits[i]);
					// reject traits incompatible with our current ones
					if (_trait.IsIncompatibleWith(Traits[i]) || _existingTrait.IsIncompatibleWith(_trait_name))
					{
						_trait = null;
						break;
					}
				}

				if (_trait == null)
					continue;
				else
					Traits.Add(_trait_name);

				if (Traits.Count == _amount)
					break;
			}
			Logger.Print(Logger.LogPriority.Debug, "AphidTraits: Selected the following traits: ", string.Join(", ", Traits));
		}
		public virtual void GenerateFoodPreferences()
		{
			FoodPreference = (FoodType)GlobalManager.Utils.GetRandomByWeight(flavor_weights);
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
		public virtual float GetMultiplier(FoodType _type) =>
			0.5f + (_type == FoodPreference ? 0.5f : 0) + GlobalManager.RNG.Randf();
		public virtual Color LerpColor(Color _color1, Color _color2)
		{
			// we combine all colors to find the strongest value and order by such
			List<float> _colors = [(_color1.R + _color2.R) / 2,
					(_color1.G + _color2.G) / 2,
					(_color1.B + _color2.B ) / 2];

			// we randomize the gain and loss of RGB values
			_colors[0] = Mathf.Clamp(_colors[0] + GlobalManager.RNG.RandfRange(-30, 30), 0.2f, 0.8f);
			_colors[1] = Mathf.Clamp(_colors[1] + GlobalManager.RNG.RandfRange(-30, 30), 0.2f, 0.8f);
			_colors[2] = Mathf.Clamp(_colors[2] + GlobalManager.RNG.RandfRange(-30, 30), 0.2f, 0.8f);

			// check that they do not equal to a grey-ish/white/black color
			if (Mathf.IsEqualApprox(_colors[0], _colors[1], 0.05) && Mathf.IsEqualApprox(_colors[1], _colors[2], 0.05))
			{
				_colors[0] += _colors[0] - 0.1f < 0.2f ?  0.1f : -0.1f;
				_colors[2] += _colors[0] - 0.2f < 0.2f ?  0.2f : -0.2f;
			}

			return new Color(_colors[0], _colors[1], _colors[2]);
		}

		/// <summary>
		/// FOR DEBUG PURPOSES
		/// </summary>
		public void DEBUG_Randomize(bool _generateGenes = true, bool _generateSkinTypes = true, bool _generateColors = true)
		{
			RandomNumberGenerator _gen = new();
			if (_generateColors)
			{
				AntennaColor = GlobalManager.Utils.GetRandomColor();
				BodyColor = GlobalManager.Utils.GetRandomColor();
				LegColor = GlobalManager.Utils.GetRandomColor();
				EyeColor = GlobalManager.Utils.GetRandomColor();
			}
			else
			{
				AntennaColor = new Color("black");
				BodyColor = new Color("green");
				LegColor = new Color("brown");
				EyeColor = new Color("black");
			}

			if (_generateSkinTypes)
			{
				AntennaType = _gen.RandiRange(0, 1);
				EyeType = _gen.RandiRange(0, 1);
				BodyType = _gen.RandiRange(0, 1);
				LegType = _gen.RandiRange(0, 1);
			}

			if (_generateGenes)
				GenerateNewAphid();
			else
			{
				Skills = [];
				Traits = [];
			}
		}
	}

	public class AphidRelationship
	{
		public Guid aphid;
		public sbyte relationship;
	}
}
