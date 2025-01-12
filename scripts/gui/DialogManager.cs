using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DialogManager : Control
{
	[Export] private Control dialogDoneSign;
	[Export] private RichTextLabel dialogText;
	[Export] private AudioStreamPlayer dialogAudio;
	[Export] private AudioStream defaultAudio;
	private AnimationPlayer dialogDoneAnimator;

	public static DialogManager Instance { get; private set; }
	public static bool IsActive { get; private set; }
	public static bool IsDialogFinished { get; private set; }
	private bool move_to_next, just_pressed;

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

	public override void _Ready()
	{
		Instance = this;
		IsActive = IsDialogFinished = false;

		Speed = 1;
		LettersPerBleep = 2;

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
			if (just_pressed)
			{
				just_pressed = false;
				return;
			}

			if (!IsDialogFinished)
				IsDialogFinished = true;
			else
				move_to_next = true;
		}

		// ultra fast text skip
		if (Input.IsActionPressed("cancel"))
		{
			dialogAudio.Stop();
			IsDialogFinished = move_to_next = true;
		}
	}

	// ======| Dialog Functions |=======
	/// <summary>
	/// Opens the dialog box and formats, writes and displays the result.
	/// </summary>
	/// <param name="_dialog_key">The TranslationKey from which read, no further formatting is needed.</param>
	/// <param name="_voice">The voice to start the dialogue with. Can be changed midway through with dialogue commands.</param>
	/// <returns></returns>
	public async Task OpenDialogBox(string _dialog_key, AudioStream _voice = null)
	{
		if (IsActive)
			return;

		// Set dialog state
		IsActive = just_pressed = true;
		dialogAudio.Stream = _voice ?? defaultAudio;
		Player.Instance.SetDisabled(true, true);
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
				move_to_next = IsDialogFinished = false;
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
				while (!move_to_next) // wait for the player to advance to the next box
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
		dialogText.VisibleCharacters = 0;
		dialogText.Text = Dialog;
		int _bbcSkipped = 0;
		RandomNumberGenerator _rng = new();
		int i = 0;
		while (i < dialogText.Text.Length)
		{
			// dialog commands start with '(', they are triggered and then redacted out of dialog 
			if (dialogText.Text[i] == '(')
				RegisterDialogCommand(i);
			
			// bbc isnt accounted for in visiblecharacters but it is in length
			// so we need to get how much we need to skip forward, then set visiblecharacters to index minus skipped bbc characters
			if (dialogText.Text[i] == '[')
			{
				while (dialogText.Text[i] != ']')
				{
					i++;
					_bbcSkipped++;
				}
				i++;
				_bbcSkipped++;
			}

			// Pre setup
			if (!IsDialogFinished)
			{
				// Queued Delay for the next sentence
				while (PaddingDelay > 0)
				{
					if (IsDialogFinished) // this means we skipped mid-delay
					{
						PaddingDelay = 0;
						break;
					}
					PaddingDelay--;
					await Task.Delay(1);
				}
			}

			dialogText.VisibleCharacters = i + 1 - _bbcSkipped;

			// Post setup
			if (!IsDialogFinished)
			{
				// Dialog bleep
				if (i % LettersPerBleep == 0)
				{
					dialogAudio.PitchScale = _rng.RandfRange(0.9f, 1.15f);
					dialogAudio.Play();
				}

				await Task.Delay(Speed * 20);
			}
			i++;
		}
		IsDialogFinished = true;
		just_pressed = false;
	}
	public void CloseDialog()
	{
		Dialog = "";
		IsActive = false;
		Player.Instance.SetDisabled(false);
		Instance.Hide();
	}
	private void RegisterDialogCommand(int _startIndex)
	{
		// ignore the first '('
		int _characterToDelete = 1;
		int _index = _startIndex + 1;
		// get command line
		string _command = "";
		while (_index < Instance.dialogText.Text.Length)
		{
			_characterToDelete++;
			if (Instance.dialogText.Text[_index] == ')' || Instance.dialogText.Text[_index] == '[')
				break; // finish if you find end of command or start of args
			_command += Instance.dialogText.Text[_index];
			_index++;
		}

		// get args in parenthesis if command has a set of parenthesis too
		string _argLine = null;
		if (dialogText.Text[_index] == '[')
		{
			_index++;
			while (_index < dialogText.Text.Length)
			{
				_characterToDelete++;
				if (dialogText.Text[_index] == ']')
				{
					_characterToDelete++;
					break;
				}
				_argLine += dialogText.Text[_index];
				_index++;
			}
		}
		// args are splited by a coma
		string[] _args = _argLine?.Split(",");

		// execute command, remove it from dialogue and insert given text if any 
		if (Commands.ContainsKey(_command))
		{
			string _textToInsert =  Commands[_command].Execute(_args) ?? string.Empty;
			dialogText.Text = dialogText.Text.Remove(_startIndex, _characterToDelete);
			dialogText.Text = dialogText.Text.Insert(_startIndex, _textToInsert);
		}
	}

	public interface IDialogCommand
	{
		public string Execute(string[] _args)
		{
			return string.Empty;
		}
		public void Finish()
		{
			return;
		}
	}
	public class DotCommand : IDialogCommand
	{
		public string Execute(string[] _args)
		{
			float _padding = 0.3f;
			if (_args != null)
				_ = float.TryParse(_args[0], out _padding);

			Instance.PaddingDelay = (int)(_padding * 100);
			return string.Empty;
		}
	}
	public class PlayerNameCommand : IDialogCommand
	{
		public string Execute(string[] _) =>
			Player.Data.Name;
	}
	public class VoiceCommand : IDialogCommand
	{
		public string Execute(string[] _args)
		{
			// Get voice
			string _voice = _args[0];
			Instance.dialogAudio.Stream = ResourceLoader.Load<AudioStream>($"{GlobalManager.SFXPath}/dialog/{_voice}_idle.wav");
			return string.Empty;
		}
	}
    public class PronounsPossesive : IDialogCommand
    {
        public string Execute(string[] _args)
        {
           if (OptionsManager.Settings.Locale == "es_ES")
		   {
				return "w";
		   }
		   return "e";
        }
    }
}