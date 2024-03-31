using Godot;

public partial class NPCDialog : Sprite2D
{
	[Export] private Area2D dialogArea;
	[Export] private AnimationPlayer animator;
	[ExportCategory("Dialog Params")]
	[Export] private int lines;
	[Export] private string character, action;
	private bool IsNearby, IsFlipped;

	public override void _Ready()
	{
		dialogArea.AreaEntered += (Area2D _node) => IsNearby = true;
		dialogArea.AreaExited += (Area2D _node) => IsNearby = false;
		animator.Play($"{character}/idle");
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
		string[] _lines = new string[lines];
		for (int i = 0; i < _lines.Length; i++)
			_lines[i] = $"{character}_{action}_{i}";
			animator.Play($"{character}/talk");
		SetFlipDirection(Player.Instance.GlobalPosition - GlobalPosition);
		await DialogManager.OpenDialog(_lines, character);
		animator.Play($"{character}/idle");
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
