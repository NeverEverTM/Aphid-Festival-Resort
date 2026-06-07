using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// The main processing script, in charge of startup, global variables, scene change and resource loads.
/// It also includes a Utils class for miscellaneous helper functions.
/// </summary>
internal partial class GlobalManager : Node2D
{
	public static GlobalManager Instance { get; private set; }
	/// <summary>
	/// Global game version, used by savefiles.
	/// </summary>
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
		ABSOLUTE_RECIPES_DB_PATH = "res://databases/recipes/",
		ABSOLUTE_FOODS_DB_PATH = "res://databases/food/",
		ABSOLUTE_JOBS_DB_PATH = "res://databases/jobs/";

	// TODO: move this into its own Assets Access
	/// <summary>
	/// =========| GLOBALLY LOADED VALUE DICTIONARY |===========
	/// </summary>
	public static readonly Dictionary<string, ItemData> G_ITEMS = [];
	public static readonly Dictionary<string, ItemData> G_STRUCTURES = [];
	public static readonly Dictionary<string, FoodData> G_FOOD = [];
	public static readonly List<RecipeData> G_RECIPES = [];

	private static readonly Dictionary<string, Texture2D> G_ICONS = [];
	public static readonly Dictionary<string, AudioStream> G_AUDIO = [];
	private static readonly Dictionary<string, Texture2D> G_SKINS = [];
	private static readonly ResourcePreloader G_PARTICLES = new();
	private static readonly Dictionary<string, JobData> G_JOBS = [];

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
	public static JobData GetJob(string _key)
	{
		if (_key != null && G_JOBS.TryGetValue(_key, out JobData value))
			return value;
		else
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, $"GlobalManager: No such <{_key}> job exists");
			return null;
		}
	}
	public static JobData GetRandomJob(JobData.JobDifficulty _difficulty, List<string> _excludeList)
	{
		List<JobData> _jobList = [.. G_JOBS.Values.Where((j) => j.Difficulty == _difficulty).Where((j) => !_excludeList.Contains(j.ID))];

        if (_jobList.Count == 0) // if no jobs were left, get a repeat
        {
            _jobList = [.. G_JOBS.Values.Where((j) => j.Difficulty == _difficulty)];
            DebugLogger.Print(DebugLogger.LogPriority.Debug, "JobMenu: Got a repeat in " + _difficulty.ToString());
        }
		return _jobList[RNG.RandiRange(0, _jobList.Count - 1)];
	}

	private PhysicsDirectSpaceState2D spaceState;
	private readonly static List<GpuParticles2D> ACTIVE_PARTICLES_CACHED = [];
	public readonly static RandomNumberGenerator RNG = new();

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
			await LOAD_DICTIONARY(ABSOLUTE_ICONS_PATH, (_id, _resource) => G_ICONS.Add(_id, _resource as Texture2D), 
				(_id) => !G_ICONS.ContainsKey(_id));
			await LOAD_DICTIONARY_RECURSIVE(ABSOLUTE_SKINS_PATH, (_id, _resource) => G_SKINS.Add(_id, _resource as Texture2D), 
				(_id) => !G_SKINS.ContainsKey(_id));
			await LOAD_DICTIONARY_RECURSIVE(ABSOLUTE_SFX_PATH, (_id, _resource) => G_AUDIO.Add(_id, _resource as AudioStream), 
				(_id) => !G_AUDIO.ContainsKey(_id));
			await LOAD_ITEMS();
			await LOAD_DICTIONARY(ABSOLUTE_FOODS_DB_PATH, (_id, _resource) => G_FOOD.Add(_id, _resource as FoodData));
			await LOAD_LIST(ABSOLUTE_RECIPES_DB_PATH, (_resource) => G_RECIPES.Add(_resource as RecipeData));
			await LOAD_DICTIONARY(ABSOLUTE_JOBS_DB_PATH, (_id, _resource) => { (_resource as JobData).ID = _id; G_JOBS.Add(_id, _resource as JobData); });
			await CACHE_PARTICLES();

			IsBusy = false;
		}
		catch (Exception _err)
		{
			THROW_CRASH(_err);
		}
	}

	private static async Task LOAD_ITEMS()
	{
		string[] _items = DirAccess.GetFilesAt(ABSOLUTE_ITEMS_DB_PATH);

		for (int i = 0; i < _items.Length; i++)
		{
			string _filename = _items[i].Replace(".import", string.Empty);
			if (_filename.EndsWith(".tscn"))
				continue;
			string _id = _filename.Split('.')[0];

			ItemData _data = ResourceLoader.Load<ItemData>(ABSOLUTE_ITEMS_DB_PATH + _filename);
#if DEBUG
			if (_data.Type == ItemData.ItemType.Structure && await STRUCTURE_CHECK_FAILED(_id))
				continue;
			else if (await ITEM_CHECK_FAILED(_id))
				continue;
			await TRANSLATION_CHECK(_id);
#endif
			if (_data.Type == ItemData.ItemType.Structure)
			{
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
				G_STRUCTURES.Add(_id, _data);
				_node.QueueFree();
			}
			else
			{
				G_ITEMS.Add(_id, _data);
			}
		}
		return;
	}
#if DEBUG
	private static Task<bool> STRUCTURE_CHECK_FAILED(string _id)
	{
		if (G_STRUCTURES.ContainsKey(_id))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"StructureDatabase: <{_id}> is duplicated.");
			return Task.FromResult(true);
		}

		return Task.FromResult(false);
	}
	private static Task<bool> ITEM_CHECK_FAILED(string _id)
	{
		if (G_ITEMS.ContainsKey(_id))
		{
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"ItemDatabase: <{_id}> is duplicated.");
			return Task.FromResult(true);
		}

		return Task.FromResult(false);
	}
	private static Task TRANSLATION_CHECK(string _id)
	{
		if (Instance.Tr(_id + "_name") == _id + "_name")
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"Database: <{_id}> has no name.");
		if (Instance.Tr(_id + "_desc") == _id + "_desc")
			DebugLogger.Print(DebugLogger.LogPriority.Warning, $"Database: <{_id}> has no description.");
		return Task.CompletedTask;
	}
#endif	

	private static async Task CACHE_PARTICLES()
	{
		var _particleList = DirAccess.GetFilesAt(ABSOLUTE_PARTICLES_PATH);

		for (int i = 0; i < _particleList.Length; i++)
		{
			var _resource = await PRELOAD_RESOURCE(ABSOLUTE_PARTICLES_PATH + _particleList[i]);
			var _particle = (_resource as PackedScene).Instantiate() as GpuParticles2D;

			// cache particle to memory
			Instance.AddChild(_particle);
			await Task.Delay(2);
			_particle.QueueFree();

			G_PARTICLES.AddResource(_particleList[i].Split('.')[0], _resource);
		}
	}
	
	private static async Task LOAD_DICTIONARY(string _absolutePath, Action<string, Resource> _addAction, Func<string, bool> _validIDCheck = null, string _directory = null)
	{
		string[] _files = DirAccess.GetFilesAt(_absolutePath);

		for (int i = 0; i < _files.Length; i++)
		{
			string _filename = _files[i].Replace(".import", string.Empty),
				_id = (_directory != null ? _directory + "/" : string.Empty) + _filename.Split('.')[0];

			if (_validIDCheck != null && !_validIDCheck(_id))
				continue;

			var _resource = await PRELOAD_RESOURCE(_absolutePath + _filename);
			_addAction.Invoke(_id, _resource);
			DebugLogger.Print(DebugLogger.LogPriority.Debug, $"ResourceLoad: At <{_absolutePath}> Filename: {_filename} ID: {_id}");
		}
	}
	private static async Task LOAD_LIST(string _absolutePath, Action<Resource> _addAction)
	{
		string[] _files = DirAccess.GetFilesAt(_absolutePath);

		for (int i = 0; i < _files.Length; i++)
		{
			string _filename = _files[i].Replace(".import", string.Empty);

			var _resource = await PRELOAD_RESOURCE(_absolutePath + _filename);
			_addAction(_resource);
		}
	}
	private static async Task LOAD_DICTIONARY_RECURSIVE(string _absolutePath, Action<string, Resource> _addAction, Func<string, bool> _validIDCheck = null)
	{
		string[] _directories = DirAccess.GetDirectoriesAt(_absolutePath);
		for (int i = 0; i < _directories.Length; i++)
			await LOAD_DICTIONARY(_absolutePath + _directories[i] + "/", _addAction, _validIDCheck, _directories[i]);
	}

	// MARK: Dedicated Util Functions
	/// <summary>
	/// Emits a set of particles from the database. Automatically disposes of particles upon finish of a oneshot or when moving scenes.
	/// </summary>
	/// <param name="_name">Name of the particle scene.</param>
	/// <param name="_position">Global position for the particle</param>
	/// <param name="_parentless">Adds it as a child of the root instead of the scene, it will still be disposed off after a scene reload.</param>
	/// <param name="_essential">Particles marked as "non-essential" will not spawn when the particle limit has been reached.</param>
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
		public static Godot.Collections.Dictionary RaycastBetween(Vector2 from, Vector2 to, Godot.Collections.Array<Rid> _excludeList)
		{
			var query = PhysicsRayQueryParameters2D.Create(from, to);
			query.HitFromInside = true;
			query.Exclude = _excludeList;
			return Instance.spaceState.IntersectRay(query);
		}
		public static Godot.Collections.Dictionary RaycastTowards(Vector2 _position, Vector2 _direction, Godot.Collections.Array<Rid> _excludeList)
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
		public static List<Godot.Collections.Dictionary> RaycastRect(Rect2 _rect, Godot.Collections.Array<Rid> _excludeList)
		{
			Vector2 _topRight = _rect.Position + new Vector2(_rect.Size.X, 0), _bottomLeft = _rect.Position + new Vector2(0, _rect.Size.Y);

			Vector2[][] _points =
			[
				[ _rect.Position, _topRight ],
				[ _topRight, _rect.End, ],
				[ _rect.End, _bottomLeft ],
				[ _bottomLeft, _rect.Position ],
				[ _rect.Position, _rect.End ],
				[  _bottomLeft, _topRight ],
			];

			List<Godot.Collections.Dictionary> _intersectedColliders = [];

			for (int i = 0; i < _points.Length; i++)
			{
				var query = PhysicsRayQueryParameters2D.Create(_points[i][0], _points[i][1]);
				query.HitFromInside = true;
				query.Exclude = _excludeList;

				var _collisionCheck = Instance.spaceState.IntersectRay(query);
				if (_collisionCheck.Count > 0)
					_intersectedColliders.Add(_collisionCheck);
				else
					_intersectedColliders.Add(null);
			}
	
			return _intersectedColliders;
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
