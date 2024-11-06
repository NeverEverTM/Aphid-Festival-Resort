using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public static class SaveSystem
{
	public static string Profile { get; private set; }
	public static string ProfilePath { get; private set; }

	public const string
	ProfilesDir = "user://profiles",
	ConfigurationDir = "user://config/",
	DEFAULT_PROFILE = "default",
	ProfileBackupDir = "/backup",
	ProfileAlbumDir = "/screenshots/",
	ProfileAphidDataDir = "/aphids/",
	ProfileResortsDir = "/resorts/",
	jsonExtension = ".json";

	public static readonly Dictionary<Guid, AphidInstance> Aphids = new();
	public static readonly List<ISaveData> ProfileData = new();

	public static void CreateBaseDirectories()
	{
		DirAccess.MakeDirAbsolute(ProfilesDir);
		DirAccess.MakeDirAbsolute(ConfigurationDir);
	}
	/// <summary>
	/// Allows a class to store and load serializeable data as part of the current profile.
	/// </summary>
	public static void AddToProfileData(ISaveData _data)
	{
		ProfileData.Add(_data);
		Logger.Print(Logger.LogPriority.Log, $"ProfileData: <{_data.GetId()}> has been set as serializeable profile data.");
	}

	// Aphid related methods
	public static Guid AddAphidInstance(AphidInstance _buddy)
	{
		Guid _guid = Guid.NewGuid();
		_buddy.ID = _guid.ToString();
		Aphids.Add(_guid, _buddy);
		return _guid;
	}
	public static void RemoveAphidInstance(Guid _guid)
	{
		if (!Aphids.ContainsKey(_guid))
		{
			GD.PrintErr("SaveSystem: (RemoveAphidInstance)", _guid, " does not exist.");
			return;
		}
		Aphids.Remove(_guid);
	}
	public static AphidInstance GetAphidInstance(Guid _guid)
	{
		if (Aphids.ContainsKey(_guid))
			return Aphids[_guid];

		return null;
	}
	public static void ReplaceAphidInstance(Guid _guid, AphidInstance _buddy) =>
		Aphids[_guid] = _buddy;

	// ==========| Resort Saving Methods |============
	public static async Task SaveProfile()
	{
		string _backupFolder = ProfilePath + ProfileBackupDir;

		// save game data
		await SaveClassData(GameData.Instance);

		// Save Aphid Data
		await SaveAllAphids(_backupFolder, false);
		await SaveAllAphids(ProfilePath);

		// Save serialized classes
		for (int i = 0; i < ProfileData.Count; i++)
			await SaveClassData(ProfileData[i]);

		Logger.Print(Logger.LogPriority.Log, $"ProfileSave: Saved profile <{Profile}> to <{ProfilePath}>.");
	}
	private static Task SaveClassData(ISaveData _class)
	{
		try
		{
			string _json = _class.SaveData();
			// Save current backup version
			using var _backup = FileAccess.Open(ProfilePath + ProfileBackupDir + _class.GetDataPath() + _class.GetId() + jsonExtension, FileAccess.ModeFlags.Write);
			_backup.StorePascalString(_json);
			// Save current profile version
			using var _profile = FileAccess.Open(ProfilePath + _class.GetDataPath() + _class.GetId() + jsonExtension, FileAccess.ModeFlags.Write);
			_profile.StorePascalString(_json);
		}
		catch (Exception _e)
		{
			Logger.Print(Logger.LogPriority.Error, $"ProfileSave: Error on saving class <{_class.GetId()}>." + _e);
		}
		return Task.CompletedTask;
	}

	private static Task SaveAllAphids(string _path, bool _logSave = true)
	{
		try
		{
			using var _stream = FileAccess.Open($"{_path + ProfileAphidDataDir}/aphids.json", FileAccess.ModeFlags.Write);

			foreach (KeyValuePair<Guid, AphidInstance> _pair in Aphids)
			{
				var _guid = _pair.Key.ToString();
				if (!SaveAphid(_stream, _pair.Value))
					continue;
				else if (_logSave)
					Logger.Print(Logger.LogPriority.Log, $"Succesfully saved aphid. ID: {_guid}.");
			}

			_stream.Close();
		}
		catch (Exception _err)
		{
			GD.Print(_err.Message);
			GD.Print(_err.StackTrace);
			return Task.CompletedTask;
		}
		return Task.CompletedTask;
	}
	private static bool SaveAphid(FileAccess _stream, AphidInstance _data)
	{
		try
		{
			// Open the stream and write both the status and genes inside of it
			_stream.StorePascalString(_data.ID);
			_stream.StorePascalString(JsonSerializer.Serialize(_data.Status));
			_stream.StorePascalString(JsonSerializer.Serialize(_data.Genes));
			_stream.Flush();
		}
		catch (Exception _err)
		{
			GD.Print("SaveAphid: Error");
			GD.PrintErr(_err.Message);
			GD.PrintErr(_err.StackTrace);
			return false;
		}
		return true;
	}

	// ==========| Resort Loading Methods |===========
	public static async Task LoadProfile()
	{
		string _backupFolder = ProfilePath + ProfileBackupDir;
		List<ISaveData> _profileList = ProfileData.OrderByDescending(_profile => _profile.LoadOrderPriority).ToList();
		Aphids.Clear();

		// load GameData first and check for compability problems
		await LoadClassData(GameData.Instance);
		if (BackwardsCompatibility.CheckVersionCompatibility().Result)
			return;

		// Load Aphid Data
		try { await LoadAllAphids(ProfilePath); }
		catch (Exception _e)
		{
			await LoadAllAphids(_backupFolder);
			Logger.Print(Logger.LogPriority.Error, Logger.GameTermination.Major, _e);
		}

		// Load all save data classes
		for (int i = 0; i < _profileList.Count; i++)
			await LoadClassData(_profileList[i]);

		Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: Loaded profile <{Profile}> to memory.");
	}
	private static async Task<bool> LoadClassData(ISaveData _class)
	{
		string _relativePath = _class.GetDataPath() + _class.GetId() + jsonExtension;
		try
		{
			using var _stream = FileAccess.Open(ProfilePath + _relativePath, FileAccess.ModeFlags.Read);
			await _class.LoadData(_stream.GetPascalString());
			return true;
		}
		catch (Exception _e)
		{
			Logger.Print(Logger.LogPriority.Warning, $"ProfileLoad: {_class.GetId()} was not able to be loaded." + _e);
			try
			{
				using var _stream_backup = FileAccess.Open(ProfilePath + ProfileBackupDir + _relativePath, FileAccess.ModeFlags.Read);
				await _class.LoadData(_stream_backup.GetPascalString());
				return true;
			}
			catch (Exception _e_backup)
			{
				Logger.Print(Logger.LogPriority.Error,
				$"[CRITICAL] ProfileLoad: Backup for {_class.GetId()} was not able to be loaded." + _e_backup);
				await _class.SetData();
			}
			return false; // Something went REALLY bad
		}
	}

	private static Task LoadAllAphids(string _path)
	{
		using var _stream = FileAccess.Open(_path + ProfileAphidDataDir + "aphids.json", FileAccess.ModeFlags.Read);

		while (_stream.GetPosition() < _stream.GetLength())
		{
			var _instance = new AphidInstance();
			if (!LoadAphid(_stream, ref _instance))
				continue;

			Aphids.Add(new Guid(_instance.ID), _instance);
			ResortManager.CreateAphid(_instance);
			Logger.Print(Logger.LogPriority.Log, $"Succesfully loaded aphid. ({_instance.ID})");
		}

		return Task.CompletedTask;
	}
	private static bool LoadAphid(FileAccess _stream, ref AphidInstance _instance)
	{
		try
		{
			_instance.ID = _stream.GetPascalString();
			_instance.Status = JsonSerializer.Deserialize<AphidData.Status>(_stream.GetPascalString());
			_instance.Genes = JsonSerializer.Deserialize<AphidData.Genes>(_stream.GetPascalString());
		}
		catch (Exception _err)
		{
			Logger.Print(Logger.LogPriority.Error, Logger.GameTermination.Major, $"Failed to load aphid {_instance?.ID}", _err);
			return false;
		}
		return true;
	}

	// ==========| Profile Managment Methods |==========
	// Used for new games to set default vaules to all serializeables
	public static async Task SetProfileData()
	{
		// Load all save data classes
		for (int i = 0; i < ProfileData.Count; i++)
		{
			try
			{
				await ProfileData[i].SetData();
			}
			catch (Exception _err)
			{
				GD.PrintErr(_err);
			}
		}
	}
	public static void SelectProfile(string _profile = DEFAULT_PROFILE)
	{
		ProfilePath = $"{ProfilesDir}/{_profile}";
		Profile = _profile;
	}

	public static async Task CreateProfile()
	{
		// Create directories for current profile
		string _backupPath = ProfilePath + ProfileBackupDir;
		await CreateNewProfile(ProfilePath);
		await CreateNewProfile(_backupPath);

		GD.Print($"ProfileCreate: Succesfully created profile of <{Profile}>.");
	}
	private static Task CreateNewProfile(string _path)
	{
		DirAccess.MakeDirAbsolute(_path);
		var _dir = DirAccess.Open(_path);

		if (_dir == null)
		{
			GD.PrintErr(DirAccess.GetOpenError());
			return Task.CompletedTask;
		}

		_dir.MakeDir("aphids");
		_dir.MakeDir("resorts");
		_dir.MakeDir("screenshots");
		return Task.CompletedTask;
	}

	public static Task DeleteProfile(string _profile)
	{
		var _path = ProjectSettings.GlobalizePath(ProfilePath);

		if (string.IsNullOrEmpty(_profile))
			return Task.CompletedTask;
		if (!_path.Contains(Profile) || !_path.Contains("profiles"))
		{
			GD.Print($"Cannot delete file in path: {_path}");
			return Task.CompletedTask;
		}

		// Scary!
		System.IO.Directory.Delete(_path, true);
		GD.Print($"ProfileDelete: Succesfully deleted profile <{_profile}>.");
		return Task.CompletedTask;
	}
	public static Task FlushProfile()
	{
		ProfileData.Clear();
		Aphids.Clear();
		return Task.CompletedTask;
	}

	public interface ISaveData
	{
		/// <summary>
		/// The load order priority of this class, it goes from highest to lowest, so 0 > 100. 
		public int LoadOrderPriority { get => 0; }
		/// <summary>
		/// Gets the public ISaveData ID of this class.
		/// </summary>
		public string GetId();
		/// <summary>
		/// Gets the path to data location (Defaults to root of its relative folder)
		/// Global data is stored directly in the game's directory and Profile data inside their respective folder. 
		/// </summary>
		public string GetDataPath()
		{
			return "/";
		}
		/// <summary>
		/// Attempts to save data of the game before serializing the data class.
		/// Returns the class to serialize.
		/// </summary>
		public string SaveData();
		/// <summary>
		/// Attempts to use the loaded data to set itself up.
		/// </summary>
		public Task LoadData(string _json);
		/// <summary>
		/// Creates a new instance of the data present, this is used to restart back to a default state.
		/// </summary>
		/// <returns></returns>
		public Task SetData();
	}

	public class SaveMetadata
	{
		public readonly string ID;
		public int LoadOrderPriority = 0; // Higher numbers means higher priority.
		public string Extension = ".sav";
		public string RootPath = "user://";
		public string RelativePath = "";
		public SaveMetadata(string ID)
		{
			this.ID = ID;
		}
	}

	public class SaveData<T> : SaveMetadata
	{
		public T Data;
		public JsonSerializerOptions JSONOptions = null;

		public SaveData(string ID) : base(ID) { }

		public virtual Task Save()
		{
			using var _file = FileAccess.Open(
				RootPath + RelativePath + ID + Extension,
				FileAccess.ModeFlags.Write);
			_file.StoreString(JsonSerializer.Serialize(Data, JSONOptions));
			Logger.Print(Logger.LogPriority.Log, "SaveData: Saved succesfully - path: " + RootPath + RelativePath + ID + Extension);
			return Task.CompletedTask;
		}
		public virtual T Load(bool loadToClass = true)
		{
			string _path = RootPath + RelativePath + ID + Extension;
			T _data = GetDefault();

			if (FileAccess.FileExists(_path))
			{
				using var _file = FileAccess.Open(_path,
					FileAccess.ModeFlags.Read);
				_data = JsonSerializer.Deserialize<T>(_file.GetAsText());
			}

			if (loadToClass)
				Data = _data;

			Logger.Print(Logger.LogPriority.Log, $"SaveData: Loaded succesfully(toClass={loadToClass}) - LP: "
				+ LoadOrderPriority + " path: " + RootPath + RelativePath + ID + Extension);
			return _data;
		}
		public virtual T GetDefault()
		{
			return default;
		}
	}
	public abstract class SaveDataGD<T> : SaveMetadata
	{
		public T Data;

		public SaveDataGD(string ID) : base(ID) { }

		public virtual Task Save()
		{
			using var _file = FileAccess.Open(
				RootPath + RelativePath + ID + Extension,
				FileAccess.ModeFlags.Write);
			_file.StoreString(Json.Stringify(GetVariantData()));
			Logger.Print(Logger.LogPriority.Log, "SaveData: Saved succesfully - LP: " + LoadOrderPriority + " path: " + RootPath + RelativePath + ID + Extension);
			return Task.CompletedTask;
		}
		public virtual Variant Load(bool loadToClass = true)
		{
			string _path = RootPath + RelativePath + ID + Extension;
			Variant _data = GetDefault();

			if (FileAccess.FileExists(_path))
			{
				using var _file = FileAccess.Open(_path,
					FileAccess.ModeFlags.Read);
				_data = Json.ParseString(_file.GetAsText());
			}

			if (loadToClass)
				SetVariantData(_data);
			Logger.Print(Logger.LogPriority.Log, $"SaveData: Loaded succesfully(toClass={loadToClass}) - LP: "
			+ LoadOrderPriority + " path: " + RootPath + RelativePath + ID + Extension);
			return _data;
		}
		public abstract Variant GetVariantData();
		public abstract void SetVariantData(Variant Data);
		public virtual Variant GetDefault()
		{
			return default;
		}
	}

	public static class BackwardsCompatibility
	{
		public const string dataFolder = "/data";

		/// <summary>
		/// Checks if certain processes for this savefile need to be done to upgrade it between versions.
		/// </summary>
		/// <returns>A Task with a bool return type, this marks wheter it replaces the current profile load method.</returns>
		public static async Task<bool> CheckVersionCompatibility()
		{
			switch (GameData.Data.Version)
			{
				case 120:
					// Pre-1.2
					if (DirAccess.DirExistsAbsolute(ProfilePath + dataFolder))
					{
						await May2024Fix();
						return true;
					}
					else // Pre 1.3
						DirAccess.MakeDirAbsolute(ProfilePath + ProfileAlbumDir);
					break;
			}
			return false;
		}

		private static async Task May2024Fix()
		{
			// Load data and rename it to "..._Legacy"
			GD.PrintRich("[color=green]Upgrading savefile to Version 120. Critical Error is completely intentional[/color]");
			await LoadProfileData_Legacy();
			string _legacy_path = ProjectSettings.GlobalizePath(ProfilesDir + $"/{Profile}_Legacy");
			System.IO.Directory.Move(ProjectSettings.GlobalizePath(ProfilePath), _legacy_path);
			// Recreate the profile from scrtach and save to it, then delete the legacy save
			await CreateProfile();
			await SaveProfile();

			System.IO.Directory.Delete(_legacy_path, true);
		}
		private static async Task LoadProfileData_Legacy()
		{
			Aphids.Clear();
			string _backupFolder = ProfilePath + ProfileBackupDir;

			// Load Aphid Data
			try { await LoadAllAphids_Legacy(ProfilePath); }
			catch (Exception _e)
			{
				await LoadAllAphids_Legacy(_backupFolder);
				GD.PrintErr(_e);
			}

			// Load all save data classes
			for (int i = 0; i < ProfileData.Count; i++)
			{
				try
				{
					using var _profile = FileAccess.Open(ProfilePath + dataFolder + $"/{ProfileData[i].GetId()}.json", FileAccess.ModeFlags.Read);
					await ProfileData[i].LoadData(_profile.GetPascalString());
				}
				catch (Exception _e)
				{
					GD.PrintErr(_e);
					using var _profile = FileAccess.Open(_backupFolder + dataFolder + $"/{ProfileData[i].GetId()}.json", FileAccess.ModeFlags.Read);
					await ProfileData[i].LoadData(_profile.GetPascalString());
					continue;
				}
			}

			GD.Print($"ProfileLoad: Loaded profile <{Profile}> to memory.");
		}
		private static Task LoadAllAphids_Legacy(string _path)
		{
			var _aphidFolder = _path + ProfileAphidDataDir;
			var _dir = DirAccess.Open(_aphidFolder);
			var _files = _dir.GetFiles();

			for (int i = 0; i < _files.Length; i++)
			{
				if (_files[i].EndsWith("aphids.json"))
					continue;
				var _instance = new AphidInstance();
				if (!LoadAphid_Legacy(_aphidFolder + $"/{_files[i]}", ref _instance))
					continue;

				_instance.ID = _files[i];
				Aphids.Add(new Guid(_files[i]), _instance);
				ResortManager.CreateAphid(_instance);
				GD.Print($"Succesfully loaded aphid. ID: {_files[i]}.");
			}

			return Task.CompletedTask;
		}
		private static bool LoadAphid_Legacy(string _path, ref AphidInstance _instance)
		{
			try
			{
				using var _file = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
				_instance.Status = JsonSerializer.Deserialize<AphidData.Status>(_file.GetPascalString());
				_instance.Genes = JsonSerializer.Deserialize<AphidData.Genes>(_file.GetPascalString());
			}
			catch (Exception _err)
			{
				GD.PrintErr(_err.Message);
				GD.PrintErr(_err.StackTrace);
				return false;
			}
			return true;
		}
	}
}