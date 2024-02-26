using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class GameManager : Node2D
{
	public static GameManager Instance;
	public static Camera2D GlobalCamera;
	public readonly static RandomNumberGenerator RNG = new();

	public static bool IsBusy = true;

	public const string
		ResortScenePath = "res://scenes/resort.tscn",
		MenuScenePath = "res://scenes/menu.tscn",
		LoadingScreenPath = "res://scenes/loading_screen.tscn",
		ItemPath = "res://items", 
		IconPath = "res://sprites/icons",
		SkinsPath = "res://skins",
		DatabasesPath = "res://databases";

	// =========| LOADED VALUES |===========
	public static readonly Dictionary<string, Texture2D> ICONS = new();
	public static readonly Dictionary<string, Item> ITEMS = new();
	public struct Item
	{
		public int cost;
		public int unlockableLevel;
	}

	// Variables
	private Vector2 windowSize;
	private PhysicsDirectSpaceState2D spaceState;
	private readonly List<GpuParticles2D> particles = new();

	public static Label BOOT_LOADING_LABEL;

    public override void _EnterTree()
    {
        Instance = this;

		GetViewport().SizeChanged += OnSizeChange;
		OnSizeChange();
    }
    public override void _Ready()
    {
		// Runtime Params
		spaceState = GetWorld2D().DirectSpaceState;
    }
    public override void _Process(double delta)
    {
		for (int i = 0; i < particles.Count; i++)
		{
			if (particles[i].Emitting)
				continue;
			particles[i].QueueFree();
			particles.RemoveAt(i);
		}

		if (Input.IsActionJustPressed("debug_0"))
			_ = SaveSystem.SaveProfile();
    }

	/// <summary>
	/// Initializes primary systems before main menu boot.
	/// </summary>
	public async static Task INTIALIZE_GAME_PROCESS()
	{
		try
		{
			BOOT_LOADING_LABEL.Text = Instance.Tr("BOOT_0");
			SaveSystem.CreateBaseDirectories();
			SaveSystem.SetProfile("default");
			await SaveSystem.CreateProfile();
			BOOT_LOADING_LABEL.Text = Instance.Tr("BOOT_1") + " (0/?)";
			await LOAD_ICONS();
			BOOT_LOADING_LABEL.Text = Instance.Tr("BOOT_2") + " (0/?)";
			await LOAD_SKINS();
			BOOT_LOADING_LABEL.Text = Instance.Tr("BOOT_3");
			await LOAD_DATABASES();
			BOOT_LOADING_LABEL.Visible = false;
			IsBusy = false;
		}
		catch (Exception _err)
		{
			GD.PrintErr(_err);
			Instance.GetTree().Quit(1);
		}
	}
	private async static Task LOAD_ICONS()
	{
		string[] _icons = DirAccess.GetFilesAt(IconPath);
		string _tr = Instance.Tr("BOOT_1");
		ResourceLoader.ThreadLoadStatus _status;
		BOOT_LOADING_LABEL.Text = $"{_tr} (0/{_icons.Length})";

		for(int i = 0; i < _icons.Length; i++)
		{
			string _path = $"{IconPath}/{_icons[i]}";
			ResourceLoader.LoadThreadedRequest(_path, "", true);
			_status = ResourceLoader.LoadThreadedGetStatus(_path);
			while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
			{
				await Task.Delay(1);
				_status = ResourceLoader.LoadThreadedGetStatus(_path);
			}

			if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
			{
				if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
					GD.PrintErr($"Item <{_icons[i]}> is not a valid resource or request.");
				else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
					GD.PrintErr($"Icon <{_icons[i]}> esd not able to be loaded.");

				Instance.GetTree().Quit(2);
			}

			ICONS.Add(_icons[i].Remove(_icons[i].Length - 5), ResourceLoader.LoadThreadedGet(_path) as Texture2D);	
			BOOT_LOADING_LABEL.Text = $"{_tr} ({i + 1}/{_icons.Length})";
		}
	}
	private async static Task LOAD_SKINS()
	{
		string[] _skins = DirAccess.GetFilesAt(SkinsPath);
		string _tr = Instance.Tr("BOOT_2");
		ResourceLoader.ThreadLoadStatus _status;
		BOOT_LOADING_LABEL.Text = $"{_tr} (0/{_skins.Length})";

		for(int i = 0; i < _skins.Length; i++)
		{
			if (_skins[i].EndsWith(".import"))
				continue;

			string _path = $"{SkinsPath}/{_skins[i]}";
			ResourceLoader.LoadThreadedRequest(_path, "", true);
			_status = ResourceLoader.LoadThreadedGetStatus(_path);
			while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
			{
				await Task.Delay(1);
				_status = ResourceLoader.LoadThreadedGetStatus(_path);
			}

			if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
			{
				if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
					GD.PrintErr($"Item <{_skins[i]}> is not a valid resource or request.");
				else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
					GD.PrintErr($"Icon <{_skins[i]}> esd not able to be loaded.");

				Instance.GetTree().Quit(2);
			}

			BOOT_LOADING_LABEL.Text = $"{_tr} ({i + 1}/{_skins.Length})";
		}
	}
	private static Task LOAD_DATABASES()
	{
		// Get Items
		FileAccess _file = FileAccess.Open(DatabasesPath + "/items_values.csv", FileAccess.ModeFlags.Read);
		while(_file.GetPosition() < _file.GetLength())
		{
			Item _item = new()
			{
				cost = 0,
				unlockableLevel = 0
			};
			ITEMS.Add("name", _item);
		}
		return Task.CompletedTask;
	}

	public void CleanAllParticles()
	{
		for (int i = 0; i < particles.Count; i++)
			particles[i].QueueFree();
		particles.Clear();
	}
	private void OnSizeChange()
	{
		windowSize = GetViewport().GetVisibleRect().Size;
		windowSize = -windowSize * 0.5f;
	}

    // =====| General Utils |=====
	public async static Task LoadScene(string _path)
	{
		IsBusy = true;
		Instance.CleanAllParticles();

		// load the load screen
		LoadScreen _loading = ResourceLoader.Load<PackedScene>(LoadingScreenPath).Instantiate() as LoadScreen;
		Instance.GetTree().Root.AddChild(_loading);
		await _loading.CreateLeaves();

		// await for scene creation
		ResourceLoader.LoadThreadedRequest(_path, "", true);
		ResourceLoader.ThreadLoadStatus _status = ResourceLoader.LoadThreadedGetStatus(_path);

		while (_status == ResourceLoader.ThreadLoadStatus.InProgress)
		{
			await Task.Delay(1);
			_status = ResourceLoader.LoadThreadedGetStatus(_path);
		}

		if (_status != ResourceLoader.ThreadLoadStatus.Loaded)
		{
			if (_status == ResourceLoader.ThreadLoadStatus.InvalidResource)
				GD.PrintErr($"Item at <{_path}> is not a valid resource or request.");
			else if (_status == ResourceLoader.ThreadLoadStatus.Failed)
				GD.PrintErr($"Scene at <{_path}> was not able to be loaded.");

			IsBusy = false;
			Instance.GetTree().Quit(2);
		}
		Instance.GetTree().ChangeSceneToPacked(ResourceLoader.LoadThreadedGet(_path) as PackedScene);
		Instance.RequestReady();
		await _loading.SweepLeaves();
		IsBusy = false;
    }

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
	public static Vector2 GetMouseGlobalPosition()
	{
		return GlobalCamera.GlobalPosition + (Instance.GetViewport().GetMousePosition() + Instance.windowSize) * 0.5f;
	}
	public static GpuParticles2D EmitParticles(PackedScene _particles, Vector2 _position)
	{
		var _item = _particles.Instantiate() as GpuParticles2D;
		_item.GlobalPosition = _position;
		_item.Emitting = true;

		Instance.AddChild(_item);
		Instance.particles.Add(_item);

		return _item;
	}
	public static int GetRandomByWeight(RandomNumberGenerator _engine, float[] weights)
	{
		// We take a sum of all weights
		float _total = 0;
		Array.ForEach(weights, (float _weight) => {
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
	public static Color GetRandomColor(bool _capColorLoss = true, bool _randomizeAlpha = false) 
	{
		byte[] _rgba = new byte[] { (byte)RNG.RandiRange(0,255), (byte)RNG.RandiRange(0,255), 
		(byte)RNG.RandiRange(0,255), _randomizeAlpha ? (byte)(RNG.RandiRange(0,205) + 50) : (byte)255 }; 
		
		if (_capColorLoss && _rgba[0] < 100 && _rgba[1] < 100 && _rgba[2] < 100)
			_rgba[RNG.RandiRange(0, 2)] = 100;
		
		return Color.Color8(_rgba[0], _rgba[1], _rgba[2], _rgba[3]);
	}
	public static Vector2 GetRandomVector(float _rangeMin, float _rangeMax) => new(RNG.RandfRange(_rangeMin, _rangeMax), RNG.RandfRange(_rangeMin, _rangeMax));
	public static Vector2 GetRandomVector_X(float _rangeMin, float _rangeMax, float _Y = 0) => new(RNG.RandfRange(_rangeMin, _rangeMax), _Y);
	public static Vector2 GetRandomVector_Y(float _rangeMin, float _rangeMax, float _X = 0) => new(_X, RNG.RandfRange(_rangeMin, _rangeMax));
}