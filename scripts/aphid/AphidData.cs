using Godot;
using System;
using System.Collections.Generic;

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
		Breed_Cooldown = 1800, Harvest_Cooldown = 180, Food_Drain_Time = 11, Water_Drain_Time = 9, Care_Drain_Time = 12;

	internal const int HARVEST_VALUE_BABY = 4, HARVEST_VALUE_ADULT = 8;
	internal const float PET_DURATION = 0.8f;
	internal const float COLOR_RANGE = 0.15f;

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
		public float HarvestBuildup { get; set; }
		public AphidActions.BreedState.BreedEnum BreedMode { get; set; }

		// Lifetime
		public float Health { get; set; }
		public float Age { get; set; }
		public bool IsAdult { get; set; }

		// State
		public float PositionX { get; set; }
		public float PositionY { get; set; }
		public Aphid.StateEnum LastActiveState { set; get; }
		public string HomeResort { get; set; }

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

		public Dictionary<string, Aphid.Skill> Skills { get; set; } = [];
		public List<string> Traits { get; set; } = [];
		public Dictionary<Guid, Aphid.Relationship> Relationships { get; set; } = [];

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


			// randomize two traits
			GenerateTraits(2);
			// inherit two random traits from the father and mother
			InheritTrait(_father);
			InheritTrait(_mother);

			GenerateFoodPreferences();

			AntennaType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.AntennaType;
			EyeType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.EyeType;
			BodyType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.BodyType;
			LegType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.LegType;
			AntennaColor = LerpColor(_mother.Genes.AntennaColor, _father.Genes.AntennaColor, _mother, _father);
			EyeColor = LerpColor(_mother.Genes.EyeColor, _father.Genes.EyeColor, _mother, _father);
			BodyColor = LerpColor(_mother.Genes.BodyColor, _father.Genes.BodyColor, _mother, _father);
			LegColor = LerpColor(_mother.Genes.LegColor, _father.Genes.LegColor, _mother, _father);

			Relationships.Add(_mother.GUID, new(_mother.GUID, Aphid.Relationship.RelationshipLevel.Parent, 50, true));
		}

		public virtual void GenerateSkills()
		{
			Skills = new()
			{
				{"stamina", new Aphid.Skill("stamina")},
				{"strength", new Aphid.Skill("strength")},
				{"intelligence", new Aphid.Skill("intelligence")},
				{"speed", new Aphid.Skill("speed")},
			};
		}
		public virtual void InheritSkills(AphidInstance _father, AphidInstance _mother)
		{
			foreach (var _pair in Skills)
			{
				int _total = Mathf.CeilToInt((_father.Genes.Skills[_pair.Key].Level + _mother.Genes.Skills[_pair.Key].Level) / 4);
				_pair.Value.Level = _total;
			}
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
		public virtual void InheritTrait(AphidInstance _aphid)
		{
			for (int timeout = 0; timeout < 500; timeout++)
			{
				string _trait_name = _aphid.Genes.Traits[GlobalManager.RNG.RandiRange(0, _aphid.Genes.Traits.Count - 1)];
				if (Traits.Contains(_trait_name))
					continue;

				bool _incompatible = false;
				for (int i = 0; i < Traits.Count; i++)
				{
					Aphid.ITrait _trait = AphidTraits.GetTraitByName(Traits[i]);
					if (_trait.IsIncompatibleWith(_trait_name))
					{
						_incompatible = true;
						break;
					}
				}
				if (_incompatible)
					continue;

				Traits.Add(_trait_name);
			}
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
		public virtual Color LerpColor(Color _color1, Color _color2, AphidInstance _mother, AphidInstance _father = null)
		{
			// we combine all colors to find the strongest value and order by such
			List<float> _colors = [GD.RandRange(0,1) == 0 ? _color1.R : _color2.R,
					GD.RandRange(0,1) == 0 ? _color1.G : _color2.G,
					GD.RandRange(0,1) == 0 ? _color1.B : _color2.B];

			// we randomize the gain and loss of RGB values
			_colors[0] = Mathf.Clamp(_colors[0] + GlobalManager.RNG.RandfRange(-COLOR_RANGE, COLOR_RANGE), 0.1f, 0.9f);
			_colors[1] = Mathf.Clamp(_colors[1] + GlobalManager.RNG.RandfRange(-COLOR_RANGE, COLOR_RANGE), 0.1f, 0.9f);
			_colors[2] = Mathf.Clamp(_colors[2] + GlobalManager.RNG.RandfRange(-COLOR_RANGE, COLOR_RANGE), 0.1f, 0.9f);

			// check that they do not equal to a grey-ish/white/black color
			if (Mathf.IsEqualApprox(_colors[0], _colors[1], 0.05) && Mathf.IsEqualApprox(_colors[1], _colors[2], 0.05))
			{
				_colors[0] = Mathf.Clamp(_colors[0] + GlobalManager.RNG.RandfRange(-COLOR_RANGE, 0), 0.1f, 0.9f);
				_colors[2] = Mathf.Clamp(_colors[0] + GlobalManager.RNG.RandfRange(0, COLOR_RANGE), 0.1f, 0.9f);
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
				AntennaType = _gen.RandiRange(0, 2);
				EyeType = _gen.RandiRange(0, 2);
				BodyType = _gen.RandiRange(0, 2);
				LegType = _gen.RandiRange(0, 2);
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
}
