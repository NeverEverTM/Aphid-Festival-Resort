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
		if ((int)LogMode > (int)priority)
			return;

		string _time = DateTime.Now.ToString("hh:mm:ss");
		string _message = Array.ConvertAll(args, x => x.ToString()).Join(" ");
		if (priority == LogPriority.Log)
			GD.Print(_message.Insert(0, $"[{_time}] [Log]: "));
		else if (priority == LogPriority.Warning)
			GD.Print(_message.Insert(0, $"[{_time}] |-[WARN]-|: "));
		else
			GD.PrintErr(_message.Insert(0, $"[{_time}] ||===[ERROR]===||: "));
		DebugConsole.Print(_message);
	}
	public static void Print(LogPriority priority, GameTermination mode, params object[] args)
	{
		Print(priority, args);

        switch (mode)
        {
            case GameTermination.Minor:
                break;
            case GameTermination.Major:
			_ = GlobalManager.LoadScene(GlobalManager.SceneName.Menu);
                break;
            case GameTermination.Complete:
				GlobalManager.Instance.GetTree().Quit(1);
                break;
        }
    }
}
