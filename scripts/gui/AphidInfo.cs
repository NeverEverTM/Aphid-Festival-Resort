using Godot;

public partial class AphidInfo : Control
{
	[Export] private AnimationPlayer aphid_info_panel;
	[ExportCategory("Bio")]
	[Export] private TextEdit name_label;
	[Export] private BaseButton show_hash_button;
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
		name_label.FocusExited += SetName;
		show_hash_button.Pressed += CopyHash;


		Player.Instance.OnPickup += OnAphidPickup;
		Player.Instance.OnDrop += OnAphidDrop;
	}
	public override void _Input(InputEvent _event)
	{
		if (!CanvasManager.IsInFocus)
			return;

		if (_event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			if (_input.KeyLabel == Key.Enter)
			{
				GetViewport().SetInputAsHandled();
				CanvasManager.RemoveFocus();
				return;
			}

			// allow character deletion and moving through the text
			if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
				return;

			if (name_label.Text.Length > 20)
				GetViewport().SetInputAsHandled();
		}
	}

	private void SetName()
	{
		if (aphid == null)
			return;

		aphid.Instance.Genes.Name = name_label.Text;
		CanvasManager.RemoveFocus();
	}
	private void CopyHash()
	{
		if (aphid == null)
			return;

		DisplayServer.ClipboardSet(aphid.Instance.ID);
	}

	private void OnAphidPickup(string _tag)
	{
		if (_tag != "aphid")
			return;

		aphid_info_panel.Play("swipe_right");
		aphid = Player.Instance.PickupItem as Aphid;
		UpdateAphidInfo();
	}
	private void OnAphidDrop()
	{
		aphid = null;
		aphid_info_panel.Play("swipe_left");
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
