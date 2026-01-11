using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
	TEMP_CACHE_DIR = "user://_cache/",
	PROFILE_BACKUP_DIR = "/_backup",
	PROFILE_AUTOSAVE_DIR = "/_autosave",
	PROFILE_APHIDS_DIR = "aphids",
	PROFILE_RESORTS_DIR = "resorts",
	PROFILE_ALBUM_DIR = "/screenshots/",
	CONFIG_DIR = "config",
	CONFIGFILE_EXTENSION = ".cfg",
	JSONFILE_EXTENSION = ".json",
	SAVEFILE_EXTENSION = ".data";

	private static readonly List<SaveMetadata> ProfileSaveModules = [];
	public delegate void SaveEvent();
	public static event SaveEvent OnFinish;

	public static void CreateBaseDirectories()
	{
		DirAccess.MakeDirAbsolute(TEMP_CACHE_DIR);
		DirAccess.MakeDirAbsolute(PROFILES_DIR);
		DirAccess.MakeDirAbsolute(System.IO.Path.Combine(USERROOT_DIR + CONFIG_DIR));
	}

	// MARK: Profile Saving

	/// <summary>
	/// Saves all current save modules to memory as the set profile.
	/// </summary>
	/// <param name="_autosave">Autosaves do not overwrite their respectives main savefiles</param>
	/// <param name="_force">Force a saving to happen, only used by some higher authority classes that know saving is safe in the moment.</param>
	/// <returns></returns>
	public static async Task<bool> SaveProfile(bool _autosave = false, bool _force = false)
	{
		if (!_force && GlobalManager.IsBusy)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, "ProfileSave: Cannot save at given time");
			return false;
		}
		GlobalManager.IsBusy = true;
		List<SaveMetadata> _profileList = [.. ProfileSaveModules.OrderByDescending(_profile => _profile.LoadOrderPriority)];

		if (_autosave)
			DebugLogger.Print(DebugLogger.LogPriority.Info, "ProfileSave: AutoSave started.");

		// Save serialized classes
		for (int i = 0; i < _profileList.Count; i++)
        {
            Task _task = SaveClassData(_profileList[i], _autosave); 
			await _task;
			if (_task.IsFaulted || _task.IsCanceled)
            {
				GlobalManager.CreatePopup($"Error for {_profileList[i].ID}", CanvasManager.Instance);
				return false;
            }
        }

		GlobalManager.IsBusy = false;
		DebugLogger.Print(DebugLogger.LogPriority.Log, $"ProfileSave: Saved profile <{Profile}> to <{ProfilePath}>.");
		return true;
	}
	private static Task SaveClassData(SaveMetadata _class, bool _autosave = false)
	{
		try
		{
			if (!_autosave)
			{
				// primary 
				_class.CallSave(ProfilePath + PROFILE_BACKUP_DIR, false);

				// current
				_class.CallSave(ProfilePath);

				// auxiliary 
				_class.CallSave(ProfilePath + PROFILE_AUTOSAVE_DIR, false);
			}
			else
			{
				// auxiliary 
				_class.CallSave(ProfilePath + PROFILE_AUTOSAVE_DIR, false);
			}
		}
		catch (Exception _e)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, $"ProfileSave: Error on saving class <{_class.ID}>." + _e);
			return Task.FromException(_e);
		}
		return Task.CompletedTask;
	}

	// MARK: Profile Loading
	/// <summary>
	/// Loads profile data from memory. Only loads currently queued up savemodules.
	/// </summary>
	/// <returns></returns>
	public static async Task LoadProfile()
	{
		string[] _paths =
		[
			ProfilePath + PROFILE_AUTOSAVE_DIR,
			ProfilePath,
			ProfilePath + PROFILE_BACKUP_DIR
		];
		List<SaveMetadata> _profileList = [.. ProfileSaveModules.OrderByDescending(_profile => _profile.LoadOrderPriority)];

		// verify profile dir creation
		for (int i = 0; i < _paths.Length; i++)
			await CreateProfileDir(_paths[i], i != 1);

		// load all save data classes
		for (int i = 0; i < _profileList.Count; i++)
			LoadClassData(_profileList[i], _paths);

		OnFinish?.Invoke();
		DebugLogger.Print(DebugLogger.LogPriority.Log, $"ProfileLoad: Loaded profile <{Profile}> to memory.");
	}
	private static bool LoadClassData(SaveMetadata _class, string[] _paths)
	{
		for (int i = 0; i < _paths.Length; i++)
		{
			try
			{
				_class.CallLoad(ProfilePath);
				return true;
			}
			catch (Exception _error)
			{ DebugLogger.Print(DebugLogger.LogPriority.Warning, $"ProfileLoad: {_class.ID} was not able to be loaded." + _error); }
		}
		return false;
	}

	// MARK: Profile Managment
	public static void AddSaveModule(SaveMetadata _module)
	{
		if (!ProfileSaveModules.Contains(_module))
			ProfileSaveModules.Add(_module);
	}
	public static void RemoveSaveModule(SaveMetadata _module)
	{
		_module.CallDispose();
		if (!ProfileSaveModules.Remove(_module))
			DebugLogger.Print(DebugLogger.LogPriority.Error, string.Format("SaveSystem: Failed on removing {0}", _module.ID));
	}
	public static void RemoveSaveModule(string _id)
	{
		if (ProfileSaveModules.Exists((m) => m.ID.Equals(_id)))
		{
			SaveMetadata _module = ProfileSaveModules.Find((m) => m.ID.Equals(_id));
			_module.CallDispose();
			ProfileSaveModules.Remove(_module);
		}
		else
			DebugLogger.Print(DebugLogger.LogPriority.Error, string.Format("SaveSystem: Failed on removing {0}", _id));
	}
	public static bool HasSaveModule(string _id) =>
		ProfileSaveModules.Exists((m) => m.ID == _id);
	public static void ClearSaveModules() =>
		ProfileSaveModules.Clear();

	/// <summary>
	/// Used for new games to set default vaules to all serializeables
	/// </summary>
	public static Task SetProfileData()
	{
		// Load all save data classes
		for (int i = 0; i < ProfileSaveModules.Count; i++)
		{
			ProfileSaveModules[i].RootPath = ProfilePath;
			ProfileSaveModules[i].CallSet();
		}
		return Task.CompletedTask;
	}
	public static void SelectProfile(string _profile)
	{
		ProfilePath = GetProfilePath(_profile);
		Profile = _profile;
	}
	public static string GetProfilePath(string _profile) =>
		$"{PROFILES_DIR}/{_profile}";

	public static async Task CreateProfile()
	{
		// Create directories for current profile
		await CreateProfileDir(ProfilePath);
		await CreateProfileDir(ProfilePath + PROFILE_BACKUP_DIR, true);
		await CreateProfileDir(ProfilePath + PROFILE_AUTOSAVE_DIR, true);

		DebugLogger.Print(DebugLogger.LogPriority.Info, $"ProfileCreate: Succesfully created profile of <{Profile}>.");
	}
	private static Task CreateProfileDir(string _path, bool _isCache = false)
	{
		// create profile directory
		DirAccess.MakeDirAbsolute(_path);
		var _dir = DirAccess.Open(_path);

		if (_dir == null)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "SaveSystem: DirAccess error on opening directory:\n", DirAccess.GetOpenError());
			return Task.CompletedTask;
		}

		// create subdirectories
		_dir.MakeDir("aphids");
		_dir.MakeDir("resorts");
		if (!_isCache)
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
			DebugLogger.Print(DebugLogger.LogPriority.Error, $"ProfileDelete: Cannot delete file in path: {_path}");
			return Task.CompletedTask;
		}

		// Scary!
		System.IO.Directory.Delete(_path, true); // DirAccess.Remove does not have an option to be recursive
		DebugLogger.Print(DebugLogger.LogPriority.Info, $"ProfileDelete: Succesfully deleted profile <{_profile}>.");
		return Task.CompletedTask;
	}

	// ==================================================================
	// MARK: Interfaces And Bases
	public abstract class SaveMetadata(string ID, int LoadPriority = 0) : IEqualityComparer<SaveMetadata>
	{
		public readonly string ID = ID;
		/// <summary>
		/// Higher number means higher load priority.
		/// </summary>
		internal int LoadOrderPriority = LoadPriority;
		/// <summary>
		/// Source directory. Usually by modified functions for dynamic pahts (such as, for savefile data). 
		/// </summary>
		public string RootPath = USERROOT_DIR;
		/// <summary>
		/// Relative path from root. If left empty, file will be stored directly at the root.
		/// </summary>
		public string RelativePath = string.Empty;
		/// <summary>
		/// File extension. Defaults to ".data"
		/// </summary>
		public string Extension = SAVEFILE_EXTENSION;
		public uint GameVersion { get; protected set; } = GlobalManager.GAME_VERSION;

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

		public abstract void CallSave(string _rootPath, bool _printToLog = true);
		public abstract void CallLoad(string _rootPath);
		public abstract void CallSet();
		public abstract void CallDispose();

		public bool Equals(SaveMetadata x, SaveMetadata y) =>
			x.ID == y.ID;

		public int GetHashCode([DisallowNull] SaveMetadata obj) =>
			ID.GetHashCode();
	}
	/// <summary>
	/// Used to handle data manipulation separately from the SaveModule.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IDataModule<T>
	{
		public void Set(T _data);
		public T Get();
		/// <summary>
		/// Fetch the default value of this object.
		/// </summary>
		public T Default();
		/// <summary>
		/// Initialize this function to get rid of unneeded data.
		/// </summary>
		public void Dispose() { }
	}
	/// <summary>
	/// Generic string version of IDataModule. Used for structures and items.
	/// </summary>
	public interface IDataModule
	{
		public void Set(string _data);
		public string Get();
		/// <summary>
		/// Sets the default value for this object.
		/// </summary>
		public void Default()
		{
			return;
		}
		/// <summary>
		/// Initialize this function to get rid of unneeded data.
		/// </summary>
		public void Dispose()
		{
			return;
		}
	}

	// ==================================================================

	// MARK: SaveData Module
	/// <summary>
	/// Core component to save runtime data to system via Json serialization.
	/// This class is NOT meant to be the data holder. Instead, it requires the class type of the data to serialize.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SaveModule<T>(string ID, IDataModule<T> _module, int LoadPriority = 0) : SaveMetadata(ID, LoadPriority)
	{
		public JsonSerializerOptions JsonOptions = null;
		protected IDataModule<T> Data = _module;

		/// <param name="_logToPrint">[DEBUG] Allow printing to console.</param>
		/// <returns></returns>
		public virtual Task Save(bool _logToPrint = true)
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

			if (_logToPrint)
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"ProfileSave: Saved succesfully - Version: {GameVersion} Path: {_path}.");
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
					GameVersion = 0;
					DebugLogger.Print(DebugLogger.LogPriority.Error, "ProfileLoad: Unable to load game version", _error);
				}

				_data = PostLoad(_raw_data);
			}
			else
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"ProfileLoad: {ID} was not found. Creating new instance. - Version: {GameVersion} Path: " + GetPath());

			if (loadToClass)
				Data.Set(_data);

			DebugLogger.Print(DebugLogger.LogPriority.Log, $"ProfileLoad: Loaded succesfully(toClass={loadToClass}) - Version: {GameVersion} LP: "
				+ LoadOrderPriority + " Path: " + _path);
			return _data;
		}

		/// <summary>
		/// Method that deserializes raw data back into runtime data. Runs after data has been loaded from the filestream.
		/// </summary>
		public virtual T PostLoad(string _raw_data)
		{
			return JsonSerializer.Deserialize<T>(_raw_data);
		}
		/// <summary>
		/// Method that returns the file's path. Used to patch-in alternative paths for backwards compatibilites.
		/// </summary>
		/// <returns></returns>
		public virtual string PreLoad()
		{
			string _path = GetPath(), _path_old = _path.Replace(".data", "_data.json");

			// patch for 0.1.3v savefiles, renames file with "_data" and replace it with ".data"
			if (!FileAccess.FileExists(_path) && FileAccess.FileExists(_path_old))
				System.IO.File.Move(ProjectSettings.GlobalizePath(_path_old), ProjectSettings.GlobalizePath(_path));

			return _path;
		}

		public override void CallSave(string _rootPath, bool _printToLog)
		{
			RootPath = _rootPath;
			Save(_printToLog);
		}
		public override void CallLoad(string _rootPath)
		{
			RootPath = _rootPath;
			Load();
		}
		public override void CallSet() => Data.Set(Data.Default());
		public override void CallDispose() => Data.Dispose();
	}
}