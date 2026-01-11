using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class AphidInfo : Control
{
	public static AphidInfo Instance { get; set; }
	[Export] private AnimationPlayer menu_player;
	[Export] private BaseButton showButton;
	[ExportCategory("Bio")]
	[Export] private TextEdit name_label;
	[Export] private TextureRect ageDisplay;
	[Export] private Texture2D[] age = new Texture2D[2];
	[ExportGroup("Status Bars")]
	[Export] private TextureProgressBar foodBar, waterBar, affectionBar, sleepBar, bondshipBar;
	[ExportGroup("Skill Bars")]
	[Export] private TextureProgressBar staminaBar, strengthBar, speedBar, intelligenceBar;
	[Export] private Label staminaLabel, strengthLabel, speedLabel, intelligenceLabel;

	private Aphid current_aphid;
	private List<Node2D> nearby_aphids = [];
	private StringName AVAILABLE_ANIM = new("available"), UNAVAILABLE_ANIM = new("unavailable"),
			OPEN_AVAILABLE = new("open_available"), CLOSE_AVAILABLE = new("close_available");

	public bool IsBeingDisplayed { get; private set; }
	public bool AreAphidsNearby { get; private set; }
	public bool IsAphidPickedUp { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
		name_label.FocusExited += SetCurrentAphidName;
		SceneManager.AddEventListener(ConnectEvents, SceneManager.EventEnum.OnPostLoad);
		showButton.Pressed += () => Display(!IsBeingDisplayed);
	}
	public override void _ExitTree()
	{
		Display(false);
		Instance = null;
	}

	public override void _Process(double delta)
	{
		if (IsBeingDisplayed && IsInstanceValid(current_aphid))
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
	private void ConnectEvents(SceneManager.SceneArgs _args)
	{
		Player.Instance.AddEventListener(OnPickup, Player.PickupEventEnum.OnPickup);
		Player.Instance.AddEventListener(OnDrop, Player.PickupEventEnum.OnDrop);
		Player.Instance.AddEventListener(OnInteractableEnter, Player.InteractableEventEnum.OnInteractableEnter);
		Player.Instance.AddEventListener(OnInteractableExit, Player.InteractableEventEnum.OnInteractableExit);
		CanvasManager.Menus.OnSwitch.Add((_l, _m) =>
		{
			if (_m != null && _m.Equals("pause"))
				return;
			Display(false);
		});
	}
	private void OnPickup(Player.PickupArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;

		IsAphidPickedUp = true;
		SelectAphid(Player.Instance.HeldPickup.Entity_Aphid);
		SetControlPrompt();
	}
	private void OnDrop(Player.PickupArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;

		IsAphidPickedUp = false;
		SelectAphid(null);
		Display(false);
	}
	private void OnInteractableEnter(Player.InteractableEventArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;

		if (nearby_aphids.Contains(_args.Entity))
			return;

		nearby_aphids.Add(_args.Entity);

		// if there is aphids nearby now, and there weren't before, update interface
		bool _wereAphidsNearby = AreAphidsNearby;
		AreAphidsNearby = nearby_aphids.Count > 0;
		if (AreAphidsNearby && _wereAphidsNearby != AreAphidsNearby)
		{
			SetControlPrompt();
			Instance.menu_player.Play(AVAILABLE_ANIM);
		}
	}
	private void OnInteractableExit(Player.InteractableEventArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;
		nearby_aphids.Remove(_args.Entity);

		// if there are no aphids nearby now, and there were before, update interface
		bool _wereAphidsNearby = AreAphidsNearby;
		AreAphidsNearby = nearby_aphids.Count > 0;
		if (!IsAphidPickedUp && !AreAphidsNearby && _wereAphidsNearby != AreAphidsNearby)
		{
			if (IsBeingDisplayed)
				Display(false, true);
			else
			{
				SetControlPrompt();
				Instance.menu_player.Play(UNAVAILABLE_ANIM);
			}
		}
	}
	// Interface
	private void SetCurrentAphidName()
	{
		if (current_aphid == null || string.IsNullOrWhiteSpace(name_label.Text))
			return;

		current_aphid.Instance.Genes.Name = name_label.Text;
	}

	// Main Behaviour
	public void Display(bool _state, bool _force = false)
	{
		if (IsBeingDisplayed == _state)
			return;
		IsBeingDisplayed = _state;

		if (_force)
			menu_player.Play(IsBeingDisplayed ? StringNames.OpenAnim : StringNames.CloseAnim);
		else
			menu_player.Play(IsBeingDisplayed ? OPEN_AVAILABLE : CLOSE_AVAILABLE);
		if (IsBeingDisplayed && current_aphid != null)
			Update(true);

		SetControlPrompt();
	}
	public void DisplayClosestAphid(bool _force = false)
	{
		if (IsBeingDisplayed)
			Display(false, _force);
		else
		{
			current_aphid = SelectClosestAphid();
			if (current_aphid != null)
				Display(true, _force);
			else
				Display(false, _force);
		}
	}
	private void SetControlPrompt()
	{
		CanvasManager.RemoveControlPrompt(InputNames.Pull);

		if (IsBeingDisplayed)
			CanvasManager.AddControlPrompt("close_info", InputNames.Pull, InputNames.Pull);
		else if (IsAphidPickedUp || AreAphidsNearby)
			CanvasManager.AddControlPrompt("show_info", InputNames.Pull, InputNames.Pull);
	}

	/// <summary>
	/// Allows to select an aphid to show information for.
	/// </summary>
	/// <param name="_aphid">The aphid to show, if given null, it deselects the aphid.</param>
	public void SelectAphid(Aphid _aphid = null)
	{
		// if null or we already have one, deselect
		if (_aphid == null || current_aphid != null)
			current_aphid = null;

		// otherwise, select the given one, or find the closest one
		else if (_aphid != null)
			current_aphid = _aphid;
	}
	/// <summary>
	/// Select closest aphid to Player
	/// </summary>
	public Aphid SelectClosestAphid()
	{
		float _minDistance = float.PositiveInfinity;
		Node2D _closestAphid = null;
		for (int i = 0; i < nearby_aphids.Count; i++)
		{
			float _min = Player.Instance.GlobalPosition.DistanceSquaredTo(nearby_aphids[i].GlobalPosition);
			if (_min < _minDistance)
			{
				_minDistance = _min;
				_closestAphid = nearby_aphids[i];
			}
		}

		return _closestAphid as Aphid;
	}

	private void Update(bool _forceUpdate = false)
	{
		if (_forceUpdate)
		{
			if (name_label.HasFocus())
				name_label.ReleaseFocus();
			name_label.Text = current_aphid.Instance.Genes.Name;
		}
		else
		{
			if (!name_label.HasFocus())
				name_label.Text = current_aphid.Instance.Genes.Name;
		}

		foodBar.Value = current_aphid.Instance.Status.Hunger;
		waterBar.Value = current_aphid.Instance.Status.Thirst;
		affectionBar.Value = current_aphid.Instance.Status.Affection;
		sleepBar.Value = 100 - current_aphid.Instance.Status.Tiredness;
		bondshipBar.Value = current_aphid.Instance.Status.Bondship;

		List<AphidActions.Skill> _skills = [.. current_aphid.Instance.Genes.Skills.Values.ToList()];

		staminaBar.Value = _skills[0].Points; // stamina
		staminaLabel.Text = Tr("bio_skill_stamina") + $" (lvl {_skills[0].Level})";

		strengthBar.Value = _skills[1].Points; // strength
		strengthLabel.Text = Tr("bio_skill_strength") + $" (lvl {_skills[1].Level})";

		intelligenceBar.Value = _skills[2].Points; // intelligence
		intelligenceLabel.Text = Tr("bio_skill_intelligence") + $" (lvl {_skills[2].Level})";

		speedBar.Value = _skills[3].Points; // speed
		speedLabel.Text = Tr("bio_skill_speed") + $" (lvl {_skills[3].Level})";

		ageDisplay.Texture = age[current_aphid.Instance.Status.IsAdult ? 1 : 0];
	}
}
