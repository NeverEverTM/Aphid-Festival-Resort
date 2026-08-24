using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

/// <summary>
/// This class holds declarations related to savedata and global aphid behaviour variables, such as time caps, food types, timer intervals, etc. 
/// </summary>
public partial class AphidData : Aphid
{
    /// <summary>
    /// Aphids can have a taste for the first five. Bland is used for miscellaneous items that should not be consumed raw, and Neutral acts as a special food that can be consumed by anyone, and applies the same values to all.
    /// </summary>
    public enum FoodType { Sweet, Sour, Salty, Bitter, Vile, Bland, Neutral }
    public enum SkillEnum { Speed, Strength, Intelligence, Stamina }
    public enum BreedMode { Inactive = -1, WithItself = 0, WithPartner = 1, AsPartner = 2 }
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

    public readonly static string[] SkillNames = ["speed", "strength", "intelligence", "stamina"];
    private readonly static float[] flavor_weights = [22, 22, 22, 22, 12],
        preference_options = [0.25f, 0.5f, 0.75f, 1.0f];
    public readonly static string[] NameArchive =
    [
        "Apuff", "Amok", "Amor",
        "Alphred", "Amimir", "Alan",
        "Apa", "Artyom", "Artic",
        "Apy", "Alpha", "Abigail",
        "Azure", "Arty", "API",
        "Atlas", "Amelia", "Audrey",
        "Alex", "Axel", "Axye",
        "Ape", "Artichoke", "Angel",
        "Apu", "Arthur", "Aria",
        "Ace", "Amber", "Atheleya",
        "Arial", "Aphid", "Ale",
        "Azriel", "Armando", "Andrew", "Asriel",
        "Afty", "Alison", "Astrid",
        "Axel", "April", "August",
        "Alicia", "Apartment Complex",
    ];
    public readonly static string[] SpecialNameArchive =
    [
        "Kiwi", "Miriam", "Genow", "Mar",
        "Leif", "Kabbu", "Vi", "Theo", "Jeb", "Buggy", "Toffee",
        "Lea", "Mr Von Aphid", "Madeline", "Brassmo", "Summer",
    ];

    /// <summary>
    /// Measured in seconds.
    /// </summary>
    internal static int Age_Adulthood = 1200, Age_Lifetime = 7200,
        Breed_Cooldown = 2400, Harvest_Cooldown = 180;

    /// <summary>
    /// Measured in seconds.
    /// </summary>
    internal const int BASE_HUNGER_DECAY = 11, BASE_THIRST_DECAY = 9, BASE_AFFECTION_DECAY = 12,
        BASE_BONDSHIP_DECAY = 50, BASE_BONDSHIP_GRACE = 700, SKILL_LEVEL_CAP = 100;
    /// <summary>
    /// Measured in seconds.
    /// </summary>
    internal const float PET_DURATION = 0.8f, BASE_REST_DECAY = 11f,
        BASE_REST_GAIN = 3f;

    internal const int MIN_REST_TO_WAKEUP = 75, MIN_REST_FOR_SLEEP = 15, HARVEST_VALUE_BABY = 2, HARVEST_VALUE_ADULT = 4;
    internal const float COLOR_RANGE = 0.15f;

    /// <summary>
    /// Current living status of the aphid and its needs
    /// </summary>
    public class Status
    {
        // Basic stats
        [JsonInclude] public float Hunger { get; protected set; } = 50;
        [JsonInclude] public float Thirst { get; protected set; } = 50;
        [JsonInclude] public float Rest { get; protected set; } = 100;
        [JsonInclude] public float Affection { get; protected set; } = 50;
        [JsonInclude] public float Bondship { get; protected set; } = 0;

        // Production & Breeding
        public float BreedBuildup { get; set; } = 0;
        public BreedMode BreedMode { get; set; } = BreedMode.Inactive;
        public float HarvestBuildup { get; set; } = 0;
        public bool IsReadyForHarvest { get; set; }

        // Lifetime
        public float Health { get; set; } = 100;
        public float Age { get; set; } = 0;
        public bool IsAdult { get; set; } = false;
        public bool IsDead { get; set; } = false;

        // State
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public string HomeResort { get; set; }
        public StateEnum LastActiveState { set; get; }
        public EntityStatusType Mode { get; set; } = EntityStatusType.Active;
        /// <summary>
        /// Time at which this aphid was last loaded, uses the last playtime as an anchor.
        /// </summary>
        public double LastTimeLoaded { get; set; }

        // Training
        public TrainData LastTraining { get; set; }

        public virtual void AddHunger(float _amount) =>
            Hunger = Math.Clamp(Hunger + _amount, 0, 100);
        public virtual void AddThirst(float _amount) =>
            Thirst = Math.Clamp(Thirst + _amount, 0, 100);
        public virtual void AddRest(float _amount) =>
            Rest = Math.Clamp(Rest + _amount, 0, 100);
        public virtual void AddAffection(int _amount) =>
           Affection = Math.Clamp(Affection + _amount, 0, 100);
        public virtual void AddBondship(int _amount) =>
            Bondship = Math.Clamp(Bondship + _amount, 0, 100);

        public void StartPatch(uint version)
        {
            if (version < 301)
            {
                if (HarvestBuildup >= Harvest_Cooldown && !IsReadyForHarvest)
                    IsReadyForHarvest = true;

                if (LastTimeLoaded == 0)
                    LastTimeLoaded = GameManager.Data.Playtime;

                if (string.IsNullOrWhiteSpace(HomeResort))
                    HomeResort = "golden";

                Rest = 100 - Rest;
                return;
            }
        }
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

        public Dictionary<string, Skill> Skills { get; set; } = [];
        public List<string> Traits { get; set; } = [];
        public Dictionary<Guid, Relationship> Relationships { get; set; } = [];

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
            Relationships.Add(_mother.GUID, new(_mother.GUID, Relationship.RelationshipLevel.Parent, 50, true));
            if (!_alone)
                Relationships.Add(_father.GUID, new(_father.GUID, Relationship.RelationshipLevel.Parent, 50, true));
            Father = _father.GUID;
            Mother = _mother.GUID;

            // inherit skills
            GenerateSkills();
            InheritSkills(_father, _mother);

            // inherit two random traits from the father and mother
            InheritTrait(_father);
            InheritTrait(_mother);
            // randomize two traits, or more if one failed
            GenerateTraits(2 + (2 - Traits.Count));

            if (Traits.Count > 4)
                DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidData: More than four traits were generated!");
            else if (Traits.Count < 4)
                DebugLogger.Print(DebugLogger.LogPriority.Warning, "AphidData: Less than four traits were generated!");

            // generate preferences
            GenerateFoodPreferences();

            // generate skin
            AntennaType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.AntennaType;
            EyeType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.EyeType;
            BodyType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.BodyType;
            LegType = _parents[GlobalManager.RNG.RandiRange(0, 1)].Genes.LegType;
            AntennaColor = MixAphidColor(_mother.Genes.AntennaColor, _father.Genes.AntennaColor);
            EyeColor = MixAphidColor(_mother.Genes.EyeColor, _father.Genes.EyeColor);
            BodyColor = MixAphidColor(_mother.Genes.BodyColor, _father.Genes.BodyColor);
            LegColor = MixAphidColor(_mother.Genes.LegColor, _father.Genes.LegColor);
        }

        public virtual void GenerateSkills()
        {
            Skills = new()
            {
                {"stamina", new Skill("stamina")},
                {"strength", new Skill("strength")},
                {"intelligence", new Skill("intelligence")},
                {"speed", new Skill("speed")},
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
            List<string> _excludeList = [.. Traits]; // exclude the traits already added

            for (int i = 0; i < Traits.Count; i++) // and exclude any incompatible traits with our trait list
            {
                AphidTraits.GetTraitByID(Traits[i], out ITrait _trait);
                ExcludeIncompatibleTraits(_trait, ref _excludeList);
            }

            for (int i = 0; i < _amount; i++)
            {
                ITrait _trait = AphidTraits.GetRandomTrait(_excludeList);
                if (_trait == null)
                {
                    DebugLogger.Print(DebugLogger.LogPriority.Error, "AphidData: Ran out of traits to select!");
                    return;
                }
                Traits.Add(_trait.ID);
                _excludeList.Add(_trait.ID);
                ExcludeIncompatibleTraits(_trait, ref _excludeList);
            }
#if DEBUG
            DebugLogger.Print(DebugLogger.LogPriority.Debug, "AphidData: Selected the following traits: ", string.Join(", ", Traits));
#endif
        }
        protected static void ExcludeIncompatibleTraits(ITrait _trait, ref List<string> _traits)
        {
            if (_trait.IncompatibleTraits == null)
                return;
            _traits = [.. _traits, .. _trait.IncompatibleTraits];
            _traits = [.. _traits.Distinct()];
        }
        public virtual void InheritTrait(AphidInstance _aphid)
        {
            for (int timeout = 0; timeout < 500; timeout++)
            {
                string _traitName = _aphid.Genes.Traits[GlobalManager.RNG.RandiRange(0, _aphid.Genes.Traits.Count - 1)];
                if (Traits.Contains(_traitName))
                    continue;

                bool _incompatible = false;
                for (int i = 0; i < Traits.Count; i++)
                {
                    ITrait _trait = AphidTraits.CreateTraitByID(Traits[i]);
                    if (_trait.IsIncompatibleWith(_traitName))
                    {
                        _incompatible = true;
                        break;
                    }
                }
                if (_incompatible)
                    continue;

#if DEBUG
                DebugLogger.Print(DebugLogger.LogPriority.Debug, $"AphidData: Trait {_traitName} inherited");
#endif
                Traits.Add(_traitName);
                return;
            }

            DebugLogger.Print(DebugLogger.LogPriority.Error, "AphidGenes: Failed to inherit skill!");
        }
        protected virtual void GenerateFoodPreferences()
        {
            List<float> _preference_options = ShuffleList([.. preference_options]);
            FoodPreference = (FoodType)GlobalManager.Utils.GetRandomByWeight(flavor_weights);
            FoodMultipliers = [
                GetFoodMultiplier(FoodType.Sweet, _preference_options[0]),
                GetFoodMultiplier(FoodType.Sour, _preference_options[1]),
                GetFoodMultiplier(FoodType.Salty, _preference_options[2]),
                GetFoodMultiplier(FoodType.Bitter, _preference_options[3]),
                GetFoodMultiplier(FoodType.Vile), // the rare Vile preference
				0.5f, // Bland
				1 // Neutral
			];
        }
        public static List<T> ShuffleList<T>(List<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = GlobalManager.RNG.RandiRange(0,n);
                (list[n], list[k]) = (list[k], list[n]);
            }
            return list;
		}
        public static Color GetRandomColor(bool _randomizeAlpha = false)
		{
			byte[] _rgba = [ (byte)GlobalManager.RNG.RandiRange(0,255), (byte)GlobalManager.RNG.RandiRange(0,255),
				(byte)GlobalManager.RNG.RandiRange(0,255), _randomizeAlpha ? (byte)(GlobalManager.RNG.RandiRange(0,205) + 50) : (byte)255 ];

			return Color.Color8(_rgba[0], _rgba[1], _rgba[2], _rgba[3]);
		}
        protected virtual float GetFoodMultiplier(FoodType _type, float _extra)
        {
            return 0.5f + (_type == FoodPreference ? 0.5f : 0) + _extra;
        }
        protected virtual float GetFoodMultiplier(FoodType _type) =>
            0.5f + (_type == FoodPreference ? 0.5f : 0);

        public static Color MixAphidColor(Color _color1, Color _color2)
        {
            // we combine all colors to find the strongest value and order by such
            List<float> _colors = [GlobalManager.RNG.RandiRange(0,1) == 0 ? _color1.R : _color2.R,
                    GlobalManager.RNG.RandiRange(0,1) == 0 ? _color1.G : _color2.G,
                    GlobalManager.RNG.RandiRange(0,1) == 0 ? _color1.B : _color2.B];

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
        public void StartPatch(uint version)
        {
            if (version < 301)
            {
                if (Skills.Count == 0)
                    GenerateSkills();

                if (Traits.Count == 0)
                    GenerateTraits();

                while (Traits.Count >= 5)
                {
                    for (int i = 0; i < 4; i++)
                        Traits.RemoveAt(Traits.Count - 1);
                }

                GenerateFoodPreferences();
            }
        }

        /// <summary>
        /// FOR DEBUG PURPOSES
        /// </summary>
        public void DEBUG_Randomize(bool _generateGenes = true, bool _generateSkinTypes = true, bool _generateColors = true)
        {
            RandomNumberGenerator _gen = new();
            if (_generateColors)
            {
                AntennaColor = GetRandomColor();
                BodyColor = GetRandomColor();
                LegColor = GetRandomColor();
                EyeColor = GetRandomColor();
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
        public int RawPointGain { get; set; }
        public float RawBaseTime { get; set; }
        public SkillEnum Skill { get; set; }
        public readonly Dictionary<string, int> PointBuffs = [];
        /// <summary>
        /// Get the true point value gained per training cycle
        /// </summary>
        /// <param name="_multiplier">Multiply the result by this amount</param>
        /// <param name="_applyMultiBeforeBuffs">Applies the multiplication ONLY to the raw point gain, before buffs are applied</param>
        /// <returns></returns>
        public int GetPointGain(float _multiplier, bool _applyMultiBeforeBuffs = true)
        {
            if (_applyMultiBeforeBuffs)
                return (int)(RawPointGain * _multiplier) + PointBuffs.Values.Sum();
            else
                return (int)((RawPointGain + PointBuffs.Values.Sum()) * _multiplier);
        }
        /// <summary>
        /// Get the true point value gained per training cycle
        /// </summary>
        public int GetPointGain()
        {
            return RawPointGain + PointBuffs.Values.Sum();
        }
    }
}
