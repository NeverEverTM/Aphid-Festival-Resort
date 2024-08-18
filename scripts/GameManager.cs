using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Godot;

public partial class GameManager : Node2D
{
	public const int GAME_VERSION = 130;

	public static GameManager Instance { get; private set; }
	public static Camera2D GlobalCamera { get; set; }
	public readonly static RandomNumberGenerator RNG = new();

	public delegate void LoadSceneHandler();
	public static event LoadSceneHandler OnPreLoadScene, OnPostLoadScene;

	public enum SceneName { Resort, Menu }
	public static SceneName Scene { get; private set; }

	public static bool IsBusy = true;

	public const string
		ResortScenePath = "res://scenes/resort.tscn",
		MenuScenePath = "res://scenes/menu.tscn",
		LoadingScreenPath = "res://scenes/ui/loading_screen.tscn",
		ConfirmationWindowPath = "res://scenes/ui/confirmation_panel.tscn",
		PopupWindowPath = "res://scenes/ui/popup.tscn",
		OutlineShader = "res://scripts/shaders/outline.gdshader";
	public const string
		MusicPath = "res://sfx/music",
		SFXPath = "res://sfx",
		IconPath = "res://sprites/icons",
		ItemPath = "res://databases/items",
		SkinsPath = "res://databases/skins",
		StructuresPath = "res://databases/structures",
		DatabasesPath = "res://databases";

	// =========| LOADED VALUES |===========
	public static readonly Dictionary<string, Texture2D> G_ICONS = new();
	public static readonly Dictionary<string, Item> G_ITEMS = new();
	public static readonly Dictionary<string, Food> G_FOOD = new();
	public static readonly List<Recipe> G_RECIPES = new();
	public static readonly Dictionary<string, string> G_STRUCTURES = new();
	public readonly struct Item
	{
		public readonly int cost;
		public readonly int unlockableLevel;
		public readonly string tag;
		public readonly string shopTag;
		public Item(int cost, int unlockableLevel, string tag, string shopTag)
		{
			this.cost = cost;
			this.unlockableLevel = unlockableLevel;
			this.tag = tag;
			this.shopTag = shopTag;
		}
	}
	public readonly struct Food
	{
		public readonly AphidData.FoodType type;
		public readonly float food_value;
		public readonly float drink_value;
		public Food(AphidData.FoodType type, float food_value, float drink_value)
		{
			this.type = type;
			this.food_value = food_value;
			this.drink_value = drink_value;
		}
	}
	public readonly struct Recipe
	{
		public readonly string Result;
		public readonly string Ingredient1;
		public readonly string Ingredient2;
		public Recipe(string result, string ingredient1, string ingredient2)
		{
			Result = result;
			Ingredient1 = ingredient1;
			Ingredient2 = ingredient2;
		}
	}

	public static Texture2D GetIcon(string _key)
	{
		if (G_ICONS.ContainsKey(_key))
			return G_ICONS[_key];
		else
			return G_ICONS["missing"];
	}
	public static string GetStructurePath(string _key)
	{
		if (G_ICONS.ContainsKey(_key))
			return G_STRUCTURES[_key];
		else
			return G_STRUCTURES["missing"];
	}

	// Variables
	public static Vector2 ScreenCenter { get; private set; }
	public static Vector2 ScreenSize { get; private set; }
	private PhysicsDirectSpaceState2D spaceState;
	private readonly List<GpuParticles2D> particles = new();

	public static Label BOOT_LOADING_LABEL;

	public override void _EnterTree()
	{
		Instance = this;
		Scene = SceneName.Menu;

		GetViewport().SizeChanged += OnSizeChange;
		OnSizeChange();
#if DEBUG
		Logger.LogMode = Logger.LogLevel.Verbose;
#endif
	}
	////// GAMEMANAGER READY GETS CALLED WHEN LOADING NEW ROOT SCENE
	public override void _Ready()
	{
		// Runtime Params
		spaceState = GetWorld2D().DirectSpaceState;
	}
	public override void _Process(double delta)
	{
		if (Scene == SceneName.Resort)
			GameData.Data.Playtime += (float)delta;
		// Cleans particles periodically once finished
		for (int i = 0; i < particles.Count; i++)
		{
			if (particles[i].Emitting)
				continue;
			particles[i].QueueFree();
			particles.RemoveAt(i);
		}
	}
	private void OnSizeChange()
	{
		ScreenSize = GetViewport().GetVisibleRect().Size;
		ScreenCenter = ScreenSize * 0.5f;
	}
	// MARK: Game Initialization
	
	/// <summary>
	/// Pre-initalization checks and setup
	/// </summary>
	public async static Task PREPARE_GAME_PROCESS()
	{
		SaveSystem.CreateBaseDirectories();
		await SaveSystem.LoadGlobalData();
	}

	/// <summary>
	/// Initializes primary systems and loads values to memory. MainMenu triggers it as part of its wake up.
	/// In order to be called again, BOOT_LOADING_LABEL must be set to a valid text display node.
	/// </summary>
	public async static Task INTIALIZE_GAME_PROCESS()
	{
		// show loading screen animation
		Control _node = BOOT_LOADING_LABEL.GetParent() as Control;
		_node.Visible = true;
		(_node.GetChild(1) as AnimatedSprite2D).Play("default");

		try
		{
			await LOAD_ICONS();
			await LOAD_SKINS();
			await LOAD_SFX();
			await LOAD_DATA();
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
		string[] _icons = DirAccess.GetFilesAt(IconPath);
		ResourceLoader.ThreadLoadStatus _status;

		for (int i = 0; i < _icons.Length; i++)
		{
			string _path = $"{IconPath}/{_icons[i]}";
			ResourceLoader.LoadThreadedRequest(_path, "", true);
			_status = ResourceLoader.LoadThreadedGetStatus(_path);

			await Task.Delay(1);
			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_0")} ({i + 1}/{_icons.Length})";

			// Wait until it yields
			while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
				_status = ResourceLoader.LoadThreadedGetStatus(_path);

			// action state: if anything but good, close application
			if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
			{
				if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
					GD.PushError($"Item <{_icons[i]}> is not a valid resource or request.");
				else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
					GD.PushError($"Icon <{_icons[i]}> was not able to be loaded.");

				Instance.GetTree().Quit(2);
			}

			G_ICONS.Add(_icons[i].Remove(_icons[i].Length - 5), ResourceLoader.LoadThreadedGet(_path) as Texture2D);
		}
	}
	private static async Task LOAD_SKINS()
	{
		string[] _skins = DirAccess.GetFilesAt(SkinsPath);
		ResourceLoader.ThreadLoadStatus _status;

		for (int i = 0; i < _skins.Length; i++)
		{
			if (_skins[i].EndsWith(".import"))
				continue;

			await Task.Delay(1);
			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_1")} ({i + 1}/{_skins.Length})";

			// start thread and await for its response
			string _path = $"{SkinsPath}/{_skins[i]}";
			ResourceLoader.LoadThreadedRequest(_path, "", true);
			_status = ResourceLoader.LoadThreadedGetStatus(_path);
			while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
				_status = ResourceLoader.LoadThreadedGetStatus(_path);

#if DEBUG
			await Task.Delay(1);
#endif

			// action states
			if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
			{
				if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
					GD.PrintErr($"Item <{_skins[i]}> is not a valid resource or request.");
				else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
					GD.PrintErr($"Icon <{_skins[i]}> was not able to be loaded.");

				Instance.GetTree().Quit(2);
			}
		}
	}
	private static async Task LOAD_SFX()
	{
		// Get all SFX paths (only checks folders at SFX root folder)
		List<string> _sfx = new();
		string[] _directories = DirAccess.GetDirectoriesAt(SFXPath);
		for (int i = 0; i < _directories.Length; i++)
			SEARCH_SFX_FOLDER(SFXPath + $"/{_directories[i]}", ref _sfx);
		SEARCH_SFX_FOLDER(SFXPath, ref _sfx);

		// Begin loading all sfx assets
		string[] _resources = _sfx.ToArray();
		ResourceLoader.ThreadLoadStatus _status;

		for (int i = 0; i < _resources.Length; i++)
		{
			// Start thread and wait for its response
			ResourceLoader.LoadThreadedRequest(_resources[i], "", true);
			_status = ResourceLoader.LoadThreadedGetStatus(_resources[i]);
			while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
				_status = ResourceLoader.LoadThreadedGetStatus(_resources[i]);

			await Task.Delay(1);
			BOOT_LOADING_LABEL.Text = $"{Instance.Tr("BOOT_2")} ({i + 1}/{_resources.Length})";

			// action states
			if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
			{
				if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
					GD.PrintErr($"Item <{_resources[i]}> is not a valid resource or request.");
				else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
					GD.PrintErr($"Icon <{_resources[i]}> was not able to be loaded.");

				Instance.GetTree().Quit(2);
			}
		}

		// Load aphid SFX
		string _path = $"{SFXPath}/aphid/";
		Aphid.Audio_Nom = ResourceLoader.Load<AudioStream>(_path + "nom.wav");
		Aphid.Audio_Idle = ResourceLoader.Load<AudioStream>(_path + "idle.wav");
		Aphid.Audio_Idle_Baby = ResourceLoader.Load<AudioStream>(_path + "baby_idle.wav");
		Aphid.Audio_Step = ResourceLoader.Load<AudioStream>(_path + "step.wav");
		Aphid.Audio_Jump = ResourceLoader.Load<AudioStream>(_path + "jump.wav");
		Aphid.Audio_Hurt = ResourceLoader.Load<AudioStream>(_path + "hurt.wav");
		Aphid.Audio_Boing = ResourceLoader.Load<AudioStream>(_path + "boing.wav");

		// Player SFX
		Player.Audio_Whistle = ResourceLoader.Load<AudioStream>(SFXPath + "/whistle.wav");
	}
	private static void SEARCH_SFX_FOLDER(string _path, ref List<string> _sfx)
	{
		string[] _files = DirAccess.GetFilesAt(_path);
		for (int a = 0; a < _files.Length; a++)
		{
			if (_files[a].EndsWith(".import"))
				continue;
			_sfx.Add($"{_path}/{_files[a]}");
		}
	}
	private static async Task LOAD_DATA()
	{
		await LOAD_DATABASE("items", (string[] _info) =>
		{
			G_ITEMS.Add(_info[0], new(
				cost: int.Parse(_info[1]),
				unlockableLevel: int.Parse(_info[2]),
				tag: _info[3].ToString(),
				shopTag: _info[4].ToString()
			));
		});
		await LOAD_DATABASE("foods", (string[] _info) =>
		{
			G_FOOD.Add(_info[0], new(
				type: (AphidData.FoodType)int.Parse(_info[1]),
				food_value: float.Parse(_info[2]),
				drink_value: float.Parse(_info[3])
			));
		});
		await LOAD_DATABASE("recipes", (string[] _info) =>
		{
			G_RECIPES.Add(new Recipe(
				_info[0],
				_info[1],
				_info[2]
			));
		});
	}
	private static async Task LOAD_DATABASE(string _fileName, Action<string[]> _onItem)
	{
		FileAccess _file = FileAccess.Open(DatabasesPath + $"/{_fileName}.csv", FileAccess.ModeFlags.Read);
		string _header = _file.GetCsvLine()[0], _boot = Instance.Tr($"BOOT_{_header}");
		while (_file.GetPosition() < _file.GetLength())
		{
			_onItem(_file.GetCsvLine());
			await Task.Delay(1);
			BOOT_LOADING_LABEL.Text = $"{_boot} ({(int)((float)_file.GetPosition() / (float)_file.GetLength() * 100)}%)";
		}
	}

	// MARK: Dedicated Functions
	public static GpuParticles2D EmitParticles(PackedScene _particles, Vector2 _position)
	{
		var _item = _particles.Instantiate() as GpuParticles2D;
		_item.GlobalPosition = _position;
		_item.Emitting = true;

		Instance.AddChild(_item);
		Instance.particles.Add(_item);

		return _item;
	}
	public static GpuParticles2D EmitParticles(string _name, Vector2 _position) =>
		EmitParticles(ResourceLoader.Load<PackedScene>($"res://scenes/particles/{_name}.tscn"), _position);
	public void CleanAllParticles()
	{
		for (int i = 0; i < particles.Count; i++)
			particles[i].QueueFree();
		particles.Clear();
	}

	public static void CleanSaveData()
	{
		SaveSystem.Aphids.Clear();
		SaveSystem.ProfileData.Clear();
	}
	public async static Task LoadScene(SceneName _scene)
	{
		Scene = _scene;
		string _path = _scene switch
		{
			SceneName.Menu => MenuScenePath,
			SceneName.Resort => ResortScenePath,
			_ => "N/A"
		};

		if (_path == "N/A")
			return;

		IsBusy = true;
		OnPreLoadScene?.Invoke();
		Instance.CleanAllParticles();
		SoundManager.CleanAllSounds();

		// load the load screen
		LoadScreen _loading = ResourceLoader.Load<PackedScene>(LoadingScreenPath).Instantiate() as LoadScreen;
		Instance.GetTree().Root.AddChild(_loading);
		await _loading.CreateLeaves();

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

		// set scene and request ready/events
		Instance.GetTree().ChangeSceneToPacked(ResourceLoader.LoadThreadedGet(_path) as PackedScene);
		Instance.RequestReady();
		OnPostLoadScene?.Invoke();
		await Task.Delay(1);
		IsBusy = false;
		await _loading.SweepLeaves();
	}
	public static void CreatePopup(string _translation_key, Node _parent)
	{
		Control _popup = ResourceLoader.Load<PackedScene>(PopupWindowPath).Instantiate() as Control;
		_popup.Position = ScreenCenter - _popup.Size / 2;
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
	public static int GetRandomByWeight(RandomNumberGenerator _engine, float[] weights)
	{
		// We take a sum of all weights
		float _total = 0;
		Array.ForEach(weights, (float _weight) =>
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
		GD.Print("It did happen :pensive:");
		return 0; // Should in theory, never happen
	}
	public static int GetRandomByWeight(float[] weights) =>
		GetRandomByWeight(RNG, weights);
	public static class Utils
	{
		public static Godot.Collections.Dictionary Raycast(Vector2 _position, Vector2 _direction, Godot.Collections.Array<Rid> _excludeList)
		{
			var query = PhysicsRayQueryParameters2D.Create(_position, _position + _direction);
			query.Exclude = _excludeList;
			return Instance.spaceState.IntersectRay(query);
		}
		public static Godot.Collections.Dictionary Raycast(PhysicsRayQueryParameters2D _query, Vector2 _position, Vector2 _direction)
		{
			_query.From = _position;
			_query.To = _position + _direction;
			return Instance.spaceState.IntersectRay(_query);
		}
		/// <returns>The mouse position translated to global position/returns>
		public static Vector2 GetMouseToWorldPosition() => GlobalCamera.GlobalPosition + (Instance.GetViewport().GetMousePosition() - ScreenCenter) * 0.5f;
		public static Vector2 GetMouseToCanvasCenter() => Instance.GetViewport().GetMousePosition() - ScreenCenter;
		/// <summary>
		/// Translates world position to canvas coordinates.
		/// </summary>
		/// <returns>A 2D Vector of an objects position translated to canvas coordinates</returns>
		public static Vector2 GetWorldToCanvasPosition(Vector2 _position) => ScreenCenter + (_position - GlobalCamera.GlobalPosition) * 2;
		public static Color GetRandomColor(bool _randomizeAlpha = false)
		{
			byte[] _rgba = new byte[] { (byte)RNG.RandiRange(0,255), (byte)RNG.RandiRange(0,255),
				(byte)RNG.RandiRange(0,255), _randomizeAlpha ? (byte)(RNG.RandiRange(0,205) + 50) : (byte)255 };

			return Color.Color8(_rgba[0], _rgba[1], _rgba[2], _rgba[3]);
		}
		public static Color LerpColor(Color _color1, Color _color2) =>
			_color1.Blend(_color2);

		public static Vector2 GetRandomVector(float _rangeMin, float _rangeMax) => new(RNG.RandfRange(_rangeMin, _rangeMax), RNG.RandfRange(_rangeMin, _rangeMax));
		public static Vector2 GetRandomVector_X(float _rangeMin, float _rangeMax, float _Y = 0) => new(RNG.RandfRange(_rangeMin, _rangeMax), _Y);
		public static Vector2 GetRandomVector_Y(float _rangeMin, float _rangeMax, float _X = 0) => new(_X, RNG.RandfRange(_rangeMin, _rangeMax));
	}
}

public class StateMachine
{
	public IState currentState;

	public void ChangeState(IState _newState, EventArgs args)
	{
		currentState?.ExitState(args);
		_newState.EnterState(currentState, args);
		currentState = _newState;
	}

	public bool IsState(string _state) =>
		currentState.Name.Equals(_state);

	public interface IState
	{
		public string Name { get; }
		public void EnterState(IState _previous, EventArgs e);
		public void UpdateState(EventArgs e);
		public void ExitState(EventArgs e);
	}
}