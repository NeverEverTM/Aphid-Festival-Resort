using System.Collections.Generic;
using Godot;

public static class UpgradeHub
{
    public static readonly List<IUpgradeModuleCore> G_UPGRADES = [
        new MembershipUpgrade(),
        new CaretakerUpgrade(),
        new TaxHeavenUpgrade(),
        new JobLevelUpgrade()
    ];

    public static IUpgradeModuleCore GetUpgrade(string ID) => G_UPGRADES.Find((u) => u.ID.Equals(ID));

    /// <summary>
	/// In-game upgrade data container.
	/// </summary>
	public record UpgradeModule(int Level)
    {
        public int Level { get; set; } = Level;
    }
    /// <summary>
    /// Base implementation for an upgrade. Must be added to the AvailableUpgrades list in order to be interacted with.
    /// </summary>
    public abstract class IUpgradeModuleCore
    {
        /// <summary>
        /// The unique identifier of the module, repeated instances will throw an error.
        /// </summary>
        public abstract string ID { get; }
        /// <summary>
        /// The maximum achievable level that can be purchased.
        /// </summary>
        public abstract int MaxLevel { get; }
        /// <summary>
        /// The costs for each acquireable level.
        /// </summary>
        public abstract int[] Costs { get; }
        /// <summary>
        /// The minimum membership tier required to purchase.
        /// </summary>
        public abstract int[] MinimumTiersRequired { get; }

        /// <summary>
        /// Called once when the game starts
        /// </summary>
        /// <param name="_currentLevel">The current level for the upgrade</param>
        public virtual void OnGameLoad(int _currentLevel)
        {
            return;
        }
        /// <summary>
        /// Called at the start of every PostSceneLoad. Not called when purchased or level'd up
        /// </summary>
        /// <param name="_currentLevel">The current level for the upgrade</param>
        public virtual void OnSceneLoad(int _currentLevel)
        {
            return;
        }
    }

    public class MembershipUpgrade : IUpgradeModuleCore
    {
        public override string ID => "membership_tier";
        public override int MaxLevel => 3;
        public override int[] Costs => [100, 400, 800];
        public override int[] MinimumTiersRequired => [0, 1, 2];
    }
    public class CaretakerUpgrade : IUpgradeModuleCore
    {
        public override string ID => "caretaker";
        public override int MaxLevel => 1;
        public override int[] Costs => [700];
        public override int[] MinimumTiersRequired => [2];

        public override void OnGameLoad(int _currentLevel)
        {
            foreach (var _pair in GameManager.Aphids)
            {
                _pair.Value.Timers.Add(new CaretakeFoodTimer(_pair.Value));
                _pair.Value.Timers.Add(new CaretakeWaterTimer(_pair.Value));
            }
        }

        public class CaretakeFoodTimer : CustomBaseTimer<AphidInstance>
        {
            public CaretakeFoodTimer(AphidInstance Entity) : base(Entity)
            {
                BaseTime = AphidData.BASE_HUNGER_DECAY * 2.5f;
                TimeLeft = GetTimerTime();
            }
            public override void Update(float _timePassed)
            {
                if (TInstance.Status.Mode != AphidData.EntityStatusType.Passive)
                    return;
                base.Update(_timePassed);
            }
            public override void Finish()
            {
                TInstance.AddHunger(1);
            }
        }
        public class CaretakeWaterTimer : CustomBaseTimer<AphidInstance>
        {
            public CaretakeWaterTimer(AphidInstance Entity) : base(Entity)
            {
                BaseTime = AphidData.BASE_THIRST_DECAY * 2.5f;
                TimeLeft = GetTimerTime();
            }
            public override void Update(float _timePassed)
            {
                if (TInstance.Status.Mode != AphidData.EntityStatusType.Passive)
                    return;
                base.Update(_timePassed);
            }
            public override void Finish()
            {
                TInstance.AddThirst(1);
            }
        }
    }
    public class TaxHeavenUpgrade : IUpgradeModuleCore
    {
        public override string ID => "tax_heaven";
        public override int MaxLevel => 2;
        public override int[] Costs => [250, 500];
        public override int[] MinimumTiersRequired => [1, 2];

        private static readonly float[] Buffs = [1.25f, 1.5f];

        public override void OnGameLoad(int _currentLevel)
        {
            void ProfitBuff(Player.CurrencyArgs _args)
            {
                if (_args.Source == Player.CurrencySource.AphidGain)
                    _args.Amount = Mathf.FloorToInt(_args.Amount * Buffs[_currentLevel]);
            }
            Player.Instance.RemoveEventListener(ProfitBuff, Player.CurrencyEvents.OnCurrencyGain);
            Player.Instance.AddEventListener(ProfitBuff, Player.CurrencyEvents.OnCurrencyGain);
        }
    }
    public class JobLevelUpgrade : IUpgradeModuleCore
    {
        public override string ID => "job_level";
        public override int MaxLevel => 3;
        public override int[] Costs => [100, 300, 500];
        public override int[] MinimumTiersRequired => [0, 1, 2];

        public Dictionary<JobData.JobDifficulty, int>
        Tier1 = new()
        {
            { JobData.JobDifficulty.Easy, 2 },
            { JobData.JobDifficulty.Medium, 2 },
            { JobData.JobDifficulty.Hard, 1 },
            { JobData.JobDifficulty.Expert, 0 },
            { JobData.JobDifficulty.Master, 0 },
        },
        Tier2 = new()
        {
            { JobData.JobDifficulty.Easy, 2 },
            { JobData.JobDifficulty.Medium, 1 },
            { JobData.JobDifficulty.Hard, 1 },
            { JobData.JobDifficulty.Expert, 1 },
            { JobData.JobDifficulty.Master, 0 },
        },
        Tier3 = new()
        {
            { JobData.JobDifficulty.Easy, 0 },
            { JobData.JobDifficulty.Medium, 3 },
            { JobData.JobDifficulty.Hard, 2 },
            { JobData.JobDifficulty.Expert, 2 },
            { JobData.JobDifficulty.Master, 1 },
        };
        public override void OnGameLoad(int _currentLevel)
        {
            if (JobMenu.Data != null)
                AddDifficultyCount(_currentLevel);
        }
        public override void OnSceneLoad(int _currentLevel)
        {
            if (JobMenu.Data != null)
                AddDifficultyCount(_currentLevel);
        }
        public void AddDifficultyCount(int _currentLevel)
        {
            if (_currentLevel >= 1)
                JobMenu.Instance.AddRequestAmount("jobupgrade_tier1", Tier1);
            if (_currentLevel >= 2)
                JobMenu.Instance.AddRequestAmount("jobupgrade_tier2", Tier2);
            if (_currentLevel >= 3)
                JobMenu.Instance.AddRequestAmount("jobupgrade_tier3", Tier3);
            JobMenu.Instance.FillRequestQuota();
            DebugLogger.Print(DebugLogger.LogPriority.Debug, "UpgradeHub: Applied job increase to level ", _currentLevel);
        }
    }
}
