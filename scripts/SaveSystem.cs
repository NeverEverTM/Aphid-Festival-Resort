using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public static class SaveSystem
{
	/// <summary>
	/// The current profile loaded.
	/// </summary> 
	public static string Profile { get; private set; }
	/// <summary>
	/// Non-global path to the current profile directory.
	/// </summary>
	public static string ProfilePath { get; private set; }

	public const string
	USERROOT_DIR = "user://",
	PROFILES_DIR = "user://profiles",
	CONFIG_DIR = "config",
	PROFILEBACKUP_DIR = "/backup",
	DEFAULT_PROFILE = "default",
	ProfileAlbumDir = "/screenshots/",
	PROFILEAPHIDS_DIR = "/aphids/",
	PROFILERESORTS_DIR = "resorts",
	CONFIGFILE_EXTENSION = ".cfg",
	JSONFILE_EXTENSION = ".json",
	SAVEFILE_EXTENSION = ".data";

	public static readonly List<SaveMetadata> ProfileClassData = [];

	public static void CreateBaseDirectories()
	{
		DirAccess.MakeDirAbsolute(PROFILES_DIR);
		DirAccess.MakeDirAbsolute(System.IO.Path.Combine(USERROOT_DIR + CONFIG_DIR));
	}

	// ==========| Resort Saving Methods |============
	public static async Task SaveProfile()
	{
		string _backupFolder = ProfilePath + PROFILEBACKUP_DIR;

		// Save Aphid Data
		await SaveAllAphids(_backupFolder, false);
		await SaveAllAphids(ProfilePath);

		// Save serialized classes
		for (int i = 0; i < ProfileClassData.Count; i++)
			await SaveClassData(ProfileClassData[i]);

		Logger.Print(Logger.LogPriority.Log, $"ProfileSave: Saved profile <{Profile}> to <{ProfilePath}>.");
	}
	private static Task SaveClassData(SaveMetadata _class)
	{
		try
		{
			_class.RootPath = ProfilePath;
			_class.CallSave();
			_class.RootPath += PROFILEBACKUP_DIR;
			_class.CallSave();
		}
		catch (Exception _e)
		{
			Logger.Print(Logger.LogPriority.Error, $"ProfileSave: Error on saving class <{_class.ID}>." + _e);
		}
		return Task.CompletedTask;
	}

	private static Task SaveAllAphids(string _path, bool _logSave = true)
	{
		try
		{
			using var _stream = FileAccess.Open($"{_path + PROFILEAPHIDS_DIR}/aphids.json", FileAccess.ModeFlags.Write);

			foreach (KeyValuePair<Guid, AphidInstance> _pair in GameManager.Aphids)
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
			Logger.Print(Logger.LogPriority.Error, "ProfileLoad: Failed to load aphids. " + _err);
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
			Logger.Print(Logger.LogPriority.Error, $"SaveAphid: Error on saving aphid <{_data?.ID}>:\n", _err);
			return false;
		}
		return true;
	}

	// ==========| Resort Loading Methods |===========
	public static async Task LoadProfile()
	{
		string _backupFolder = ProfilePath + PROFILEBACKUP_DIR;
		List<SaveMetadata> _profileList = [.. ProfileClassData.OrderByDescending(_profile => _profile.LoadOrderPriority)];
		GameManager.Aphids.Clear();

		// Load all save data classes
		for (int i = 0; i < _profileList.Count; i++)
			LoadClassData(_profileList[i]);

		// Load Aphid Data
		try { await LoadAllAphids(ProfilePath); }
		catch (Exception _e)
		{
			try
			{
				await LoadAllAphids(_backupFolder);
				Logger.Print(Logger.LogPriority.Error, _e);
			}
			catch (Exception _OH_FUCK)
			{
				Logger.Print(Logger.LogPriority.Error, Logger.GameTermination.Major, _OH_FUCK);
			}
		}

		Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: Loaded profile <{Profile}> to memory.");
	}
	private static bool LoadClassData(SaveMetadata _class)
	{
		try
		{
			_class.RootPath = ProfilePath;
			_class.CallLoad();
			return true;
		}
		catch (Exception _e)
		{
			Logger.Print(Logger.LogPriority.Warning, $"ProfileLoad: {_class.ID} was not able to be loaded." + _e);
			try
			{
				_class.RootPath = System.IO.Path.Join(ProfilePath, "backup");
				_class.CallLoad();
				return true;
			}
			catch (Exception _e_backup)
			{
				Logger.Print(Logger.LogPriority.Error,
				$"[CRITICAL] ProfileLoad: Backup for {_class.ID} was not able to be loaded." + _e_backup);
			}
			return false; // Something went REALLY bad
		}
	}

	private static Task LoadAllAphids(string _path)
	{
		using var _stream = FileAccess.Open(_path + PROFILEAPHIDS_DIR + "aphids" + JSONFILE_EXTENSION, FileAccess.ModeFlags.Read);

		while (_stream?.GetPosition() < _stream?.GetLength())
		{
			if (!LoadAphid(_stream, out AphidInstance _instance))
				continue;

			GameManager.AddAphid(_instance);
			ResortManager.SpawnAphid(_instance);
			Logger.Print(Logger.LogPriority.Log, $"Succesfully loaded aphid. ({_instance.ID})");
		}

		return Task.CompletedTask;
	}
	private static bool LoadAphid(FileAccess _stream, out AphidInstance _instance)
	{
		string _id = _stream.GetPascalString();
		try
		{
			string _status = _stream.GetPascalString(), _genes = _stream.GetPascalString();

			// TODO: create aphid patcher
			if (GameManager.ProfileData.Version < 210)
			{
				_status = _status.Replace("MilkBuildup", "HarvestBuildup");

				_genes = _genes.Replace("\"Skills\":[", "\"Skills\":{");
				_genes = _genes.Replace("\"Level\":0}],", "\"Level\":0}},");

				_genes = _genes.Replace("{\"Name\":\"stamina\"", "\"stamina\":{\"Name\":\"stamina\"");
				_genes = _genes.Replace("{\"Name\":\"strength\"", "\"strength\":{\"Name\":\"strength\"");
				_genes = _genes.Replace("{\"Name\":\"intelligence\"", "\"intelligence\":{\"Name\":\"intelligence\"");
				_genes = _genes.Replace("{\"Name\":\"speed\"", "\"speed\":{\"Name\":\"speed\"");
			}

            _instance = new(Guid.Parse(_id))
            {
                Status = JsonSerializer.Deserialize<AphidData.Status>(_status),
                Genes = JsonSerializer.Deserialize<AphidData.Genes>(_genes)
            };
        }
		catch (Exception _err)
		{
			Logger.Print(Logger.LogPriority.Error, Logger.GameTermination.Major, $"Failed to load aphid {_id}", _err);
			_instance = null;
			return false;
		}
		return true;
	}

	// ==========| Profile Managment Methods |==========
	// Used for new games to set default vaules to all serializeables
	public static Task SetProfileData()
	{
		// Load all save data classes
		for (int i = 0; i < ProfileClassData.Count; i++)
		{
			ProfileClassData[i].RootPath = ProfilePath;
			ProfileClassData[i].CallSet();
		}
		return Task.CompletedTask;
	}
	public static void SelectProfile(string _profile = DEFAULT_PROFILE)
	{
		ProfilePath = $"{PROFILES_DIR}/{_profile}";
		Profile = _profile;
	}

	public static async Task CreateProfile()
	{
		// Create directories for current profile
		await CreateProfileDir(ProfilePath);
		await CreateProfileDir(ProfilePath + PROFILEBACKUP_DIR);

		Logger.Print(Logger.LogPriority.Info, $"ProfileCreate: Succesfully created profile of <{Profile}>.");
	}
	private static Task CreateProfileDir(string _path)
	{
		DirAccess.MakeDirAbsolute(_path);
		var _dir = DirAccess.Open(_path);

		if (_dir == null)
		{
			Logger.Print(Logger.LogPriority.Error, "SaveSystem: DirAccess error on opening directory:\n", DirAccess.GetOpenError());
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
			Logger.Print(Logger.LogPriority.Error, $"ProfileDelete: Cannot delete file in path: {_path}");
			return Task.CompletedTask;
		}

		// Scary!
		System.IO.Directory.Delete(_path, true); // DirAccess.Remove does not have an option to be recursive
		Logger.Print(Logger.LogPriority.Info, $"ProfileDelete: Succesfully deleted profile <{_profile}>.");
		return Task.CompletedTask;
	}
	public static Task FlushProfile()
	{
		ProfileClassData.Clear();
		GameManager.Aphids.Clear();
		return Task.CompletedTask;
	}

	// ==================================================================
	// MARK: Interfaces And Bases
	public abstract class SaveMetadata(string ID, int LoadPriority = 0) : IGlobalSaveCall
	{
		public readonly string ID = ID;
		internal int LoadOrderPriority = LoadPriority; // Higher numbers means higher priority.
		public string RootPath = USERROOT_DIR;
		public string RelativePath = string.Empty;
		public string Extension = JSONFILE_EXTENSION;
		public uint GameVersion { get; protected set;} = GlobalManager.GAME_VERSION;

		/// <summary>
		/// Choose to either save the content as plain text or with a bit of encoding.
		/// Does not make it more secure but it does slightly detract the user from modyfing it.
		/// </summary>
		protected enum SaveMode { PlainText, Obfuscated }
		protected SaveMode Mode = SaveMode.PlainText;

        /// <summary>
        /// Returns the path to the saved contents.
        /// </summary>
        /// <param name="_global">Return this path as a global OS file path instead of a Godot file path?</param>
        public string GetPath(bool _global = false) => !_global ?
				System.IO.Path.Join(RootPath, RelativePath, ID + Extension)
				: ProjectSettings.GlobalizePath(System.IO.Path.Join(RootPath, RelativePath, ID + Extension));

		public abstract void CallSave();
		public abstract void CallLoad();
		public abstract void CallSet();
	}
	/// <summary>
	/// Used tp handle data manipulation separately from the SaveModule.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IDataModule<T>
	{
		public void Set(T _data);
		public T Get();
		public T Default();
	}
	/// <summary>
	/// Used as a compability layer to globally call save methods.
	/// </summary>/
	protected interface IGlobalSaveCall
	{
		public void CallSave();
		public void CallLoad();
	}
	// ==================================================================
	// MARK: SaveData Module
	/// <summary>
	/// Core component to save runtime data to system via Json serialization.
	/// This class is NOT meant to be the data holder. Instead, it requires the class type of the data to serialize.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SaveModule<T> : SaveMetadata
	{
		public JsonSerializerOptions JsonOptions = null;

		public SaveModule(string ID, IDataModule<T> _module, int LoadPriority = 0) : base(ID, LoadPriority)
		{
			Data = _module;
			Data.Set(Data.Default());
		}

		protected IDataModule<T> Data;

		public virtual Task Save()
		{
			string _path = GetPath();
			using var _file = FileAccess.Open(_path, FileAccess.ModeFlags.Write);

			// Store Data
			if (Mode == SaveMode.PlainText)
				_file.StorePascalString(JsonSerializer.Serialize(Data.Get(), JsonOptions));
			else
				_file.StoreVar(JsonSerializer.Serialize(Data.Get(), JsonOptions));

			// Save most recent game version this file was saved in
			_file.Store32(GlobalManager.GAME_VERSION);

			Logger.Print(Logger.LogPriority.Log, "ProfileSave: Saved succesfully - path: " + _path);
			return Task.CompletedTask;
		}
		public virtual T Load(bool loadToClass = true)
		{
			string _path = PreLoad();
			T _data = Data.Default();

			if (FileAccess.FileExists(_path))
			{
				using var _file = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
				// get data from stream
				string _raw_data = Mode == SaveMode.PlainText ?
					_raw_data = _file.GetPascalString() :
					_raw_data = _file.GetVar().ToString();

				// load game version
				try
				{
					GameVersion = _file.Get32();
				}
				catch (Exception _error)
				{
					GameVersion = GlobalManager.GAME_VERSION;
					Logger.Print(Logger.LogPriority.Error,"ProfileLoad: Unable to load game version", _error);
				}

				_data = PostLoad(_raw_data);
			}
			else
				Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: {ID} was not found. Creating new instance. Path: " + RootPath + RelativePath + ID + Extension);

			if (loadToClass)
				Data.Set(_data);

			Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: Loaded succesfully(toClass={loadToClass}) - LP: "
				+ LoadOrderPriority + " path: " + _path);
			return _data;
		}

		/// <summary>
		/// Method that deserializes raw data back into runtime data.
		/// </summary>
		public virtual T PostLoad(string _raw_data)
		{
			return JsonSerializer.Deserialize<T>(_raw_data);
		}
		public virtual string PreLoad()
		{
			string _path = GetPath(), _path_old = _path.Replace(".data", "_data.json");
			
			// patch for 0.1.3v savefiles, renames file with "_data" and replace it with ".data"
			if (!FileAccess.FileExists(_path) && FileAccess.FileExists(_path_old))
				System.IO.File.Move(ProjectSettings.GlobalizePath(_path_old), ProjectSettings.GlobalizePath(_path));
			
			return _path;
		}

		public override void CallSave() => Save();
		public override void CallLoad() => Load();
		public override void CallSet() => Data.Set(Data.Default());
	}

	/// <summary>
	/// Similar to SaveData<T> but it can store Godot's Variants as intended.
	/// For example, it can store InputEvents and load them back without issue.
	/// Requires more setup to translate generic T type back into a Variant.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SaveModuleGD : SaveMetadata
	{
		public SaveModuleGD(string ID, IDataModule<Variant> _module, int LoadPriority = 0) : base(ID, LoadPriority)
		{
			Data = _module;
			Data.Set(Data.Default());
		}

		protected IDataModule<Variant> Data;

		public virtual Task Save()
		{
			string _path = GetPath();
			using var _file = FileAccess.Open(_path, FileAccess.ModeFlags.Write);

			// Store Data
			if (Mode == SaveMode.PlainText)
				_file.StorePascalString(Json.Stringify(Data.Get()));
			else
				_file.StoreVar(Data.Get());

			// Save most recent game version this file was saved in
			_file.Store32(GlobalManager.GAME_VERSION);

			Logger.Print(Logger.LogPriority.Log, "ProfileSave: Saved succesfully. path: " + _path);
			return Task.CompletedTask;
		}
		public virtual Variant Load(bool loadToClass = true)
		{
			string _path = PreLoad();
			Variant _data = Data.Default();

			if (FileAccess.FileExists(_path))
			{
				using var _file = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
				Variant _raw_data = string.Empty;

				// Load either the plain text or the encoded var data
				if (Mode == SaveMode.PlainText)
					_raw_data = _file.GetPascalString();
				else
					_raw_data = _file.GetVar();

				// load game version
				try
				{
					GameVersion = _file.Get32();
				}
				catch (Exception _error)
				{
					GameVersion = GlobalManager.GAME_VERSION;
					Logger.Print(Logger.LogPriority.Error,"ProfileLoad: Unable to load game version", _error);
				}

				_data = PostLoad(_raw_data);
			}
			else
				Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: {ID} was not found. Creating new instance. Path: " + _path);

			if (loadToClass)
				Data.Set(_data);

			Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: Loaded succesfully(toClass={loadToClass}) - LP: "
					+ LoadOrderPriority + " path: " + _path);
			return _data;
		}
		/// <summary>
		/// Method that deserializes raw data back into runtime data.
		/// </summary>
		public virtual Variant PostLoad(Variant _raw_data)
		{
			if (Mode == SaveMode.PlainText)
				return Json.ParseString(_raw_data.AsString());
			else
				return _raw_data;
		}
		public virtual string PreLoad()
		{
			return GetPath();
		}

		public override void CallSave() => Save();
		public override void CallLoad() => Load();
		public override void CallSet() => Data.Set(Data.Default());
	}
}