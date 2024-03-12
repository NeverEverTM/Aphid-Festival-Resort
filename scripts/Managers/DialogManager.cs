using Godot;
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
		Instance.dialogAudio.Stream = ResourceLoader.Load<AudioStream>($"{GameManager.DialogVoicePath}/{_voice}_{_action}.wav");

		// Index through all dialog
		for (int i = 0; i < _dialog_array.Length; i++)
		{
			Instance.dialogDoneSign.Hide();
			MoveToNext = DialogFinished = false;
			Instance.dialogText.Text = "";

			await WriteDialog(Instance.Tr(_dialog_array[i]));
			await Task.Delay(1); // padding
			Instance.dialogDoneSign.Show();
			Instance.dialogDoneAnimator.Play("squiggly");
			while (!MoveToNext) // wait for the player to advance to the next box
				await Task.Delay(1);
		}

		CloseDialog();
	}
	private static async Task WriteDialog(string _BBCtext)
	{
		for (int i = 0; i < _BBCtext.Length; i++)
		{
			if (DialogFinished)
			{
				Instance.dialogText.Text = _BBCtext;
				break;
			}
			if (i % 4 == 0)
			{
				Instance.dialogAudio.PitchScale = RNG.RandfRange(0.85f, 1.21f);
				Instance.dialogAudio.Play();
			}

			Instance.dialogText.Text += _BBCtext[i];
			await Task.Delay(10);
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
}
