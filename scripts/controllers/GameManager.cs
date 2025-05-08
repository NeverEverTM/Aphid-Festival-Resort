using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }
	public const string ID = "main";

	public static GameManagerSaveModule ProfileSaveModule { get; set; }
	internal static bool IsNewGame { get; set; }
	internal static bool APPLY_OUTOFBOUND_PATCH = false;

	public static GameData ProfileData { get; private set; }
	/// <summary>
	/// All current aphids available in this savefile. To access aphids currently loaded in the resort,
	///  go to ResortManager.Current.Aphids instead.
	/// </summary>
	public static Dictionary<Guid, AphidInstance> Aphids { get; private set; }
    public class GameManagerSaveModule(string ID, SaveSystem.IDataModule<GameData> _module, int LoadPriority = 0) : SaveSystem.SaveModule<GameData>(ID, _module, LoadPriority)
    {
        public override GameData PostLoad(string _raw_data)
        {
			if (GameVersion < 210)
				APPLY_OUTOFBOUND_PATCH = true;
            return base.PostLoad(_raw_data);
        }
    }

    public override void _EnterTree()
	{
		Instance = this;
	}
    public override async void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && GlobalManager.Scene == GlobalManager.SceneName.Resort)
			await SaveSystem.SaveProfile();
    }

    public static async void StartGame()
    {
		
		ProfileData = new();
		Aphids = [];
		SaveSystem.ProfileClassData.Add(ProfileSaveModule);
        // On New game, put intro cutscene, otherwise just load normally
		if (!IsNewGame)
		{
			await SaveSystem.LoadProfile();
			CheckForGameOver();
		}
		else
		{
			IsNewGame = false;
			var _last = CameraManager.Instance.PositionSmoothingSpeed;
			Player.Instance.SetDisabled(true);
			PlayerInventory.StoreItem("aphid_egg");
			PlayerInventory.StoreItem("aphid_egg");

			// TODO: cutscene
			Player.Instance.GlobalPosition = ResortManager.Current.SpawnPoint.GlobalPosition;
			CameraManager.Instance.GlobalPosition = Player.Instance.GlobalPosition + new Vector2(300, 30);
			await SaveSystem.SaveProfile();
			CameraManager.Instance.PositionSmoothingSpeed = 0;
			await Task.Delay(1750);
			Player.Instance.SetMovementDirection(Vector2.Right);
			CameraManager.Instance.PositionSmoothingSpeed = 0.5f;
			while (CameraManager.Instance.GetScreenCenterPosition().DistanceSquaredTo(Player.Instance.GlobalPosition) > 400)
				await Task.Delay(1);
			Player.Instance.SetMovementDirection(Vector2.Zero);
			await Task.Delay(200);
			CameraManager.Instance.PositionSmoothingSpeed = _last;
			await DialogManager.Instance.OpenDialogBox("intro_welcome");
			Player.Instance.SetDisabled(false);
		}
    }

    public override void _Process(double delta)
	{
		if (!GlobalManager.IsBusy)
			ProfileData.Playtime += (float)delta;
	}
	public static void CheckForGameOver()
	{
		// check for lose condition
		int _maxCost = 0;
		for (int i = 0; i < Player.Data.Inventory.Count; i++)
		{
			int _cost = GlobalManager.G_ITEMS[Player.Data.Inventory[i]].cost / 2;
			_maxCost += _cost;
		}
		if (Aphids.Count == 0 && Player.Data.Currency + _maxCost < 50)
			GameOver.OhNo();
	}

	/// <summary>
	/// Adds an aphid permanently to the savefile. Requires an already configured aphid in order to work.
	/// </summary>
	/// <param name="_buddy">Aphid Instance to add to the game</param>
	public static void AddAphid(AphidInstance _buddy)
	{
		if (Aphids.ContainsKey(_buddy.GUID))
		{
			Logger.Print(Logger.LogPriority.Warning, $"GameManager: The aphid '{_buddy.Genes.Name}'<{_buddy.GUID}> was already present in the list.");
			return;
		}
		Aphids.Add(_buddy.GUID, _buddy);
	}
	/// <summary>
	/// Removes an aphid from the game permanently using its GUID key. Does NOT automatically add and aphid to the Generations List.
	/// </summary>
	/// <param name="_guid">The key of the aphid to remove.</param>
	public static void RemoveAphid(Guid _guid)
	{
		if (!Aphids.TryGetValue(_guid, out AphidInstance value))
		{
			Logger.Print(Logger.LogPriority.Error, $"GameManager: Cannot delete <{_guid}> as it does not exist.");
			return;
		}
		ResortManager.Current.Aphids.Remove(value.Entity);
		Aphids.Remove(_guid);
	}

	public class SaveModule : SaveSystem.IDataModule<GameData>
	{
		public void Set(GameData _data)
		{
			ProfileData = _data;
		}
		public GameData Get()
		{
			ProfileData.Version = GlobalManager.GAME_VERSION;
			ProfileData.AphidCount = Aphids.Count;
			return ProfileData;
		}
		public GameData Default() => new();
	}
	public record GameData
	{
		public int AphidCount { get; set; }
		public float Playtime { get; set; }
		public uint Version { get; set; }

		public GameData()
		{
			AphidCount = 0;
			Playtime = 0;
			Version = GlobalManager.GAME_VERSION;
		}
	}
}
