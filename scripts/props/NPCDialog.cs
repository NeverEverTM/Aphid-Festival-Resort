using System.Threading.Tasks;
using Godot;

public partial class NPCDialog : AnimatedSprite2D, Player.IObjectInteractable
{
	[ExportCategory("Dialog Params")]
	[Export] private string[] dialogue_keys;
	/// <summary>
	/// Linear: Read lines in order, repeat the last one once you reach it
	/// Loop: Read lines in order, go back to the start once you reach the end
	/// Random: Randomly speak a line from the list
	/// </summary>
	public enum NPCDialogueMode { Linear, Loop, Random }
	[Export] public NPCDialogueMode DialogueMode = NPCDialogueMode.Linear;
	private int speak_index = -1;
	[ExportCategory("Behaviour Params")]
	[Export] public bool FlipDirection { get; private set; }
	[Export] public bool IsWandering, IsInteractable = true;
	[Export] public float tickSpeed = 6;

	public override void _Ready()
	{
		Play("default");
	}
	public override void _Process(double delta)
	{
		Scale = new(Mathf.Lerp(Scale.X, FlipDirection ? 1 : -1, (float)delta * tickSpeed), Scale.Y);

		if (IsWandering)
		{
			// TODO: Wander off code goes here, just copy the one from aphids
		}
	}

	public async void Interact()
	{
		if (!IsInteractable)
			return;
		IsInteractable = false;

		switch (DialogueMode)
		{
			case NPCDialogueMode.Linear:
				speak_index = Mathf.Min(speak_index + 1, dialogue_keys.Length - 1);
				break;
			case NPCDialogueMode.Loop:
				speak_index++;
				if (speak_index == dialogue_keys.Length)
					speak_index = 0;
				break;
			case NPCDialogueMode.Random:
				speak_index = new RandomNumberGenerator().RandiRange(0, dialogue_keys.Length - 1);
				break;
		}

		Play("talk");
		SetFlipDirection(GlobalPosition.DirectionTo(Player.Instance.GlobalPosition));
		_ = DialogManager.Instance.OpenDialog(dialogue_keys[0]);
		bool _check = true;
		while (DialogManager.IsActive)
		{
			await Task.Delay(1);
			if (_check != DialogManager.IsDialogFinished)
			{
				_check = DialogManager.IsDialogFinished;
				if (DialogManager.IsDialogFinished)
					Play("default");
				else
					Play("talk");
			}
		}
		Play("default");
		IsInteractable = true;
	}

	public void SetFlipDirection(Vector2 _direction)
	{
		if (_direction.X < 0)
			FlipDirection = true;
		else if (_direction.X > 0)
			FlipDirection = false;
	}
}
