#if DEBUG
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Godot.Attributes;
using BenchmarkDotNet.Order;
using System.Text.Json;
using System.Text.Json.Nodes;

public partial class DebugConsole : CanvasLayer
{
	public static DebugConsole Instance { get; private set; }
	public static bool IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt,
	LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair,
	DidntSayIDidntWarnYouBeforeHand;
	private static bool IsEnabled;
	private static string[] lastCommand;
	private static string lastRawCommand;
	private static AphidInstance validAphid;

	[Export] public LineEdit command_line_input;
	[Export] public RichTextLabel log_print_text;
	[Export] public Label debug_status;

	public override void _Ready()
	{
		Instance = this;
#if DEBUG
		IsEnabled = true;
		Instance.debug_status.Show();
		Print($"Debug Console Command - {GlobalManager.GAME_VERSION}v\n");
#endif
		command_line_input.TextSubmitted += (_text) =>
		{
			if (!string.IsNullOrEmpty(_text))
			{
				lastRawCommand = _text;
				if (TriggerCommand(_text.Split(" ")))
					command_line_input.ReleaseFocus();
			}
			GetViewport().SetInputAsHandled();
		};

		//await GodotBenchmarkRunner.RunWithBBCodeAsync<TestForClassMatch>(onFinish: (s) => GD.PrintRich(s));
	}
	public override void _Process(double delta)
	{
		if (validAphid != null)
		{
			string[] _list =
			[
			 	"Name: " + validAphid.Genes.Name,
				"State: " + (validAphid.Entity != null ? validAphid.Entity.State.Type.ToString() : validAphid.Status.LastActiveState),
				"Hunger: " + validAphid.Status.Hunger,
				"Thirst: " + validAphid.Status.Thirst,
				"Rest: " + validAphid.Status.Rest,
				"Affection: " + validAphid.Status.Affection,
				"Bondship: " + validAphid.Status.Bondship,
				"EntityMode: " + validAphid.Status.Mode.ToString(),
				"Age: " + (int)validAphid.Status.Age + "/" + AphidData.Age_Lifetime,
				$"BreedBuildup: {(int)validAphid.Status.BreedBuildup}/{AphidData.Breed_Cooldown}",
				"BreedMode: " + validAphid.Status.BreedMode.ToString(),
				$"HarvestBuildup: {(int)validAphid.Status.HarvestBuildup}/{AphidData.Harvest_Cooldown}",
				"FoodPreference: " + validAphid.Genes.FoodPreference.ToString(),
				"CurrentTrainData: " + (validAphid.Status.LastTraining != null ? $"{validAphid.Status.LastTraining.Skill}, {validAphid.Status.LastTraining.GetPointGain()} every {validAphid.Status.LastTraining.RawBaseTime}s" : "No Data"),
				"Traits:",
				validAphid.Genes.Traits[0],
				validAphid.Genes.Traits[1],
				validAphid.Genes.Traits[2],
				validAphid.Genes.Traits.Count > 3 ? validAphid.Genes.Traits[3] : string.Empty
			];
			debug_status.Text = string.Join("\n", _list);
		}
		else
		{
			debug_status.Text = string.Empty;
		}
	}
	public override void _Input(InputEvent @event)
	{
		if (IsEnabled)
		{
			if (@event.IsActionPressed(InputNames.Debug1))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Debug, "Hello Dolly!");
				return;
			}

			if (@event.IsActionPressed(InputNames.Debug0))
			{
				if (Visible)
					Hide();
				else
					Show();
			}

			if (@event.IsActionPressed(InputNames.Debug2) && lastCommand != null)
				TriggerCommand(lastCommand);

			if (!Visible || @event is not InputEventKey || !@event.IsPressed())
				return;
			var _event = @event as InputEventKey;

			if (_event.KeyLabel == Key.Up)
			{
				command_line_input.GrabFocus();
				command_line_input.Text = lastRawCommand;
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
				GlobalManager.CREATE_POPUP("Welcome to the next level", Instance);
			}
			else
				DidntSayIDidntWarnYouBeforeHand = false;
		}
	}
	public static bool TriggerCommand(string[] _commandLines)
	{
		if (!IsEnabled)
			return false;

		if (commands.TryGetValue(_commandLines[0], out IConsoleCommand value))
		{
			// store last command as raw string
			lastCommand = _commandLines;

			// parse args and execut command
			string[] _args = new string[_commandLines.Length - 1];
			if (_args.Length > 0)
				Array.Copy(_commandLines, 1, _args, 0, _args.Length);
			value.Execute(_args);
			Instance.command_line_input.Text = string.Empty;
			return true;
		}
		DebugLogger.Print(DebugLogger.LogPriority.Debug, $"Command '{_commandLines[0]}' does not exist. Type help for a complete list.");
		return false;
	}

	public interface IConsoleCommand
	{
		public void Execute(string[] args);
		public string HelpText { get; }
	}
	public readonly static Dictionary<string, IConsoleCommand> commands = new()
	{
		{ "help", new Andrew() },
		{ "motherload", new Motherload() },
		{ "time", new DeLorean() },
		{ "gamerule", new GameRouxls() },
		{ "aphid", new AphidPrognosis() },
		{ "give", new GrabBag() },
		{ "speak", new VisualNovel() },
		{ "build", new IKEA() },
		{ "tp", new Jaunt() },
		{ "move", new Doors() },
		{ "jobs", new WhiteCollar() },
		{ "print", new Printer() },
		{ "quit", new ExitWithoutSaving() },
		{ "run", new Run() }
	};

	public static void Print(string _message)
	{
#if DEBUG
		Instance?.log_print_text.AppendText(_message + "\n");
#endif
	}

	public static string GetArg(int _index, string[] _argList, string _default = "")
	{
		if (_index >= _argList.Length)
			return _default;

		return _argList[_index];
	}
	public static bool GetArg(int _index, string[] _argList, out string _arg, string _default = "")
	{
		if (_index >= _argList.Length)
		{
			_arg = _default;
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

	private class Andrew : IConsoleCommand
	{
		public string HelpText => "Help yourself!";

		public void Execute(string[] args)
		{
			if (GetArg(0, args, out string _command))
			{
				if (commands.TryGetValue(_command, out IConsoleCommand value))
					DebugLogger.Print(DebugLogger.LogPriority.Log, "HelpCommand: ", value.GetType().ToString(), " = ", value.HelpText);
				else
					DebugLogger.Print(DebugLogger.LogPriority.Log, "This command does not exist. Type 'help' to find all available commands");
			}
			else
			{
				DebugLogger.Print(DebugLogger.LogPriority.Log, "The format for a command is:\n<name of the command> [required parameters] (optional parameters) ('option 1'/'option 2').");
				DebugLogger.Print(DebugLogger.LogPriority.Log, "The available commands are:", commands.Keys.ToArray().Join(", "));
			}
		}
	}
	private class Motherload : IConsoleCommand
	{
		public string HelpText => "Gives you loads of money. <motherload> (amount)";

		public void Execute(string[] args)
		{
			if (!SceneManager.CurrentlyInGame)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"Motherload: No game currently running.");
				return;
			}
			int _amount = GetInt(0, args, 256);
			Player.AddCurrency(_amount);

			if (_amount < 0)
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"Motherload: Removed ${_amount} from your current game.");
			else
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"Motherload: Added ${_amount} from current game.");
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
			RoomInstance.SetTime(false, _date);

			DebugLogger.Print(DebugLogger.LogPriority.Log, $"In-Game Time is now {args[0]}:{args[1]}");
		}
	}
	private class GameRouxls : IConsoleCommand
	{
		public string HelpText => "Modify game and engine rules. <gamerule> [rule_in_snake_case/list] (some rules require a value)";
		private readonly Dictionary<string, Action<string[]>> game_rules = new()
		{
			{ "time_scale", (args) =>
				{
					Engine.TimeScale = GetFloat(1, args, 1);
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: Time scale is now: <{Engine.TimeScale}>");
				}
			},
			{ "physics_scale", (args) =>
				{
					Engine.PhysicsTicksPerSecond = GetInt(1, args, 60);
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: Physics Tics are now: <{Engine.PhysicsTicksPerSecond}/s>");
				}
			},
			{ "harvest_cooldown", (args) =>
				{
					AphidData.Harvest_Cooldown = GetInt(1, args, harvest_default);
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: Harvest Cooldown is now <{AphidData.Harvest_Cooldown}>");
				}
			},
			{ "breed_cooldown", (args) =>
				{
					AphidData.Breed_Cooldown = GetInt(1, args, breed_default);
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: Breed Cooldown is now <{AphidData.Breed_Cooldown} seconds>");
				}
			},
			{ "age_adulthood", (args) =>
				{
					AphidData.Age_Adulthood = GetInt(1, args, adult_default);
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: The age for adulthood is now <{AphidData.Age_Adulthood} seconds>");
				}
			},
			{ "age_death", (args) =>
				{
					AphidData.Age_Lifetime = GetInt(1, args, death_default);
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: The age for death is now <{AphidData.Age_Lifetime} seconds>");
				}
			},
			{ "log_mode", (args) =>
				{
					DebugLogger.LogMode = (DebugLogger.LogPriorityMode)GetInt(1, args, 2);
					DebugLogger.Print(DebugLogger.LogPriority.IgnorePriority, $"GameRules: Log mode is now <{DebugLogger.LogMode}>");
				}
			},
			{ "show_build_rect", (args) =>
				{
					BuildMenu.DEBUG_SHOW_RECTS = GetBool(1, args, false);
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: ", BuildMenu.DEBUG_SHOW_RECTS ?
						"Enabled rect visualization for furniture." : "Disabled rect visualization for furniture.");
				}
			},
		};
		private static int harvest_default = AphidData.Harvest_Cooldown, breed_default = AphidData.Breed_Cooldown,
			adult_default = AphidData.Age_Adulthood, death_default = AphidData.Age_Lifetime;

		public void Execute(string[] args)
		{
			if (GetArg(0, args) == "list")
			{
				Print("GameRules: The following rules are:");
				game_rules.Keys.ToList().ForEach(Print);
				return;
			}

			if (GetArg(0, args, out string _name) && game_rules.TryGetValue(_name, out Action<string[]> _rule))
				_rule(args);
			else
				DebugLogger.Print(DebugLogger.LogPriority.Info, $"GameRules: The rule {_name} does not exist.");
		}
	}
	private class AphidPrognosis : IConsoleCommand
	{
		public string HelpText => "Allows to debug and manipulate aphid behaviour and parameters. <aphid [command] (params)> Possible commands are:\n"
		+ "<aphid [new/create] (bool Genes) (bool Skin) (bool Color)> - Creates a new aphid and selects it\n"
		+ "<aphid [get/select/deselect] ('GUID'/'Name')> Get and display nearest aphid to mouse, can also search based on name \n"
		+ "<aphid [unload/despawn]> - Unloads the selected aphid entity\n"
		+ "<aphid [kill]> - Triggers the Kill command on the selected aphid\n"
		+ "<aphid [remove/destroy]> - Removes selected aphid from the savefile permanently, Kill command is NOT triggered\n"
		+ "<aphid [grant] [skill] (points)> - Grants skill points\n"
		+ "<aphid [hunger/thirst/rest/bondship] (amount)> - Add amount to a basic need"
		+ "<aphid [forceactive]> - Forces an aphid to be active, use if aphid is stuck on limbo";

		public void Execute(string[] args)
		{
			string _command = GetArg(0, args);

			switch (_command)
			{
				case "new":
				case "mew":
				case "create":
					AphidData.Genes _genes = new();
					_genes.DEBUG_Randomize(GetBool(1, args, true), GetBool(2, args, true), GetBool(3, args, true));
					_genes.Name += ResortManager.Current.Aphids.Count;

					validAphid = ResortManager.CreateAphid(CameraManager.GetMouseToWorldPosition(), _genes).Instance;
					return;
				case "get":
				case "select":
					if (!GetArg(1, args, out string _name))
					{
						Vector2 _mouseposition = CameraManager.GetMouseToWorldPosition();
						float _shortestDistance = float.PositiveInfinity;
						validAphid = null;

						if (IsInstanceValid(ResortManager.Current))
						{
							foreach (Aphid _aphid in ResortManager.Current.Aphids)
							{
								float _distance = _mouseposition.DistanceSquaredTo(_aphid.GlobalPosition);
								if (_distance < _shortestDistance)
								{
									validAphid = _aphid.Instance;
									_shortestDistance = _distance;
								}
							}
						}
						if (validAphid == null)
							DebugLogger.Print(DebugLogger.LogPriority.Info, $"AphidDebug: No aphid was found.");
						else
							DebugLogger.Print(DebugLogger.LogPriority.Info, $"AphidDebug: Your current aphid is: <{validAphid?.Genes.Name ?? "UNKNOWN"}>.");
					}
					else
					{
						validAphid = GameManager.Aphids.First((a) => a.Value.Genes.Name == _name).Value;
						if (validAphid == null)
							DebugLogger.Print(DebugLogger.LogPriority.Info, $"AphidDebug: No aphid was found with the name <{_name}>.");
					}
					return;
			}

			if (validAphid != null)
				ExecuteAphidCommand(_command, args);
			else
				Print("Aphid Prognosis: No valid aphid available to modify!");
		}

		private void ExecuteAphidCommand(string _command, string[] args)
		{
			switch (_command)
			{
				case "deselect":
					Print($"AphidPrognosis: Aphid {validAphid.Genes.Name} deselected");
					validAphid = null;
					return;
				case "unload":
				case "despawn":
					validAphid.Entity?.QueueFree();
					return;
				case "kill":
					validAphid.Entity?.PrepareToDie();
					SoundManager.CreateSound("misc/medic_prognosis", false).VolumeDb = -10;
					break;
				case "remove":
				case "destroy":
					GameManager.RemoveAphid(new Guid(validAphid.ID));
					validAphid.Entity?.QueueFree();
					break;
				case "grant":
					var _skill = GetArg(1, args);
					if (validAphid.Genes.Skills.ContainsKey(_skill))
					{
						Print("AphidPrognosis: No such skill exists!");
						return;
					}
					validAphid.Genes.Skills[_skill].GivePoints(Mathf.Clamp(GetInt(2, args, 1), 0, 10));
					return;
				case "hunger":
				case "h":
					var _hunger = GetInt(1, args, 1);
					validAphid.AddHunger(_hunger);
					return;
				case "thirst":
				case "t":
					var _thirst = GetInt(1, args, 1);
					validAphid.AddThirst(_thirst);
					return;
				case "rest":
				case "r":
					var _sleep = GetInt(1, args, 1);
					validAphid.AddRest(_sleep);
					return;
				case "bondship":
				case "b":
					var _bondship = GetInt(1, args, 1);
					validAphid.AddBondship(_bondship);
					return;
				case "forceactive":
					validAphid.Status.Mode = AphidData.EntityStatusType.Active;
					return;
			}
			Print("AphidPrgonosis: This command doesn't exist!");
		}
	}
	private class GrabBag : IConsoleCommand
	{
		public string HelpText => "Gives you an item. <give [string_id] (amount)>";

		public void Execute(string[] args)
		{
			if (!GetArg(0, args, out string _id))
				return;
			if (GlobalManager.G_ITEMS.TryGetValue(_id, out ItemData value))
			{
				int _amount = GetInt(1, args, 1);
				for (int i = 0; i < _amount; i++)
					PlayerInventory.StoreItem(_id);
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"GiveItem: {_id}({_amount}x) was added to your inventory.");
			}
			else
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"GiveItem: {_id} is not a valid item.");
		}
	}
	private class IKEA : IConsoleCommand
	{
		public string HelpText => "Spawns a structure. <build [string_id]>";

		public void Execute(string[] args)
		{
			if (!GetArg(0, args, out string _id))
				return;
			var _structurePath = GlobalManager.ABSOLUTE_STRUCTURES_DB_PATH + "/" + _id + ".tscn";
			if (ResourceLoader.Exists(_structurePath))
			{
				ResortManager.CreateStructure(_id, CameraManager.GetMouseToWorldPosition(),
						GetArg(1, args, null));
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"SpawnStructure: {_id} was created.");
			}
			else
				DebugLogger.Print(DebugLogger.LogPriority.Log, $"SpawnStructure: {_id} is not a valid item.");
		}
	}
	private class VisualNovel : IConsoleCommand
	{
		public string HelpText => "Visualize a dialog string in the console. <speak [id] (nodisplay)>";

		public void Execute(string[] args)
		{
			if (!GetArg(0, args, out string _key))
				return;

			if (_key.Equals(Instance.Tr(_key)))
			{
				DebugLogger.Print(DebugLogger.LogPriority.IgnorePriority, $"DialogSim: ID <{_key}> does not exist in the translation files.");
				return;
			}

			if (GetArg(1, args) == "nodisplay")
				DebugLogger.Print(DebugLogger.LogPriority.IgnorePriority, $"DialogSim: Displaying <{_key}>:", Instance.Tr(_key));
			else
				_ = DialogManager.Instance.OpenDialogBox(_key);
		}
	}
	private class Jaunt : IConsoleCommand
	{
		public string HelpText => "Teleports the player to the provided position. <tp [x] [y]>/<tp [safe]>";

		public void Execute(string[] args)
		{
			if (GetArg(0, args, out string _keyword) && _keyword.Equals("safe"))
			{
				if (GameManager.IsOutOfBounds(Player.Instance.GlobalPosition)
					|| GameManager.IsInsideGeometry(Player.Instance.GlobalPosition))
				{
					Player.Instance.GlobalPosition =
							RoomInstance.Instance.Doors[0].GlobalPosition
							+ (-RoomInstance.Instance.Doors[0].entryDirection) * 5;
					DebugLogger.Print(DebugLogger.LogPriority.Info, "PlayerTeleport: Unstucked player.");
				}
				else
					DebugLogger.Print(DebugLogger.LogPriority.Info, "PlayerTeleport: Player was supposedly in a valid position.");
				return;
			}

			Player.Instance.GlobalPosition = new Vector2(GetFloat(0, args, 0), GetFloat(1, args, 0));
			DebugLogger.Print(DebugLogger.LogPriority.Info, $"PlayerTeleport: Teleported to coordinates[{Player.Instance.GlobalPosition}]");
		}
	}
	private class Doors : IConsoleCommand
	{
		public string HelpText => "Moves the player to a new room. <room [name_id] [entry_index]>";

		public async void Execute(string[] args)
		{
			try
			{
				string _roomName = GetArg(0, args);
				int _entryIndex = GetInt(1, args, 0);
				await SceneManager.Load(_roomName, new SceneManager.RoomData(_roomName, _entryIndex, Vector2.Zero, Player.Instance.GlobalPosition));
			}
			catch (Exception _error)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Error, _error);
			}
		}
	}
	private class WhiteCollar : IConsoleCommand
	{
		public string HelpText => "Interfaces with the job system (room where a job board is must be loaded in). <jobs (reset/complete*) (index*)>";

		public void Execute(string[] args)
		{
			switch (GetArg(0, args))
			{
				case "clear":
				case "reset":
					JobMenu.Data.Available.Clear();
					JobMenu.Data.Active.Clear();
					JobMenu.Instance.SaveModule.CallSet();
					DebugLogger.Print(DebugLogger.LogPriority.Info, "DebugJob: Reseted jobs.");
					break;
				case "complete":
					int _index = GetInt(1, args, 0);
					JobMenu.Data.Active[_index].Fulfill();
					DebugLogger.Print(DebugLogger.LogPriority.Info, $"DebugJob: Completed job {_index}");
					break;
			}
		}
	}
	private class Printer : IConsoleCommand
	{
		public string HelpText => "Prints a statement on the console. Beware of leaking BBC tags.";

		public void Execute(string[] args)
		{
			DebugLogger.Print(DebugLogger.LogPriority.IgnorePriority, args);
		}
	}
	private class ExitWithoutSaving : IConsoleCommand
	{
		public string HelpText => "Quits the game without saving.";

		public void Execute(string[] args)
		{
			Instance.GetTree().Quit(69);
		}
	}

	// UNIT TESTS
	public class Run : IConsoleCommand
	{
		public string HelpText => "Runs unit tests. Development only.";
		public Dictionary<string, IRunCommand> Commands = new(){
			{ "ut_train", new UnitTest_SkillGain() },
			{ "ut_save", new UnitTest_SaveAndLoad() },
			{ "ut_balance", new UnitTest_FoodBalance() }
		};

		public void Execute(string[] args)
		{
			if (!GetArg(0, args, out string _id) || !Commands.TryGetValue(_id, out IRunCommand value))
			{
				DebugLogger.Print(DebugLogger.LogPriority.Info, "RunCommand: Command not found");
				return;
			}
			string[] _args = new string[args.Length - 1];
			if (_args.Length > 0)
				Array.Copy(args, 1, _args, 0, _args.Length);
			value.Run(_args);
		}
	}
	public interface IRunCommand
	{
		public void Run(string[] args);
	}
	public class UnitTest_SkillGain : IRunCommand
	{
		public void Run(string[] args)
		{
			int _tiredness = 0,
				_totalWaste = 0,
				_maxTired = GetInt(0, args, AphidData.MIN_REST_TO_WAKEUP),
				_minTired = GetInt(1, args, AphidData.MIN_REST_FOR_SLEEP),
				_maxAge = GetInt(3, args, AphidData.Age_Lifetime),
				_baseWasteTime = GetInt(2, args, 0),
				_wasteTime = _baseWasteTime,
				_baseSleepDecay = (int)(AphidData.BASE_REST_DECAY * 10),
				_sleepLossTimer = _baseSleepDecay,
				_baseTrainTime = 10 * 10,
				y = 0;

			double _pointsGained = 0, _sleepGainTimer = AphidData.BASE_REST_GAIN,
				_trainTimer = 100, _activeTime = 0, _sleepTime = 0;
			bool _sleeping = false;

			List<double[]> cycles = [];

			for (int i = 0; i < _maxAge * 10; i++)
			{
				if (_baseWasteTime > 0)
				{
					if (_wasteTime > 0)
						_wasteTime--;
					else
					{
						_wasteTime = _baseWasteTime;
						_totalWaste++;
						i += 10;
					}
				}
				if (_sleeping)
				{
					_sleepTime += 0.1f;
					if (_tiredness > _minTired)
					{
						if (_sleepGainTimer > 0)
							_sleepGainTimer -= 0.1f;
						else
						{
							_sleepGainTimer = AphidData.BASE_REST_GAIN;
							_tiredness--;
						}
					}
					else
					{
						cycles.Add([_activeTime, _sleepTime, _activeTime + _sleepTime, _pointsGained, y]);
						_pointsGained = _activeTime = _sleepTime = 0;
						_trainTimer = 10;
						_sleeping = false;
					}
				}
				else
				{
					_activeTime++;
					_trainTimer--;

					if (_trainTimer == 0)
					{
						_trainTimer = _baseTrainTime;
						if (i <= AphidData.Age_Adulthood * 10)
							_pointsGained += 0.2f;
						else
							_pointsGained += 0.1f;
					}

					if (_tiredness < _maxTired)
					{
						if (_sleepLossTimer > 0)
							_sleepLossTimer--;
						else
						{
							_sleepLossTimer = _baseSleepDecay;
							_tiredness++;
						}
					}
					else
					{
						y = i;
						_sleeping = true;
					}
				}
			}

			double _totalTime = cycles.Count > 0 ? cycles[0][2] : 1, _totalPoints = 0;
			cycles.Add([_activeTime, _sleepTime, _activeTime + _sleepTime, _pointsGained, (_activeTime + _sleepTime) / _totalTime]);
			cycles.ForEach(a => _totalPoints += a[3]);

			DebugLogger.Print(DebugLogger.LogPriority.Debug, string.Format("in the conditions: {4}s ({0}% max and {1}% min, {2}% difference, {3}s of waste time)", [ 100 - _minTired, 100 - _maxTired,
				Mathf.Max(_maxTired, _minTired) - Mathf.Min(_maxTired, _minTired), _totalWaste, _maxAge]));

			DebugLogger.Print(DebugLogger.LogPriority.Debug, string.Format("An aphid sleeps after {0}s and wakes up after {1}s with a gain of {2} levels, one cycle is in total {3}s for a max of {4} cycles with a lifetime gain of {5} levels",
				[(cycles[0][0] / 10).ToString("0.0"), cycles[0][1].ToString("0.0"), cycles[0][3].ToString("0.0"), cycles[0][2].ToString("0.0"), (cycles.Count - 1 + cycles[^1][4]).ToString("0.0"), _totalPoints.ToString("0.0")]));
		}
	}
	public class UnitTest_SaveAndLoad : IRunCommand
	{
		public class MyClass
		{
			public int class_value_1 { get; set; } = 1;
			public float class_value_2 { get; set; } = 2;
			public string class_value_3 { get; set; } = "3";
		}

		public void Run(string[] args)
		{
			var _array = new JsonArray
			{
				GlobalManager.GAME_VERSION,
				new MyClass()
				{
					class_value_1 = 69
				}
			};
			WritePretty(_array);
			var _prettyArray = ReadPretty();
			GD.Print(_prettyArray[0].ToString());
			GD.Print(_prettyArray[1].ToString());
		}

		public void WritePretty(JsonArray _array)
		{
			using FileAccess _stream = FileAccess.Open("user://test_one.data", FileAccess.ModeFlags.Write);
			_stream.StoreString(JsonSerializer.Serialize(_array, new JsonSerializerOptions() { WriteIndented = true }));
			_stream.Close();
		}
		public JsonArray ReadPretty()
		{
			using FileAccess _stream = FileAccess.Open("user://test_one.data", FileAccess.ModeFlags.Read);
			return JsonSerializer.Deserialize<JsonArray>(_stream.GetAsText());
		}
	}
	public class UnitTest_FoodBalance : IRunCommand
	{
		public void Run(string[] args)
		{
			var _list = GlobalManager.G_FOOD.OrderByDescending(f => f.Value.Flavor).ToDictionary();

			foreach (var _pair in _list)
				GD.Print(string.Format("|{0,5}|{1,5}|", _pair.Key, _pair.Value.Flavor.ToString()));
		}
	}

	[Orderer(SummaryOrderPolicy.FastestToSlowest)]
	public partial class TestForClassMatch : Node
	{
		[GodotBenchmark]
		public bool IsClass()
		{
			Sprite2D _sprite = new();
			return _sprite.IsClass("Node2D");
		}

		[Benchmark]
		public bool AsClass()
		{
			Sprite2D _sprite = new();
			return _sprite is Node2D;
		}
	}
}
#endif