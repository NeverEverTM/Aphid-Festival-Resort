using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }
	public const string ID = "main";

	internal static bool IsNewGame { get; set; }
	internal static bool APPLY_OUTOFBOUND_PATCH = false;

	public static GameData Data { get; private set; }
	/// <summary>
	/// All current aphids available in this savefile. To access aphids currently loaded in the resort,
	///  go to ResortManager.Current.Aphids instead.
	/// </summary>
	public static Dictionary<Guid, AphidInstance> Aphids { get; private set; } = [];
	public static Dictionary<Guid, AphidData.Genes> AphidArchive { get; set; } = [];

	public static GameSaveModule GameModule { get; private set; }
	public static AphidSaveModule AphidModule { get; private set; }
	public static GenerationsSaveModule GenerationsModule { get; private set; }

	// MARK: SaveModules
	public class GameSaveModule(string ID, SaveSystem.IDataModule<GameData> _module, int LoadPriority = 0) :
		SaveSystem.SaveModule<GameData>(ID, _module, LoadPriority)
	{
		public override GameData PostLoad(string _raw_data)
		{
			if (GameVersion < GlobalManager.GAME_VERSION)
			{
				Logger.Print(Logger.LogPriority.Warning, "GameManager: Applied Out of Bounds Patch ");
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
			Data.LastRoom = SceneManager.Current;
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
			if (GameVersion < 220)
			{
				_raw_data = _raw_data.Replace("MilkBuildup", "HarvestBuildup");

				_raw_data = _raw_data.Replace("\"Skills\":[", "\"Skills\":{");
				_raw_data = _raw_data.Replace("\"Level\":0}],", "\"Level\":0}},");

				_raw_data = _raw_data.Replace("{\"Name\":\"stamina\"", "\"stamina\":{\"Name\":\"stamina\"");
				_raw_data = _raw_data.Replace("{\"Name\":\"strength\"", "\"strength\":{\"Name\":\"strength\"");
				_raw_data = _raw_data.Replace("{\"Name\":\"intelligence\"", "\"intelligence\":{\"Name\":\"intelligence\"");
				_raw_data = _raw_data.Replace("{\"Name\":\"speed\"", "\"speed\":{\"Name\":\"speed\"");
			}
			return base.PostLoad(_raw_data);
		}
	}
	public class AphidDataModule : SaveSystem.IDataModule<Dictionary<Guid, AphidInstance>>
	{
		public Dictionary<Guid, AphidInstance> Default() => [];
		public void Set(Dictionary<Guid, AphidInstance> _data) => Aphids = _data;
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
		GameModule = new(ID, new GameDataModule(), 9999);
		AphidModule = new("aphids", new AphidDataModule(), 3008)
		{
			RelativePath = SaveSystem.PROFILE_APHIDS_DIR
		};
		GenerationsModule = new("generations", new GenerationsDataModule(), 2038)
        {
            Extension = SaveSystem.SAVEFILE_EXTENSION,
            RelativePath = SaveSystem.PROFILE_APHIDS_DIR
        };
		SaveSystem.AddSaveModule(GameModule);
		SaveSystem.AddSaveModule(AphidModule);
		SaveSystem.AddSaveModule(GenerationsModule);
		SceneManager.OnGameInit += StartGame;
	}
	public override async void _Notification(int what)
	{
		// responsible for saving the game when closing the window or exiting the application
		if (what == NotificationWMCloseRequest && GlobalManager.IsInGame)
			await SaveSystem.SaveProfile();
	}

	public static async void StartGame(string _c, bool _s)
	{
		// On New game, put intro cutscene, otherwise just load normally
		if (!IsNewGame)
			CheckForGameOver();
		else
		{
			var _last = CameraManager.Instance.PositionSmoothingSpeed;
			Player.Instance.SetDisabled(true);
			Player.Instance.GlobalPosition = ResortManager.Current.SpawnPoint.GlobalPosition;
			CameraManager.ForceCameraPosition(Player.Instance.GlobalPosition + new Vector2(1000, 0));
			CameraManager.Instance.PositionSmoothingSpeed = 0;

			// we set new game data
			await SaveSystem.SetProfileData();
			PlayerInventory.StoreItem("aphid_egg");
			PlayerInventory.StoreItem("aphid_egg");
			await SaveSystem.SaveProfile();
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
			IsNewGame = false;
		}
		Data.SavefileBoots++;
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
		// TODO: check for dropped items too
		if (Aphids.Count == 0 && Player.Data.Currency + _maxCost < 50)
			GameOver.OhNo();
	}
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
				_xtp = FieldManager.Instance.TopLeft.GlobalPosition.X, _ytp = FieldManager.Instance.TopLeft.GlobalPosition.Y,
				_xbr = FieldManager.Instance.BottomRight.GlobalPosition.X, _ybr = FieldManager.Instance.BottomRight.GlobalPosition.Y;
		if (_x < _xtp || _x > _xbr ||
				_y < _ytp || _y > _ybr)
			return true;
		return false;
	}

	// MARK: Utils Functions
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
	public static bool AddToArchive(AphidInstance _instance)
	{
		if (AphidArchive.ContainsKey(new Guid(_instance.ID)))
		{
			Logger.Print(Logger.LogPriority.Warning, $"GameManager: <{_instance.ID}> already exists in archive. Name: {_instance.Genes.Name}.>");
			return false;
		}
		AphidArchive.Add(new(_instance.ID), _instance.Genes);
		return true;
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
		if (IsInstanceValid(ResortManager.Current))
			ResortManager.Current.Aphids.Remove(value.Entity);
		Aphids.Remove(_guid);
	}
}
