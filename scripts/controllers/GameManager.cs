using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }
	public const string SAVEMODULE_ID = "main";
	internal static bool IsANewSavefile { get; set; }

	public Timer autoSaveTimer;

	public static GameData Data { get; private set; }
	/// <summary>
	/// All current aphids available in this savefile. To access aphids currently loaded in the resort,
	///  go to ResortManager.Current.Aphids instead.
	/// </summary>
	public static Dictionary<Guid, AphidInstance> Aphids { get; private set; } = [];
	public static Dictionary<Guid, AphidData.Genes> AphidArchive { get; set; } = [];
	public static Dictionary<string, GlobalUpgrades.UpgradeModule> Upgrades { get; set; } = new(){
		{ "membership_tier", new(1) }
	};
	
	public GameSaveModule GameSavefile = new(SAVEMODULE_ID, new GameDataModule(), 9999);
	public AphidSaveModule AphidSavefile = new("aphids", new AphidDataModule(), 3008)
	{
		RelativePath = SaveSystem.PROFILE_APHIDS_DIR,
	};
	public GenerationsSaveModule GenerationsSavefile = new("generations", new GenerationsDataModule(), 2038)
	{
		Extension = SaveSystem.SAVEFILE_EXTENSION,
		RelativePath = SaveSystem.PROFILE_APHIDS_DIR,
	};
	public SaveSystem.SaveModule<Dictionary<string, GlobalUpgrades.UpgradeModule>> UpgradesSavefile = new("upgrades", new UpgradesDataModule(), 1985);

	// MARK: SaveModule Declarations
	public class GameSaveModule(string ID, SaveSystem.IDataModule<GameData> _module, int LoadPriority = 0) :
		SaveSystem.SaveModule<GameData>(ID, _module, LoadPriority)
	{
		public override GameData PostLoad(string _raw_data)
		{
			if (GameVersion < 301)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Warning, "GameManager: Applied Out of Bounds Patch ");
				APPLY_OUTOFBOUND_PATCH = true;
			}
			return base.PostLoad(_raw_data);
		}
	}
	public class GameDataModule : SaveSystem.IDataModule<GameData>
	{
		public void Set(GameData _data)
		{
			Data = _data;
			Data.LastTimeLoaded = Time.GetUnixTimeFromSystem();
		}
		public GameData Get()
		{
			// calculate the time passed between now and last time you saved playtime and add it to the latter
			double _currentTime = Time.GetUnixTimeFromSystem();
			Data.Playtime += _currentTime - Data.LastTimeLoaded;
			Data.LastTimeLoaded = _currentTime;

			Data.AphidCount = Aphids.Count;
			Data.LastRoom = SceneManager.CurrentScene;
			return Data;
		}
		public GameData Default() => new();
	}
	public class AphidSaveModule(string ID, SaveSystem.IDataModule<Dictionary<Guid, AphidInstance>> _module, int LoadPriority = 0) : SaveSystem.SaveModule<Dictionary<Guid, AphidInstance>>(ID, _module, LoadPriority)
	{
		public override string PreLoad()
		{
			string _path = base.PreLoad();

			if (GameVersion < 220)
				_path = _path.Replace(SaveSystem.JSONFILE_EXTENSION, SaveSystem.SAVEFILE_EXTENSION);

			return _path;
		}
		public override Dictionary<Guid, AphidInstance> PostLoad(string _raw_data)
		{
			// v2.1 changed how these variables were named
			// https://github.com/NeverEverTM/Aphid-Festival-Resort/commit/66917b3eff6561269db6c8ccd5044d49475839b1#diff-aece67fc4fc2d3a973f0e7be27a335d3142a29cd16fa4d73631188706b6292dd
			if (GameVersion < 210)
			{
				_raw_data = _raw_data.Replace("MilkBuildup", "HarvestBuildup");

				_raw_data = _raw_data.Replace("\"Skills\":[", "\"Skills\":{");
				_raw_data = _raw_data.Replace("\"Level\":0}],", "\"Level\":0}},");

				_raw_data = _raw_data.Replace("{\"Name\":\"stamina\"", "\"stamina\":{\"Name\":\"stamina\"");
				_raw_data = _raw_data.Replace("{\"Name\":\"strength\"", "\"strength\":{\"Name\":\"strength\"");
				_raw_data = _raw_data.Replace("{\"Name\":\"intelligence\"", "\"intelligence\":{\"Name\":\"intelligence\"");
				_raw_data = _raw_data.Replace("{\"Name\":\"speed\"", "\"speed\":{\"Name\":\"speed\"");
			}

			// v3.0 changed the variables "Mother" and "Father" types
			if (GameVersion < 300)
			{
				_raw_data.Replace("\"Father\":\"", "\"Unusued1\":\""); // TODO: change this to recover parents instead
				_raw_data.Replace("\"Mother\":\"", "\"Unusued2\":\"");
			}

			Dictionary<Guid, AphidInstance> _aphidData = base.PostLoad(_raw_data);
			// Patch runtime variables
			if (GameVersion != GlobalManager.GAME_VERSION)
			{
				foreach (var _aphid in _aphidData)
				{
					_aphid.Value.Genes.StartPatch(GameVersion);
					_aphid.Value.Status.StartPatch(GameVersion);
				}
			}

			return _aphidData;
		}
	}
	public class AphidDataModule : SaveSystem.IDataModule<Dictionary<Guid, AphidInstance>>
	{
		public Dictionary<Guid, AphidInstance> Default() => [];
		public void Set(Dictionary<Guid, AphidInstance> _data)
		{
			Aphids = _data;
			foreach (var aphid in Aphids)
			{
				if (aphid.Value.Status.Mode != AphidData.EntityStatusType.Busy)
					aphid.Value.Status.Mode = AphidData.EntityStatusType.Passive;
				aphid.Value.PassiveEntity = new(aphid.Value);
			}
		}
		public Dictionary<Guid, AphidInstance> Get() => Aphids;
	}
	public class GenerationsSaveModule(string ID, SaveSystem.IDataModule<Dictionary<Guid, AphidData.Genes>> _module, int LoadPriority = 0) : SaveSystem.SaveModule<Dictionary<Guid, AphidData.Genes>>(ID, _module, LoadPriority)
	{
		public override Dictionary<Guid, AphidData.Genes> PostLoad(string _raw_data)
		{
			if (GameVersion < 220)
				return System.Text.Json.JsonSerializer.Deserialize<Savefile>(_raw_data).Archive;
			return base.PostLoad(_raw_data);
		}

		[Serializable] // This class is only kept for backwards compability
		public struct Savefile
		{
			public Dictionary<Guid, AphidData.Genes> Archive { get; set; }
		}
	}
	public class GenerationsDataModule : SaveSystem.IDataModule<Dictionary<Guid, AphidData.Genes>>
	{
		public Dictionary<Guid, AphidData.Genes> Default()
		{
			return [];
		}

		public Dictionary<Guid, AphidData.Genes> Get()
		{
			return AphidArchive;
		}

		public void Set(Dictionary<Guid, AphidData.Genes> _data)
		{
			AphidArchive = _data;
		}
	}
	public class UpgradesDataModule : SaveSystem.IDataModule<Dictionary<string, GlobalUpgrades.UpgradeModule>>
	{
		public Dictionary<string, GlobalUpgrades.UpgradeModule> Default() => [];
		public Dictionary<string, GlobalUpgrades.UpgradeModule> Get() => Upgrades;
		public void Set(Dictionary<string, GlobalUpgrades.UpgradeModule> _data) => Upgrades = _data;
	}

	public record GameData
	{
		public string LastRoom { get; set; } = "golden_resort";
		public double LastTimeLoaded { get; set; }
		public double Playtime { get; set; } = 0;

		//Stats
		public int AphidCount { get; set; } = 0;
		public int TotalAphids { get; set; } = 0;
		public int AphidsSold { get; set; } = 0;
		public int ItemsBought { get; set; } = 0;
		public int ItemsSold { get; set; } = 0;
		public int SavefileBoots { get; set; } = 0;
	}
	
	// MARK: Body
	public override void _EnterTree()
	{
		Instance = this;
		APPLY_OUTOFBOUND_PATCH = false;

		SceneManager.AddEventListener(OnGameInit, SceneManager.EventEnum.OnGameInit);
		SceneManager.AddEventListener(OnGameFinish, SceneManager.EventEnum.OnGameFinish);
	}
	public override async void _Notification(int what)
	{
		// responsible for saving the game when closing the window or exiting the application
		if (SceneManager.CurrentlyInGame && what == NotificationWMCloseRequest)
			await SaveSystem.SaveProfile();
	}
	public override void _Process(double delta)
	{
		if (!SceneManager.CurrentlyInGame)
			return;
		float _delta = (float)delta;
		foreach (var aphid in Aphids)
		{
			// processes the passive behaviour of the aphid while is gone
			if (aphid.Value.Status.Mode == AphidData.EntityStatusType.Passive)
				aphid.Value.PassiveEntity.Process(_delta);
		}
	}

	private void OnGameInit(SceneManager.SceneArgs _args)
	{
		SaveSystem.AddSaveModule(GameSavefile);
		SaveSystem.AddSaveModule(AphidSavefile);
		SaveSystem.AddSaveModule(GenerationsSavefile);
		SaveSystem.AddSaveModule(UpgradesSavefile);

		if (IsANewSavefile)
			StartNewGameCutscene();
		else
			SceneManager.AddEventListener((_) => CheckForGameOver(), SceneManager.EventEnum.OnPostLoad);

		static void _addBootCount(SceneManager.SceneArgs _)
		{
			Data.SavefileBoots++;
		}
		SceneManager.AddEventListener(_addBootCount, SceneManager.EventEnum.OnPostLoad);

		Instance.autoSaveTimer = new();
		Instance.autoSaveTimer.Timeout += () =>
		{
			CanvasManager.StartAutosavePopup();
			_ = SaveSystem.SaveProfile(true);
		};
		Instance.AddChild(Instance.autoSaveTimer);
		Instance.autoSaveTimer.Start(300);
	}
	private void OnGameFinish(SceneManager.SceneArgs _args)
	{
		if (IsInstanceValid(Instance.autoSaveTimer))
			Instance.autoSaveTimer.QueueFree();
	}

	// MARK: Temp Cutscenes
	private static async void StartNewGameCutscene()
	{
		CutsceneManager.IsActive = true;
		Player.Instance.SetDisabled(true);
		var _last = CameraManager.Instance.PositionSmoothingSpeed;
		Player.Instance.GlobalPosition = ResortManager.Current.SpawnPoint.GlobalPosition;
		CameraManager.ForceCameraPosition(Player.Instance.GlobalPosition + new Vector2(1000, 0));
		CameraManager.Instance.PositionSmoothingSpeed = 0;
		CanvasManager.SetHUDTo(false);

		// we set new game data
		await SaveSystem.SetProfileData();
		PlayerInventory.StoreItem("aphid_egg");
		PlayerInventory.StoreItem("aphid_egg");
		await SaveSystem.SaveProfile(false, true);
		await Task.Delay(1750);

		Player.Instance.SetMovementDirection(Vector2.Right);
		CameraManager.Instance.PositionSmoothingSpeed = 0.75f;
		while (CameraManager.Instance.GetScreenCenterPosition().DistanceSquaredTo(CameraManager.Instance.GetTargetPosition()) > 340)
		{
			CameraManager.Instance.PositionSmoothingSpeed += 0.01f;
			await Task.Delay(1);
		}
		Player.Instance.SetMovementDirection(Vector2.Zero);
		await Task.Delay(200);
		CameraManager.Instance.PositionSmoothingSpeed = _last;
		await DialogManager.Instance.OpenDialogBox("intro_welcome");
		Player.Instance.SetDisabled(false);
		CanvasManager.SetHUDTo(true);
		IsANewSavefile = false;
		CutsceneManager.IsActive = false;
	}
	public static void CheckForGameOver()
	{
		int _totalWorth = 0;
		for (int i = 0; i < Player.Data.Inventory.Count; i++)
		{
			int _cost = GlobalManager.G_ITEMS[Player.Data.Inventory[i]].Cost / 2;
			_totalWorth += _cost;
		}

		if (Aphids.Count == 0 && !Player.Data.Inventory.Exists((s) => s.Contains("aphid_egg")) && Player.Data.Currency + _totalWorth < 50)
			GameOver.OhNo();
	}

	// MARK: General Functions
	/// <summary>
	/// Checks if the given position intersects with any geometry that is considered "solid"
	/// </summary>
	public static bool IsInsideGeometry(Vector2 _position)
	{
		Vector2[] _list =
			[
				new(-20, -20), new(0, -20), new(20, -20),
				new(-20, 0), /* Center */ new(20, 0),
				new(-20, 20), new(0, 20), new(20, 20)
			];
		int limit = 7;

		for (int i = 0; i < _list.Length; i++)
		{
			if (limit <= 0)
				return true;

			var _hit = GlobalManager.Utils.RaycastBetween(_position,
				_position + _list[i], [Player.Instance.GetRid()]);

			string _collision = _hit.Count > 0 ? _hit["collider"].ToString() : null;

			if (_collision == null || (!_collision.Contains("ground") && !_collision.Contains("wall")))
				continue;

			limit--;
		}
		return false;
	}
	/// <summary>
	/// Checks if the given position is out of level bounds (anything not within camera view)
	/// </summary>
	public static bool IsOutOfBounds(Vector2 _position)
	{
		float _x = _position.X, _y = _position.Y,
				_xtp = RoomInstance.Instance.TopLeft.GlobalPosition.X, _ytp = RoomInstance.Instance.TopLeft.GlobalPosition.Y,
				_xbr = RoomInstance.Instance.BottomRight.GlobalPosition.X, _ybr = RoomInstance.Instance.BottomRight.GlobalPosition.Y;
		if (_x < _xtp || _x > _xbr ||
				_y < _ytp || _y > _ybr)
			return true;
		return false;
	}

	// MARK: Utils Functions
	/// <summary>
	/// Adds an aphid to the current generation.
	/// </summary>
	/// <param name="_buddy">Aphid Instance to add to the game</param>
	public static void AddAphid(AphidInstance _buddy)
	{
		if (Aphids.ContainsKey(_buddy.GUID))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"GameManager: The aphid '{_buddy.Genes.Name}'<{_buddy.GUID}> was already present in the list.");
			return;
		}
		Aphids.Add(_buddy.GUID, _buddy);
	}
	/// <summary>
	/// Adds an aphid to the generational archive, normally done when an aphid dies.
	/// </summary>
	/// <param name="_instance"></param>
	/// <returns></returns>
	public static bool AddToArchive(AphidInstance _instance)
	{
		if (AphidArchive.ContainsKey(new Guid(_instance.ID)))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"GameManager: <{_instance.ID}> already exists in archive. Name: {_instance.Genes.Name}.>");
			return false;
		}
		AphidArchive.Add(new(_instance.ID), _instance.Genes);
		return true;
	}
	/// <summary>
	/// Removes an aphid from the current generation. It does NOT add them to the generations archive, this must be done manually.
	/// </summary>
	/// <param name="_guid">The key of the aphid to remove.</param>
	public static void RemoveAphid(Guid _guid)
	{
		if (!Aphids.TryGetValue(_guid, out AphidInstance value))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, $"GameManager: Cannot delete <{_guid}> as it does not exist.");
			return;
		}
		if (IsInstanceValid(ResortManager.Current))
			ResortManager.Current.Aphids.Remove(value.Entity);
		Aphids.Remove(_guid);
	}
	public static GlobalUpgrades.UpgradeModule GetUpgrade(string _id)
	{
		if (!HasUpgrade(_id))
			return new(0);
		return Upgrades[_id];
	}
	public static bool HasUpgrade(string _id)
	{
		return Upgrades.ContainsKey(_id);
	}

	// Backwards Compability related
	internal static bool APPLY_OUTOFBOUND_PATCH = false;
	[GeneratedRegex("(?<=\"Father\":\")([^\"]+)")]
	private static partial Regex FATHER_RECOVERY();
	[GeneratedRegex("(?<=\"Mother\":\")([^\"]+)")]
	private static partial Regex MOTHER_RECOVERY();
}
