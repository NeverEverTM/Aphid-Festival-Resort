using Godot;

public partial class FreeCameraManager : Control
{
	[Export] private AnimationPlayer animator;
	[Export] private TextureButton cameraMenuButton, focusedAphidButton, pingAphidButton;
	[Export] private Control buttonGrid;
	[Export] private RichTextLabel spectatorLabel;
	[Export] private HSlider zoomLevelSlider;

	public static FreeCameraManager Instance { get; private set; }
	public static bool Enabled { get; private set; }

	private bool is_focusing_aphids, is_hud_visible;
	private int focused_aphid_index;
	private Tween disable_transition, vanish_tween;
	private bool is_camera_tab_open;
	private TextureRect current_tracker;
	private Aphid current_aphid;
	private static Vector2 APHID_OFFSET = new(-16, -36);

	public override void _Ready()
	{
		Instance = this;
		Instance.SetPhysicsProcess(Enabled);
		vanish_tween = CreateTween();
		vanish_tween.Kill();
		disable_transition = CreateTween();
		disable_transition.Kill();
		SetProcessUnhandledInput(false);
		SetProcessShortcutInput(false);

		cameraMenuButton.Pressed += SetCameraTab;
		zoomLevelSlider.ValueChanged += _value =>
		{
			if (!Enabled)
				return;

			CameraManager.SetCameraZoom((float)_value);
		};
		focusedAphidButton.Pressed += () =>
		{
			if (!Enabled)
				return;

			if (!is_focusing_aphids)
				FocusAphid(focused_aphid_index);
			else
				StopFocus();
		};
		pingAphidButton.Pressed += () => TrackAphid(CameraManager.FocusedAphid);
	}
	public override void _ExitTree()
	{
		Instance = null;
		Enabled = false;
	}
	public override void _Process(double delta)
	{
		if (Enabled && is_focusing_aphids && !IsInstanceValid(CameraManager.FocusedAphid))
			StopFocus();

		if (IsInstanceValid(current_tracker))
			ProcessAphidTrack();
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (CanvasManager.Menus.IsBusy)
		{
			if (BuildMenu.Menu.IsOpen)
				ProcessZoomScroll(@event);
			return;
		}

		if (@event.IsActionPressed(InputNames.Cancel) || @event.IsActionPressed(InputNames.Escape) || @event.IsActionPressed(InputNames.ChangeCamera))
		{
			if (CameraManager.FocusedAphid != null)
			{
				AphidInfo.SetAphid(null);
				CameraManager.UnFocus();
			}
			else
				SetFreeCameraMode(false);
			return;
		}

		// pings aphids location
		if (@event.IsActionPressed(InputNames.ChangeMode))
		{
			if (IsInstanceValid(CameraManager.FocusedAphid) && !CameraManager.FocusedAphid.Equals(current_aphid))
				TrackAphid(CameraManager.FocusedAphid);
			else
				StopTrack();
		}

		if (@event.IsActionPressed(InputNames.Pull) && AphidInfo.Enabled)
			AphidInfo.SetAphid();

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
			CanvasManager.Menus.OpenMenu(BuildMenu.Menu);
		else if (@event.IsActionPressed(InputNames.QuickAction2))
			CanvasManager.Menus.OpenMenu(FurnitureShop.Instance.Menu);
		else if (@event.IsActionPressed(InputNames.QuickAction3))
			SetCameraTab();
	}
	private void SetCameraTab()
	{
		if (!Visible)
			return;

		is_camera_tab_open = !is_camera_tab_open;
		if (is_camera_tab_open)
			animator.Play("open_submenu");
		else
			animator.Play("close_submenu");
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
	public static void FocusAphid(Aphid _aphid)
	{
		int _index = ResortManager.Current.Aphids.FindIndex(0, (a) => a.Equals(_aphid));
		FocusAphid(_index);
	}
	public static void FocusAphid(int _index)
	{
		if (Instance.disable_transition.IsValid() || ResortManager.Current.Aphids.Count == 0)
			return;

		_index = _index < 0 ? ResortManager.Current.Aphids.Count - 1 : _index;
		_index = _index == ResortManager.Current.Aphids.Count ? 0 : _index;
		Instance.focused_aphid_index = _index;

		Instance.is_focusing_aphids = true;
		Instance.spectatorLabel.Show();

		CameraManager.Focus(ResortManager.Current.Aphids[Instance.focused_aphid_index]);
		Instance.spectatorLabel.Text = $"{Instance.Tr("camera_spectating")}\n<| {CameraManager.FocusedAphid.Instance.Genes.Name} |>";
		AphidInfo.SetAphid(null);
		SoundManager.CreateSound("ui/button_select");
	}
	public static void StopFocus()
	{
		Instance.is_focusing_aphids = false;
		CameraManager.UnFocus();
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

	public static void SetFreeCameraMode(bool _state)
	{
		if (Instance.disable_transition.IsValid())
			return;

		if (_state)
		{
			if (Player.Instance.IsDisabled)
				return;
			// Set camera free from player anchor
			CameraManager.UnFocus();
			CameraManager.Instance.EnableFreeRoam = true;

			// Show free camera hud and hide other elements
			SetFreeCameraHud(true);
			Instance.SetProcessUnhandledInput(true);
			Instance.SetProcessShortcutInput(true);
			CanvasManager.ClearControlPrompts();
			CanvasManager.SetHudElements(false);
			Player.Instance.SetDisabled(true, true);
			for (int i = 0; i < Instance.buttonGrid.GetChildCount(); i++)
				(Instance.buttonGrid.GetChild(i) as BaseButton).Disabled = false;

			Enabled = true;
		}
		else
		{
			Enabled = false;
			for (int i = 0; i < Instance.buttonGrid.GetChildCount(); i++)
				(Instance.buttonGrid.GetChild(i) as BaseButton).Disabled = true;

			// Hide free camera hud
			SetFreeCameraHud(false);
			Instance.SetProcessUnhandledInput(false);
			Instance.SetProcessShortcutInput(false);
			CanvasManager.SetHudElements(true);
			CanvasManager.ClearControlPrompts();
			Instance.is_focusing_aphids = false;
			Instance.is_camera_tab_open = false;
			Instance.spectatorLabel.Hide();
			Instance.zoomLevelSlider.SetValueNoSignal(2);

			// sweep back to player
			CameraManager.Instance.EnableFreeRoam = false;
			Instance.disable_transition = CameraManager.Instance.CreateTween();
			Instance.disable_transition.SetParallel();
			Instance.disable_transition.SetEase(Tween.EaseType.Out);
			Instance.disable_transition.SetTrans(Tween.TransitionType.Circ);
			Instance.disable_transition.TweenProperty(CameraManager.Instance, "zoom",
					new Vector2(2, 2), 0.2f).FromCurrent();
			Instance.disable_transition.TweenProperty(CameraManager.Instance, "position",
					Player.Instance.GlobalPosition, 0.2f).FromCurrent();
			Instance.disable_transition.Finished += () =>
			{
				CameraManager.Focus(Player.Instance);
				Player.Instance.SetDisabled(false, true);
			};
		}
	}
	public static void SetFreeCameraHud(bool _state, bool _noTransition = false)
	{
		if (_state == Instance.is_hud_visible)
			return;
		if (_noTransition)
		{
			if (_state)
				Instance.Show();
			else
				Instance.Hide();
		}
		else
		{
			if (_state)
				Instance.animator.Play(StringNames.OpenAnim);
			else
				Instance.animator.Play(StringNames.CloseAnim);

			AphidInfo.SetAphid(null);
		}
		Instance.is_hud_visible = _state;
		Instance.is_camera_tab_open = false;
	}
}
