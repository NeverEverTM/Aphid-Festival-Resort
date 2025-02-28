using System;
using System.Linq;
using Godot;

public static class Logger
{
	public enum LogPriority { Debug, Info, Log, Warning, Error }
	public enum LogPriorityMode { All, Verbose, Default, Warnings, Exceptions, Essential }

	/// <summary>
	/// Minor: Execute a custom function to correct yourself (the first argument in the object args)
	/// Major: Exits to menu
	/// Complete: Exits the game
	/// </summary>
	public enum GameTermination { Minor, Major, Complete }
	public static LogPriorityMode LogMode { get; set; }

	public static void Print(LogPriority priority, params object[] args)
	{
		if ((int)LogMode > (int)priority)
			return;

		string _time = DateTime.Now.ToString("hh:mm:ss");
		string _message = Array.ConvertAll(args, x => x.ToString()).Join(" ");
		if (priority == LogPriority.Debug)
			GD.Print(_message.Insert(0, $"[{_time}] [DEBUG]: "));
		else if (priority == LogPriority.Info)
			GD.PrintRich(_message.Insert(0, $"[{_time}] [Info]: "));
		else if (priority == LogPriority.Log)
			GD.PrintRich(_message.Insert(0, $"[{_time}] [Log]: "));
		else if (priority == LogPriority.Warning)
			GD.PushWarning(_message.Insert(0, $"[{_time}] |-[WARN]-|: "));
		else if (priority == LogPriority.Error)
			GD.PushError(_message.Insert(0, $"[{_time}] ||===[ERROR]===||: "));
		else
			GD.PrintRich(_message.Insert(0, $"[{_time}] ||~~[ESSENTIAL]~~||: "));
		DebugConsole.Print(_message);
	}
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
			_ = GlobalManager.LoadScene(GlobalManager.SceneName.Menu);
                break;
            case GameTermination.Complete:
				GlobalManager.Instance.GetTree().Quit(1);
                break;
        }
    }
}
