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

	public enum SaveEventsEnum { OnSaveFinish, OnLoadFinish }
	public class SaveEventArgs : EventArgs
	{
		public uint GameVersion { get; set; }
		public SaveEventArgs(SaveMetadata _metadata)
		{
			GameVersion = _metadata.GameVersion;
		}
	}

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
	/// <param name="_autosave">Makes this save not overwrite backup with newer information.</param>
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
				GlobalManager.CREATE_POPUP($"Error for {_profileList[i].ID}", CanvasManager.Instance);
				return false;
			}
		}

		if (!_force) // TODO: implement something similar to the Disabled function in player to queue Busy states instead
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
				// auxiliary backup
				_class.CallSave(ProfilePath + PROFILE_AUTOSAVE_DIR, false);

				// current
				_class.CallSave(ProfilePath);

				// primary backup
				_class.CallSave(ProfilePath + PROFILE_BACKUP_DIR, false);
			}
			else
			{
				// auxiliary backup
				_class.CallSave(ProfilePath + PROFILE_AUTOSAVE_DIR, false);

				// current
				_class.CallSave(ProfilePath);
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
	/// <param name="_loadFully">Should it load this module even if it was loaded already?<para>This only affects modules that stay for the whole game session, such as Aphids data or Player data.</para></param>
	public static Task LoadProfile(bool _loadFully = true)
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
		{
			if (!CreateProfileMainDir(_paths[i], i != 1))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Error, DebugLogger.GameTermination.Complete, "ProfileLoad: Failed to create directories.");
				return Task.FromException(new("Failed to create directories"));
			}
		}

		// load all save data classes
		for (int i = 0; i < _profileList.Count; i++)
		{
			if (!_loadFully && _profileList[i].Loaded)
				continue;

			if (!LoadClassData(_profileList[i], _paths))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Error, DebugLogger.GameTermination.Complete, $"ProfileLoad: Failed to load profile <{Profile}> to memory.");
				return Task.FromException(new("Failed to load profile"));
			}
		}

		DebugLogger.Print(DebugLogger.LogPriority.Log, $"ProfileLoad: Loaded profile <{Profile}> to memory.");
		return Task.CompletedTask;
	}
	private static bool LoadClassData(SaveMetadata _class, string[] _paths)
	{
		// Attempt to load the highest priority path first, if you cant, try the next one
		for (int i = 0; i < _paths.Length; i++)
		{
			try
			{
				_class.CallLoad(_paths[i]);
				return true;
			}
			catch (Exception _error)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"ProfileLoad: {_class.ID} at path <{_paths[i]}> was not able to be loaded." + _error);
			}
		}
		return false;
	}

	// MARK: Profile Managment
	public static void AddSaveModule(SaveMetadata _module)
	{
		if (ExistsSaveModule(_module))
			return;

		ProfileSaveModules.Add(_module);
	}
	public static void RemoveSaveModule(SaveMetadata _module)
	{
		if (!ExistsSaveModule(_module))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"SaveSystem: {_module.ID} does not exist");
			return;
		}
		_module.CallDispose();
		if (!ProfileSaveModules.Remove(_module))
			DebugLogger.Print(DebugLogger.LogPriority.Error, string.Format("SaveSystem: Failed on removing {0} from Profile", _module.ID));
	}
	/// <summary>
	/// Checks if this SaveModule already exists.
	/// </summary>
	/// <returns></returns>
	public static bool ExistsSaveModule(SaveMetadata _module) =>
		ProfileSaveModules.Exists((m) => m.ID == _module.ID);
	/// <summary>
	/// Disposes of SaveModules and/or clears their data, depends on gamestate and metadata settings. <span>Check SaveMetadata.DisposeMethod for more information.</span>
	/// </summary>
	public static void ClearSaveModules(SaveMetadata.DisposeMethod _method)
	{
		for (int i = ProfileSaveModules.Count - 1; i >= 0; i--)
		{
			if ((int)ProfileSaveModules[i].DisposeMode <= (int)_method)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Debug, $"SaveSystem: Removed module <{ProfileSaveModules[i].ID}>");
				ProfileSaveModules[i].CallDispose();
				RemoveSaveModule(ProfileSaveModules[i]);
			}
		}
	}

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

	public static Task CreateProfile()
	{
		// Create directories for current profile
		bool _success = CreateProfileMainDir(ProfilePath, true) &&
				CreateProfileMainDir(ProfilePath + PROFILE_BACKUP_DIR) &&
				CreateProfileMainDir(ProfilePath + PROFILE_AUTOSAVE_DIR);

		if (_success)
			DebugLogger.Print(DebugLogger.LogPriority.Info, $"ProfileCreate: Succesfully created profile of <{Profile}>.");
		else
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, $"ProfileCreate: Error on creating <{Profile}> at <{ProfilePath}>.");
			GlobalManager.CREATE_POPUP("Error! Cannot create directories for profile (do you have any folders open?)", GlobalManager.Instance);
			return Task.FromException(new("Unable to create profile directories"));
		}
		return Task.CompletedTask;
	}
	/// <summary>
	/// Creates the file structure for a profile directory at the given path.
	/// </summary>
	/// <param name="_path">The absolute path to the directory. Allows "user://" as root.</param>
	/// <param name="_includeNonEssentials">Should it replicate non-essential folders? ex. screenshots folder.</param>
	/// <returns>Wheter or not the directory was succesfully created.</returns>
	private static bool CreateProfileMainDir(string _path, bool _includeNonEssentials = false)
	{
		// create profile directory
		DirAccess.MakeDirAbsolute(_path);
		DirAccess _mainDir = DirAccess.Open(_path);

		if (_mainDir == null)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "SaveSystem: DirAccess error on opening directory. Code:", DirAccess.GetOpenError());
			return false;
		}

		// create subdirectories
		if (!CreateProfileSubDir("aphids", _path, ref _mainDir))
			return false;
		if (!CreateProfileSubDir("resorts", _path, ref _mainDir))
			return false;

		if (_includeNonEssentials && !CreateProfileSubDir("screenshots", _path, ref _mainDir))
			return false;

		return true;
	}
	private static bool CreateProfileSubDir(string _dirName, string _path, ref DirAccess _mainDir)
	{
		if (DirAccess.DirExistsAbsolute(System.IO.Path.Combine(_path, _dirName)))
			return true;
		Error _subDir = _mainDir.MakeDir(_dirName);
		if (_subDir != Error.Ok)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "SaveSystem: Error on creating dir. Code:", _subDir);
			return false;
		}
		return true;
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
	// MARK: SaveData Module
	/// <summary>
	/// Base class for SaveModule, includes all settings and metadata for the instance.
	/// </summary>
	public abstract class SaveMetadata(string ID, int LoadOrderPriority = 0) : IEqualityComparer<SaveMetadata>
	{
		public readonly string ID = ID;
		/// <summary>
		/// Higher number means higher load priority.
		/// </summary>
		internal int LoadOrderPriority = LoadOrderPriority;
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
		protected EncoderMethod EncodeMode = EncoderMethod.JSON;
		public DisposeMethod DisposeMode = DisposeMethod.OnGameExit;

		/// <summary>
		/// Dictates when AND how this save instance is disposed off.
		/// </summary>
		public enum DisposeMethod
		{
			/// <summary>
			/// Dispose after the room changes
			/// </summary>
			OnRoomTransition,
			/// <summary>
			/// Dispose only after user exits the current profile.
			/// </summary>
			OnGameExit,
			///// <summary>
			///// Module is global
			///// </summary>
			//OnApplicationQuit,
			/// <summary>
			/// This module is not intended to be disposed of normally, used by global modules such as the settings.
			/// </summary>
			NotApplicable
		}
		/// <summary>
		/// Dictates store method, mostly unusued and defaults to JSON.
		/// </summary>
		protected enum EncoderMethod
		{
			JSON,
			Variant
		}
		/// <summary>
		/// Indicates if this modules has already been loaded before, for situations where the module persists but do not requires to be re-loaded, both Save and Load set this as true.
		/// </summary>
		public bool Loaded;

		/// <summary>
		/// Returns the path to the saved contents.
		/// </summary>
		/// <param name="_global">Return this path as a global OS file path instead of a Godot file path.</param>
		public string GetPath(bool _global = false) => !_global ?
				System.IO.Path.Join(RootPath, RelativePath, ID + Extension)
				: ProjectSettings.GlobalizePath(System.IO.Path.Join(RootPath, RelativePath, ID + Extension));

		public abstract void CallSave(string _rootPath, bool _printToLog = true);
		public abstract void CallLoad(string _rootPath);
		public abstract void CallSet();
		public abstract void CallDispose();

		protected List<Action<SaveEventArgs>> OnSaveFinish = [], OnLoadFinish = [];

		public void AddEventListener(Action<SaveEventArgs> _action, SaveEventsEnum _event)
		{
			switch (_event)
			{
				case SaveEventsEnum.OnSaveFinish:
					OnSaveFinish.Add(_action);
					break;
				case SaveEventsEnum.OnLoadFinish:
					OnLoadFinish.Add(_action);
					break;
			}
		}
		public void RemoveEventListener(Action<SaveEventArgs> _action, SaveEventsEnum _event)
		{
			switch (_event)
			{
				case SaveEventsEnum.OnSaveFinish:
					OnSaveFinish.Remove(_action);
					break;
				case SaveEventsEnum.OnLoadFinish:
					OnLoadFinish.Remove(_action);
					break;
			}
		}

		public bool Equals(SaveMetadata x, SaveMetadata y) =>
			x.ID == y.ID;

		public int GetHashCode([DisallowNull] SaveMetadata obj) =>
			ID.GetHashCode();
	}
	/// <summary>
	/// Interface that allows custom handling of the data type after or before doing save/load operations in SaveModule.
	/// </summary>
	public interface IDataModule<T>
	{
		public void Set(T _data);
		public T Get();
		/// <summary>
		/// Fetches the default value for this data type. Called to initialize it before loading or 
		/// </summary>
		public T Default();
		/// <summary>
		/// Custom method to manually handle disposal of objects. Call depends on SaveMetadata "DisposeMode" setting.
		/// </summary>
		public void Dispose() => Set(Default());
	}
	/// <summary>
	/// Generic string version of IDataModule. Used for structures and items.
	/// </summary>
	public interface IGenericDataModule
	{
		public void Set(string _data);
		public string Get();

		public void Default()
		{
			return;
		}

		public void Dispose() => Default();
	}

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
			if (EncodeMode == EncoderMethod.JSON)
				_file.StorePascalString(JsonSerializer.Serialize(Data.Get(), JsonOptions));
			else
				_file.StoreVar(JsonSerializer.Serialize(Data.Get(), JsonOptions));

			// Save most recent game version this file was saved in
			_file.Store32(GlobalManager.GAME_VERSION);

			GlobalManager.Utils.InvokeEventListeners(OnSaveFinish, new(this), true);
			if (_logToPrint)
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"ProfileSave: Saved succesfully - Version: {GameVersion} Path: {_path}.");

			Loaded = true;
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
				string _raw_data = EncodeMode == EncoderMethod.JSON ?
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

			Loaded = true;
			GlobalManager.Utils.InvokeEventListeners(OnLoadFinish, new(this), true);
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
		public override void CallDispose()
		{
			Data.Dispose();
			OnSaveFinish.Clear();
			OnLoadFinish.Clear();
		}
	}
}