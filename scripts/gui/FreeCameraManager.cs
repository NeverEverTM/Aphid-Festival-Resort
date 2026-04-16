using Godot;

/// <summary>
/// In charge of the Free Camera mode in the resort
/// </summary>
/// TODO: Refactor this entire thing to be less of a monster, maybe ill divide it in modules
public partial class FreeCameraManager : Control
{
	[Export] private AnimationPlayer animator;
	[Export] private TextureButton cameraMenuButton, focusedAphidButton, pingAphidButton;
	[Export] private Control buttonGrid;
	[Export] private RichTextLabel spectatorLabel;
	[Export] private HSlider zoomLevelSlider;

	public static FreeCameraManager Instance { get; private set; }
	public static bool Enabled { get; private set; } = false;
	public static bool IsBusy { get; private set; } = false;

	private bool is_focusing_aphids, is_hud_visible, just_loaded;
	private int focused_aphid_index;
	private Tween vanish_tween;
	private bool is_camera_tab_open;
	private TextureRect current_tracker;
	private Aphid current_aphid;
	private static Vector2 APHID_OFFSET = new(-16, -36);

	public override void _EnterTree()
	{
		Instance = this;
	}
	public override void _Ready()
	{
		SetProcessUnhandledInput(false);
		SetProcessShortcutInput(false);
		vanish_tween = CreateTween();
		vanish_tween.Kill();

		// button functions
		cameraMenuButton.Pressed += OnCameraButton;
		zoomLevelSlider.ValueChanged += OnZoomSlider;
		focusedAphidButton.Pressed += OnFocusAphid;
		pingAphidButton.Pressed += () => TrackAphid(CameraManager.FocusedAphid);
	}
	public override void _ExitTree()
	{
		Instance = null;
		Enabled = false;
		IsBusy = false;
	}

	// MARK: Processing
	public override void _Process(double delta)
	{
		if (Enabled)
		{
			if (is_focusing_aphids && !IsInstanceValid(CameraManager.FocusedAphid))
				StopFocus(); // if the aphid goes away for whatever reason, clear it out
		}

		if (IsInstanceValid(current_tracker))
			ProcessAphidTrack(); // keep tracking aphid even when the interface is off
	}
	private void ProcessAphidTrack()
	{
		Vector2 _aphidPos = CameraManager.GetWorldToCanvasPosition(current_aphid.GlobalPosition);

		if (!vanish_tween.IsValid() && current_aphid.GlobalPosition.DistanceTo(Player.Instance.GlobalPosition) < 100)
		{
			vanish_tween = current_tracker.CreateTween();
			vanish_tween.TweenProperty(current_tracker, "self_modulate", new Color(0), 1);
			vanish_tween.Finished += () =>
			{
				if (IsInstanceValid(current_tracker))
					current_tracker.QueueFree();
			};
		}
		// if is on screen, point at it
		if (_aphidPos.X > 0 && _aphidPos.X < CameraManager.SCREEN_SIZE_CANVAS.X
				&& _aphidPos.Y > 0 && _aphidPos.Y < CameraManager.SCREEN_SIZE_CANVAS.Y)
			current_tracker.GlobalPosition = _aphidPos + APHID_OFFSET;
		else // if is far away, point at its direction
			current_tracker.GlobalPosition = CameraManager.SCREEN_CENTER_CANVAS + CameraManager.SCREEN_CENTER_CANVAS.DirectionTo(_aphidPos) * 100;

	}
	public override void _UnhandledInput(InputEvent @event)
	{
		// no input action is done when a sub-menu is open
		if (CanvasManager.Menus.IsActive)
		{
			if (BuildMenu.Menu.IsOpen) // besides zoom scroll during the build menu...
				ProcessZoomScroll(@event);
			return;
		}

		OnEscapePressed(@event);

		// pings aphids location
		if (@event.IsActionPressed(InputNames.Pull))
		{
			if (IsInstanceValid(CameraManager.FocusedAphid) && !CameraManager.FocusedAphid.Equals(current_aphid))
				TrackAphid(CameraManager.FocusedAphid);
			else
				StopTrack();
		}

		if (@event.IsActionPressed(InputNames.ShowInfo) && is_focusing_aphids)
			AphidInfo.Instance.SetTo(!AphidInfo.Instance.Enabled);

		// changes focus mode
		if (@event.IsActionPressed(InputNames.OpenInventory))
		{
			if (!is_focusing_aphids)
				FocusAphid(focused_aphid_index);
			else
				StopFocus();
		}

		ProcessZoomScroll(@event);
		// processes focus mode inputs
		if (is_focusing_aphids && IsInstanceValid(CameraManager.FocusedAphid))
			ProcessSpectator(@event);
	}
	public override void _ShortcutInput(InputEvent @event)
	{
		if (!Enabled || animator.IsPlaying())
			return;

		if (@event.IsActionPressed(InputNames.QuickAction1))
			_ = CanvasManager.Menus.SetTo(BuildMenu.Menu);
		else if (@event.IsActionPressed(InputNames.QuickAction2))
			_ = CanvasManager.Menus.SetTo(FurnitureShop.Instance.Menu);
		else if (@event.IsActionPressed(InputNames.QuickAction3))
			OnCameraButton();
	}

	private void ProcessZoomScroll(InputEvent @event)
	{
		if (@event is not InputEventMouseButton || !(@event as InputEventMouseButton).Pressed)
			return;
		InputEventMouseButton _mouse = @event as InputEventMouseButton;
		if (_mouse.ButtonIndex == MouseButton.WheelUp)
			CameraManager.SetCameraZoom(0.25f, true);
		else if (_mouse.ButtonIndex == MouseButton.WheelDown)
			CameraManager.SetCameraZoom(-0.25f, true);
		zoomLevelSlider.SetValueNoSignal(CameraManager.Instance.Zoom.X);
	}
	private void ProcessSpectator(InputEvent @event)
	{
		if (@event.IsActionPressed(InputNames.Left))
			FocusAphid(focused_aphid_index - 1);
		else if (@event.IsActionPressed(InputNames.Right))
			FocusAphid(focused_aphid_index + 1);
	}
	
	public void FocusAphid(Aphid _aphid)
	{
		int _index = ResortManager.Current.Aphids.FindIndex(0, (a) => a.Equals(_aphid));
		FocusAphid(_index);
	}
	public void FocusAphid(int _index)
	{
		if (IsBusy || ResortManager.Current.Aphids.Count == 0)
			return;

		_index = _index < 0 ? ResortManager.Current.Aphids.Count - 1 : _index;
		_index = _index == ResortManager.Current.Aphids.Count ? 0 : _index;
		focused_aphid_index = _index;

		is_focusing_aphids = true;
		spectatorLabel.Show();

		CameraManager.Focus(ResortManager.Current.Aphids[focused_aphid_index]);
		spectatorLabel.Text = $"{Tr("camera_spectating")}\n<| {CameraManager.FocusedAphid.Instance.Genes.Name} |>";
		AphidInfo.Instance.SelectAphid(ResortManager.Current.Aphids[focused_aphid_index]);
		AphidInfo.Instance.SetDisplayMode(AphidInfo.DisplayMode.PartiallyShown);
		SoundManager.CreateSound("ui/button_select");
	}
	public static void StopFocus()
	{
		Instance.is_focusing_aphids = false;
		CameraManager.UnFocus();
		AphidInfo.Instance.SetTo(false, false);
		Instance.spectatorLabel.Hide();
	}
	
	public void TrackAphid(Aphid _aphid)
	{
		if (_aphid == null)
			return;
		// if we already have a tracker delete it
		if (IsInstanceValid(current_tracker))
		{
			StopTrack();
			return;
		}

		// create tracker
		current_aphid = _aphid;
		TextureRect _track = new()
		{
			Texture = GlobalManager.GetIcon(_aphid.Instance.Status.IsAdult ?
					"aphid_adult" : "aphid_child"),
			Scale = new(2, 2)
		};
		current_tracker = _track;

		// set it to vanish after a while
		Timer _vanishTimer = new();
		_track.AddChild(_vanishTimer);

		_vanishTimer.Timeout += () =>
		{
			vanish_tween = CreateTween();
			vanish_tween.TweenProperty(_track, "self_modulate", new Color("white"), 5);
			vanish_tween.Finished += () =>
			{
				if (IsInstanceValid(_track))
					_track.QueueFree();
			};
		};

		CanvasManager.Instance.AddChild(_track);
		_vanishTimer.Start(30);
		SoundManager.CreateSound("ui/switch_mode");
	}
	public void StopTrack()
	{
		if (!IsInstanceValid(current_tracker))
			return;
		vanish_tween.Kill();
		current_tracker?.QueueFree();
		current_aphid = null;
	}

	/// <summary>
	/// When escaping, exit the interface, unless we are in a focus, then only exit the focus
	/// </summary>
	/// <param name="event"></param>
	private void OnEscapePressed(InputEvent @event)
	{
		if (just_loaded)
		{
			just_loaded = false;
			return;
		}

		if (@event.IsActionPressed(InputNames.ChangeCamera))
			SetTo(false);

		if (@event.IsActionPressed(InputNames.Cancel) || @event.IsActionPressed(InputNames.Escape))
		{
			if (CameraManager.FocusedAphid != null)
			{
				AphidInfo.Instance.SetTo(false, true);
				CameraManager.UnFocus();
			}
			else
				SetTo(false);
		}
	}

	// MARK: Button Functions
	private void OnCameraButton()
	{
		if (!Enabled)
			return;

		is_camera_tab_open = !is_camera_tab_open;
		if (is_camera_tab_open)
			animator.Play("open_submenu");
		else
			animator.Play("close_submenu");
	}
	private void OnZoomSlider(double _value)
	{
		if (!Enabled)
			return;

		CameraManager.SetCameraZoom((float)_value);
	}
	private void OnFocusAphid()
	{
		if (!Enabled)
			return;

		if (!is_focusing_aphids)
			FocusAphid(focused_aphid_index);
		else
			StopFocus();
	}

	// MARK: Enabling
	/// <summary>
	/// Switches on and off the Free Camera interface.
	/// </summary>
	public static void Set() => SetTo(!Enabled);
	/// <summary>
	/// Sets the Free Camera interface on or off.
	/// </summary>
	/// <param name="_state"></param>
	public static void SetTo(bool _state)
	{
		if (IsBusy)
		{
			SoundManager.CreateSound("ui/button_fail");
			return;
		}

		if (Enabled == _state)
		{
			SoundManager.CreateSound("ui/button_fail");
			return;
		}

		IsBusy = true;
		Instance.just_loaded = true;
		Enabled = _state;

		if (_state)
			Instance.Enable();
		else
			Instance.Disable();
	}
	/// <summary>
	/// Sets the state HUD of the free camera interface.
	/// </summary>
	/// <param name="_state">Wheter or not is visible</param>
	/// <param name="_noTransition">Wheter or not a transition should play when changing states</param>
	public static void SetHUDTo(bool _state, bool _noTransition = false)
	{
		if (_state == Instance.is_hud_visible)
			return;

		if (_noTransition)
			Instance.Visible = _state;
		else
			Instance.animator.Play(_state ? StringNames.OpenAnim : StringNames.CloseAnim);

		AphidInfo.Instance.SetTo(false, true);
		Instance.is_camera_tab_open = false;
		Instance.is_hud_visible = _state;
	}

	private void Enable()
	{
		// Set camera free from player anchor
		CameraManager.UnFocus();
		CameraManager.EnableFreeRoam = true;

		SetProcessUnhandledInput(true);
		SetProcessShortcutInput(true);
		Player.Instance.SetDisabled(true, true);

		// Show free camera hud and hide other elements
		SetHUDTo(true);
		CanvasManager.ClearControlPrompts();
		CanvasManager.SetHUDTo(false);

		// activate hud buttons
		for (int i = 0; i < buttonGrid.GetChildCount(); i++)
			(buttonGrid.GetChild(i) as BaseButton).Disabled = false;

		IsBusy = false;
	}
	private void Disable()
	{
		// deactivate hud buttons
		for (int i = 0; i < Instance.buttonGrid.GetChildCount(); i++)
			(Instance.buttonGrid.GetChild(i) as BaseButton).Disabled = true;

		SetProcessUnhandledInput(false);
		SetProcessShortcutInput(false);

		// set the canvas back to visible
		CanvasManager.ClearControlPrompts();
		SetHUDTo(false);
		CanvasManager.SetHUDTo(true);

		is_focusing_aphids = is_camera_tab_open = just_loaded = false;
		spectatorLabel.Hide();
		zoomLevelSlider.SetValueNoSignal(CameraManager.DEFAULT_CAMERA_ZOOM);

		PlayDisableAnim().Finished += () =>
		{
			IsBusy = false;
			CameraManager.Focus(Player.Instance);
			CameraManager.SetCameraZoom(CameraManager.DEFAULT_CAMERA_ZOOM);
			Player.Instance.SetDisabled(false, true);
		};
	}

	private Tween PlayDisableAnim()
	{
		CameraManager.EnableFreeRoam = false;

		var _disableAnim = CameraManager.Instance.CreateTween();
		_disableAnim.SetParallel();
		_disableAnim.SetEase(Tween.EaseType.Out);
		_disableAnim.SetTrans(Tween.TransitionType.Circ);
		_disableAnim.TweenProperty(CameraManager.Instance, "zoom",
				new Vector2(CameraManager.DEFAULT_CAMERA_ZOOM, CameraManager.DEFAULT_CAMERA_ZOOM), 0.2f).FromCurrent();
		_disableAnim.TweenProperty(CameraManager.Instance, "position",
				Player.Instance.GlobalPosition, 0.2f).FromCurrent();

		return _disableAnim;
	}
}
