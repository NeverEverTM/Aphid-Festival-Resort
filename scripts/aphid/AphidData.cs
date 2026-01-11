using Godot;
using System;
using System.Collections.Generic;

public partial class AphidData : Aphid
{
	/// <summary>
	/// Aphids can have a taste for the first five. Bland is used for miscellaneous items that should not be consumed raw, and Neutral acts as a special food that can be consumed by anyone, and applies the same values to all.
	/// </summary>
	public enum FoodType { Sweet, Sour, Salty, Bitter, Vile, Bland, Neutral }
	public enum SkillEnum { Speed, Strength, Intelligence, Stamina }
	public enum BreedMode { Inactive = -1, WithItself = 0, WithPartner = 1, AsPartner = 2 }
	public enum ValueFlagsEnum { RestTimeMultitplier, IdleTimeMultiplier, BreedTimeMultiplier }
	public enum BoolFlagsEnum { IsHeavySleeper, IsPicky, CanOvereat }
	public enum EntityStatusType 
	{ 
		/// <summary>
        /// Entity is currently loaded in.
        /// </summary>
		Active,
		/// <summary>
        /// Entity is not loaded and is running passively on the background.
        /// </summary>
		Passive,
		/// <summary>
        /// Entity is not loaded and does not run passively on the background.
        /// </summary>
		Busy
	}
	
	public readonly static string[] SkillNames = [ "speed", "strength", "intelligence", "stamina" ];
	private readonly static float[] flavor_weights = [22, 22, 22, 22, 12];
	public readonly static string[] NameArchive =
	[
		"Apuff", "Amok", "Amor",
		"Alphred", "Amimir",
		"Apa", "Artyom",
		"Apy", "Alpha", "Abigail",
		"Azure", "Arty", "API",
		"Atlas", "Amelia", "Audrey",
		"Alex", "Axel", "Axye",
		"Ape", "Artichoke", "Angel",
		"Apu", "Arthur", "Aria",
		"Ace", "Amber", "Atheleya",
		"Arial", "Aphid", "Amor",
		"Azriel", "Armando", "Andew",
		"Afty", "Alison", "Astrid",
		"Axel", "April", "August",
		"Alicia", "Apartment Complex",
	];
	public readonly static string[] SpecialNameArchive =
	[
		"Kiwi", "Miriam",
		"Leif", "Kabbu", "Vi", "Theo", "Jeb", "Buggy", "Toffee",
		"Lea", "Mr Von Aphid", "Madeline", "Brassmo", "Summer",
	];
	/// <summary>
	/// Measured in seconds.
	/// </summary>
	internal static int Age_Adulthood = 1200, Age_Death = 7200,
		Breed_Cooldown = 2400, Harvest_Cooldown = 180;
	/// <summary>
	/// Measured in seconds.
	/// </summary>
	internal const int MAX_TIREDNESS_SLEEP = 75, MIN_TIREDNESS_WAKEUP = 15,
		BASE_FOOD_DECAY = 11, BASE_THIRST_DECAY = 9, BASE_AFFECTION_DECAY = 12,
		BASE_BONDSHIP_DECAY = 50, BASE_BONDSHIP_GRACE = 700;

	internal const int HARVEST_VALUE_BABY = 2, HARVEST_VALUE_ADULT = 5;

	internal const float PET_DURATION = 0.8f, BASE_SLEEP_DECAY = 9.5f,
		BASE_SLEEP_GAIN = 3.5f;

	internal const float COLOR_RANGE = 0.15f;

	/// <summary>
	/// Current living status of the aphid and its needs
	/// </summary>
	public class Status
	{
		// Basic stats
		public float Hunger { get; set; } = 50;
		public float Thirst { get; set; } = 50;
		public float Tiredness { get; set; } = 0;
		public float Affection { get; set; } = 50;
		public float Bondship { get; set; } = 0;

		// Production & Breeding
		public float BreedBuildup { get; set; }
		public float HarvestBuildup { get; set; }
		public BreedMode BreedMode { get; set; } = BreedMode.Inactive;

		// Lifetime
		public float Health { get; set; } = 100;
		public float Age { get; set; } = 0;
		public bool IsAdult { get; set; } = false;

		// State
		public float PositionX { get; set; }
		public float PositionY { get; set; }
		public Aphid.StateEnum LastActiveState { set; get; }
		public string HomeResort { get; set; }
   		public EntityStatusType Mode { get; set; } = EntityStatusType.Active;
		/// <summary>
        /// Time at which this aphid was last loaded, uses the last playtime as an anchor.
        /// </summary>
		public double LastTimeLoaded;

		// Training
		public TrainData CurrentTraining;
	}
	/// <summary>
	/// Genetic information about the aphid's preferences and personality.
	/// </summary>
	public record Genes
	{
		public string Name { get; set; }
		public string Owner { get; set; }
		public Guid Father { get; set; }
		public Guid Mother { get; set; }

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

		public Dictionary<string, AphidActions.Skill> Skills { get; set; } = [];
		public List<string> Traits { get; set; } = [];
		public Dictionary<Guid, AphidActions.Relationship> Relationships { get; set; } = [];

		/// <summary>
		/// This function generates new info completely from scratch without taking inheritance into account.
		/// Used exclusively for new aphids. Does not change their color and skin.
		/// </summary>
		public void GenerateNewAphid()
		{
			Name = NameArchive[GlobalManager.RNG.RandiRange(0, NameArchive.Length - 1)];
			Mother = Father = Guid.Empty;
			Owner = Player.Data?.Name;
			GenerateSkills();
			GenerateFoodPreferences();
			GenerateTraits();
		}
		public void BreedNewAphid(AphidInstance _father, AphidInstance _mother, bool _alone = false)
		{
			AphidInstance[] _parents = [_mother, _father];
			Name = NameArchive[GlobalManager.RNG.RandiRange(0, NameArchive.Length - 1)];
			Owner = Player.Data?.Name;

			// generate mother and father relationships
			Relationships.Add(_mother.GUID, new(_mother.GUID, AphidActions.Relationship.RelationshipLevel.Parent, 50, true));
			if (!_alone)
				Relationships.Add(_father.GUID, new(_father.GUID, AphidActions.Relationship.RelationshipLevel.Parent, 50, true));
			Father = _father.GUID;
			Mother = _mother.GUID;

			// inherit skills
			GenerateSkills();
			InheritSkills(_father, _mother);

			// randomize two traits
			GenerateTraits(2);
			// inherit two random traits from the father and mother
			InheritTrait(_father);
			InheritTrait(_mother);

			if (Traits.Count > 4)
				DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidData: More than four traits.");

			// generate preferences
				GenerateFoodPreferences();

			// generate skin
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
			Skills = new()
			{
				{"stamina", new AphidActions.Skill("stamina")},
				{"strength", new AphidActions.Skill("strength")},
				{"intelligence", new AphidActions.Skill("intelligence")},
				{"speed", new AphidActions.Skill("speed")},
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
				AphidTraits.ITrait _trait = AphidTraits.GetRandomTrait(out string _trait_name);

				if (Traits.Contains(_trait_name))
					continue;

				for (int i = 0; i < Traits.Count; i++)
				{
					AphidTraits.ITrait _existingTrait = AphidTraits.GetTraitByName(Traits[i]);
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
			DebugLogger.Print(DebugLogger.LogPriority.Debug, "AphidTraits: Selected the following traits: ", string.Join(", ", Traits));
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
					AphidTraits.ITrait _trait = AphidTraits.GetTraitByName(Traits[i]);
					if (_trait.IsIncompatibleWith(_trait_name))
					{
						_incompatible = true;
						break;
					}
				}
				if (_incompatible)
					continue;

				Traits.Add(_trait_name);
				return;
			}
		}
		public virtual void GenerateFoodPreferences()
		{
			FoodPreference = (FoodType)GlobalManager.Utils.GetRandomByWeight(flavor_weights);
			FoodMultipliers = [
				GetMultiplier(FoodType.Sweet),
				GetMultiplier(FoodType.Sour),
				GetMultiplier(FoodType.Salty),
				GetMultiplier(FoodType.Bitter),
				FoodPreference == FoodType.Vile ? GetMultiplier(FoodType.Vile) : -1, // the rare Vile preference
				GetMultiplier(FoodType.Bland) - 0.4f,
				1
			];
		}
		public virtual float GetMultiplier(FoodType _type) =>
			0.5f + (_type == FoodPreference ? 0.5f : 0) + GlobalManager.RNG.Randf();
		public static Color LerpColor(Color _color1, Color _color2)
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

	public class TrainData
    {
        public int PointGain = 0;
		public float BaseTime = 0;
		public SkillEnum Skill = SkillEnum.Stamina;
    }
}
