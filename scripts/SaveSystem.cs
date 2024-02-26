using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public static class SaveSystem
{
	private static readonly Dictionary<Guid, AphidInstance> aphids = new();
	public static string Profile { get; private set; }
	public static string CurrentProfilePath { get; private set; }

	public delegate void SavingEventHandler();
	public static event SavingEventHandler OnSave;
	public delegate void LoadingEventHandler();
	public static event LoadingEventHandler OnLoad;

	public const string
	ProfilesDirectory = "user://profiles",
	GlobalDataPath = "user://global.save",
	defaultProfile = "default",
	backupsProfile = "/[backup]",
	aphidsGUID = "/aphids.guid",
	playerData = "/player.data",
	aphidsFolder = "/aphids",
	resortFolder = "/resort"; 

	// General file managment
	public static void CreateBaseDirectories()
	{
		DirAccess.MakeDirAbsolute(ProfilesDirectory);
		DirAccess.MakeDirAbsolute(ProfilesDirectory + backupsProfile);
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
		string _backupFolder = ProfilesDirectory + backupsProfile + $"/{Profile}";
		
		// Player Data
		await SavePlayer(_backupFolder);
		await SavePlayer(CurrentProfilePath);

		// Save All Resort Data
		await SaveAllAphids(_backupFolder, false);
		await SaveAllAphids(CurrentProfilePath);

		OnSave?.Invoke();
		GD.Print($"ProfileSave: Saved profile <{Profile}> to memory.");
	}
	
	private static Task SavePlayer(string _path)
	{
		var _jsonPlayer = JsonSerializer.Serialize(Player.Data);
		var _file = FileAccess.Open(_path + playerData, FileAccess.ModeFlags.Write);
		_file.StorePascalString(_jsonPlayer);
		return Task.CompletedTask;
	}
	
	private static Task SaveAllAphids(string _path, bool _logSave = true)
	{
		try 
		{ 
			foreach(KeyValuePair<Guid, AphidInstance> _pair in aphids)
			{
				var _guid = _pair.Key.ToString();
				if (!SaveAphid($"{_path + aphidsFolder}/{_guid}", _pair.Value))
					continue;
				else if (_logSave)
					GD.Print($"Succesfully saved aphid. ID: {_guid}.");
			}
		}
		catch(Exception _err)
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
		// Player Data
		await LoadPlayer();

		// Load Resort Data
		aphids.Clear();
		await LoadAllAphids();

		OnLoad?.Invoke();
		GD.Print($"Loaded profile <{Profile}> to memory.");
	}
	
	private static Task LoadPlayer()
	{
		try
		{
			var _file = FileAccess.Open(CurrentProfilePath + playerData, FileAccess.ModeFlags.Read);
			var _data = _file.GetPascalString();
			Player.Data = JsonSerializer.Deserialize<Player.SaveData>(_data);
		}
		catch(Exception _err)
		{
			GD.PrintErr(_err);
		}
		return Task.CompletedTask;
	} 
	
	private static Task LoadAllAphids()
	{
		var _aphidFolder = CurrentProfilePath + aphidsFolder;
		var _dir = DirAccess.Open(_aphidFolder);
		var _files = _dir.GetFiles();
		try 
		{ 
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
		} 
		catch(Exception _err)
		{
			GD.Print(_err.Message);
			GD.Print(_err.StackTrace);
			return Task.CompletedTask;
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

	// ==========| Profile Managment Methods |==========
	/// <summary>
	/// Creates a profile based on the current set one.
	/// </summary>
	public static async Task CreateProfile()
	{
		// Create directories for current profile
		string _backupPath = ProfilesDirectory + backupsProfile + $"/{Profile}";

		DirAccess.MakeDirAbsolute(CurrentProfilePath);
		var _dir = DirAccess.Open(CurrentProfilePath);
		_dir.MakeDir("aphids");
		_dir.MakeDir("resort");

		// Backup profile
		DirAccess.MakeDirAbsolute(_backupPath);
		var _backupDir = DirAccess.Open(_backupPath);
		_backupDir.MakeDir("aphids");
		_backupDir.MakeDir("resort");

		await SaveProfile();

		GD.Print($"ProfileCreate: Succesfully created profile of <{Profile}>.");
	}
	/// <summary>
	/// Deletes the given profile from the user data.
	/// </summary>
	/// <param name="_profile"></param>
	public static Task DeleteProfile(string _profile)
	{
		var _path = ProjectSettings.GlobalizePath(CurrentProfilePath);

		if (!_path.Contains(Profile) ||! _path.Contains("profiles"))
		{
			GD.Print($"Cannot delete file in path: {_path}");
			return Task.CompletedTask;
		}

		// Scary!
		System.IO.Directory.Delete(_path, true);
		GD.Print($"ProfileDelete: Succesfully deleted profile <{_profile}>.");
		return Task.CompletedTask;
	}
	/// <summary>
	/// Sets current profile as given one, also set its folder path.
	/// </summary>
	/// <param name="_profile"></param>
	public static void SetProfile(string _profile = defaultProfile)
	{
		CurrentProfilePath = $"{ProfilesDirectory}/{_profile}";
		Profile = _profile;
	}
	public static async void ResetProfile(string _profile = defaultProfile)
	{
		await DeleteProfile(_profile);
		await CreateProfile();
	}
}
