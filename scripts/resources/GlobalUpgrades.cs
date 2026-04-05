using Godot;
using System;
using System.Collections.Generic;

public static  class GlobalUpgrades
{
    public static readonly List<IUpgradeModuleCore> AVAILABLE_UPGRADES = [
        new MembershipUpgrade(),
    ];

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
    public interface IUpgradeModuleCore
    {
		/// <summary>
		/// The unique identifier of the module, repeated instances will throw an error.
		/// </summary>
        public string ID { get; }
		/// <summary>
		/// The maximum achievable level that can be purchased.
		/// </summary>
        public int MaxLevel { get; }
		/// <summary>
		/// The costs for each acquireable level.
		/// </summary>
		public int[] Costs { get; }
		/// <summary>
		/// The minimum membership tier required to purchase.
		/// </summary>
		public int[] MinimumTiersRequired { get; }

        /// <summary>
        /// Fired once when the upgrade is bought and/or equipped. OnRefresh is NOT called during this.
        /// </summary>
        public void OnPurchase(int _currentLevel);
        /// <summary>
        /// Fired after a new scene loads, during PostLoad. OnEquip is NOT called during this.
        /// </summary>
        public void OnReload(int _currentLevel);
    }

    public class MembershipUpgrade : IUpgradeModuleCore
    {
        public string ID => "membership_tier";
        public int MaxLevel => 3;
        public int[] Costs => [ 100, 400, 800 ];
        public int[] MinimumTiersRequired => [ 0, 1, 2 ];

        public void OnPurchase(int _currentLevel)
        {
            
        }

        public void OnReload(int _currentLevel)
        {
            
        }
    }
}
