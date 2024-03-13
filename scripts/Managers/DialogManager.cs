using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DialogManager : Control
{
	[Export] private Control dialogBox, dialogDoneSign;
	private AnimationPlayer dialogDoneAnimator;
	[Export] private RichTextLabel dialogText;
	[Export] private AudioStreamPlayer dialogAudio;

	private static RandomNumberGenerator RNG = new();
	private static DialogManager Instance;
	public static bool IsActive;
	private static bool MoveToNext, DialogFinished, JustPressed;
	// Dialog Params
	public static int Speed = 1, LettersPerBleep = 4, PaddingDelay = 0;
	public static readonly Dictionary<string, IDialogCommand> Commands = new()
	{
		{ "...", new DotCommand() }
	};
	public static readonly List<IDialogCommand> ActiveCommands = new();

	public override void _Ready()
	{
		Instance = this;
		IsActive = false;
		dialogDoneAnimator = dialogDoneSign.GetChild(0) as AnimationPlayer;
	}
	public override void _Process(double delta)
	{
		if (!IsActive)
			return;

		if (Input.IsActionJustPressed("interact"))
		{
			// This is done so an interaction that triggers a dialog box doesnt skip the first text
			if (JustPressed)
			{
				JustPressed = false;
				return;
			}

			// End Dialog sooner or pass to next dialog box
			if (!DialogFinished)
				DialogFinished = true;
			else
				MoveToNext = true;
		}

		if (Input.IsActionPressed("cancel"))
			DialogFinished = MoveToNext = true;
	}

	// ======| Dialog Functions |=======
	public static async Task OpenDialog(string[] _dialog_array, string _voice, string _action = "idle")
	{
		if (IsActive)
			return;

		// Set dialog state
		IsActive = JustPressed = true;
		Player.Instance.SetDisabled(true);
		if (!Instance.dialogBox.Visible)
			Instance.dialogBox.Show();

		// Get voice
		Instance.dialogAudio.Stream = ResourceLoader.Load<AudioStream>($"{GameManager.SFXPath}/dialog/{_voice}_{_action}.wav");

		// Index through all dialog
		for (int i = 0; i < _dialog_array.Length; i++)
		{
			Instance.dialogDoneSign.Hide();
			MoveToNext = DialogFinished = false;
			Instance.dialogText.Text = "";
			PaddingDelay = 0;

			await WriteDialog(Instance.Tr(_dialog_array[i]));
			await Task.Delay(1); // padding
			Instance.dialogDoneSign.Show();
			Instance.dialogDoneAnimator.Play("squiggly");
			while (!MoveToNext) // wait for the player to advance to the next box
				await Task.Delay(1);
		}

		CloseDialog();
	}
	private static async Task WriteDialog(string _dialog)
	{
		for (int i = 0; i < _dialog.Length; i++)
		{
			if (_dialog[i] == '[') // Command triggered
			{
				i += RegisterDialogCommand(i + 1, _dialog);
				continue;
			}
			Instance.dialogText.Text += _dialog[i];
			
			// Avoid doing this if dialog has been skipped
			if (!DialogFinished)
			{
				if (i % LettersPerBleep == 0) // Dialog bleep
				{
					Instance.dialogAudio.PitchScale = RNG.RandfRange(0.85f, 1.21f);
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
		DialogFinished = true;
		JustPressed = false;
	}
	public static void CloseDialog()
	{
		IsActive = false;
		Player.Instance.SetDisabled(false);
		Instance.dialogBox.Hide();
	}
	private static int RegisterDialogCommand(int _index, string _dialog)
	{
		if (_dialog[_index] == '/')
		{
			ActiveCommands[^1].Remove();
			ActiveCommands.RemoveAt(ActiveCommands.Count - 1);
			return 2;
		}

		int _skip = 0;
		string _command = "";
		for(int _c = _index; _c < _dialog.Length; _c++, _skip++)
		{
			if (_dialog[_c] == ']')
				break;
			_command += _dialog[_c];
		}

		if (Commands.ContainsKey(_command))
			Commands[_command].Execute();
		return _skip + 1;
	}

	public interface IDialogCommand
	{
		/// <summary>
		/// Executes the command, if the command is not self-ending -> (like a single-use delay),
		/// then make sure to add manually this class to ActiveCommands in order for [/] to end your action.
		/// </summary>
		public void Execute();
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
        public void Execute() =>
            PaddingDelay = 500;
    }
}
