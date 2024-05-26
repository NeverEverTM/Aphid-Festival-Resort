using System;
using Godot;

public static class Logger
{
	public enum LogPriority { Log, Warning, Error }
	public enum GameTermination { Minor, Major, Complete }
	public enum LoggerMode { Verbose, WarnAndError, OnlyError, None }
	public static LoggerMode LogMode = LoggerMode.Verbose;

	public static void Print(object message, LogPriority priority, params object[] args)
	{
		switch (priority)
		{
			case LogPriority.Log:
				if (LogMode == LoggerMode.Verbose)
					GD.Print("[LOG] " + message, args);
			break;
			case LogPriority.Warning:
				if (LogMode == LoggerMode.Verbose || LogMode == LoggerMode.WarnAndError)
					GD.PushWarning("[WARN] " + message, args);
			break;
			case LogPriority.Error:
				if (LogMode != LoggerMode.None)
					GD.PushError("[ERROR] " + message, args);
			break;
		}
	}
	public static void Print(object message, LogPriority priority, GameTermination mode, params object[] args)
	{
		Print(message, priority, args);

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
