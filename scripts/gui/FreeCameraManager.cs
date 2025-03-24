using System;
using Godot;

public partial class FreeCameraManager : Control
{
	[Export] private AnimationPlayer animator;
	[Export] private TextureButton cameraMenuButton, focusedAphidButton;
	[Export] RichTextLabel spectatorLabel;
	[Export] private HSlider zoomLevelSlider;

	public static FreeCameraManager Instance { get; private set; }
	/// <summary>
	/// A buffered version of EnableFreeCamera
	/// </summary>
	public bool FreeCameraWasActive;
	public bool Enabled { get; private set; }
	public bool EnableMouseFollow;
	public Aphid FocusedObject;

	private bool is_focusing_aphids;
	private int focused_aphid_index;
	private Tween disable_transition;
	private Vector2 camera_offset = new(0, -15);
	private bool is_camera_tab_open;

	public override void _Ready()
	{
		Instance = this;
		Instance.SetPhysicsProcess(Instance.Enabled);
		disable_transition = CreateTween();
		disable_transition.Kill();

		cameraMenuButton.Pressed += () =>
		{
			if (animator.CurrentAnimation != string.Empty)
				return;
			is_camera_tab_open = !is_camera_tab_open;
			if (is_camera_tab_open)
				animator.Play("open_submenu");
			else
				animator.Play("close_submenu");
		};
		zoomLevelSlider.ValueChanged += _value => GlobalManager.SetCameraZoom((float)_value);
		focusedAphidButton.Pressed += () =>
		{
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
		FreeCameraWasActive = Enabled;
		if (is_focusing_aphids && !IsInstanceValid(FocusedObject))
			StopFocus();
	}
	public override void _PhysicsProcess(double delta)
	{
		if (FocusedObject == null)
		{
			// for moving buildings and stuff (yes, its intentional that it stacks with arrow movement)
			if (EnableMouseFollow)
			{
				Vector2 _movement = GlobalManager.Utils.GetMouseToWorldPosition() - GlobalManager.GlobalCamera.GlobalPosition;
				if (Math.Abs(_movement.X) > GlobalManager.SCREEN_CENTER_GLOBAL.X * 0.8f || Math.Abs(_movement.Y) > GlobalManager.SCREEN_CENTER_GLOBAL.Y * 0.8f)
					GlobalManager.GlobalCamera.GlobalPosition += _movement.Normalized() * 8;
			}
			GlobalManager.GlobalCamera.GlobalPosition += Input.GetVector(InputNames.Left, InputNames.Right, InputNames.Up, InputNames.Down) * 8;
		}
		else
			GlobalManager.GlobalCamera.GlobalPosition = FocusedObject.GlobalPosition + camera_offset;

		// we still clamp the camera's position cause while the actual camera respects the bounds,
		//  its actual global position does not
		GlobalManager.GlobalCamera.GlobalPosition = GlobalManager.GlobalCamera.GlobalPosition.Clamp(FieldManager.Instance.TopLeft.GlobalPosition + GlobalManager.SCREEN_CENTER_GLOBAL,
				FieldManager.Instance.BottomRight.GlobalPosition - GlobalManager.SCREEN_CENTER_GLOBAL);
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!CanvasManager.Menus.IsBusy && @event.IsActionPressed(InputNames.ChangeCamera))
		{
			SetFreeCameraMode(!Enabled);
			return;
		}

		if (Enabled)
		{
			if (!CanvasManager.Menus.IsBusy && (@event.IsActionPressed(InputNames.Cancel) || @event.IsActionPressed(InputNames.Escape)))
			{
				SetFreeCameraMode(false);
				return;
			}

			// manages zoom scroll
			if (@event is InputEventMouseButton && (@event as InputEventMouseButton).Pressed)
			{
				InputEventMouseButton _mouse = @event as InputEventMouseButton;
				if (_mouse.ButtonIndex == MouseButton.WheelUp)
					GlobalManager.SetCameraZoom(0.25f, true);
				else if (_mouse.ButtonIndex == MouseButton.WheelDown)
					GlobalManager.SetCameraZoom(-0.25f, true);
				zoomLevelSlider.SetValueNoSignal(GlobalManager.GlobalCamera.Zoom.X);
			}

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
			if (is_focusing_aphids && IsInstanceValid(FocusedObject))
				ProcessSpectator(@event);
		}
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
		is_focusing_aphids = true;
		spectatorLabel.Show();
		if (_index >= ResortManager.CurrentResort.AphidsOnResort.Count)
			_index = 0;
		FocusedObject = ResortManager.CurrentResort.AphidsOnResort[_index];
		spectatorLabel.Text = $"{Tr("camera_spectating")}\n<| {FocusedObject.Instance.Genes.Name} |>";
		SoundManager.CreateSound("ui/button_select");
	}
	public void StopFocus()
	{
		is_focusing_aphids = false;
		FocusedObject = null;
		spectatorLabel.Hide();
	}
	public static void SetFreeCameraMode(bool _state)
	{
		if (Instance.disable_transition.IsValid())
			return;

		if (_state)
		{
			if (CanvasManager.Instance.IsInFocus || CanvasManager.Menus.IsBusy || DialogManager.IsActive)
				return;
			Instance.Enabled = true;
			// Set camera free from player anchor
			Player.Instance.RemoveChild(GlobalManager.GlobalCamera);
			Instance.GetTree().Root.AddChild(GlobalManager.GlobalCamera);
			GlobalManager.GlobalCamera.GlobalPosition = Player.Instance.GlobalPosition;

			// Show free camera hud and hide other elements
			SetFreeCameraHud(true);
			CanvasManager.ClearControlPrompts();
			CanvasManager.SetHudElements(false);
		}
		else
		{
			Instance.Enabled = false;
			Vector2 _prev_g_position = GlobalManager.GlobalCamera.GlobalPosition;

			// Move Camera back to player
			Instance.GetTree().Root.RemoveChild(GlobalManager.GlobalCamera);
			Player.Instance.AddChild(GlobalManager.GlobalCamera);

			// sweep back to player
			GlobalManager.GlobalCamera.GlobalPosition = _prev_g_position;
			Instance.disable_transition = GlobalManager.GlobalCamera.CreateTween();
			Instance.disable_transition.SetEase(Tween.EaseType.Out);
			Instance.disable_transition.SetTrans(Tween.TransitionType.Circ);
			Instance.disable_transition.TweenProperty(GlobalManager.GlobalCamera, "zoom", new Vector2(2, 2), 0.2f);
			Instance.disable_transition.TweenProperty(GlobalManager.GlobalCamera, "position", new Vector2(0, -20), 0.2f);

			// Hide free camera hud
			SetFreeCameraHud(false);
			CanvasManager.SetHudElements(true);
			CanvasManager.ClearControlPrompts();
			Instance.is_focusing_aphids = false;
			Instance.is_camera_tab_open = false;
			Instance.FocusedObject = null;
			Instance.spectatorLabel.Hide();
			Instance.zoomLevelSlider.SetValueNoSignal(2);
		}

		Player.Instance.SetDisabled(Instance.Enabled, true);
		Instance.SetPhysicsProcess(Instance.Enabled);
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
			Instance.animator.Play(_state ? "open" : "close");
	}
}
