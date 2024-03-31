using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public static class SaveSystem
{
	public static readonly Dictionary<Guid, AphidInstance> aphids = new();
	public static string Profile { get; private set; }
	public static string CurrentProfilePath { get; private set; }

	public const string
	ProfilesDirectory = "user://profiles",
	GlobalDataPath = "user://global.save",
	defaultProfile = "default",
	aphidsGUID = "/aphids.guid",
	backupFolder = "/backup",
	aphidsFolder = "/aphids",
	resortFolder = "/resort";

	public static readonly List<ISaveData> SaveData = new()
	{
		Player.savecontroller
	};

	// General file managment
	public static void CreateBaseDirectories()
	{
		DirAccess.MakeDirAbsolute(ProfilesDirectory);
	}

	// Aphid related methods
	public static Guid AddAphidInstance(AphidInstance _buddy)
	{
		Guid _guid = Guid.NewGuid();
		_buddy.ID = _guid.ToString();
		aphids.Add(_guid, _buddy);
		return _guid;
	}
	public static void RemoveAphidInstance(Guid _guid) =>
		aphids.Remove(_guid);
	public static AphidInstance GetAphidInstance(Guid _guid) =>
		aphids[_guid];
	public static void ReplaceAphidInstance(Guid _guid, AphidInstance _buddy) =>
		aphids[_guid] = _buddy;

	// Global player data
	public static void SaveGlobalData()
	{
		using var _file = FileAccess.Open(GlobalDataPath, FileAccess.ModeFlags.Write);
		_file.StorePascalString(Profile);
	}
	public static void LoadGlobalData()
	{
		using var _file = FileAccess.Open(GlobalDataPath, FileAccess.ModeFlags.Read);
		SetProfile(_file.GetPascalString());
	}

	// ==========| Resort Saving Methods |============
	public static async Task SaveProfile()
	{
		string _backupFolder = CurrentProfilePath + backupFolder;

		// Save Aphid Data
		await SaveAllAphids(_backupFolder, false);
		await SaveAllAphids(CurrentProfilePath);

		// Save globlal classes
		for (int i = 0; i < SaveData.Count; i++)
		{
			// Save current backup version
			try { await SaveData[i].SaveData(_backupFolder); }
			catch (Exception _e)
			{
				GD.PrintErr(_e);
				continue;
			}
			// Save current profile version
			try { await SaveData[i].SaveData(CurrentProfilePath); }
			catch (Exception _e)
			{ GD.PrintErr(_e); }
		}

		GD.Print($"ProfileSave: Saved profile <{Profile}> to <{ProjectSettings.GlobalizePath(CurrentProfilePath)}>.");
	}

	private static Task SaveAllAphids(string _path, bool _logSave = true)
	{
		try
		{
			foreach (KeyValuePair<Guid, AphidInstance> _pair in aphids)
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
	public async static Task LoadProfile()
	{
		aphids.Clear();
		string _backupFolder = CurrentProfilePath + backupFolder;

		// Load Aphid Data
		try { await LoadAllAphids(CurrentProfilePath); }
		catch (Exception _e)
		{
			await LoadAllAphids(_backupFolder); 
			GD.PrintErr(_e);
		}

		// Load all save data classes
		for (int i = 0; i < SaveData.Count; i++)
		{
			try { await SaveData[i].LoadData(CurrentProfilePath); }
			catch (Exception _e)
			{
				GD.PrintErr(_e);
				await SaveData[i].LoadData(_backupFolder);
				continue;
			}
		}

		GD.Print($"Loaded profile <{Profile}> to memory.");
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
			aphids.Add(new Guid(_files[i]), _instance);
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
		public Task SaveData(string _path);
		public Task LoadData(string _path);
	}

	// ==========| Profile Managment Methods |==========
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
		_dir.MakeDir("resort");
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