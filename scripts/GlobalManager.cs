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
	public static GlobalManager Instance { get; private set; }
	public readonly static RandomNumberGenerator RNG = new();
	public const uint GAME_VERSION = 301;
	public static bool IsBusy { get; internal set; } = true;

	public const string
		LEAF_LOADING_SCENE = "uid://ddfk4hhfrlxpa",
		FADE_LOADING_SCENE = "uid://cxt1r6y5y6260",
		CONFIRM_WINDOW_SCENE = "uid://blrpv4ys07erj",
		POPUP_WINDOW_SCENE = "uid://dwp7dadam0k12",
		CG_OUTLINE_SHADER = "uid://dc60jiy0ptbuc",
		OUTLINE_SHADER = "uid://dw8sws2xkkyr6",
		ITEM_ENTITY = "uid://d3miyavfmn4oh",
		APHID_ENTITY = "uid://7oo48cet73pb",
		PLAYER_PREFAB = "uid://b2tg0d8sg4vd0",
		CANVAS_PREFAB = "uid://bufmc14xek8uk",
		APHID_SLOT_PREFAB = "uid://d7m5e6tlxyve";
	public const string
		ABSOLUTE_SFX_PATH = "res://sfx/",
		ABSOLUTE_SCENES_PATH = "res://scenes/",
		ABSOLUTE_ROOMS_PATH = "res://scenes/rooms/",
		ABSOLUTE_PARTICLES_PATH = "res://scenes/particles/",
		ABSOLUTE_SPRITES_PATH = "res://sprites/",
		ABSOLUTE_ICONS_PATH = "res://sprites/icons/",
		ABSOLUTE_DATABASES_PATH = "res://databases/",
		ABSOLUTE_SKINS_PATH = "res://databases/skins/",
		ABSOLUTE_ITEMS_DB_PATH = "res://databases/items/",
		ABSOLUTE_STRUCTURES_DB_PATH = "res://databases/structures/",
		ABSOLUTE_JOBS_DB_PATH = "res://databases/jobs/",
		ABSOLUTE_RECIPES_DB_PATH = "res://databases/recipes/",
		ABSOLUTE_FOODS_DB_PATH = "res://databases/food/";

	// =========| GLOBALLY LOADED VALUES |===========
	public static readonly Dictionary<string, ItemData> G_ITEMS = [];
	public static readonly Dictionary<string, ItemData> G_STRUCTURES = [];
	public static readonly Dictionary<string, FoodData> G_FOOD = [];
	public static readonly List<RecipeData> G_RECIPES = [];

	public static readonly Dictionary<string, Texture2D> G_ICONS = [];
	public static readonly Dictionary<string, AudioStream> G_AUDIO = [];
	public static readonly Dictionary<string, Texture2D> G_SKINS = [];
	public static readonly ResourcePreloader G_PARTICLES = new();

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

	public override void _EnterTree()
	{
		Instance = this;
		SaveSystem.CreateBaseDirectories();
#if DEBUG
		DebugLogger.LogMode = DebugLogger.LogPriorityMode.All;
#else
		DebugLogger.LogMode = DebugLogger.LogPriorityMode.Default;
#endif
	}
	public override void _Ready()
	{
		spaceState = GetWorld2D().DirectSpaceState;
		OptionsManager.SaveModule.Load();
		ControlsManager.SaveModule.Load();
	}
	public override void _Process(double delta)
	{
		// Cleans particles periodically once finished
		for (int i = ACTIVE_PARTICLES_CACHED.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(ACTIVE_PARTICLES_CACHED[i]) || ACTIVE_PARTICLES_CACHED[i].IsQueuedForDeletion())
				ACTIVE_PARTICLES_CACHED.RemoveAt(i);
		}
	}

	// MARK: Game Initialization
	/// <summary>
	/// Initializes primary systems and loads values to memory. MainMenu triggers it as part of the game intro.
	/// </summary>
	public async static Task INTIALIZE_GAME_PROCESS()
	{
		try
		{
			await LOAD_ICONS();
			await LOAD_SKINS();
			await LOAD_SFX();
			await LOAD_ITEMS();
			await LOAD_FOOD();
			await LOAD_RECIPES();
			await LOAD_STRUCTURES();
			await LOAD_PARTICLES();
			await LOAD_TRAITS();

			IsBusy = false;
		}
		catch (Exception _err)
		{
			THROW_CRASH(_err);
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

			// Wait until it yields
			var _resource = await PRELOAD_RESOURCE(ABSOLUTE_ICONS_PATH + _fileName);
			G_ICONS.Add(_id, _resource as Texture2D);
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
			// BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_1")} ({i + 1}/{_files.Length})";
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
	}
	private static async Task SEARCH_SFX_FOLDER(string _directory)
	{
		string[] _files = DirAccess.GetFilesAt(ABSOLUTE_SFX_PATH + _directory);
		for (int i = 0; i < _files.Length; i++)
		{
			// _filename = ui/audio_example.wav
			// _id = ui/audio_example
			string _fileName = _directory + "/" + _files[i].Replace(".import", string.Empty),
					_id = _fileName.Split('.')[0];

			if (G_AUDIO.ContainsKey(_id))
				continue;

			G_AUDIO.Add(_id, await PRELOAD_RESOURCE(ABSOLUTE_SFX_PATH + _fileName) as AudioStream);
		}
	}
	private static Task LOAD_ITEMS()
	{
		string[] _items = DirAccess.GetFilesAt(ABSOLUTE_ITEMS_DB_PATH);

		for (int i = 0; i < _items.Length; i++)
		{
			string _filename = _items[i].Replace(".import", string.Empty);
			if (_filename.EndsWith(".tscn"))
				continue;
			string _id = _filename.Split('.')[0];

#if DEBUG
			if (G_ITEMS.ContainsKey(_id))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"ItemDatabase: <{_id}> is duplicated.");
				continue;
			}
			if (Instance.Tr(_id + "_name") == _id + "_name")
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"ItemDatabase: <{_id}> has no name.");
			if (Instance.Tr(_id + "_desc") == _id + "_desc")
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"ItemDatabase: <{_id}> has no description.");
#endif
			G_ITEMS.Add(_id, ResourceLoader.Load<ItemData>(ABSOLUTE_ITEMS_DB_PATH + _filename));
		}
		return Task.CompletedTask;
	}
	private static async Task LOAD_STRUCTURES()
	{
		string[] _structures = DirAccess.GetFilesAt(ABSOLUTE_STRUCTURES_DB_PATH);

		for (int i = 0; i < _structures.Length; i++)
		{
			string _filename = _structures[i].Replace(".import", string.Empty);
			if (_filename.EndsWith(".tscn"))
				continue;
			string _id = _filename.Split('.')[0];

			if (G_STRUCTURES.ContainsKey(_id))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"StructureDatabase: <{_id}> is duplicated.");
				continue;
			}
			if (Instance.Tr(_id + "_name") == _id + "_name")
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"StructureDatabase: <{_id}> has no name.");
			if (Instance.Tr(_id + "_desc") == _id + "_desc")
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"StructureDatabase: <{_id}> has no description.");

			Node _node = (await PRELOAD_RESOURCE(ABSOLUTE_STRUCTURES_DB_PATH + _id + ".tscn") as PackedScene).Instantiate<Node>();

			// load current texture if an icon doesnt exist already
			if (!G_ICONS.ContainsKey(_id))
			{
				if (_node.IsClass("Sprite2D") && (_node as Sprite2D) != null)
					G_ICONS.Add(_id, (_node as Sprite2D).Texture);
				else if (_node.IsClass("AnimatedSprite2D") && (_node as AnimatedSprite2D) != null)
					G_ICONS.Add(_id, (_node as AnimatedSprite2D).SpriteFrames.GetFrameTexture("default", 0));
				else
					DebugLogger.Print(DebugLogger.LogPriority.Warning, $"StructureDatabase: <{_id}> does not have a valid icon, nor could one be set up.");
			}

			G_STRUCTURES.Add(_id, ResourceLoader.Load<ItemData>(ABSOLUTE_STRUCTURES_DB_PATH + _filename));
			_node.QueueFree();
		}
	}
	private static Task LOAD_FOOD()
	{
		string[] _foods = DirAccess.GetFilesAt(ABSOLUTE_FOODS_DB_PATH);

		for (int i = 0; i < _foods.Length; i++)
		{
			string _filename = _foods[i].Replace(".import", string.Empty);
			string _id = _filename.Split('.')[0];

# if DEBUG
			if (G_FOOD.ContainsKey(_id))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"FoodDatabase: <{_id}> is duplicated.");
				continue;
			}
			if (!G_ITEMS.ContainsKey(_id))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Warning, $"FoodDatabase: <{_id}> does not exist as an item.");
				continue;
			}
#endif

			G_FOOD.Add(_id, ResourceLoader.Load<FoodData>(ABSOLUTE_FOODS_DB_PATH + _filename));
		}
		return Task.CompletedTask;
	}
	private static Task LOAD_RECIPES()
	{
		string[] _recipes = DirAccess.GetFilesAt(ABSOLUTE_RECIPES_DB_PATH);

		for (int i = 0; i < _recipes.Length; i++)
		{
			string _filename = _recipes[i].Replace(".import", string.Empty);

			RecipeData _recipe = ResourceLoader.Load<RecipeData>(ABSOLUTE_RECIPES_DB_PATH + _filename);
			G_RECIPES.Add(_recipe);
		}
		return Task.CompletedTask;
	}
	private static async Task LOAD_PARTICLES()
	{
		var _particleList = DirAccess.GetFilesAt(ABSOLUTE_PARTICLES_PATH);

		for (int i = 0; i < _particleList.Length; i++)
		{
			// BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_3")} ({i}/{_particleList.Length})";
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
	private static Task LOAD_TRAITS()
	{
		for (int i = 0; i < AphidTraits.TRAITS.Count; i++)
			AphidTraits.G_TRAITS.Add(AphidTraits.TRAITS[i].ID, AphidTraits.TRAITS[i].GetType());
		return Task.CompletedTask;
	}

	// # MARK: Debug
	// public static string[][] FETCH_CSV_DATABASE(string _path)
	// {
	// 	using FileAccess _file = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
	// 	List<string[]> _document = [];

	// 	while (_file.GetPosition() < _file.GetLength())
	// 		_document.Add(_file.GetCsvLine());

	// 	return [.. _document];
	// }
	// public static Task EXPORT_ITEM_DATABASE(string[][] _document)
	// {
	// 	for (int i = 1; i < _document.Length; i++)
	// 	{
	// 		string[] _info = _document[i];
	// 		string _tag = _info[3], _id = _info[0];

	// 		int _cost = int.Parse(_info[1]),
	// 			_unlockableLevel = int.Parse(_info[2]);
	// 		StringNames.GlobalTags _itemTag = _tag switch
	// 		{
	// 			"item" => StringNames.GlobalTags.Item,
	// 			"food" => StringNames.GlobalTags.Food,
	// 			"decoration" => StringNames.GlobalTags.Decoration,
	// 			"equipment" => StringNames.GlobalTags.Equipment,
	// 			"playground" => StringNames.GlobalTags.Playground,
	// 			"interactable" => StringNames.GlobalTags.Interactable,
	// 			_ => throw new Exception()
	// 		};
	// 		ItemData.ShopOwner _shop = _info[4] switch
	// 		{
	// 			"item" => ItemData.ShopOwner.Item,
	// 			"furniture" => ItemData.ShopOwner.Furniture,
	// 			_ => ItemData.ShopOwner.NoShop
	// 		};
	// 		ItemData _data = new()
	// 		{
	// 			ID = _id,
	// 			Cost = _cost,
	// 			LevelRequirement = _unlockableLevel,
	// 			Tag = _itemTag,
	// 			Shop = _shop,
	// 			ShopOrderPriority = i
	// 		};

	// 		bool _isItem = _itemTag == StringNames.GlobalTags.Item || _itemTag == StringNames.GlobalTags.Food;
	// 		string _resourcePath = (_isItem ? ABSOLUTE_ITEMS_DB_PATH : ABSOLUTE_STRUCTURES_DB_PATH) + _id;
	// 		ResourceSaver.Save(_data, _resourcePath + ".tres");
	// 	}
	// 	return Task.CompletedTask;
	// }
	// public static Task EXPORT_FOOD_DATABASE(string[][] _document)
	// {
	// 	for (int i = 1; i < _document.Length; i++)
	// 	{
	// 		string[] _info = _document[i];

	// 		Dictionary<string, int> _converter = new() { { "speed", 0 }, { "strength", 1 }, { "intelligence", 2 }, { "stamina", 3 } };
	// 		string[] _skills_names = string.IsNullOrWhiteSpace(_info[4]) ? [] : _info[4].Split(','),
	// 		_skills_values = string.IsNullOrWhiteSpace(_info[5]) ? [] : _info[5].Split(',');

	// 		var _keys = Array.ConvertAll(_skills_names, s => (AphidData.SkillEnum)_converter[s]);
	// 		var _values = Array.ConvertAll(_skills_values, int.Parse);

	// 		Godot.Collections.Dictionary<AphidData.SkillEnum, int> _dict = [];
	// 		for (int s = 0; s < _keys.Length; s++)
	// 			_dict.Add(_keys[s], _values[s]);

	// 		FoodData _data = new()
	// 		{
	// 			Item = ResourceLoader.Load<ItemData>(ABSOLUTE_ITEMS_DB_PATH + _info[0] + ".tres"),
	// 			Type = (AphidData.FoodType)int.Parse(_info[1]),
	// 			FoodValue = int.Parse(_info[2]),
	// 			DrinkValue = int.Parse(_info[3]),
	// 			Skills = _dict
	// 		};

	// 		ResourceSaver.Save(_data, ABSOLUTE_FOODS_DB_PATH + _info[0] + ".tres", ResourceSaver.SaverFlags.ReplaceSubresourcePaths);
	// 	}

	// 	return Task.CompletedTask;
	// }
	// public static Task EXPORT_RECIPES_DATABASE(string[][] _document)
	// {
	// 	Dictionary<string, List<string[]>> _recipes = [];
	// 	for (int i = 1; i < _document.Length; i++)
	// 	{
	// 		string[] _info = _document[i];

	// 		if (!_recipes.ContainsKey(_info[0]))
	// 			_recipes.Add(_info[0], [[_info[1], _info[2]]]);
	// 		else
	// 			_recipes[_info[0]].Add([_info[1], _info[2]]);
	// 	}

	// 	foreach (var _pair in _recipes)
	// 	{
	// 		RecipeData _data = new(Owner: ResourceLoader.Load<FoodData>(ABSOLUTE_FOODS_DB_PATH + _pair.Key + ".tres"),
	// 			Combinations: []);

	// 		for (int i = 0; i < _pair.Value.Count; i++)
	// 		{
	// 			GD.Print(ABSOLUTE_FOODS_DB_PATH + _pair.Value[i][0] + ".tres");
	// 			GD.Print(ABSOLUTE_FOODS_DB_PATH + _pair.Value[i][1] + ".tres");
	// 			Godot.Collections.Array<FoodData> _combination = [];
	// 			_combination.Add(ResourceLoader.Load<FoodData>(ABSOLUTE_FOODS_DB_PATH + _pair.Value[i][0] + ".tres"));
	// 			if (!string.IsNullOrWhiteSpace(_pair.Value[i][1]))
	// 				_combination.Add(ResourceLoader.Load<FoodData>(ABSOLUTE_FOODS_DB_PATH + _pair.Value[i][1] + ".tres"));

	// 			_data.Combinations.Add(_combination);
	// 		}

	// 		ResourceSaver.Save(_data, ABSOLUTE_RECIPES_DB_PATH + _pair.Key + ".tres", ResourceSaver.SaverFlags.ReplaceSubresourcePaths);
	// 	}
	// 	return Task.CompletedTask;
	// }

	// MARK: Dedicated Util Functions
	/// <summary>
	/// Emits a set of particles from the database. Automatically disposes of particles upon finish of a oneshot or when moving scenes.
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
		var _particle = (G_PARTICLES.GetResource(_name) as PackedScene).Instantiate() as GpuParticles2D;
		_particle.GlobalPosition = _position;
		_particle.Emitting = true;
		_particle.ProcessMode = ProcessModeEnum.Pausable;
		_parent.AddChild(_particle);

		ACTIVE_PARTICLES_CACHED.Add(_particle);
		if (!_essential && ACTIVE_PARTICLES_CACHED.Count > 20)
			_particle.Hide();
		_particle.Finished += () =>
		{
			ACTIVE_PARTICLES_CACHED.Remove(_particle);
			_particle.QueueFree();
		};
		return _particle;
	}
	public static void CleanAllParticles()
	{
		for (int i = 0; i < ACTIVE_PARTICLES_CACHED.Count; i++)
			ACTIVE_PARTICLES_CACHED[i].QueueFree();
		ACTIVE_PARTICLES_CACHED.Clear();
	}
	/// <summary>
	/// Function used to load resources in the background. In case of error, this function automatically quits the game.
	/// </summary>
	/// <param name="_path">Path to the resource.</param>
	/// <param name="_useSubThreads">Allow resource load using multiple threads, this however, can cause noticeable game stutter.</param>
	/// <returns></returns>
	public static async Task<Resource> PRELOAD_RESOURCE(string _path, bool _useSubThreads = true)
	{
		ResourceLoader.LoadThreadedRequest(_path, "", _useSubThreads);
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
				DebugLogger.Print(DebugLogger.LogPriority.Error, $"PRELOAD_RESOURCE: Resource <{_path.Substring(_path.LastIndexOf('/'))}> is not a valid resource or request.");
			else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
				DebugLogger.Print(DebugLogger.LogPriority.Error, $"PRELOAD_RESOURCE: Resource <{_path.Substring(_path.LastIndexOf('/'))}> is unable to load.");

			Instance.GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
			Instance.GetTree().Quit(2);
			return null;
		}

		return ResourceLoader.LoadThreadedGet(_path);
	}
	public static void THROW_CRASH(Exception _err)
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
		DebugLogger.Print(DebugLogger.LogPriority.Error, _err);
	}
	public static void CREATE_POPUP(string _translation_key, Node _parent)
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
			DebugLogger.Print(DebugLogger.LogPriority.Error, "GlobalManager: Weighted RNG. It did happen :pensive:");
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

		public static string GetTooltipText(string _id)
		{
			return Instance.Tr(_id + "_name") + "\n" +
				Instance.Tr(_id + "_desc");
		}
		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
		}
		/// <summary>
		/// Calls the listeners on the refered list with the given arguments, can optionally clear the list of all listeners after doing so.
		/// </summary>
		public static Task InvokeAwaitableEventListeners<T>(List<Action<T>> _list, T _args, bool _clearOnFinish = false)
		{
			try
			{
				for (int i = 0; i < _list.Count; i++)
					_list[i].Invoke(_args);
			}
			catch (Exception _error)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Error, _error);
			}

			if (_clearOnFinish)
				_list.Clear();
			return Task.CompletedTask;
		}
		public static void InvokeEventListeners<T>(List<Action<T>> _list, T _args, bool _clearOnFinish = false)
		{
			try
			{
				for (int i = 0; i < _list.Count; i++)
					_list[i].Invoke(_args);
			}
			catch (Exception _error)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Error, _error);
			}

			if (_clearOnFinish)
				_list.Clear();
		}
	}
}
