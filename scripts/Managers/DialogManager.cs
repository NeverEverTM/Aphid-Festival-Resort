using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DialogManager : Control
{
	[Export] private Control dialogDoneSign;
	[Export] private RichTextLabel dialogText;
	[Export] private AudioStreamPlayer dialogAudio;
	private AnimationPlayer dialogDoneAnimator;

	private static DialogManager Instance;
	public static bool IsActive { get; set; }
	private static bool MoveToNext, DialogFinished, JustPressed;
	
	// Dialog Params
	public static string Dialog { get; set; }
	public static int Speed { get; set; } 
	public static int LettersPerBleep  { get; set; } 
	public static int PaddingDelay { get; set; }
	public static string[] CurrentArgs { get; set; }
	public static readonly Dictionary<string, IDialogCommand> Commands = new()
	{
		{ "...", new DotCommand() },
		{ "name", new PlayerNameCommand() }
	};
	public static readonly List<IDialogCommand> ActiveCommands = new();

	public override void _Ready()
	{
		Instance = this;
		IsActive = false;

		Speed = 1;
		LettersPerBleep = 4;
		PaddingDelay = 0;

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
	public static async Task OpenDialog(string[] _dialog_array, string _voice)
	{
		if (IsActive)
			return;

		// Set dialog state
		IsActive = JustPressed = true;
		if (!Instance.Visible)
			Instance.Show();

		if (Player.Instance.PickupItem != null)
			Player.Instance.Drop();
		Player.Instance.SetDisabled(true);

		// Get voice
		Instance.dialogAudio.Stream = ResourceLoader.Load<AudioStream>($"{GameManager.SFXPath}/dialog/{_voice}_idle.wav");

		// Index through all dialog
		for (int i = 0; i < _dialog_array.Length; i++)
		{
			Instance.dialogDoneSign.Hide();
			MoveToNext = DialogFinished = false;
			Instance.dialogText.Text = "";
			PaddingDelay = 0;

			Dialog = Instance.Tr(_dialog_array[i]);
			if (CurrentArgs != null)
				Dialog = string.Format(Dialog, CurrentArgs);

			await WriteDialog();
			await Task.Delay(1); // padding
			Instance.dialogDoneSign.Show();
			Instance.dialogDoneAnimator.Play("squiggly");
			while (!MoveToNext) // wait for the player to advance to the next box
				await Task.Delay(1);
		}

		CloseDialog();
	}
	private static async Task WriteDialog()
	{
		RandomNumberGenerator _rng = new();
		for (int i = 0; i < Dialog.Length; i++)
		{
			if (Dialog[i] == '[') // Command triggered
			{
				i += RegisterDialogCommand(i + 1);
				continue;
			}
			Instance.dialogText.Text += Dialog[i];
			
			// Avoid doing this if dialog has been skipped
			if (!DialogFinished)
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
		DialogFinished = true;
		JustPressed = false;
	}
	public static void CloseDialog()
	{
		IsActive = false;
		Player.Instance.SetDisabled(false);
		Instance.Hide();
	}
	private static int RegisterDialogCommand(int _index)
	{
		if (Dialog[_index] == '/')
		{
			ActiveCommands[^1].Remove();
			ActiveCommands.RemoveAt(ActiveCommands.Count - 1);
			return 2;
		}

		int _skip = 0;
		string _command = "";
		for(int _c = _index; _c < Dialog.Length; _c++, _skip++)
		{
			if (Dialog[_c] == ']')
				break;
			_command += Dialog[_c];
		}
		_skip++;

		if (Commands.ContainsKey(_command))
			Commands[_command].Execute(_index + _skip);
		return _skip;
	}

	public interface IDialogCommand
	{
		/// <summary>
		/// Executes the command, if the command is not self-ending -> (like a single-use delay),
		/// then make sure to add manually this class to ActiveCommands in order for [/] to end your action.
		/// </summary>
		public void Execute(int _index);
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
        public void Execute(int _) =>
            PaddingDelay = 500;
    }
    public class PlayerNameCommand : IDialogCommand
    {
        public void Execute(int _index) =>
            Dialog = Dialog.Insert(_index, Player.Data.Name);
    }
}