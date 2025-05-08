using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// The main processing script, in charge of startup, global variables, scene change and resource loads.
/// It also includes a Utils class for miscellaneous helper functions.
/// </summary>
internal partial class GlobalManager : Node2D
{
	public const uint GAME_VERSION = 210;

	public static GlobalManager Instance { get; private set; }
	public readonly static RandomNumberGenerator RNG = new();

	public delegate void LoadSceneHandler(SceneName _name);
	public static event LoadSceneHandler OnPreLoadScene, OnPostLoadScene;

	public enum SceneName { Resort, Menu }
	public static SceneName Scene { get; private set; }

	public static bool IsBusy { get; set; }

	public const string
		RESORT_SCENE = "uid://dsw70b8747xh8",
		MENU_SCENE = "uid://by6u0617yy7oe",
		LOADING_SCENE = "uid://ddfk4hhfrlxpa",
		CONFIRM_WINDOW_SCENE = "uid://blrpv4ys07erj",
		POPUP_WINDOW_SCENE = "uid://dwp7dadam0k12",
		CG_OUTLINE_SHADER = "uid://dc60jiy0ptbuc",
		OUTLINE_SHADER = "uid://dw8sws2xkkyr6",
		ITEM_ENTITY = "uid://d3miyavfmn4oh",
		APHID_ENTITY = "uid://7oo48cet73pb";
	public const string
		ABSOLUTE_SFX_PATH = "res://sfx/",
		ABSOLUTE_SCENES_PATH = "res://scenes/",
		ABSOLUTE_PARTICLES_PATH = "res://scenes/particles/",
		ABSOLUTE_SPRITES_PATH = "res://sprites/",
		ABSOLUTE_ICONS_PATH = "res://sprites/icons/",
		ABSOLUTE_DATABASES_PATH = "res://databases/",
		ABSOLUTE_SKINS_PATH = "res://databases/skins/",
		ABSOLUTE_ITEMS_DB_PATH = "res://databases/items",
		ABSOLUTE_STRUCTURES_DB_PATH = "res://databases/structures";

	// =========| GLOBAL LOADED VALUES |===========
	public static readonly Dictionary<string, Item> G_ITEMS = [];
	public static readonly Dictionary<string, Food> G_FOOD = [];
	public static readonly List<Recipe> G_RECIPES = [];

	// todo: replace G_ICONS and G_AUDIO with ResourcePreloaders, add G_SKINS while you are at it
	public static readonly Dictionary<string, Texture2D> G_ICONS = [];
	public static readonly Dictionary<string, AudioStream> G_AUDIO = [];
	public static readonly ResourcePreloader G_PARTICLES = new();
	public static readonly Dictionary<string, Texture2D> G_SKINS = [];

	public readonly struct Item(int cost, int unlockableLevel, string tag, string shopTag)
	{
		public readonly int cost = cost;
		/// <summary>
		/// Currently unusued.
		/// </summary>
		public readonly int unlockableLevel = unlockableLevel;
		/// <summary>
		/// General functionality meta tag.
		/// </summary>
		public readonly string tag = tag;
		/// <summary>
		/// Shop it belongs to.
		/// </summary>
		public readonly string shopTag = shopTag;
	}
	public readonly struct Food(AphidData.FoodType type, float food_value, float drink_value, string[] skill_list, int[] skill_values)
	{
		public readonly AphidData.FoodType type = type;
		public readonly float food_value = food_value;
		public readonly float drink_value = drink_value;
		public readonly string[] skill_list = skill_list;
		public readonly int[] skill_values = skill_values;
	}
	public readonly struct Recipe(string result, string ingredient1, string ingredient2)
	{
		public readonly string Result = result;
		public readonly string Ingredient1 = ingredient1;
		public readonly string Ingredient2 = ingredient2;
	}

	public static Texture2D GetIcon(string _key)
	{
		if (_key != null && G_ICONS.TryGetValue(_key, out Texture2D value))
			return value;
		else
			return new PlaceholderTexture2D();
	}
	public static Texture2D GetSkin(string _id)
	{
		if (_id != null && G_SKINS.TryGetValue(_id, out Texture2D _texture))
			return _texture;
		else
			return new PlaceholderTexture2D()
			{
				Size = new(32, 32)
			};
	}

	private PhysicsDirectSpaceState2D spaceState;
	private readonly static List<GpuParticles2D> ACTIVE_PARTICLES_CACHED = [];

	internal static Label BOOT_LOADING_LABEL;

	public override void _EnterTree()
	{
		Instance = this;
		IsBusy = true;
		Scene = SceneName.Menu;
		SaveSystem.CreateBaseDirectories();
#if DEBUG
		Logger.LogMode = Logger.LogPriorityMode.All;
#else
		Logger.LogMode = Logger.LogPriorityMode.Default;
#endif
		GameManager.ProfileSaveModule = new(GameManager.ID, new GameManager.SaveModule(), 9999)
		{
			Extension = SaveSystem.SAVEFILE_EXTENSION
		};
	}
	////// GAMEMANAGER READY GETS CALLED WHEN LOADING NEW ROOT SCENE
	public override void _Ready()
	{
		// Runtime Params
		spaceState = GetWorld2D().DirectSpaceState;
		ControlsManager.InputBinds.Load();
		OptionsManager.Module.Load();
	}
	public override void _Process(double delta)
	{
		// Cleans particles periodically once finished
		for (int i = 0; i < ACTIVE_PARTICLES_CACHED.Count; i++)
		{
			bool _valid = IsInstanceValid(ACTIVE_PARTICLES_CACHED[i]);
			if (_valid && ACTIVE_PARTICLES_CACHED[i].Emitting)
				continue;
			if (_valid)
				ACTIVE_PARTICLES_CACHED[i].QueueFree();
			ACTIVE_PARTICLES_CACHED.RemoveAt(i);
		}
	}
	
	// MARK: Game Initialization
	/// <summary>
	/// Initializes primary systems and loads values to memory. MainMenu triggers it as part of its wake up.
	/// In order to be called again, BOOT_LOADING_LABEL must be set to a valid text display node.
	/// </summary>
	public async static Task INTIALIZE_GAME_PROCESS(bool _playIntro = true)
	{
		Control _node = new();
		// show loading screen animation
		if (_playIntro)
		{
			_node = BOOT_LOADING_LABEL.GetParent() as Control;
			_node.Visible = true;
			(_node.GetChild(1) as AnimatedSprite2D).Play("default");
		}
		else
		{
			BOOT_LOADING_LABEL = new()
			{
				Visible = false
			};
		}

		try
		{
			await LOAD_ICONS();
			await LOAD_STRUCTURE_ICONS();
			await LOAD_SKINS();
			await LOAD_SFX();
			await LOAD_DATA();
			await LOAD_PARTICLES();
			IsBusy = false;
			_node.Visible = false;
		}
		catch (Exception _err)
		{
			AcceptDialog _dialog = new()
			{
				DialogText = "Critical Error: " + _err.Message,
				PopupWindow = true,
				Title = "The game has given up on you"
			};
			_dialog.Canceled += () => Instance.GetTree().Quit(1);
			_dialog.Confirmed += () => Instance.GetTree().Quit(1);
			Instance.AddChild(_dialog);
			_dialog.PopupCentered();
			Logger.Print(Logger.LogPriority.Error, _err);

			await Task.Delay(int.MaxValue);
		}
	}
	private static async Task LOAD_ICONS()
	{
		string[] _icons = DirAccess.GetFilesAt(ABSOLUTE_ICONS_PATH);

		for (int i = 0; i < _icons.Length; i++)
		{
			string _fileName = _icons[i].Replace(".import", string.Empty), _id = _fileName.Split('.')[0];
			if (G_ICONS.ContainsKey(_id))
				continue;
			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_0")} (1/2) ({i + 1}/{_icons.Length})";

			// Wait until it yields
			var _resource = await PRELOAD_RESOURCE(ABSOLUTE_ICONS_PATH + _fileName);
			G_ICONS.Add(_id, _resource as Texture2D);
		}
	}
	private static async Task LOAD_STRUCTURE_ICONS()
	{
		string[] _structures = DirAccess.GetFilesAt(ABSOLUTE_STRUCTURES_DB_PATH);

		for (int i = 0; i < _structures.Length; i++)
		{
			if (!_structures[i].EndsWith(".tscn"))
				continue;

			// first we check if we loaded a structure icon with the same name before
			string _structureName = _structures[i].Replace(".tscn", string.Empty);
			if (G_ICONS.ContainsKey(_structureName))
				continue;

			string _path = $"{ABSOLUTE_STRUCTURES_DB_PATH}/{_structures[i]}";
			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_0")} (2/2) ({i + 1})";

			// we load and create an icon directly from the resources sprite
			Node2D _packedScene = (await PRELOAD_RESOURCE(_path) as PackedScene).Instantiate() as Node2D;
			if (_packedScene is Sprite2D)
				G_ICONS.Add(_structureName, (_packedScene as Sprite2D).Texture);
			else if (_packedScene is AnimatedSprite2D)
				G_ICONS.Add(_structureName, (_packedScene as AnimatedSprite2D).SpriteFrames.GetFrameTexture("default", 0));
			_packedScene.QueueFree();
		}
	}
	private static async Task LOAD_SKINS()
	{
		string[] _directories = DirAccess.GetDirectoriesAt(ABSOLUTE_SKINS_PATH);
		for (int i = 0; i < _directories.Length; i++)
			await SEARCH_SKIN_FOLDER(_directories[i]);
	}
	private static async Task SEARCH_SKIN_FOLDER(string _directory)
	{
		string[] _files = DirAccess.GetFilesAt(ABSOLUTE_SKINS_PATH + _directory);
		for (int i = 0; i < _files.Length; i++)
		{
			// _filename = 0/skin_piece.res
			// _id = 0/skin_piece
			string _fileName = _directory + "/" + _files[i].Replace(".import", string.Empty),
					_id = _fileName.Split('.')[0];

			if (G_SKINS.ContainsKey(_id))
				continue;
			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_1")} ({i + 1}/{_files.Length})";
			var _resource = await PRELOAD_RESOURCE(ABSOLUTE_SKINS_PATH + _fileName);
			G_SKINS.Add(_id, _resource as Texture2D);
		}
	}
	private static async Task LOAD_SFX()
	{
		// Get all SFX paths (only checks folders at SFX root folder)
		string[] _directories = DirAccess.GetDirectoriesAt(ABSOLUTE_SFX_PATH);
		for (int i = 0; i < _directories.Length; i++)
			await SEARCH_SFX_FOLDER(_directories[i]);

		/// replace all of this since SoundManager now has direct access to the sound cache
		// Load aphid SFX
		string aphidPath = $"{ABSOLUTE_SFX_PATH}aphid/";
		Aphid.Audio_Nom = ResourceLoader.Load<AudioStream>(aphidPath + "nom.wav");
		Aphid.Audio_Idle = ResourceLoader.Load<AudioStream>(aphidPath + "idle.wav");
		Aphid.Audio_Idle_Baby = ResourceLoader.Load<AudioStream>(aphidPath + "baby_idle.wav");
		Aphid.Audio_Step = ResourceLoader.Load<AudioStream>(aphidPath + "step.wav");
		Aphid.Audio_Jump = ResourceLoader.Load<AudioStream>(aphidPath + "jump.wav");
		Aphid.Audio_Hurt = ResourceLoader.Load<AudioStream>(aphidPath + "hurt.wav");
		Aphid.Audio_Boing = ResourceLoader.Load<AudioStream>(aphidPath + "boing.wav");
	}
	private static async Task SEARCH_SFX_FOLDER(string _directory)
	{
		string[] _files = DirAccess.GetFilesAt(ABSOLUTE_SFX_PATH + _directory);
		for (int i = 0; i < _files.Length; i++)
		{
			// _filename = audio_example.wav
			// _id = ui/audio_example
			string _fileName = _directory + "/" + _files[i].Replace(".import", string.Empty),
					_id = _fileName.Split('.')[0];

			if (G_AUDIO.ContainsKey(_id))
				continue;

			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_2")} ({_directory}[{i}/{_files.Length}])";
			G_AUDIO.Add(_id, await PRELOAD_RESOURCE(ABSOLUTE_SFX_PATH + _fileName) as AudioStream);
		}
	}
	private static async Task LOAD_DATA()
	{
		await LOAD_DATABASE("items", _info =>
		{
			if (G_ITEMS.ContainsKey(_info[0]))
			{
				Logger.Print(Logger.LogPriority.Warning, $"ItemDatabase: <{_info[0]}> is duplicated.");
				return;
			}
			if (Instance.Tr(_info[0] + "_name") == _info[0] + "_name")
			{
				Logger.Print(Logger.LogPriority.Warning, $"ItemDatabase: <{_info[0]}> has no name.");
#if !DEBUG
				return;
#endif
			}
			if (Instance.Tr(_info[0] + "_desc") == _info[0] + "_desc")
			{
				Logger.Print(Logger.LogPriority.Warning, $"ItemDatabase: <{_info[0]}> has no description.");
#if !DEBUG
				return;
#endif
			}
			G_ITEMS.Add(_info[0], new(
				cost: int.Parse(_info[1]),
				unlockableLevel: int.Parse(_info[2]),
				tag: _info[3].ToString(),
				shopTag: _info[4].ToString()
			));
		});
		await LOAD_DATABASE("foods", _info =>
		{
			if (G_FOOD.ContainsKey(_info[0]))
			{
				Logger.Print(Logger.LogPriority.Warning, $"FoodDatabase: <{_info[0]}> is duplicated.");
				return;
			}
			if (!G_ITEMS.ContainsKey(_info[0]))
			{
				Logger.Print(Logger.LogPriority.Warning, $"FoodDatabase: <{_info[0]}> does not exist as an item.");
				return;
			}
			G_FOOD.Add(_info[0], new(
				type: (AphidData.FoodType)int.Parse(_info[1]),
				food_value: float.Parse(_info[2]),
				drink_value: float.Parse(_info[3]),
				skill_list: string.IsNullOrWhiteSpace(_info[4]) ? null : _info[4].Split(','),
				skill_values: string.IsNullOrWhiteSpace(_info[5]) ? null
						: Array.ConvertAll(_info[5].Split(','), s => int.Parse(s))
			));
		});
		await LOAD_DATABASE("recipes", _info =>
		{
			if (!string.IsNullOrWhiteSpace(_info[1]) && !G_ITEMS.ContainsKey(_info[1]))
			{
				Logger.Print(Logger.LogPriority.Warning, $"RecipeDatabase: Ingredient <{_info[1]}> does not exist as an item and cannot be an ingredient.");
				return;
			}
			if (!string.IsNullOrWhiteSpace(_info[2]) && !G_ITEMS.ContainsKey(_info[2]))
			{
				Logger.Print(Logger.LogPriority.Warning, $"RecipeDatabase: Ingredient <{_info[2]}> does not exist as an item and cannot be an ingredient.");
				return;
			}
			G_RECIPES.Add(new Recipe(
				_info[0],
				_info[1],
				_info[2]
			));
		});
	}
	private static Task LOAD_DATABASE(string _fileName, Action<string[]> _onItem)
	{
		FileAccess _file = FileAccess.Open(ABSOLUTE_DATABASES_PATH + _fileName + ".csv", FileAccess.ModeFlags.Read);
		string _header = _file.GetCsvLine()[0], _boot = Instance.Tr($"BOOT_{_header}");
		while (_file.GetPosition() < _file.GetLength())
		{
			_onItem(_file.GetCsvLine());
			BOOT_LOADING_LABEL.Text = $"{_boot} ({(int)((float)_file.GetPosition() / (float)_file.GetLength() * 100)}%)";
		}
		return Task.CompletedTask;
	}
	private static async Task LOAD_PARTICLES()
	{
		var _particleList = DirAccess.GetFilesAt(ABSOLUTE_PARTICLES_PATH);

		for (int i = 0; i < _particleList.Length; i++)
		{
			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_3")} ({i}/{_particleList.Length})";
			var _resource = await PRELOAD_RESOURCE(ABSOLUTE_PARTICLES_PATH + _particleList[i]);
			var _particle = (_resource as PackedScene).Instantiate() as GpuParticles2D;

			// we instantiate it and then delete it
			// we do this so Godot properly loads it now, so it doesnt cause a lag spike later
			// UPDATE: Godot 4 supposedly does this by default now but it stays just in case
			Instance.AddChild(_particle);
			_particle.QueueFree();

			G_PARTICLES.AddResource(_particleList[i].Split('.')[0], _resource);
		}
	}
	private static async Task<Resource> PRELOAD_RESOURCE(string _path)
	{
		ResourceLoader.LoadThreadedRequest(_path, "", true);
		ResourceLoader.ThreadLoadStatus _status = ResourceLoader.LoadThreadedGetStatus(_path);

		// start thread and await for its response
		while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
		{
			await Task.Delay(1);
			_status = ResourceLoader.LoadThreadedGetStatus(_path);
		}

		// action states
		if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
		{
			if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
				Logger.Print(Logger.LogPriority.Error, $"PRELOAD_RESOURCE: Resource <{_path.Substring(_path.LastIndexOf('/'))}> is not a valid resource or request.");
			else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
				Logger.Print(Logger.LogPriority.Error, $"PRELOAD_RESOURCE: Resource <{_path.Substring(_path.LastIndexOf('/'))}> is unable to load.");

			Instance.GetTree().Quit(2);
			return null;
		}

		return ResourceLoader.LoadThreadedGet(_path);
	}

	// MARK: Dedicated Functions
	/// <summary>
	/// Spawns a set of particles and manages its place in memory. Used to handle long-lasting particles in memory automatically.
	/// </summary>
	/// <param name="_name">Name of the particles prefab</param>
	/// <param name="_position">Position to spawn them in</param>
	/// <param name="_parentless">Automatically adds it as a child outside the current scene</param>
	/// <param name="_essential">Non-essential particles cannot spawn when above the particle limit</param>
	/// <returns></returns>
	public static GpuParticles2D EmitParticles(string _name, Vector2 _position, bool _essential = true)
			=> EmitParticles(_name, _position, Instance, _essential);
	public static GpuParticles2D EmitParticles(string _name, Vector2 _position, Node2D _parent, bool _essential = true)
	{
		var _item = (G_PARTICLES.GetResource(_name) as PackedScene).Instantiate() as GpuParticles2D;
		_item.GlobalPosition = _position;
		_item.Emitting = true;
		_item.ProcessMode = ProcessModeEnum.Pausable;
		_parent.AddChild(_item);

		ACTIVE_PARTICLES_CACHED.Add(_item);
		if (!_essential && ACTIVE_PARTICLES_CACHED.Count > 20)
			_item.Visible = false;
		return _item;
	}
	public static void CleanAllParticles()
	{
		for (int i = 0; i < ACTIVE_PARTICLES_CACHED.Count; i++)
			ACTIVE_PARTICLES_CACHED[i].QueueFree();
		ACTIVE_PARTICLES_CACHED.Clear();
	}

	public async static Task LoadScene(SceneName _scene)
	{
		await Task.Delay(1);
		string _path = _scene switch
		{
			SceneName.Menu => MENU_SCENE,
			SceneName.Resort => RESORT_SCENE,
			_ => "N/A"
		};
		if (_path == "N/A")
			return;
		Logger.Print(Logger.LogPriority.Log, $"GlobalManager: Loading scene <{Scene}>.");
		Scene = _scene;
		IsBusy = true;
		SoundManager.StopSong();

		// load the load screen
		LoadScreen _loading = ResourceLoader.Load<PackedScene>(LOADING_SCENE).Instantiate() as LoadScreen;
		Instance.GetTree().Root.AddChild(_loading);
		_ = _loading.CreateLeaves();

		// await for scene creation
		ResourceLoader.LoadThreadedRequest(_path);
		ResourceLoader.ThreadLoadStatus _status = ResourceLoader.LoadThreadedGetStatus(_path);

		while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
			_status = ResourceLoader.LoadThreadedGetStatus(_path);

		if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
		{
			if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
				GD.PrintErr($"Item at <{_path}> is not a valid resource or request.");
			else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
				GD.PrintErr($"Scene at <{_path}> was not able to be loaded.");

			IsBusy = false;
			Instance.GetTree().Quit(2);
		}

		// wait until full transition, then do all your shit
		while (!_loading.IsDone)
			await Task.Delay(1);
		OnPreLoadScene?.Invoke(Scene);
		SaveSystem.ProfileClassData.Clear();
		CleanAllParticles();
		SoundManager.CleanAllSounds();
		await Task.Delay(1);

		// set scene and request ready/events
		Instance.GetTree().ChangeSceneToPacked(ResourceLoader.LoadThreadedGet(_path) as PackedScene);
		Instance.RequestReady();
		OnPostLoadScene?.Invoke(Scene);
		IsBusy = false;
		await _loading.SweepLeaves();
	}
	public static void CreatePopup(string _translation_key, Node _parent)
	{
		Control _popup = ResourceLoader.Load<PackedScene>(POPUP_WINDOW_SCENE).Instantiate() as Control;
		_popup.Position = CameraManager.SCREEN_CENTER_CANVAS - _popup.Size / 2;
		(_popup.GetChild(0) as Label).Text = Instance.Tr(_translation_key);
		_parent.AddChild(_popup);
		Timer _timer = new()
		{
			OneShot = true
		};
		_timer.Timeout += () => _popup.QueueFree();
		_popup.AddChild(_timer);

		Timer _vanish = new()
		{
			OneShot = true
		};
		_vanish.Timeout += () =>
		{
			Tween tween = _popup.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Linear);
			tween.TweenProperty(_popup, "modulate", new Color(1, 1, 1, 0), 0.5f);
		};
		_timer.AddChild(_vanish);

		_vanish.Start(1.5f);
		_timer.Start(2);
	}
	
	public static class Utils
	{
		public static Godot.Collections.Dictionary RaycastBetween(Vector2 from, Vector2 to, 
				Godot.Collections.Array<Rid> _excludeList)
		{
			var query = PhysicsRayQueryParameters2D.Create(from, to);
			query.HitFromInside = true;
			query.Exclude = _excludeList;
			return Instance.spaceState.IntersectRay(query);
		}
		public static Godot.Collections.Dictionary RaycastTowards(Vector2 _position, Vector2 _direction, 
				Godot.Collections.Array<Rid> _excludeList)
		{
			var query = PhysicsRayQueryParameters2D.Create(_position, _position + _direction);
			query.HitFromInside = true;
			query.Exclude = _excludeList;
			return Instance.spaceState.IntersectRay(query);
		}
		public static Godot.Collections.Dictionary Raycast(PhysicsRayQueryParameters2D _query)
		{
			return Instance.spaceState.IntersectRay(_query);
		}

		public static int GetRandomByWeight(float[] weights) =>
			GetRandomByWeight(RNG, weights);
		public static int GetRandomByWeight(RandomNumberGenerator _engine, float[] weights)
		{
			// We take a sum of all weights
			float _total = 0;
			Array.ForEach(weights, _weight =>
            {
				_total += _weight;
			});

			// We get a random number between 0 and total
			float _random_cap = Mathf.Ceil(_engine.Randf() * _total);

			// Guess where the cap landed and give that as our result
			float _array_cursor = 0;
			for (int i = 0; i < weights.Length; i++)
			{
				_array_cursor += weights[i];
				if (_array_cursor >= _random_cap)
					return i;
			}
			Logger.Print(Logger.LogPriority.Error, "GlobalManager: Weighted RNG. It did happen :pensive:");
			return 0; // Should in theory, never happen
		}

		public static Color GetRandomColor(bool _randomizeAlpha = false)
		{
			byte[] _rgba = [ (byte)RNG.RandiRange(0,255), (byte)RNG.RandiRange(0,255),
				(byte)RNG.RandiRange(0,255), _randomizeAlpha ? (byte)(RNG.RandiRange(0,205) + 50) : (byte)255 ];

			return Color.Color8(_rgba[0], _rgba[1], _rgba[2], _rgba[3]);
		}

		public static Vector2 GetRandomVector(float _rangeMin, float _rangeMax) => new(RNG.RandfRange(_rangeMin, _rangeMax), RNG.RandfRange(_rangeMin, _rangeMax));
		public static Vector2 GetRandomVector_X(float _rangeMin, float _rangeMax, float _Y = 0) => new(RNG.RandfRange(_rangeMin, _rangeMax), _Y);
		public static Vector2 GetRandomVector_Y(float _rangeMin, float _rangeMax, float _X = 0) => new(_X, RNG.RandfRange(_rangeMin, _rangeMax));

		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
		}
	}
}