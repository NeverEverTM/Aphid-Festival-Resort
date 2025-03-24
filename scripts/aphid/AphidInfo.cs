using System.Collections.Generic;
using System.Reflection;
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
	[Export] private Label staminaLabel, strengthLabel, speedLabel, intelligenceLabel;

	private Aphid aphid;

	public override void _Ready()
	{
		name_label.FocusEntered += () => CanvasManager.SetFocus(name_label);
		name_label.FocusExited += SetAphidName;

		Player.Instance.OnFind += (string _tag, Node2D _node) =>
		{
			if (aphid != null || _tag != Aphid.Tag || _node.Equals(aphid))
				return;
			Display(_node);
		};
		Player.Instance.OnLose += (_, _node) =>
		{
			// do not drop info if current aphid isnt the one we are dropping or the player has them in their hands
			if (aphid == null || !aphid.Equals(_node) || _node.Equals(Player.Instance.HeldPickup.aphid))
				return;
			Display(null);
		};
	}
	public override void _Process(double delta)
	{
		if (aphid != null)
		{
			UpdateAphidInfo();
			if (Player.Instance.IsDisabled)
				Display(null);
		}
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

		if (aphid == null)
			return;

		if (string.IsNullOrWhiteSpace(name_label.Text))
			return;

		aphid.Instance.Genes.Name = name_label.Text;
	}
	private void Display(Node2D _node)
	{
		if (_node != null)
		{
			aphid = _node as Aphid;
			menu_player.Play("open");
		}
		else
		{
			aphid = null;
			menu_player.Play("close");
		}
	}

	StringName stamina = new("bio_skill_stamina"), strength = new("bio_skill_strength"),
			intelligence = new("bio_skill_intelligence"), speed = new("bio_skill_speed");

	private void UpdateAphidInfo()
	{
		name_label.Text = aphid.Instance.Genes.Name;

		foodBar.Value = aphid.Instance.Status.Hunger;
		waterBar.Value = aphid.Instance.Status.Thirst;
		affectionBar.Value = aphid.Instance.Status.Affection;
		sleepBar.Value = 100 - aphid.Instance.Status.Tiredness;
		bondshipBar.Value = aphid.Instance.Status.Bondship;

		List<Aphid.Skill> _skills = [.. aphid.Instance.Genes.Skills];

		staminaBar.Value = _skills[0].Points; // stamina
		staminaLabel.Text = Tr(stamina) + $" (lvl {_skills[0].Level})";

		strengthBar.Value = _skills[1].Points; // strength
		strengthLabel.Text = Tr(strength) + $" (lvl {_skills[1].Level})";

		intelligenceBar.Value = _skills[2].Points; // intelligence
		intelligenceLabel.Text = Tr(intelligence) + $" (lvl {_skills[2].Level})";

		speedBar.Value = _skills[3].Points; // speed
		speedLabel.Text = Tr(speed) + $" (lvl {_skills[3].Level})";

		ageDisplay.Texture = age[aphid.Instance.Status.IsAdult ? 1 : 0];
	}
}
