using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NPCBehaviour : AnimatedSprite2D, Player.IPlayerInteractable
{
	/// <summary>
	/// Linear: Read lines in order, repeat the last one once you reach it
	/// Loop: Read lines in order, go back to the start once you reach the end
	/// Random: Randomly speak a line from the list
	/// </summary>
	private enum NPCDialogueMode { Linear, Loop, Random }
	public const string Tag = "npc";

	[ExportCategory("Dialog Params")]
	[Export] private string ID;
	[Export] private NPCDialogueMode DialogueMode = NPCDialogueMode.Linear;
	private int speak_index = -1;
	private List<string> current_lines = [];
	private List<string> spoken_lines = []; // for Random
	[ExportCategory("Behaviour Params")]
	[Export] protected bool flipDirection;
	[Export] public bool isInteractable = true, isBusy = false;
	[Export] protected float tickSpeed = 1.0f;

	public override void _Ready()
	{
		Play("default");
		SetDialogueLines();
	}
    public override void _PhysicsProcess(double delta)
    {
       TickFlip((float)delta);
    }
	public void SetDialogueLines(string _action = "flavor")
	{
		int _index = 0;
		string _key = $"{ID}_{_action}_{_index}";
		while(!_key.Equals(Tr($"{ID}_{_action}_{_index}")))
		{
			current_lines.Add(_key);
			_index++;
			_key = $"{ID}_{_action}_{_index}";
		}
	}
    public virtual async void Interact()
	{
		if (isBusy || !isInteractable)
			return;
		isBusy = true;

		string _dialogue_key = string.Empty;
		switch (DialogueMode)
		{
			case NPCDialogueMode.Linear:
				speak_index = Mathf.Min(speak_index + 1, current_lines.Count - 1);
				_dialogue_key = current_lines[speak_index];
				break;
			case NPCDialogueMode.Loop:
				speak_index++;
				if (speak_index == current_lines.Count)
					speak_index = 0;
				_dialogue_key = current_lines[speak_index];
				break;
			case NPCDialogueMode.Random:
				if (spoken_lines.Count == 0)
					spoken_lines = [.. current_lines];
				var _newIndex = new RandomNumberGenerator().RandiRange(0, spoken_lines.Count - 1);
				_dialogue_key = spoken_lines[_newIndex];
				spoken_lines.RemoveAt(_newIndex);
				break;
		}

		Play("talk");
		SetFlipDirection(Player.Instance.GlobalPosition - GlobalPosition);
		Player.Instance.SetFlipDirection(GlobalPosition - Player.Instance.GlobalPosition);
		// TODO; DialogManager should use an actor object to manipulate rather than to do it manually
		_ = DialogManager.Instance.OpenDialogBox(_dialogue_key, SoundManager.GetAudioStream("dialog/" + $"{ID}_idle"));
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
		isBusy = false;
	}
	public void TickFlip(float delta) =>
		Scale = new(Mathf.Lerp(Scale.X, flipDirection ? 1 : -1, (float)delta * (6 * tickSpeed)), Scale.Y);
	public void SetFlipDirection(Vector2 _direction)
	{
		if (_direction.X < 0)
			flipDirection = true;
		else if (_direction.X > 0)
			flipDirection = false;
	}
}