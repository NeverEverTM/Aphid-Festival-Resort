using System.Threading.Tasks;
using Godot;

public partial class NPCBehaviour : AnimatedSprite2D, Player.IPlayerInteractable
{
	public const string Tag = "npc";
	[ExportCategory("Dialog Params")]
	[Export] private string[] dialogue_keys;
	/// <summary>
	/// Linear: Read lines in order, repeat the last one once you reach it
	/// Loop: Read lines in order, go back to the start once you reach the end
	/// Random: Randomly speak a line from the list
	/// </summary>
	private enum NPCDialogueMode { Linear, Loop, Random }
	[Export] private NPCDialogueMode DialogueMode = NPCDialogueMode.Linear;
	[Export] private AudioStream DefaultVoice;
	private int speak_index = -1;
	[ExportCategory("Behaviour Params")]
	[Export] protected bool FlipDirection { get; set; }
	[Export] public bool IsInteractable = true, IsBusy = false;
	[Export] protected float tickSpeed = 6;

	public override void _Ready()
	{
		Play("default");
	}
    public override void _PhysicsProcess(double delta)
    {
       TickFlip((float)delta);
    }
    public virtual async void Interact()
	{
		if (IsBusy || !IsInteractable)
			return;
		IsBusy = true;

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
		SetFlipDirection(Player.Instance.GlobalPosition - GlobalPosition);
		Player.Instance.SetFlipDirection(GlobalPosition - Player.Instance.GlobalPosition);
		_ = DialogManager.Instance.OpenDialogBox(dialogue_keys[speak_index], DefaultVoice);
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
		IsBusy = false;
	}
	public void TickFlip(float delta) =>
		Scale = new(Mathf.Lerp(Scale.X, FlipDirection ? 1 : -1, (float)delta * tickSpeed), Scale.Y);
	public void SetFlipDirection(Vector2 _direction)
	{
		if (_direction.X < 0)
			FlipDirection = true;
		else if (_direction.X > 0)
			FlipDirection = false;
	}
}