using Godot;

public partial class FreeCameraManager : Control
{
	[Export] private AnimationPlayer animator;
	[Export] private TextureButton cameraMenuButton, focusedAphidButton;
	[Export] private Control buttonGrid;
	[Export] private RichTextLabel spectatorLabel;
	[Export] private HSlider zoomLevelSlider;

	public static FreeCameraManager Instance { get; private set; }
	public bool Enabled { get; private set; }

	private bool is_focusing_aphids;
	private int focused_aphid_index;
	private Tween disable_transition, vanish_tween;
	private bool is_camera_tab_open;
	private TextureRect current_tracker;
	private Aphid current_aphid;
	private static Vector2 APHID_OFFSET = new(-16, -36);

	public override void _Ready()
	{
		Instance = this;
		Instance.SetPhysicsProcess(Instance.Enabled);
		disable_transition = CreateTween();
		disable_transition.Kill();

		cameraMenuButton.Pressed += () =>
		{
			if (!Enabled || animator.CurrentAnimation != string.Empty)
				return;

			is_camera_tab_open = !is_camera_tab_open;
			if (is_camera_tab_open)
				animator.Play("open_submenu");
			else
				animator.Play("close_submenu");
		};
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
	}
	public override void _ExitTree()
	{
		Instance = null;
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
			if (Enabled && CanvasManager.Menus.CurrentMenu.Name.Equals("build"))
				ProcessZoomScroll(@event);
			return;
		}

		if (@event.IsActionPressed(InputNames.ChangeCamera))
		{
			SetFreeCameraMode(!Enabled);
			return;
		}

		if (Enabled)
		{
			if (@event.IsActionPressed(InputNames.Cancel) || @event.IsActionPressed(InputNames.Escape))
			{
				SetFreeCameraMode(false);
				return;
			}

			ProcessZoomScroll(@event);

			// changes focus mode
			if (@event.IsActionPressed(InputNames.ChangeMode))
			{
				if (!is_focusing_aphids)
					FocusAphid(focused_aphid_index);
				else
					StopFocus();
				return;
			}

			// processes focus mode inputs
			if (is_focusing_aphids && IsInstanceValid(CameraManager.FocusedAphid))
				ProcessSpectator(@event);
		}
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
		{
			focused_aphid_index = focused_aphid_index == 0 ?
				ResortManager.CurrentResort.AphidsOnResort.Count - 1 : focused_aphid_index - 1;
			FocusAphid(focused_aphid_index);
		}
		else if (@event.IsActionPressed(InputNames.Right))
		{
			focused_aphid_index = focused_aphid_index == ResortManager.CurrentResort.AphidsOnResort.Count - 1 ?
				0 : focused_aphid_index + 1;
			FocusAphid(focused_aphid_index);
		}
	}
	private void FocusAphid(int _index)
	{
		if (Instance.disable_transition.IsValid())
			return;

		is_focusing_aphids = true;
		spectatorLabel.Show();
		if (_index >= ResortManager.CurrentResort.AphidsOnResort.Count)
			_index = 0;
		CameraManager.Focus(ResortManager.CurrentResort.AphidsOnResort[_index]);
		spectatorLabel.Text = $"{Tr("camera_spectating")}\n<| {CameraManager.FocusedAphid.Instance.Genes.Name} |>";
		SoundManager.CreateSound("ui/button_select");
	}
	public void StopFocus()
	{
		is_focusing_aphids = false;
		CameraManager.UnFocus();
		spectatorLabel.Hide();
	}
	private void TrackAphid(Aphid _aphid)
	{
		// if we already have a tracker delete it
		if (IsInstanceValid(current_tracker))
		{
			vanish_tween.Kill();
			current_tracker.Free();
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

		AddChild(_track);
		_vanishTimer.Start(30);
		SoundManager.CreateSound("ui/switch_mode");
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
			if (CanvasManager.Instance.IsInFocus || CanvasManager.Menus.IsBusy || DialogManager.IsActive)
				return;
			// Set camera free from player anchor
			CameraManager.UnFocus();
			CameraManager.Instance.EnableFreeRoam = true;

			// Show free camera hud and hide other elements
			SetFreeCameraHud(true);
			CanvasManager.ClearControlPrompts();
			CanvasManager.SetHudElements(false);
			Player.Instance.SetDisabled(true, true);
			for (int i = 0; i < Instance.buttonGrid.GetChildCount(); i++)
				(Instance.buttonGrid.GetChild(i) as BaseButton).Disabled = false;

			Instance.Enabled = true;
		}
		else
		{
			Instance.Enabled = false;
			for (int i = 0; i < Instance.buttonGrid.GetChildCount(); i++)
				(Instance.buttonGrid.GetChild(i) as BaseButton).Disabled = true;

			// Hide free camera hud
			SetFreeCameraHud(false);
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
		if (_noTransition)
		{
			if (_state)
				Instance.Show();
			else
				Instance.Hide();
		}
		else
			Instance.animator.Play(_state ? StringNames.OpenAnim : StringNames.CloseAnim);
	}
}
