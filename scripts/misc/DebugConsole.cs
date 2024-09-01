using Godot;
using System.Collections.Generic;

public partial class DebugConsole : CanvasLayer
{
	public static DebugConsole Instance { get; private set;}
	public static bool IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt,
	LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair,
	DidntSayIDidntWarnYouBeforeHand;
	private static bool IsEnabled;
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
		if (command_line_input.HasFocus() && @event is InputEventKey && (@event as InputEventKey).KeyLabel == Key.Enter)
			TriggerCommand(command_line_input.Text.Split(" "));
	}

	public override void _Process(double delta)
	{
		if (!IsEnabled)
		{
			if (Input.IsActionJustPressed("debug_0"))
			{
				if (!IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt ||
							!LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair)
					return;

				if (DidntSayIDidntWarnYouBeforeHand)
				{
					if (!IsEnabled)
					{
						IsEnabled = true;
						GameManager.CreatePopup("Welcome to the next level", this);
					}
				}
				else
				{
					DidntSayIDidntWarnYouBeforeHand = true;
					GD.Print("DEBUG MODE ENGAGED: PRESS DEBUG KEY AGAIN TO CONFIRM");
				}
			}
			else if (DidntSayIDidntWarnYouBeforeHand)
				DidntSayIDidntWarnYouBeforeHand = false;
		}
		else
		{
			if (Input.IsActionJustPressed("debug_0"))
			{
				if (Visible)
					Hide();
				else
					Show();
			}

			if (Input.IsActionJustPressed("debug_1"))
				TriggerCommand(lastCommand);
		}
	}

	/// <summary>
	/// Base interface for console commands.
	/// </summary>
	public interface IConsoleCommand
	{
		public void Execute(string[] args);
		public string HelpText { get; }
	}
	public readonly static Dictionary<string, IConsoleCommand> commands = new()
	{
		{ "spawn", new Spawner() }
	};
	public static bool TriggerCommand(string[] _commandLines)
	{
		if (!IsEnabled)
			return false;
		Instance.command_line_input.Text = "";

		if (commands.ContainsKey(_commandLines[0]))
		{
			lastCommand = _commandLines;
			commands[_commandLines[0]].Execute(_commandLines);
			return true;
		}
		return false;
	}

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

	private class Spawner : IConsoleCommand
	{
		public string HelpText => "Spawns aphid :aphid:";

		public void Execute(string[] args)
		{
			AphidData.Genes _genes = new();
			_genes.RandomizeColors();
			_genes.Name += ResortManager.Instance.AphidsOnResort.Count;
			ResortManager.CreateNewAphid(GameManager.Utils.GetMouseToWorldPosition(), _genes);
		}
	}
}
