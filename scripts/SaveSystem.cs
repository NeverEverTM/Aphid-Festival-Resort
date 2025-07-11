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
	PROFILE_BACKUP_DIR = "/backup",
	PROFILE_APHIDS_DIR = "aphids",
	PROFILE_RESORTS_DIR = "resorts",
	PROFILE_ALBUM_DIR = "/screenshots/",
	CONFIG_DIR = "config",
	DEFAULT_PROFILE = "default",
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
	public static async Task SaveProfile()
	{
		// Save serialized classes
		for (int i = 0; i < ProfileSaveModules.Count; i++)
			await SaveClassData(ProfileSaveModules[i]);

		Logger.Print(Logger.LogPriority.Log, $"ProfileSave: Saved profile <{Profile}> to <{ProfilePath}>.");
	}
	private static Task SaveClassData(SaveMetadata _class)
	{
		try
		{
			_class.RootPath = ProfilePath;
			_class.CallSave();
			_class.RootPath += PROFILE_BACKUP_DIR;
			_class.CallSave(true);
		}
		catch (Exception _e)
		{
			Logger.Print(Logger.LogPriority.Error, $"ProfileSave: Error on saving class <{_class.ID}>." + _e);
		}
		return Task.CompletedTask;
	}

	// MARK: Profile Loading
	public static Task LoadProfile()
	{
		string _backupFolder = ProfilePath + PROFILE_BACKUP_DIR;
		List<SaveMetadata> _profileList = [.. ProfileSaveModules.OrderByDescending(_profile => _profile.LoadOrderPriority)];

		// Load all save data classes
		for (int i = 0; i < _profileList.Count; i++)
			LoadClassData(_profileList[i]);

		OnFinish?.Invoke();
		Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: Loaded profile <{Profile}> to memory.");
		return Task.CompletedTask;
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
			Logger.Print(Logger.LogPriority.Error, string.Format("SaveSystem: Failed on removing {0}", _module.ID));
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
			Logger.Print(Logger.LogPriority.Error, string.Format("SaveSystem: Failed on removing {0}", _id));
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
	public static void SelectProfile(string _profile = DEFAULT_PROFILE)
	{
		ProfilePath = $"{PROFILES_DIR}/{_profile}";
		Profile = _profile;
	}

	public static async Task CreateProfile()
	{
		// Create directories for current profile
		await CreateProfileDir(ProfilePath);
		await CreateProfileDir(ProfilePath + PROFILE_BACKUP_DIR);

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

		public abstract void CallSave(bool _disallowPrint = false);
		public abstract void CallLoad();
		public abstract void CallSet();
		public abstract void CallDispose();

		public bool Equals(SaveMetadata x, SaveMetadata y) =>
			x.ID == y.ID;

		public int GetHashCode([DisallowNull] SaveMetadata obj) =>
			ID.GetHashCode();
	}
	/// <summary>
	/// Used tp handle data manipulation separately from the SaveModule.
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
		public void Dispose() {}
	}
	// ==================================================================

	// TODO: test saving a packedbytedata to see if load speed and storage size is reduced
	// add a CallDispose() to dispose of current usunued data
	// get rid of savemodulegd and see if it can be integrated with SaveModule<Generic>

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

		/// <param name="_disallowPrint">[DEBUG] Disallow printing to console.</param>
		/// <returns></returns>
		public virtual Task Save(bool _disallowPrint = false)
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

			if (!_disallowPrint)
				Logger.Print(Logger.LogPriority.Log, $"ProfileSave: Saved succesfully - Version: {GameVersion} Path: {_path}.");
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
					Logger.Print(Logger.LogPriority.Error, "ProfileLoad: Unable to load game version", _error);
				}

				_data = PostLoad(_raw_data);
			}
			else
				Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: {ID} was not found. Creating new instance. - Version: {GameVersion} Path: " + RootPath + RelativePath + ID + Extension);

			if (loadToClass)
				Data.Set(_data);

			Logger.Print(Logger.LogPriority.Log, $"ProfileLoad: Loaded succesfully(toClass={loadToClass}) - Version: {GameVersion} LP: "
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

		public override void CallSave(bool _disallowPrint) => Save(_disallowPrint);
		public override void CallLoad() => Load();
		public override void CallSet() => Data.Set(Data.Default());
		public override void CallDispose() => Data.Dispose();
	}

	/// <summary>
	/// Similar to SaveData<T> but it can store Godot's Variants as intended.
	/// For example, it can store InputEvents and load them back without issue.
	/// Requires more setup to translate generic T type back into a Variant.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SaveModuleGD(string ID, IDataModule<Variant> _module, int LoadPriority = 0) : SaveMetadata(ID, LoadPriority)
	{
		protected IDataModule<Variant> Data = _module;

		public virtual Task Save(bool _disallowPrint = false)
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

			if (!_disallowPrint)
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
					GameVersion = 0;
					Logger.Print(Logger.LogPriority.Error, "ProfileLoad: Unable to load game version", _error);
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

		public override void CallSave(bool _disallowPrint) => Save(_disallowPrint);
		public override void CallLoad() => Load();
		public override void CallSet() => Data.Set(Data.Default());
		public override void CallDispose() => Data.Dispose();
	}
}