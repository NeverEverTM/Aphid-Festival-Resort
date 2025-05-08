using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class AphidInfo : Control
{
	public static AphidInfo Instance { get; set; }
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
	private readonly List<Node2D> nearby_aphids = [];
	public static bool Enabled { get; private set; }
	public static bool Available { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
		Available = false;
		Enabled = false;
	}
	public override void _ExitTree()
	{
		Instance = null;
		Available = false;
		Enabled = false;
	}

	public override void _Ready()
	{
		name_label.FocusExited += SetName;

		Player.Instance.OnPickup += (_tag, _node) =>
		{
			if (_tag == Aphid.Tag)
			{
				nearby_aphids.Remove(_node);
				SetAphid(Player.Instance.HeldPickup.aphid);
			}
		};
		Player.Instance.OnDrop += (_tag, _node) =>
		{
			if (_tag == Aphid.Tag)
				SetAphid(null);
		};
		Player.Instance.OnInteractableEnter += (_tag, _node) =>
		{
			if (_tag == Aphid.Tag && !nearby_aphids.Contains(_node))
			{
				nearby_aphids.Add(_node);
                SetControlPrompt();
			}
		};
		Player.Instance.OnInteractableExit += (_tag, _node) =>
		{
			if (_tag == Aphid.Tag)
			{
				nearby_aphids.Remove(_node);
                SetControlPrompt();
			}
		};
	}
	public override void _Process(double delta)
	{
		Available = nearby_aphids.Count > 0;
		if (Enabled && aphid != null)
			Update();
	}
	public override void _Input(InputEvent @event)
	{
		if (!name_label.HasFocus() || !(@event is InputEventKey && @event.IsPressed()))
			return;

		var _input = @event as InputEventKey;
		// unfocus
		if (_input.KeyLabel == Key.Enter || _input.KeyLabel == Key.Escape)
		{
			name_label.ReleaseFocus();
			AcceptEvent();
			return;
		}

		// allow character deletion and moving through the text
		if (_input.KeyLabel == Key.Backspace || _input.KeyLabel == Key.Left || _input.KeyLabel == Key.Right)
			return;

		// limit for name length
		if (name_label.Text.Length > 20)
			AcceptEvent();
	}

	private void SetName()
	{
		if (aphid == null || string.IsNullOrWhiteSpace(name_label.Text))
			return;

		aphid.Instance.Genes.Name = name_label.Text;
	}
	public static void Display(bool _state)
	{
		if (Enabled == _state)
			return;
		Enabled = _state;

		Instance.menu_player.Play(Enabled ? StringNames.OpenAnim : StringNames.CloseAnim);
		if (Enabled)
			Instance.Update(true);

        Instance.SetControlPrompt();
	}
	private void SetControlPrompt()
	{
		CanvasManager.RemoveControlPrompt(InputNames.Pull);
		Available = nearby_aphids.Count > 0;

		if (Enabled)
			CanvasManager.AddControlPrompt("close_info", InputNames.Pull, InputNames.Pull);
		else if (Available)
			CanvasManager.AddControlPrompt("show_info", InputNames.Pull, InputNames.Pull);
	}
	public static void SetAphid()
	{
		// if we have an aphid already, unfocus
		if (Instance.aphid != null)
		{
			Instance.aphid = null;
			Display(false);
			return;
		}

		// get closest aphid and show info for that one
		float _minDistance = float.PositiveInfinity;
		Node2D _aphid = null;
		for (int i = 0; i < Instance.nearby_aphids.Count; i++)
		{
			var _min = Player.Instance.GlobalPosition.DistanceSquaredTo(Instance.nearby_aphids[i].GlobalPosition);
			if (_min < _minDistance)
			{
				_minDistance = _min;
				_aphid = Instance.nearby_aphids[i];
			}
		}

		if (_aphid != null)
			Instance.aphid = _aphid as Aphid;
		else
			Logger.Print(Logger.LogPriority.Log, "AphidInfo: No aphid available");
		Display(true);
	}
	public static void SetAphid(Aphid _aphid)
	{
		Instance.aphid = _aphid;
		Display(_aphid != null);
	}
	private void Update(bool _forceUpdate = false)
	{
		if (_forceUpdate)
		{
			if (name_label.HasFocus())
				name_label.ReleaseFocus();
			name_label.Text = aphid.Instance.Genes.Name;
		}
		else
		{
			if (!name_label.HasFocus())
				name_label.Text = aphid.Instance.Genes.Name;
		}

		foodBar.Value = aphid.Instance.Status.Hunger;
		waterBar.Value = aphid.Instance.Status.Thirst;
		affectionBar.Value = aphid.Instance.Status.Affection;
		sleepBar.Value = 100 - aphid.Instance.Status.Tiredness;
		bondshipBar.Value = aphid.Instance.Status.Bondship;

		List<Aphid.Skill> _skills = [.. aphid.Instance.Genes.Skills.Values.ToList()];

		staminaBar.Value = _skills[0].Points; // stamina
		staminaLabel.Text = Tr("bio_skill_stamina") + $" (lvl {_skills[0].Level})";

		strengthBar.Value = _skills[1].Points; // strength
		strengthLabel.Text = Tr("bio_skill_strength") + $" (lvl {_skills[1].Level})";

		intelligenceBar.Value = _skills[2].Points; // intelligence
		intelligenceLabel.Text = Tr("bio_skill_intelligence") + $" (lvl {_skills[2].Level})";

		speedBar.Value = _skills[3].Points; // speed
		speedLabel.Text = Tr("bio_skill_speed") + $" (lvl {_skills[3].Level})";

		ageDisplay.Texture = age[aphid.Instance.Status.IsAdult ? 1 : 0];
	}
}
