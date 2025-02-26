using Godot;

public partial class AphidInfo : Control
{
	[Export] private AnimationPlayer menu_player;
	[ExportCategory("Bio")]
	[Export] private TextEdit name_label;
	[Export] private TextureRect ageDisplay;
	[Export] private Texture2D[] age = new Texture2D[2];
	[ExportGroup("Status Bars")]
	[Export] private TextureProgressBar foodBar, waterBar, affectionBar, sleepBar, bondshipBar;
	[ExportGroup("Skill Bars")]
	[Export] private TextureProgressBar staminaBar, strengthBar, speedBar, intelligenceBar;

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
		if (!CanvasManager.IsFocus(name_label))
			return;

		if (_event is InputEventKey && _event.IsPressed())
		{
			InputEventKey _input = _event as InputEventKey;

			// unfocus
			if (_input.KeyLabel == Key.Enter || _input.KeyLabel == Key.Escape)
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

		menu_player.Play("open");
		aphid = Player.Instance.HeldPickup.aphid;
		UpdateAphidInfo();
	}
	private void OnAphidDrop()
	{
		if (aphid == null)
			return;
		aphid = null;
		menu_player.Play("close");
	}
	private void UpdateAphidInfo()
	{
		name_label.Text = aphid.Instance.Genes.Name;

		foodBar.Value = aphid.Instance.Status.Hunger;
		waterBar.Value = aphid.Instance.Status.Thirst;
		affectionBar.Value = aphid.Instance.Status.Affection;
		sleepBar.Value = 100 - aphid.Instance.Status.Tiredness;
		bondshipBar.Value = aphid.Instance.Status.Bondship;

		staminaBar.Value = aphid.Instance.Genes.Skills.Find((s) => s.Name == "stamina").Points;
		strengthBar.Value = aphid.Instance.Genes.Skills.Find((s) => s.Name == "strength").Points;
		speedBar.Value = aphid.Instance.Genes.Skills.Find((s) => s.Name == "speed").Points;
		intelligenceBar.Value = aphid.Instance.Genes.Skills.Find((s) => s.Name == "intelligence").Points;

		ageDisplay.Texture = age[aphid.Instance.Status.IsAdult ? 1 : 0];
	}
}
