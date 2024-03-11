using Godot;
using System.Threading.Tasks;

public partial class DialogManager : Control
{
	[Export] private Control dialogBox;
	[Export] private RichTextLabel dialogText;

	private static DialogManager Instance;
	public static bool IsActive;
	private static bool MoveToNext, DialogFinished, JustPressed;

    public override void _Ready()
    {
        Instance = this;
		IsActive = false;
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

			if (!DialogFinished)
				DialogFinished = true;
			else
				MoveToNext = true;
		}

		if (Input.IsActionPressed("cancel"))
			DialogFinished = MoveToNext = true;
    }

	// ======| Dialog Functions |=======
    public static async Task OpenDialog(string[] _dialog_array)
	{
		if (IsActive)
			return;
		IsActive = JustPressed = true;
		Player.Instance.SetDisabled(true);

		if (!Instance.dialogBox.Visible)
			Instance.dialogBox.Show();

		for (int i = 0; i < _dialog_array.Length; i++)
		{
			MoveToNext = DialogFinished = false;
			Instance.dialogText.Text = "";

			await WriteDialog(Instance.Tr(_dialog_array[i]));
			await Task.Delay(1);
			while (!MoveToNext)
				await Task.Delay(1);
		}

		CloseDialog();
	}
	public static async Task OpenDialog(string _dialog) =>
		await OpenDialog(new string[] { _dialog });
	private static async Task WriteDialog(string _BBCtext)
	{
		for (int i = 0; i < _BBCtext.Length; i++)
		{
			if (DialogFinished)
			{
				Instance.dialogText.Text = _BBCtext;
				break;
			}
			if (i % 2 == 0)
			{
				//Sound
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
