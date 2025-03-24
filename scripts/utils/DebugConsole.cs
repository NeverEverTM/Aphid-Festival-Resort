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
	private static bool IsEnabled;
	private static string[] lastCommand;
	private static string lastRawCommand;
	private static Aphid validAphid;

	[Export] public TextEdit command_line_input;
	[Export] public RichTextLabel log_print_text;
	[Export] public Label debug_status;

	public override void _Ready()
	{
		Instance = this;
#if DEBUG
		IsEnabled = true;
#endif
		Print($"Debug Console Command - {GlobalManager.GAME_VERSION}v\n");
	}
	public override void _Process(double delta)
	{
		if (IsInstanceValid(validAphid))
		{
			debug_status.Text = "Name: " + validAphid.Instance.Genes.Name;
			debug_status.Text += "\nState: " + validAphid.State.Type.ToString();
			debug_status.Text += "\nFood: " + validAphid.Instance.Status.Hunger;
			debug_status.Text += "\nWater: " + validAphid.Instance.Status.Thirst;
			debug_status.Text += "\nBondship: " + validAphid.Instance.Status.Bondship;
			debug_status.Text += "\nHealth: " + validAphid.Instance.Status.Health;
			debug_status.Text += "\nAge/s: " + (int)validAphid.Instance.Status.Age + "/" + AphidData.Age_Death;
			debug_status.Text += $"\nBreedBuildup: {(int)validAphid.Instance.Status.BreedBuildup}/{AphidData.Breed_Cooldown}";
			debug_status.Text += "\nBreedMode: " + validAphid.Instance.Status.BreedMode;
			debug_status.Text += $"\nHarvestBuildup: {(int)validAphid.Instance.Status.MilkBuildup}/{AphidData.Harvest_Cooldown}";
			debug_status.Text += "\nFoodPreference: " + validAphid.Instance.Genes.FoodPreference.ToString();
			debug_status.Text += "\nTraits: \n";
			for (int i = 0; i < validAphid.Instance.Genes.Traits.Count; i++)
				debug_status.Text += $"{validAphid.Instance.Genes.Traits[i]}\n";
		}
		else
			validAphid = null;
	}
	public override void _Input(InputEvent @event)
	{
		if (IsEnabled)
		{
			if (@event.IsActionPressed("debug_0"))
			{
				if (Visible)
					Hide();
				else
					Show();
			}

			if (@event.IsActionPressed("debug_1") && lastCommand != null)
				TriggerCommand(lastCommand);

			if (@event.IsActionPressed("debug_2"))
			{
				command_line_input.GrabFocus();
				command_line_input.Text = lastRawCommand;
			}

			if (command_line_input.HasFocus() && @event is InputEventKey &&
				(@event as InputEventKey).KeyLabel == Key.Enter && @event.IsPressed())
			{
				if (!string.IsNullOrEmpty(command_line_input.Text))
				{
					lastRawCommand = command_line_input.Text;
					if (TriggerCommand(command_line_input.Text.Split(" ")))
						command_line_input.ReleaseFocus();
				}
				GetViewport().SetInputAsHandled();
			}
		}
		else
		{
			if (!IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt ||
				!LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair)
				return;

			CheckForUnlock(@event);
		}
	}

	private static void CheckForUnlock(InputEvent @event)
	{
		if (!DidntSayIDidntWarnYouBeforeHand)
		{
			if (@event.IsActionPressed("debug_0"))
				DidntSayIDidntWarnYouBeforeHand = true;
		}
		else if (!IsEnabled)
		{
			if (@event.IsActionPressed("debug_2"))
			{
				IsEnabled = true;
				SoundManager.CreateSound(SoundManager.GetAudioStream("ui/kitchen_success"));
				GlobalManager.CreatePopup("Welcome to the next level", Instance);
			}
			else
				DidntSayIDidntWarnYouBeforeHand = false;
		}


	}
	public static bool TriggerCommand(string[] _commandLines)
	{
		if (!IsEnabled)
			return false;

		if (commands.ContainsKey(_commandLines[0]))
		{
			// store last command as raw string
			lastCommand = _commandLines;

			// parse args and execut command
			string[] _args = new string[_commandLines.Length - 1];
			if (_args.Length > 0)
				Array.Copy(_commandLines, 1, _args, 0, _args.Length);

			commands[_commandLines[0]].Execute(_args);
			Instance.command_line_input.Text = string.Empty;
			return true;
		}
		Logger.Print(Logger.LogPriority.Debug, $"Command '{_commandLines[0]}' does not exist. Type help for a complete list.");
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
		{ "motherload", new Motherload() },
		{ "time", new DeLorean() },
		{ "gamerule", new GameRules() },
		{ "aphid", new AphidDebug() },
		{ "give", new GiveItem() },
		{ "dialog", new DialogSim() }
	};

	public static void Print(string _message) =>
		Instance?.log_print_text.AppendText(_message + "\n");

	public static string GetArg(int _index, string[] _argList)
	{
		if (_index >= _argList.Length)
			return string.Empty;

		return _argList[_index];
	}
	public static bool GetArg(int _index, string[] _argList, out string _arg)
	{
		if (_index >= _argList.Length)
		{
			_arg = string.Empty;
			return false;
		}
		_arg = _argList[_index];
		return true;
	}
	public static int GetInt(int _index, string[] _argList, int _default)
	{
		if (!GetArg(_index, _argList, out string _arg))
			return _default;

		if (!int.TryParse(_arg, out int _argNumber))
			return _default;
		else
			return _argNumber;
	}
	public static float GetFloat(int _index, string[] _argList, float _default)
	{
		if (!GetArg(_index, _argList, out string _arg))
			return _default;

		if (!float.TryParse(_arg, out float _argNumber))
			return _default;
		else
			return _argNumber;
	}
	public static bool GetBool(int _index, string[] _argList, bool _default)
	{
		if (!GetArg(_index, _argList, out string _arg))
			return _default;

		if (_arg.Equals("0"))
			return false;
		else if (_arg.Equals("1"))
			return true;
		else if (_arg.Equals("*"))
			return _default;

		if (!bool.TryParse(_arg, out bool _argBool))
			return _default;
		else
			return _argBool;
	}

	private class Help : IConsoleCommand
	{
		public string HelpText => "Help yourself!";

		public void Execute(string[] args)
		{
			if (args.Length > 0)
			{
				if (commands.ContainsKey(args[0]))
					Logger.Print(Logger.LogPriority.Log, "HelpCommand: ", commands[args[0]], " = ", commands[args[0]].HelpText);
				else
					Logger.Print(Logger.LogPriority.Log, "This command does not exist. Type 'help' to find all available commands");
			}
			else
			{
				Logger.Print(Logger.LogPriority.Log, "The format for a command is:\n<name of the command> [required parameters] (optional parameters) ('option 1'/'option 2').");
				Logger.Print(Logger.LogPriority.Log, "The available commands are:");
				commands.Keys.ToList().ForEach(Print);
			}
		}
	}
	private class Motherload : IConsoleCommand
	{
		public string HelpText => "Gives you loads of money. <motherload> (amount) ('debt')";

		public void Execute(string[] args)
		{
			if (GlobalManager.Scene != GlobalManager.SceneName.Resort)
			{
				Logger.Print(Logger.LogPriority.Log, $"Motherload: No game currently running.");
				return;
			}
			int _amount = 500;
			if (args.Length > 0)
				_amount = int.Parse(args[0]);

			if (args.Length > 1 && args[1] == "debt")
			{
				Player.Data.Currency += _amount;
				Logger.Print(Logger.LogPriority.Log, $"Motherload: Removed ${_amount} to your current game.");
			}
			else
			{
				Player.Data.Currency += _amount;
				Logger.Print(Logger.LogPriority.Log, $"Motherload: Added ${_amount} to current game.");
			}
			CanvasManager.UpdateCurrency();
		}
	}
	private class DeLorean : IConsoleCommand
	{
		public string HelpText => "Change the hour of the day, 24 hours only. <time> [hh] [mm]";

		public void Execute(string[] args)
		{
			if (args.Length < 2)
				return;
			var _date = Time.GetDatetimeDictFromSystem();
			_date["hour"] = args[0];
			_date["minute"] = args[1];
			FieldManager.Instance.SetTime(_date);

			Logger.Print(Logger.LogPriority.Log, $"In-Game Time is now {args[0]}:{args[1]}");
		}
	}
	private class GameRules : IConsoleCommand
	{
		public string HelpText => "Modify game and engine rules. <gamerule> [rule_in_snake_case/list] (some rules require a value)";
		private readonly Dictionary<string, Action<string[]>> game_rules = new()
		{
			{ "time_scale", (args) =>
				{
					Engine.TimeScale = GetFloat(1, args, 1);
					Logger.Print(Logger.LogPriority.Info, $"GameRules: Time scale is now: <{Engine.TimeScale}>");
				}
			},
			{ "physics_scale", (args) =>
				{
					Engine.PhysicsTicksPerSecond = GetInt(1, args, 60);
					Logger.Print(Logger.LogPriority.Info, $"GameRules: Physics Tics are now: <{Engine.PhysicsTicksPerSecond}/s>");
				}
			},
			{ "harvest_cooldown", (args) =>
				{
					AphidData.Harvest_Cooldown = GetInt(1, args, harvest_default);
					Logger.Print(Logger.LogPriority.Info, $"GameRules: Harvest Cooldown is now <{AphidData.Harvest_Cooldown}>");
				}
			},
			{ "breed_cooldown", (args) =>
				{
					AphidData.Breed_Cooldown = GetInt(1, args, breed_default);
					Logger.Print(Logger.LogPriority.Info, $"GameRules: Breed Cooldown is now <{AphidData.Breed_Cooldown} seconds>");
				}
			},
			{ "age_adulthood", (args) =>
				{
					AphidData.Age_Adulthood = GetInt(1, args, adult_default);
					Logger.Print(Logger.LogPriority.Info, $"GameRules: The age for adulthood is now <{AphidData.Age_Adulthood} seconds>");
				}
			},
			{ "age_death", (args) =>
				{
					AphidData.Age_Death = GetInt(1, args, death_default);
					Logger.Print(Logger.LogPriority.Info, $"GameRules: The age for death is now <{AphidData.Age_Death} seconds>");
				}
			},
			{ "log_mode", (args) =>
				{
					Logger.LogMode = (Logger.LogPriorityMode)GetInt(1, args, 2);
					Logger.Print(Logger.LogPriority.Error, $"GameRules: Log mode is now <{Logger.LogMode}>");
				}
			},
		};
		private static int harvest_default = AphidData.Harvest_Cooldown, breed_default = AphidData.Breed_Cooldown,
			adult_default = AphidData.Age_Adulthood, death_default = AphidData.Age_Death;

		public void Execute(string[] args)
		{
			if (GetArg(0, args) == "list")
			{
				Print("GameRules: The following rules are:");
				game_rules.Keys.ToList().ForEach(Print);
				return;
			}

			if (GetArg(0, args, out string _rule) && game_rules.ContainsKey(_rule))
			{
				game_rules[_rule](args);
			}
			else
				Logger.Print(Logger.LogPriority.Info, $"GameRules: This rule does NOT exst.");
		}
	}
	private class AphidDebug : IConsoleCommand
	{
		public string HelpText => "Tools for debugging aphids. <aphid (new[generateGenes,generateSkin,generateColor]/get/despawn/kill/remove)>";

		public void Execute(string[] args)
		{
			switch (GetArg(0, args))
			{
				case "new":
				case "mew":
				case "spawn":
				case "create":
					AphidData.Genes _genes = new();
					_genes.DEBUG_Randomize(GetBool(1, args, true), GetBool(2, args, true), GetBool(3, args, true));
					_genes.Name += ResortManager.CurrentResort.AphidsOnResort.Count;
					ResortManager.CreateAphid(GlobalManager.Utils.GetMouseToWorldPosition(), _genes);
					break;
				case "get":
				case "select":
					if (!GetArg(1, args, out string _name))
					{
						Vector2 _mouseposition = GlobalManager.Utils.GetMouseToWorldPosition();
						float _shortestDistance = float.PositiveInfinity;
						validAphid = null;

						foreach (Aphid _aphid in ResortManager.CurrentResort.AphidsOnResort)
						{
							float _distance = _mouseposition.DistanceSquaredTo(_aphid.GlobalPosition);
							if (_distance < _shortestDistance)
							{
								validAphid = _aphid;
								_shortestDistance = _distance;
							}
						}
						if (!IsInstanceValid(validAphid))
						{
							Instance.debug_status.Hide();
							Logger.Print(Logger.LogPriority.Info, $"AphidDebug: No aphid was found.");
						}
						else
						{
							Instance.debug_status.Show();
							Logger.Print(Logger.LogPriority.Info, $"AphidDebug: Your current aphid is: <{validAphid.Instance?.Genes.Name ?? "UNKNOWN"}>.");
						}
					}
					else
					{
						validAphid = ResortManager.CurrentResort.AphidsOnResort.Find((a) => a.Instance.Genes.Name == _name);
						if (!IsInstanceValid(validAphid))
							Logger.Print(Logger.LogPriority.Info, $"AphidDebug: No aphid was found with the name <{_name}>.");
					}
					break;
				case "unload":
				case "despawn":
					validAphid?.QueueFree();
					break;
				case "kill":
					if (!IsInstanceValid(validAphid))
						return;
					validAphid.Die();
					break;
				case "remove":
					if (!IsInstanceValid(validAphid))
						return;
					GameManager.RemoveAphid(new Guid(validAphid.Instance.ID));
					validAphid.QueueFree();
				break;
			}
		}
	}
	private class GiveItem : IConsoleCommand
	{
		public string HelpText => "Gives you an item. <give [string_id] (amount)>";

		public void Execute(string[] args)
		{
			if (!GetArg(0, args, out string _id))
				return;
			if (GlobalManager.G_ITEMS.ContainsKey(_id) && GlobalManager.G_ITEMS[_id].shopTag != "furniture")
			{
				int _amount = GetInt(1, args, 1);
				for (int i = 0; i < _amount; i++)
					PlayerInventory.StoreItem(_id);
				Logger.Print(Logger.LogPriority.Log, $"GiveItem: {_id}({_amount}x) was added to your inventory.");
			}
			else
				Logger.Print(Logger.LogPriority.Log, $"GiveItem: {_id} is not a valid item.");
		}
	}
    private class DialogSim : IConsoleCommand
    {
        public string HelpText => "This is a text";

        public void Execute(string[] args)
        {
            if (!GetArg(0, args, out string _id))
				return;
			if (_id.Equals(Instance.Tr(_id)))
			{
				Logger.Print(Logger.LogPriority.IgnorePriority, $"DialogSim: ID <{_id}> does not exist in the translation files.");
				return;
			}
			Logger.Print(Logger.LogPriority.IgnorePriority, $"DialogSim: Displaying <{_id}>:");
			Logger.Print(Logger.LogPriority.IgnorePriority, "[color=cyan]", Instance.Tr(_id), "[/color]");
        }
    }
}