using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Contains logic for the current room instance, such as time changes, upgrade application, etc.
/// </summary>
public partial class RoomInstance : Node2D
{
	public static RoomInstance Instance { get; set; }
	private bool initialized = false;

	[Export] private CanvasModulate ColorCube;
	/// <summary>
	/// If this field is "inside", then it is not affected by hourly colorcubes
	/// </summary>
	[Export] public bool IsInside = false;
	[Export] public Node2D TopLeft, BottomRight;
	[Export] public RoomDoor[] Doors = [];

	public static Rect2 RoomBounds;
	public enum DayHourMode { Morning, Noon, Afternoon, Sunset, Night }
	public static DayHourMode TimeOfDay { get; set; }
	/// <summary>
	/// Colorcube colors for the current field depending on the hour.
	/// </summary>
	public static readonly Color[] DayFilters =
	[
		new(0x418980FF), // Morning
		new(0xFFFFFFFF), // Noon
		new(0xfc8d83FF), // Afternoon
		new(0x9b4daaFF), // Sunset
		new(0x1d2b87FF)  // Night
	];
	private readonly Dictionary<TimeEvents, List<Action<TimeArgs>>> Events = new()
	{
		{ TimeEvents.OnTimeChange, new() },
		{ TimeEvents.OnHourChange, new() },
	};
	public enum TimeEvents
	{
		/// <summary>
		/// Whenever SetTime is called. ex. on room load, on the hourly check, via commands, etc.
		/// </summary>
		OnTimeChange,
		/// <summary>
		/// Called after the DayHour changes and the colorcube is fully interpolated. ex. going from Morning to Noon.
		/// </summary>
		OnHourChange
	}
	public class TimeArgs(DayHourMode LastTime, bool Initialized)
	{
		public static DayHourMode TimeOfDay { get => RoomInstance.TimeOfDay; }
		public DayHourMode LastTime = LastTime;
		public bool Initialized = Initialized;
	}
	public void AddEventListener(Action<TimeArgs> _action, TimeEvents _event) =>
			Events[_event].Add(_action);
	public void RemoveEventListener(Action<TimeArgs> _action, TimeEvents _event) =>
		Events[_event].Remove(_action);

	public override void _EnterTree()
	{
		Instance = this;
		float _distanceX = TopLeft.GlobalPosition.DistanceTo(new(BottomRight.GlobalPosition.X, 0)),
			_distanceY = TopLeft.GlobalPosition.DistanceTo(new(0, BottomRight.GlobalPosition.Y));
		RoomBounds = new(TopLeft.GlobalPosition.X, TopLeft.GlobalPosition.Y, _distanceX, _distanceY);
	}
	public override void _ExitTree()
	{
		Instance = null;
	}
	public override void _Ready()
	{
		CameraManager.Instance.LimitTop = (int)TopLeft.GlobalPosition.Y;
		CameraManager.Instance.LimitBottom = (int)BottomRight.GlobalPosition.Y;
		CameraManager.Instance.LimitLeft = (int)TopLeft.GlobalPosition.X;
		CameraManager.Instance.LimitRight = (int)BottomRight.GlobalPosition.X;

		SceneManager.AddEventListener(OnPostLoad, SceneManager.EventEnum.OnPostLoad);
	}
	private void OnPostLoad(SceneManager.SceneArgs _args)
	{
		foreach (var _pair in GameManager.Upgrades)
		{
			int _index = GlobalUpgrades.AVAILABLE_UPGRADES.FindIndex((u) => u.ID == _pair.Key);
			if (_index != -1)
				GlobalUpgrades.AVAILABLE_UPGRADES[_index].OnReload(_pair.Value.Level);
		}
		SetTime(true);
		StartTimeLoop();
	}

	private void StartTimeLoop()
	{
		Timer _timeloop = new()
		{
			OneShot = true,
			ProcessMode = ProcessModeEnum.Always
		};
		_timeloop.Timeout += () =>
		{
			// start timeloop to popup after every change of hour
			_timeloop.Start(GetRemainingHourTime());
			SetTime(false, Time.GetDatetimeDictFromSystem());
		};
		AddChild(_timeloop);
		_timeloop.Start(GetRemainingHourTime());
	}
	public static float GetRemainingHourTime()
	{
		var _date = Time.GetDatetimeDictFromSystem();
		return ((60 - (int)_date["minute"]) * 60) - (int)_date["second"];
	}
	// sets the atmosphere according to time and weather and triggers hour change events
	public static void SetTime(bool _forceTransition, Godot.Collections.Dictionary _date = null)
	{
		DebugLogger.Print(DebugLogger.LogPriority.Debug, $"RoomInstance: Time Set. Forced?=<{_forceTransition}>");
		_date ??= Time.GetDatetimeDictFromSystem();
		byte _hour = (byte)_date["hour"];
		TimeArgs _args = new(TimeOfDay, Instance.initialized);
		DayHourMode currentTime;
		// 10PM to 6AM is Night 
		if (_hour < 6 || _hour >= 22)
			currentTime = DayHourMode.Night;
		else if (_hour < 10) // 6AM to 10AM is Morning
			currentTime = DayHourMode.Morning;
		else if (_hour < 16) // 10AM to 4PM is Noon
			currentTime = DayHourMode.Noon;
		else if (_hour < 20)// 4PM to 8PM is Afternoon
			currentTime = DayHourMode.Afternoon;
		else // 8PM to 10PM is Sunset
			currentTime = DayHourMode.Sunset;

		if (TimeOfDay != currentTime || _forceTransition)
		{
			TimeOfDay = currentTime;

			if (!Instance.IsInside) // do not apply colorcube transition to enclosed spaces like buildings
			{
				if (_forceTransition)
					Instance.ColorCube.Color = DayFilters[(int)currentTime];
				else
					Instance.InterpolateColorCube(currentTime);
			}

			GlobalManager.Utils.InvokeEventListeners(Instance.Events[TimeEvents.OnHourChange], _args);
		}

		GlobalManager.Utils.InvokeEventListeners(Instance.Events[TimeEvents.OnTimeChange], _args);
		Instance.initialized = true;
	}
	public void InterpolateColorCube(DayHourMode timeDay)
	{
		Tween _tween = ColorCube.CreateTween();
		_tween.SetTrans(Tween.TransitionType.Cubic);
		_tween.TweenProperty(ColorCube, "color", DayFilters[(int)timeDay], 3);
	}
}
