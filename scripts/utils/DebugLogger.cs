using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Godot;

public static class DebugLogger
{
	public enum LogPriority { Debug, Info, Log, Warning, Error, IgnorePriority }
	public enum LogPriorityMode { All, Verbose, Default, Warnings, Exceptions }

	/// <summary>
	/// Determines how does the game handle a termination of process.
	/// </summary>
	public enum GameTermination {
		/// <summary>
		/// Execute a custom function to correct yourself (the first argument in the object args)
		/// </summary>
		Custom,
		/// <summary>
		/// Loads player into the "golden hallway" room as a temporal solution.
		/// </summary>
		Minor,
		/// <summary>
		/// Exits to menu, losing all unsaved progress.
		/// </summary>
		Major,
		/// <summary>
		/// Inmediately terminates the whole game, losing all unsaved progress and runtime variables.
		/// </summary>
		Complete
		}
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

		switch (priority)
		{
			case LogPriority.Warning:
				DebugConsole.Print("[color=yellow]" + _message + "[/color]");
				GD.PushWarning(_message);
				break;
			case LogPriority.Error:
				DebugConsole.Print("[color=red]" + _message + "[/color]");
				GD.PushError(_message);
				break;
			default:
				DebugConsole.Print(_message);
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
			case GameTermination.Custom:
				if (args.Length > 0 && args[0] is Action)
					(args[0] as Action)();
				break;
			case GameTermination.Minor:
				Task.Run(() => SceneManager.Load("golden_hallway", new("golden_hallway", 0, Vector2.Left, new())));
				break;
			case GameTermination.Major:
				Task.Run(() => SceneManager.Switch("menu", false));
				break;
			case GameTermination.Complete:
				GlobalManager.Instance.GetTree().Quit(1);
				break;
		}
	}
}
