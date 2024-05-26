using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public static class SaveSystem
{
	public static readonly Dictionary<Guid, AphidInstance> Aphids = new();
	public static string Profile { get; private set; }
	public static string CurrentProfilePath { get; private set; }

	public const string
	ProfilesDirectory = "user://profiles",
	defaultProfile = "default",
	backupFolder = "/backup",
	aphidsFolder = "/aphids/",
	resortsFolder = "/resorts/",
	jsonExtension = ".json";

	public static List<ISaveData> ProfileData = new();
	public static readonly List<ISaveData> GlobalData = new();

	public static void CreateBaseDirectories()
	{
		DirAccess.MakeDirAbsolute(ProfilesDirectory);
	}
	/// <summary>
	/// Allows a class to store and load serializeable data as part of the current profile.
	/// </summary>
	public static void AddToProfileData(ISaveData _data)
	{
		ProfileData.Add(_data);
		GD.Print($"ProfileData: <{_data.GetId()}> has been set as serializeable profile data.");
	}
	/// <summary>
	/// Allows a class to store and load serializeable data for use in the application.
	/// </summary>
	public static void AddToGlobalData(ISaveData _data)
	{
		GlobalData.Add(_data);
		GD.Print($"GlobalData: <{_data.GetId()}> has been set as serializeable global data.");
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

	// Data used for user experience
	public static void SaveGlobalData()
	{
		for (int i = 0; i < GlobalData.Count; i++)
		{
			string _json = GlobalData[i].SaveData();
			using var _profile = FileAccess.Open("user://" + $"{GlobalData[i].GetId()}.json", FileAccess.ModeFlags.Write);
			_profile.StorePascalString(_json);
		}
		GD.Print("GlobalSave: Data has been saved.");
	}
	public static async Task LoadGlobalData()
	{
		for (int i = 0; i < GlobalData.Count; i++)
		{
			string _path = "user://" + $"{GlobalData[i].GetId()}.json";
			if (!FileAccess.FileExists(_path))
				continue;
			using var _profile = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
			await GlobalData[i].LoadData(_profile.GetPascalString());
		}
		GD.Print("GlobalLoad: Data has been loaded.");
	}

	// ==========| Resort Saving Methods |============
	public static async Task SaveProfileData()
	{
		string _backupFolder = CurrentProfilePath + backupFolder;

		// save game data
		await SaveClassData(GameData.Instance);

		// Save Aphid Data
		await SaveAllAphids(_backupFolder, false);
		await SaveAllAphids(CurrentProfilePath);

		// Save serialized classes
		for (int i = 0; i < ProfileData.Count; i++)
			await SaveClassData(ProfileData[i]);

		Logger.Print($"ProfileSave: Saved profile <{Profile}> to <{ProjectSettings.GlobalizePath(CurrentProfilePath)}>.", Logger.LogPriority.Log);
	}
	private static Task SaveClassData(ISaveData _class)
	{
		try{
			string _json = _class.SaveData();
			// Save current backup version
			using var _backup = FileAccess.Open(CurrentProfilePath + backupFolder + _class.GetDataPath() + _class.GetId() + jsonExtension, FileAccess.ModeFlags.Write);
			_backup.StorePascalString(_json);
			// Save current profile version
			using var _profile = FileAccess.Open(CurrentProfilePath + _class.GetDataPath() + _class.GetId() + jsonExtension, FileAccess.ModeFlags.Write);
			_profile.StorePascalString(_json);
		}
		catch (Exception _e)
		{
			Logger.Print($"ProfileSave: Error on saving class <{_class.GetId()}>" + _e, Logger.LogPriority.Error);
		}
		return Task.CompletedTask;
	}

	private static Task SaveAllAphids(string _path, bool _logSave = true)
	{
		try
		{
			using var _stream = FileAccess.Open($"{_path + aphidsFolder}/aphids.json", FileAccess.ModeFlags.Write);

			foreach (KeyValuePair<Guid, AphidInstance> _pair in Aphids)
			{
				var _guid = _pair.Key.ToString();
				if (!SaveAphid(_stream, _pair.Value))
					continue;
				else if (_logSave)
					Logger.Print($"Succesfully saved aphid. ID: {_guid}.", Logger.LogPriority.Log);
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
	public static async Task LoadProfileData()
	{
		string _backupFolder = CurrentProfilePath + backupFolder;
		ProfileData = ProfileData.OrderByDescending(_profile => _profile.LoadOrderPriority).ToList();
		Aphids.Clear();

		// load GameData first and check for compability problems
		await LoadClassData(GameData.Instance);
		if (BackwardsCompatibility.CheckVersionCompatibility().Result)
			return;

		// Load Aphid Data
		try { await LoadAllAphids(CurrentProfilePath); }
		catch (Exception _e)
		{
			await LoadAllAphids(_backupFolder);
			GD.PrintErr(_e);
		}

		// Load all save data classes
		for (int i = 0; i < ProfileData.Count; i++)
			await LoadClassData(ProfileData[i]);

		Logger.Print($"ProfileLoad: Loaded profile <{Profile}> to memory.", Logger.LogPriority.Log);
	}
	private static async Task<bool> LoadClassData(ISaveData _class)
	{
		string _relativePath = _class.GetDataPath() + _class.GetId() + jsonExtension;
		try
		{
			using var _stream = FileAccess.Open(CurrentProfilePath + _relativePath, FileAccess.ModeFlags.Read);
			await _class.LoadData(_stream.GetPascalString());
			return true;
		}
		catch (Exception _e)
		{
			Logger.Print($"ProfileLoad: {_class.GetId()} was not able to be loaded." + _e, Logger.LogPriority.Warning);
			try
			{
				using var _stream_backup = FileAccess.Open(CurrentProfilePath + backupFolder + _relativePath, FileAccess.ModeFlags.Read);
				await _class.LoadData(_stream_backup.GetPascalString());
				return true;
			}
			catch (Exception _e_backup)
			{
				Logger.Print($"[CRITICAL] ProfileLoad: Backup for {_class.GetId()} was not able to be loaded." + _e_backup, Logger.LogPriority.Error);
				await _class.SetData();
			}
			return false; // Something went REALLY bad
		}
	}

	private static Task LoadAllAphids(string _path)
	{
		using var _stream = FileAccess.Open(_path + aphidsFolder + "aphids.json", FileAccess.ModeFlags.Read);

		while (_stream.GetPosition() < _stream.GetLength())
		{
			var _instance = new AphidInstance();
			if (!LoadAphid(_stream, ref _instance))
				continue;

			Aphids.Add(new Guid(_instance.ID), _instance);
			ResortManager.CreateAphid(_instance);
			GD.Print($"Succesfully loaded aphid. ID: {_instance.ID}.");
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
			GD.PrintErr(_err.Message);
			GD.PrintErr(_err.StackTrace);
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
	public static void SetProfile(string _profile = defaultProfile)
	{
		CurrentProfilePath = $"{ProfilesDirectory}/{_profile}";
		Profile = _profile;
	}

	public static async Task CreateProfile()
	{
		// Create directories for current profile
		string _backupPath = CurrentProfilePath + backupFolder;
		await CreateNewProfile(CurrentProfilePath);
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
		return Task.CompletedTask;
	}

	public static Task DeleteProfile(string _profile)
	{
		var _path = ProjectSettings.GlobalizePath(CurrentProfilePath);

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
	public static async void ResetProfile(string _profile = defaultProfile)
	{
		await DeleteProfile(_profile);
		await CreateProfile();
	}

	public interface ISaveData
	{
		/// <summary>
		/// The load order priority of this class, it goes from highest to lowest, so 0 > 100. 
		/// </summary>
		public int LoadOrderPriority { get => 0; }
		/// <summary>
		/// Gets the public ISaveData ID of this class.
		/// </summary>
		public string GetId();
		/// <summary>
		/// Gets the path to data location (Defaults to root of its relative folder)
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
		public Task SetData();
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
					if (DirAccess.DirExistsAbsolute(CurrentProfilePath + dataFolder))
					{
						await May2024Fix();
						return true;
					}
					break;
			}
			return false;
		}

		private static async Task May2024Fix()
		{
			// Load data and rename it to "..._Legacy"
			GD.PrintRich("[color=green]Upgrading savefile to Version 120. Critical Error is completely intentional[/color]");
			await LoadProfileData_Legacy();
			string _legacy_path = ProjectSettings.GlobalizePath(ProfilesDirectory + $"/{Profile}_Legacy");
			System.IO.Directory.Move(ProjectSettings.GlobalizePath(CurrentProfilePath), _legacy_path);
			// Recreate the profile from scrtach and save to it, then delete the legacy save
			await CreateProfile();
			await SaveProfileData();

			System.IO.Directory.Delete(_legacy_path, true);
		}
		private static async Task LoadProfileData_Legacy()
		{
			Aphids.Clear();
			string _backupFolder = CurrentProfilePath + backupFolder;

			// Load Aphid Data
			try { await LoadAllAphids_Legacy(CurrentProfilePath); }
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
					using var _profile = FileAccess.Open(CurrentProfilePath + dataFolder + $"/{ProfileData[i].GetId()}.json", FileAccess.ModeFlags.Read);
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
			var _aphidFolder = _path + aphidsFolder;
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