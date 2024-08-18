using System;
using System.Linq;
using Godot;

public static class Logger
{
	public enum LogPriority { Log, Warning, Error }
	public enum GameTermination { Minor, Major, Complete }
	public enum LogLevel { Verbose, WarnAndError, OnlyError, None }
	public static LogLevel LogMode = LogLevel.WarnAndError;

	public static void Print(LogPriority priority, params object[] args)
	{
		switch (priority)
		{
			case LogPriority.Log:
				if (LogMode == LogLevel.Verbose)
					GD.Print(new string[] {"[LOG] " }.Concat(args).ToArray());
			break;
			case LogPriority.Warning:
				if (LogMode == LogLevel.Verbose || LogMode == LogLevel.WarnAndError)
					GD.PushWarning(new string[] {"[WARN] " }.Concat(args).ToArray());
			break;
			case LogPriority.Error:
				if (LogMode != LogLevel.None)
					GD.PushError(new string[] {"[ERROR] " }.Concat(args).ToArray());
			break;
		}
	}
	public static void Print(LogPriority priority, GameTermination mode, params object[] args)
	{
		Print(priority, args);

        switch (mode)
        {
            case GameTermination.Minor:
                break;
            case GameTermination.Major:
                break;
            case GameTermination.Complete:
				GameManager.Instance.GetTree().Quit(1);
                break;
        }
    }
}
