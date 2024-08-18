using Godot;

public partial class AphidInfo : Control
{
	[Export] private AnimationPlayer menu_player;
	[ExportCategory("Bio")]
	[Export] private TextEdit name_label;
	[Export] private TextureProgressBar bondship;
	[Export] private TextureRect ageDisplay;
	[Export] private Texture2D[] age = new Texture2D[2];
	[ExportCategory("Stats Knobs")]
	[Export] private Control hungerK;
	[Export] private Control thirstK, affectionK;

	private const int offsetY = -9, lengthSize = 240;
	private Aphid aphid;

	public override void _Ready()
	{
		name_label.FocusEntered += () => CanvasManager.SetFocus(name_label);
		name_label.FocusExited += SetAphidName;

		Player.Instance.OnPickup += OnAphidPickup;
		Player.Instance.OnDrop += OnAphidDrop;
	}
	public override void _Input(InputEvent _event)
	{
		if (!Visible)
			return;

		if (!CanvasManager.IsFocus(name_label))
			return;

		if (_event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			// unfocus
			if (_input.KeyLabel == Key.Enter)
			{
				GetViewport().SetInputAsHandled();
				CanvasManager.RemoveFocus();
				return;
			}

			// allow character deletion and moving through the text
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			// limit for name length
			if (name_label.Text.Length > 20)
				GetViewport().SetInputAsHandled();
		}
	}

	private void SetAphidName()
	{
		if (CanvasManager.IsFocus(name_label))
			CanvasManager.RemoveFocus();

		if (aphid == null )
			return;

		if (string.IsNullOrWhiteSpace(name_label.Text))
			return;

		aphid.Instance.Genes.Name = name_label.Text;
	}

	private void OnAphidPickup(string _tag)
	{
		if (_tag != "aphid")
			return;

		menu_player.Play("swipe_right");
		aphid = Player.Instance.PickupItem as Aphid;
		UpdateAphidInfo();
	}
	private void OnAphidDrop()
	{
		aphid = null;
		menu_player.Play("swipe_left");
	}
	private void UpdateAphidInfo()
	{
		name_label.Text = aphid.Instance.Genes.Name;

		hungerK.SetPosition(new(aphid.Instance.Status.Hunger * 0.01f * lengthSize, offsetY));
		thirstK.SetPosition(new(aphid.Instance.Status.Thirst * 0.01f * lengthSize, offsetY));
		affectionK.SetPosition(new(aphid.Instance.Status.Affection * 0.01f * lengthSize, offsetY));

		ageDisplay.Texture = age[aphid.Instance.Status.IsAdult ? 1 : 0];
		bondship.Value = aphid.Instance.Status.Bondship;
	}
}
