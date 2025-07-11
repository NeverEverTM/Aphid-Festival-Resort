using Godot;

public partial class FieldManager : Node2D
{
	public static FieldManager Instance { get; set; }

	[Export] private CanvasModulate ColorCube;
	[Export] public bool IsInside = false;
	[Export] public Node2D TopLeft, BottomRight;
	[Export] public RoomDoor[] Doors = [];

	public enum DayHours { Morning, Noon, Sunset, Night }
	public static DayHours TimeOfDay { get; set; }
	public static readonly Color[] DayFilters =
	[
		new(0.706f, 0.933f, 0.992f),
		new(1, 1, 1),
		new(0.984f, 0.62f, 0.553f),
		new(0.133f, 0.298f, 0.592f)
	];
	public readonly static Color[] WeatherColors =
	[
		new(0.22f, 0.608f, 0.898f), // Morning
		new(0.984f, 0.796f, 0.039f), // Noon
		new(0.987f, 0.371f, 0), // Sunset
		new(0.435f, 0.33f, 0.823f) // Night
	];

	public delegate void HourlyCall();
	public static HourlyCall OnTimeChange { get; set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		Timer _timeloop = new()
		{
			OneShot = true
		};
		_timeloop.Timeout += () =>
		{
			SetTime(false);
			StartWeatherPopup();

			// start timeloop to popup after every change of hour
			var _date = Time.GetDatetimeDictFromSystem();
			_timeloop.ProcessMode = ProcessModeEnum.Always;
			_timeloop.Start(((60 - (int)_date["minute"]) * 60) - (int)_date["second"]);
		};
		AddChild(_timeloop);
		_timeloop.Start();

		CameraManager.Instance.LimitTop = (int)TopLeft.GlobalPosition.Y;
		CameraManager.Instance.LimitBottom = (int)BottomRight.GlobalPosition.Y;
		CameraManager.Instance.LimitLeft = (int)TopLeft.GlobalPosition.X;
		CameraManager.Instance.LimitRight = (int)BottomRight.GlobalPosition.X;

		SetTime(true, null);
	}

	// opens weather overlay and creates timer to hide it automatically
	public void StartWeatherPopup()
	{
		if (IsInstanceValid(CanvasManager.Instance))
			CanvasManager.OpenWeather(WeatherColors[(int)TimeOfDay]);
		Timer _timer = new()
		{
			OneShot = true
		};
		_timer.Timeout += () =>
		{
			if (IsInstanceValid(CanvasManager.Instance))
				CanvasManager.CloseWeather();
		};
		AddChild(_timer);
		_timer.Start(5);

	}
	// sets the atmosphere according to time and weather and triggers hour change events
	public void SetTime(bool _instantTransition, Godot.Collections.Dictionary _date = null)
	{
		_date ??= Time.GetDatetimeDictFromSystem();

		DayHours timeDay = DayHours.Night;
		byte _hour = (byte)_date["hour"];
		// 8PM to 6AM is Night 
		if (_hour >= 6 && _hour < 20)
		{
			if (_hour < 10) // 6AM to 10AM is Morning
				timeDay = DayHours.Morning;
			else if (_hour < 16) // 10AM to 4PM is Noon
				timeDay = DayHours.Noon;
			else // 4PM to 8PM is Sunset
				timeDay = DayHours.Sunset;
		}
		if (!IsInside)
		{
			if (!_instantTransition)
			{
				Tween _tween = GetTree().CreateTween();
				_tween.BindNode(ColorCube);
				_tween.TweenProperty(ColorCube, "color", DayFilters[(int)timeDay], 3);
			}
			else
				ColorCube.Color = DayFilters[(int)timeDay];
		}
		TimeOfDay = timeDay;
		OnTimeChange?.Invoke();
	}
}
