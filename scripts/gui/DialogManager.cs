using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DialogManager : Control
{
	[Export] private Control dialogDoneSign;
	[Export] private RichTextLabel dialogText;
	[Export] private AudioStreamPlayer dialogAudio;
	private AnimationPlayer dialogDoneAnimator;

	public static DialogManager Instance { get; private set; }
	public static bool IsActive { get; private set; }
	public static bool IsDialogFinished { get; private set; }
	private bool MoveToNext, JustPressed;

	private const char commandStart = '(', commandEnd = ')';

	// Dialog Params
	public string Dialog { get; set; }
	public int Speed { get; set; }
	public int LettersPerBleep { get; set; }
	public int PaddingDelay { get; set; }
	public static string[] CurrentArgs { get; set; }
	public static readonly Dictionary<string, IDialogCommand> Commands = new()
	{
		{ "...", new DotCommand() },
		{ "name", new PlayerNameCommand() },
		{ "char", new VoiceCommand() }
	};
	public static readonly List<IDialogCommand> ActiveCommands = new();

	public override void _Ready()
	{
		Instance = this;
		IsActive = IsDialogFinished = false;

		Speed = 1;
		LettersPerBleep = 4;
		PaddingDelay = 0;

		dialogDoneAnimator = dialogDoneSign.GetChild(0) as AnimationPlayer;
	}
	public override void _Process(double delta)
	{
		if (!IsActive)
			return;

		// End Dialog sooner or pass to next dialog box
		if (Input.IsActionJustPressed("interact"))
		{
			// This is done so it doesnt skip the first text
			if (JustPressed)
			{
				JustPressed = false;
				return;
			}

			if (!IsDialogFinished)
				IsDialogFinished = true;
			else
				MoveToNext = true;
		}

		// ultra fast text skip
		if (Input.IsActionPressed("cancel"))
		{
			dialogAudio.Stop();
			IsDialogFinished = MoveToNext = true;
		}
	}

	// ======| Dialog Functions |=======
	public async Task OpenDialog(string _dialog_key)
	{
		if (IsActive)
			return;
		// Set dialog state
		IsActive = JustPressed = true;
		Player.Instance.SetDisabled(true);
		if (!Instance.Visible)
			Instance.Show();

		// Index through all dialog
		string[] _dialogueLines = Instance.Tr(_dialog_key).Split("\n");
		try
		{
			for (int i = 0; i < _dialogueLines.Length; i++)
			{
				// reset dialog state
				Instance.dialogDoneSign.Hide();
				MoveToNext = IsDialogFinished = false;
				Instance.dialogText.Text = "";
				PaddingDelay = 0;

				// get current dialog box by finding current splits
				Dialog = Instance.Tr(_dialogueLines[i]).Replace(">", string.Empty);
				while (true)
				{
					if (i + 1 == _dialogueLines.Length || _dialogueLines[i + 1].StartsWith(">"))
						break;

					Dialog += "\n" + _dialogueLines[i + 1];
					i++;
				}
				if (CurrentArgs != null)
					Dialog = string.Format(Dialog, CurrentArgs);

				// write dialog into box
				await WriteDialog();
				await Task.Delay(1); // padding
				Instance.dialogDoneSign.Show();
				Instance.dialogDoneAnimator.Play("squiggly");
				while (!MoveToNext) // wait for the player to advance to the next box
					await Task.Delay(1);
			}
		}
		catch (Exception _err)
		{
			Logger.Print(Logger.LogPriority.Error, "DialogManager: Failed to complete dialog of key: ", _dialog_key, _err);
		}

		CloseDialog();
	}
	private async Task WriteDialog()
	{
		RandomNumberGenerator _rng = new();
		for (int i = 0; i < Dialog.Length; i++)
		{
			if (Dialog[i] == commandStart) // Command active
			{
				i += RegisterDialogCommand(i + 1);
				continue;
			}

			if (Dialog[i] == '[') //  BBC active
			{
				while (Dialog[i] != ']')
				{
					Instance.dialogText.Text += Dialog[i];
					i++;
				}
				Instance.dialogText.Text += Dialog[i];
				continue;
			}

			Instance.dialogText.Text += Dialog[i];

			// Avoid doing this if dialog has been skipped
			if (!IsDialogFinished)
			{
				if (i % LettersPerBleep == 0) // Dialog bleep
				{
					Instance.dialogAudio.PitchScale = _rng.RandfRange(0.9f, 1.15f);
					Instance.dialogAudio.Play();
				}
				if (PaddingDelay > 0)
				{
					await Task.Delay(PaddingDelay);
					PaddingDelay = 0;
				}

				await Task.Delay(Speed * 10);
			}
		}
		IsDialogFinished = true;
		JustPressed = false;
	}
	public void CloseDialog()
	{
		Dialog = "";
		IsActive = false;
		Player.Instance.SetDisabled(false);
		Instance.Hide();
	}
	private int RegisterDialogCommand(int _index)
	{
		if (Dialog[_index] == '/')
		{
			ActiveCommands[^1].Remove();
			ActiveCommands.RemoveAt(ActiveCommands.Count - 1);
			return 2;
		}

		int _skip = 0;
		// get command line
		string _command = "";
		for (int _c = _index; _c < Dialog.Length; _c++, _skip++)
		{
			if (Dialog[_c] == commandEnd || Dialog[_c] == commandStart)
			{
				_skip++;
				break;
			}
			_command += Dialog[_c];
		}
		// get args in parenthesis
		string _argLine = "";
		if (Dialog[_index + _skip - 1] == commandStart)
		{
			for (int _c = _index + _skip; _c < Dialog.Length; _c++, _skip++)
			{
				if (Dialog[_c] == commandEnd)
				{
					_skip += 2;
					break;
				}
				_argLine += Dialog[_c];
			}
		}
		string[] _args = _argLine.Split(",");

		if (Commands.ContainsKey(_command))
			Commands[_command].Execute(_index + _skip, _args);
		return _skip;
	}

	public interface IDialogCommand
	{
		/// <summary>
		/// Executes the command, if the command is not self-ending -> (like a single-use delay),
		/// then make sure to add manually this class to ActiveCommands in order for [/] to end your action.
		/// </summary>
		public void Execute(int _index, string[] _args);
		/// <summary>
		/// Ends your action via the last [/] seen.
		/// </summary>
		public void Remove()
		{
			return;
		}
	}
	public class DotCommand : IDialogCommand
	{
		public void Execute(int _, string[] _args)
		{
			float _padding;
			if (_args.Length == 0 || !float.TryParse(_args[0], out _padding))
				_padding = 0.5f;

			Instance.PaddingDelay = (int)(_padding * 1000);
		}
	}
	public class PlayerNameCommand : IDialogCommand
	{
		public void Execute(int _index, string[] _args) =>
			Instance.Dialog = Instance.Dialog.Insert(_index, Player.Data.Name ??= "Superstar");
	}
	public class VoiceCommand : IDialogCommand
	{
		public void Execute(int _index, string[] _args)
		{
			// Get voice
			string _voice = _args[0];
			Instance.dialogAudio.Stream = ResourceLoader.Load<AudioStream>($"{GameManager.SFXPath}/dialog/{_voice}_idle.wav");
		}
	}
}