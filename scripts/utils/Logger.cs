using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Godot;

public static class Logger
{
	public enum LogPriority { Debug, Info, Log, Warning, Error, IgnorePriority }
	public enum LogPriorityMode { All, Verbose, Default, Warnings, Exceptions }

	/// <summary>
	/// Minor: Execute a custom function to correct yourself (the first argument in the object args)
	/// Major: Exits to menu
	/// Complete: Exits the game
	/// </summary>
	public enum GameTermination { Minor, Major, Complete }
	public static LogPriorityMode LogMode { get; set; }
	private static readonly string[] LOG_STARTERS = [
		"[DEBUG]:",
		"[INFO]:",
		"[LOG]:",
		"|-[WARN]-|:",
		"||===[ERROR]===||:",
		"*[SPECIAL LOG]:"
	];

	[StackTraceHidden]
	public static void Print(LogPriority priority, params object[] args)
	{
		if ((int)LogMode > (int)priority)
			return;

		string _time = DateTime.Now.ToString("hh:mm:ss");
		StringBuilder _string = new("[{0}] {1} ");
		_string.AppendJoin(" ", Array.ConvertAll(args, x => x.ToString()));
		string _message = string.Format(_string.ToString(), _time, LOG_STARTERS[(int)priority]);

		DebugConsole.Print(_message);
		switch (priority)
		{
			case LogPriority.Warning:
				GD.PushWarning(_message);
				break;
			case LogPriority.Error:
				GD.PushError(_message);
				break;
			default:
				GD.Print(_message);
				break;
		}
	}

	[StackTraceHidden]
	public static void Print(LogPriority priority, GameTermination mode, params object[] args)
	{
		Print(priority, args);

		switch (mode)
		{
			case GameTermination.Minor:
				if (args.Length > 0 && args[0] is Action)
					(args[0] as Action)();
				break;
			case GameTermination.Major:
				_ = SceneManager.Switch("menu", false, false);
				break;
			case GameTermination.Complete:
				GlobalManager.Instance.GetTree().Quit(1);
				break;
		}
	}
}
