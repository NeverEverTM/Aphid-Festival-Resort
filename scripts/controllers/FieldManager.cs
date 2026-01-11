using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Manages a room's "field" and time changes to the field.
/// </summary>
public partial class FieldManager : Node2D
{
	public static FieldManager Instance { get; set; }

	[Export] private CanvasModulate ColorCube;
	/// <summary>
	/// If this field is "inside", then it is not affected by hourly colorcubes
	/// </summary>
	[Export] public bool IsInside = false;
	[Export] public Node2D TopLeft, BottomRight;
	[Export] public RoomDoor[] Doors = [];

	public enum DayHourMode { Morning, Noon, Afternoon, Sunset, Night }
	public static DayHourMode TimeOfDay { get; set; }
	/// <summary>
	/// Colorcube colors for the current field depending on the hour.
	/// </summary>
	public static readonly Color[] DayFilters =
	[
		new(0.706f, 0.933f, 0.992f), // Morning
		new(1, 1, 1), // Noon
		new(0.984f, 0.62f, 0.553f), // Afternoon
		new(0.478f, 0.211f, 0.341f), // Sunset
		new(0.133f, 0.298f, 0.592f)  // Night
	];
	public readonly List<Action<DayHourMode>> OnTimeChange = [];

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
			// start timeloop to popup after every change of hour
			var _date = Time.GetDatetimeDictFromSystem();
			SetTime(false, _date);
			_timeloop.ProcessMode = ProcessModeEnum.Always;
			_timeloop.Start(((60 - (int)_date["minute"]) * 60) - (int)_date["second"]);
		};
		AddChild(_timeloop);
		_timeloop.Start();

		CameraManager.Instance.LimitTop = (int)TopLeft.GlobalPosition.Y;
		CameraManager.Instance.LimitBottom = (int)BottomRight.GlobalPosition.Y;
		CameraManager.Instance.LimitLeft = (int)TopLeft.GlobalPosition.X;
		CameraManager.Instance.LimitRight = (int)BottomRight.GlobalPosition.X;

		SetTime(true);
	}

	// sets the atmosphere according to time and weather and triggers hour change events
	public void SetTime(bool _instantTransition, Godot.Collections.Dictionary _date = null)
	{
		_date ??= Time.GetDatetimeDictFromSystem();
        byte _hour = (byte)_date["hour"];

        DayHourMode timeDay;
        // 10PM to 6AM is Night 
        if (_hour < 6 || _hour >= 22)
            timeDay = DayHourMode.Night;
        else if (_hour < 10) // 6AM to 10AM is Morning
            timeDay = DayHourMode.Morning;
        else if (_hour < 16) // 10AM to 4PM is Noon
            timeDay = DayHourMode.Noon;
        else if (_hour < 20)// 4PM to 8PM is Afternoon
            timeDay = DayHourMode.Afternoon;
        else // 8PM to 10PM is Sunset
            timeDay = DayHourMode.Sunset;

        if (!IsInside)
		{
			if (!_instantTransition)
			{
				Tween _tween = GetTree().CreateTween();
				_tween.BindNode(ColorCube);
				_tween.SetTrans(Tween.TransitionType.Cubic);
				_tween.TweenProperty(ColorCube, "color", DayFilters[(int)timeDay], 3);
			}
			else
				ColorCube.Color = DayFilters[(int)timeDay];
		}
		TimeOfDay = timeDay;
		for (int i = 0; i < OnTimeChange.Count; i++)
        {
            try
            {
				OnTimeChange[i](TimeOfDay);
            }
			catch(Exception _error)
            {
                GD.PrintErr(_error);
            }
        }
	}
}
