using Godot;
using System.Collections.Generic;

public partial class NPCDialog : AnimatedSprite2D
{
	[Export] private Area2D dialogArea;
	[ExportCategory("Dialog Params")]
	[Export] private string character;
	[Export] private string action;
	private bool IsNearby, IsFlipped;

	public override void _Ready()
	{
		dialogArea.AreaEntered += (Area2D _node) => IsNearby = true;
		dialogArea.AreaExited += (Area2D _node) => IsNearby = false;
		Play($"{character}_idle");
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsNearby)
			return;
		if (Input.IsActionJustPressed("interact") && !DialogManager.IsActive)
		{
			ActivateDialog();
			GetViewport().SetInputAsHandled();
		}
	}
    public override void _Process(double delta) =>
		TickFlip((float)delta);
    private async void ActivateDialog()
	{
		List<string> _dialogue = new();
		for (int i = 0; i < 99; i++)
		{
			string _line = $"{character}_{action}_{i}";
			if (Tr(_line) == _line)
				break;
			_dialogue.Add(_line);
		}
		Play($"{character}_talk");
		SetFlipDirection(Player.Instance.GlobalPosition - GlobalPosition);
		await DialogManager.OpenDialog(_dialogue.ToArray(), character);
		Play($"{character}_idle");
	}

	public void SetFlipDirection(Vector2 _direction)
	{
		if (_direction.X < 0)
			IsFlipped = true;
		else if (_direction.X > 0)
			IsFlipped = false;
	}
	public void TickFlip(float _delta)
	{
		if (IsFlipped)
			Scale = new(Mathf.Lerp(Scale.X, 1, _delta * 5), Scale.Y);
		else
			Scale = new(Mathf.Lerp(Scale.X, -1, _delta * 5), Scale.Y);
	}
}
