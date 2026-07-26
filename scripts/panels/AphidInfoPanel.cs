using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class AphidInfoPanel : Control
{
	public static AphidInfoPanel Instance { get; set; }
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

	private StringName AVAILABLE_ANIM = new("available"), UNAVAILABLE_ANIM = new("unavailable"),
			OPEN_AVAILABLE = new("open_available"), CLOSE_AVAILABLE = new("close_available");

	private Aphid current_aphid = null;
	private readonly List<Node2D> nearby_aphids = [];

	/// <summary>
	/// Wheter or not is actively showing its information.
	/// </summary>
	public bool Enabled { get; private set; } = false;
	public DisplayMode CurrentDisplay = DisplayMode.Hidden;
	public enum DisplayMode
	{
		Hidden, Shown, PartiallyShown
	}
	public bool AreAphidsNearby { get; private set; } = false;
	public bool IsAphidPickedUp { get; private set; } = false;

	// MARK: Initialization
	public override void _EnterTree()
	{
		Instance = this;
		name_label.FocusExited += ApplyName;
		SceneManager.AddEventListener(ConnectEvents, SceneManager.EventEnum.OnPostLoad);
		showButton.Pressed += SetByButton;

		// fixes a small stutter that happens when opening the menu for the first time, by opening it briefly before start
		Show();
		menu_player.Play(StringNames.CloseAnim);
	}
	public override void _ExitTree()
	{
		Instance = null;
	}

	// MARK: Processing
	public override void _Process(double delta)
	{
		if (Enabled)
		{
			if (IsInstanceValid(current_aphid))
				UpdateGUI();
		}
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

	// MARK: Events
	private void ConnectEvents(SceneManager.SceneArgs _args)
	{
		Player.Instance.AddEventListener(OnPickup, Player.PickupEvents.OnPickup);
		Player.Instance.AddEventListener(OnDrop, Player.PickupEvents.OnDrop);
		Player.Instance.AddEventListener(OnInteractableEnter, Player.InteractableEvents.OnInteractableEnter);
		Player.Instance.AddEventListener(OnInteractableExit, Player.InteractableEvents.OnInteractableExit);
		CanvasManager.Menus.AddEventListener(OnMenuChange, MenuHandler.MenuEvents.OnPostSwitch);
	}
	private void OnPickup(Player.PickupArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;

		IsAphidPickedUp = true;
		if (Enabled)
			CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.CloseInfo);
		else
			CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.ShowInfo);
	}
	private void OnDrop(Player.PickupArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;

		IsAphidPickedUp = false;
		if (Enabled)
			SetTo(false, !AreAphidsNearby);
		else if (!AreAphidsNearby && _args.IsNoAnim)
			SetTo(false, true);
	}
	private void OnInteractableEnter(Player.InteractableArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;

		if (nearby_aphids.Contains(_args.Entity))
			return;

		nearby_aphids.Add(_args.Entity);

		// if there is aphids nearby now, and there weren't before, update interface
		bool _wereAphidsNearby = AreAphidsNearby;
		AreAphidsNearby = nearby_aphids.Count > 0;
		if (AreAphidsNearby && _wereAphidsNearby != AreAphidsNearby )
		{
			CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.ShowInfo);
			SetDisplayMode(DisplayMode.PartiallyShown);
		}
	}
	private void OnInteractableExit(Player.InteractableArgs _args)
	{
		if (_args.Tag != StringNames.GlobalTags.Aphid)
			return;

		if (!nearby_aphids.Remove(_args.Entity))
			return;

		// if there are no aphids nearby now, and there were before, update interface
		bool _wereAphidsNearby = AreAphidsNearby;
		AreAphidsNearby = nearby_aphids.Count > 0;
		if (!IsAphidPickedUp)
		{
			if (_wereAphidsNearby && !AreAphidsNearby)
				SetTo(false, true);
		}
	}
	private void OnMenuChange(MenuHandler.MenuArgs _args)
	{
		if (_args.Current?.Name == "pause" || _args.Next?.Name == "pause")
			return;

		SetTo(false, true);
	}

	// MARK: User Interface
	private void ApplyName()
	{
		if (current_aphid == null || string.IsNullOrWhiteSpace(name_label.Text))
			return;

		current_aphid.Instance.Genes.Name = name_label.Text;
	}
	private void UpdateGUI(bool _forceUpdate = false)
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
		sleepBar.Value = current_aphid.Instance.Status.Rest;
		bondshipBar.Value = current_aphid.Instance.Status.Bondship;

		List<Aphid.Skill> _skills = [.. current_aphid.Instance.Genes.Skills.Values.ToList()];

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

	// MARK: General
	/// <summary>
	/// Enables/Disables the information panel. This function should always be used when interfacing with this subpanel.
	/// </summary>
	/// <param name="_force">Force a change of state, this will also force it to fully hide when state is set to false</param>
	public static void SetTo(bool _state, bool _force = false)
	{
		if (!IsInstanceValid(Instance))
			return;

		if (Instance.Enabled == _state && !_force)
			return;

		if (_state && Instance.current_aphid == null)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "AphidInfo: Attempted to show aphid when none is selected.");
			return;
		}

		Instance.Enabled = _state;
		Instance.SetDisplayMode(_state ? DisplayMode.Shown : (_force ? DisplayMode.Hidden : DisplayMode.PartiallyShown));
		CanvasManager.RemoveControlPrompt(CanvasManager.ControlPrompt.ShowInfo);

		if (Instance.Enabled)
		{
			CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.CloseInfo, 1);
			Instance.UpdateGUI(true);
		}
		else
		{
			if (CanvasManager.Menus.Processing || CanvasManager.Menus.IsActive)
				return;
			if (Instance.AreAphidsNearby || Instance.IsAphidPickedUp)
				CanvasManager.AddControlPrompt(CanvasManager.ControlPrompt.ShowInfo, 1);	
		}
	}
	public void SetByButton()
	{
		if (FreeCameraManager.Enabled)
			SetTo(!Enabled);

		if (IsAphidPickedUp)
			SetByPickup();
		else
			SetByClosest();
	}
	public void SetByClosest()
	{
		if (Enabled)
			SetTo(false);
		else
		{
			current_aphid = SelectClosestAphid();
			if (current_aphid != null)
				SetTo(true);
		}
	}
	public void SetByPickup()
	{
		if (Enabled)
			SetTo(false);
		else
		{
			current_aphid = Player.Instance.HeldItem.Entity_Aphid;
			SetTo(true);
		}
	}

	/// <summary>
	/// Sets the current display animation for the panel. DO NOT USE THIS outside very niche applications, use SetTo() instead for more consistent behaviour.
	/// </summary>
	/// <param name="_mode">The animation mode to select</param>
	public void SetDisplayMode(DisplayMode _mode)
	{
		if (_mode == CurrentDisplay)
			return;

		switch (_mode)
		{
			case DisplayMode.PartiallyShown:
				switch (CurrentDisplay)
				{
					case DisplayMode.Hidden:
						menu_player.Play(AVAILABLE_ANIM);
						break;
					case DisplayMode.Shown:
						menu_player.Play(CLOSE_AVAILABLE);
						break;
				}
				break;
			case DisplayMode.Hidden:
				switch (CurrentDisplay)
				{
					case DisplayMode.PartiallyShown:
						menu_player.Play(UNAVAILABLE_ANIM);
						break;
					case DisplayMode.Shown:
						menu_player.Play(StringNames.CloseAnim);
						break;
				}
				break;
			case DisplayMode.Shown:
				switch (CurrentDisplay)
				{
					case DisplayMode.Hidden:
						menu_player.Play(StringNames.OpenAnim);
						break;
					case DisplayMode.PartiallyShown:
						menu_player.Play(OPEN_AVAILABLE);
						break;
				}
				break;
		}
		CurrentDisplay = _mode;
	}

	/// <summary>
	/// Sets the given aphid as the one to display information of.
	/// </summary>
	/// <param name="_aphid">The aphid to show, if given null, it deselects the aphid.</param>
	public void SelectAphid(Aphid _aphid)
	{
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
}
