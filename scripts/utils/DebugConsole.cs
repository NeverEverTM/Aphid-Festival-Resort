using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DebugConsole : CanvasLayer
{
	public static DebugConsole Instance { get; private set; }
	public static bool IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt,
	LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair,
	DidntSayIDidntWarnYouBeforeHand;
	private static bool IsEnabled, lastPauseState;
	private static string[] lastCommand;

	[Export] public TextEdit command_line_input;
	[Export] public RichTextLabel log_print_text;

	public override void _Ready()
	{
		Instance = this;
#if DEBUG
		IsEnabled = true;
#endif
	}

	public override void _Input(InputEvent @event)
	{
		if (IsEnabled)
		{
			if (command_line_input.HasFocus() && @event is InputEventKey && (@event as InputEventKey).KeyLabel == Key.Enter)
				TriggerCommand(command_line_input.Text.Split(" "));
		}
		else
		{
			if (!IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt ||
				!LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair)
				return;

			CheckForUnlock(@event);
		}
	}

	public override void _Process(double delta)
	{
		if (IsEnabled)
			if (Input.IsActionJustPressed("debug_0"))
			{
				if (Visible)
				{
					Hide();
					GetTree().Paused = lastPauseState;
				}
				else
				{
					Show();
					lastPauseState = GetTree().Paused;
					GetTree().Paused = true;
				}
			}

		if (Input.IsActionJustPressed("debug_1"))
		{
			if (lastCommand != null)
				TriggerCommand(lastCommand);
		}

		if (Input.IsActionJustPressed("debug_2"))
		{
			Logger.Print(Logger.LogPriority.Log, "Buggy");
		}
	}
	private static void CheckForUnlock(InputEvent @event)
	{
		if (@event.IsActionPressed("debug_0"))
		{
			if (!DidntSayIDidntWarnYouBeforeHand)
			{
				DidntSayIDidntWarnYouBeforeHand = true;
				GD.Print("DEBUG MODE ENGAGED: PRESS DEBUG KEY AGAIN TO CONFIRM");

			}
			else if (!IsEnabled)
			{
				IsEnabled = true;
				GlobalManager.CreatePopup("Welcome to the next level", GlobalManager.Instance);
			}
		}
		else if (DidntSayIDidntWarnYouBeforeHand && @event is InputEventKey)
			DidntSayIDidntWarnYouBeforeHand = false;
	}
	public static bool TriggerCommand(string[] _commandLines)
	{
		if (!IsEnabled)
			return false;
		Instance.command_line_input.Text = "";

		if (commands.ContainsKey(_commandLines[0]))
		{
			lastCommand = _commandLines;

			// parse args and execut command
			string[] _args = new string[_commandLines.Length];
			if (_commandLines.Length > 1)
				Array.Copy(_commandLines, 1, _args, 0, _args.Length - 1);
			commands[_commandLines[0]].Execute(_args);
			return true;
		}
		return false;
	}

	public interface IConsoleCommand
	{
		public void Execute(string[] args);
		public string HelpText { get; }
	}
	public readonly static Dictionary<string, IConsoleCommand> commands = new()
	{
		{ "help", new Help() },
		{ "spawn", new Spawner() },
		{ "motherload", new Motherload() },
		{ "time", new DeLorean() }
	};
	
	public static void Print(string _message) =>
		Instance?.log_print_text.AppendText(_message + "\n");
	public static bool CheckArgLength(int argLength, int _validLength)
	{
		if (argLength != _validLength)
			return false;
		return true;
	}
	public static bool CheckValidType(string _value, out int _validValue)
	{
		if (!int.TryParse(_value, out _validValue))
			return false;
		return true;
	}
	public static bool CheckValidType(string _value, out float _validValue)
	{
		if (!float.TryParse(_value, out _validValue))
			return false;
		return true;
	}
	public static bool CheckValidType(string _value, out bool _validValue)
	{
		if (!bool.TryParse(_value, out _validValue))
			return false;
		return true;
	}

    private class Help : IConsoleCommand
    {
        public string HelpText => "Help yourself!";

        public void Execute(string[] args)
        {
			if (args.Length > 0)
			{
				if (commands.ContainsKey(args[0]))
            		Print(commands[args[0]].HelpText);
				else
					Print("This command does not exist. Type 'help' to find all available commands");
			}
			else
			{
				Print("Command available are:\n");
				for(int i = 0; i < commands.Count; i++)
					Print(commands.ElementAt(i).Key);
			}
			
        }
    }
    private class Spawner : IConsoleCommand
	{
		public string HelpText => "Spawns randomized aphids. <spawn>";

		public void Execute(string[] args)
		{
			AphidData.Genes _genes = new();
			_genes.DEBUG_Randomize();
			_genes.Name += ResortManager.CurrentResort.AphidsOnResort.Count;
			ResortManager.CreateAphid(GlobalManager.Utils.GetMouseToWorldPosition(), _genes);
		}
	}
	private class Motherload : IConsoleCommand
	{
		public string HelpText => "Gives you loads of money. <motherload> (specific amount)";

		public void Execute(string[] args)
		{
			int _amount = 500;
			if (args.Length > 0)
				CheckValidType(args[0], out _amount);

			Player.Data.Currency += _amount;
			CanvasManager.UpdateCurrency();
			Logger.Print(Logger.LogPriority.Log, $"You have been transfered ${_amount} from the Bank of Bugaria, free of charge!");
		}
	}
	private class DeLorean : IConsoleCommand
	{
		public string HelpText => "Change the hour of the day, 24 hours only. <time> [hh] [mm]";

		public void Execute(string[] args)
		{
			var _date = Time.GetDatetimeDictFromSystem();
			_date["hour"] = args[0];
			_date["minute"] = args[1];
			FieldManager.Instance.SetTime(_date);

			Logger.Print(Logger.LogPriority.Log, $"In-Game Time is now {args[0]}:{args[1]}");
		}
	}
    private class AphidDebug : IConsoleCommand
    {
        public string HelpText => "<aphid (select/create/)>";

        public void Execute(string[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}