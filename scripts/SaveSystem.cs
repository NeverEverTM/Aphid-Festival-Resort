using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public static class SaveSystem
{
	public static readonly Dictionary<Guid, AphidInstance> AphidsOnResort = new();
	public static string Profile { get; private set; }
	public static string CurrentProfilePath { get; private set; }

	public const string
	ProfilesDirectory = "user://profiles",
	defaultProfile = "default",
	aphidsGUID = "/aphids.guid",
	backupFolder = "/backup",
	aphidsFolder = "/aphids",
	dataFolder = "/data";

	public static readonly List<ISaveData> ProfileData = new();
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
		GD.Print($"ProfileData: <{_data.CLASS_ID}> has been set as serializeable profile data.");
	}
	/// <summary>
	/// Allows a class to store and load serializeable data for use in the application.
	/// </summary>
	public static void AddToGlobalData(ISaveData _data)
	{
		GlobalData.Add(_data);
		GD.Print($"GlobalData: <{_data.CLASS_ID}> has been set as serializeable global data.");
	}

	// Aphid related methods
	public static Guid AddAphidInstance(AphidInstance _buddy)
	{
		Guid _guid = Guid.NewGuid();
		_buddy.ID = _guid.ToString();
		AphidsOnResort.Add(_guid, _buddy);
		return _guid;
	}
	public static void RemoveAphidInstance(Guid _guid) =>
		AphidsOnResort.Remove(_guid);
	public static AphidInstance GetAphidInstance(Guid _guid) =>
		AphidsOnResort[_guid];
	public static void ReplaceAphidInstance(Guid _guid, AphidInstance _buddy) =>
		AphidsOnResort[_guid] = _buddy;

	// Data used for user experience
	public static void SaveGlobalData()
	{
		for (int i = 0; i < GlobalData.Count; i++)
		{
			string _json = GlobalData[i].SaveData();
			using var _profile = FileAccess.Open("user://" + $"{GlobalData[i].CLASS_ID}.json", FileAccess.ModeFlags.Write);
			_profile.StorePascalString(_json);
		}
		GD.Print("GlobalSave: Data has been saved.");
	}
	public static async Task LoadGlobalData()
	{
		for (int i = 0; i < GlobalData.Count; i++)
		{
			string _path = "user://" + $"{GlobalData[i].CLASS_ID}.json";
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

		// Save Aphid Data
		await SaveAllAphids(_backupFolder, false);
		await SaveAllAphids(CurrentProfilePath);

		// Save serialized classes
		for (int i = 0; i < ProfileData.Count; i++)
		{
			string _json = ProfileData[i].SaveData();
			// Save current backup version
			using var _backup = FileAccess.Open(_backupFolder + dataFolder + $"/{ProfileData[i].CLASS_ID}.json", FileAccess.ModeFlags.Write);
			_backup.StorePascalString(_json);
			// Save current profile version
			using var _profile = FileAccess.Open(CurrentProfilePath + dataFolder + $"/{ProfileData[i].CLASS_ID}.json", FileAccess.ModeFlags.Write);
			_profile.StorePascalString(_json);
		}

		GD.Print($"ProfileSave: Saved profile <{Profile}> to <{ProjectSettings.GlobalizePath(CurrentProfilePath)}>.");
	}

	private static Task SaveAllAphids(string _path, bool _logSave = true)
	{
		try
		{
			foreach (KeyValuePair<Guid, AphidInstance> _pair in AphidsOnResort)
			{
				var _guid = _pair.Key.ToString();
				if (!SaveAphid($"{_path + aphidsFolder}/{_guid}", _pair.Value))
					continue;
				else if (_logSave)
					GD.Print($"Succesfully saved aphid. ID: {_guid}.");
			}
		}
		catch (Exception _err)
		{
			GD.Print(_err.Message);
			GD.Print(_err.StackTrace);
			return Task.CompletedTask;
		}
		return Task.CompletedTask;
	}
	private static bool SaveAphid(string _path, AphidInstance _data)
	{
		try
		{
			// Open a new file using the guid as name, write both the status and genes inside of it
			using var _file = FileAccess.Open(_path, FileAccess.ModeFlags.Write);
			string _json = JsonSerializer.Serialize(_data.Status);
			_file.StorePascalString(_json);
			_json = JsonSerializer.Serialize(_data.Genes);
			_file.StorePascalString(_json);
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
		AphidsOnResort.Clear();
		string _backupFolder = CurrentProfilePath + backupFolder;

		// Load Aphid Data
		try { await LoadAllAphids(CurrentProfilePath); }
		catch (Exception _e)
		{
			await LoadAllAphids(_backupFolder);
			GD.PrintErr(_e);
		}

		// Load all save data classes
		for (int i = 0; i < ProfileData.Count; i++)
		{
			try
			{
				using var _profile = FileAccess.Open(CurrentProfilePath + dataFolder + $"/{ProfileData[i].CLASS_ID}.json", FileAccess.ModeFlags.Read);
				await ProfileData[i].LoadData(_profile.GetPascalString());
			}
			catch (Exception _e)
			{
				GD.PrintErr(_e);
				using var _profile = FileAccess.Open(_backupFolder + dataFolder + $"/{ProfileData[i].CLASS_ID}.json", FileAccess.ModeFlags.Read);
				await ProfileData[i].LoadData(_profile.GetPascalString());
				continue;
			}
		}

		GD.Print($"ProfileLoad: Loaded profile <{Profile}> to memory.");
	}

	private static Task LoadAllAphids(string _path)
	{
		var _aphidFolder = _path + aphidsFolder;
		var _dir = DirAccess.Open(_aphidFolder);
		var _files = _dir.GetFiles();

		for (int i = 0; i < _files.Length; i++)
		{
			var _instance = new AphidInstance();
			if (!LoadAphid(_aphidFolder + $"/{_files[i]}", ref _instance))
				continue;

			_instance.ID = _files[i];
			AphidsOnResort.Add(new Guid(_files[i]), _instance);
			ResortManager.CreateAphid(_instance);
			GD.Print($"Succesfully loaded aphid. ID: {_files[i]}.");
		}

		return Task.CompletedTask;
	}
	private static bool LoadAphid(string _path, ref AphidInstance _instance)
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

	public interface ISaveData
	{
		public string CLASS_ID { get; }
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
		_dir.MakeDir("data");
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
}