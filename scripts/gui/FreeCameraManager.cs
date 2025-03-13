using System;
using Godot;

public partial class FreeCameraManager : Control
{
	[Export] private AnimationPlayer animator;
	[Export] private TextureButton buildButton, storageButton, cameraMenuButton, focusedAphidButton;
	[Export] private HSlider zoomLevelSlider;
	[Export] private BuildMenu build_menu;
	[Export] private Control cameraBg;

	public static FreeCameraManager Instance { get; private set; }
	/// <summary>
	/// A buffered version of EnableFreeCamera
	/// </summary>
	public bool FreeCameraWasActive;
	public bool Enabled { get; private set; }
	public bool EnableMouseFollow;
	/// <summary>
	/// A buffered version of BuildMenu.IsStorageOpen
	/// </summary>
	private bool was_in_menu;
	private bool is_focusing_aphids;
	private int focused_aphid_index;
	private Node2D focused_object;
	private Tween disable_transition;
	private Vector2 camera_offset = new(0,-15);

	public override void _Ready()
	{
		Instance = this;
		disable_transition = CreateTween();
		disable_transition.Kill();

		// move to build menu
		buildButton.Pressed += () => CanvasManager.Menus.OpenMenu(new MenuUtil.MenuInstance("build",
				build_menu.menu_player, build_menu.OnOpenMenu, build_menu.OnCloseMenu, false));
		storageButton.Pressed += build_menu.SetStorage; // move to build menu
		cameraMenuButton.Pressed += () =>
		{
			if (!cameraBg.Visible)
				Instance.animator.Play("open_submenu");
			else
				Instance.animator.Play("close_submenu");
		};
		zoomLevelSlider.ValueChanged += _value => GlobalManager.SetCameraZoom((float)_value);
		focusedAphidButton.Pressed += () =>
		{
			is_focusing_aphids = !is_focusing_aphids;
			if (is_focusing_aphids)
			{
				if (focused_aphid_index >= ResortManager.CurrentResort.AphidsOnResort.Count)
					focused_aphid_index = 0;
				focused_object = ResortManager.CurrentResort.AphidsOnResort[focused_aphid_index];
			}
			else
				focused_object = null;
		};
	}
	public override void _ExitTree()
	{
		Instance = null;
	}
	public override void _PhysicsProcess(double delta)
	{
		FreeCameraWasActive = Enabled;

		if (Enabled)
		{
			if (focused_object == null)
			{
				// for moving buildings and stuff (yes, its intentional that it stacks with arrow movement)
				if (EnableMouseFollow)
				{
					Vector2 _movement = GlobalManager.Utils.GetMouseToWorldPosition() - GlobalManager.GlobalCamera.GlobalPosition;
					if (Math.Abs(_movement.X) > GlobalManager.SCREEN_CENTER_GLOBAL.X * 0.8f || Math.Abs(_movement.Y) > GlobalManager.SCREEN_CENTER_GLOBAL.Y * 0.8f)
						GlobalManager.GlobalCamera.GlobalPosition += _movement.Normalized() * 8;
				}
				GlobalManager.GlobalCamera.GlobalPosition += Input.GetVector("left", "right", "up", "down") * 8;
			}
			else
				GlobalManager.GlobalCamera.GlobalPosition = focused_object.GlobalPosition + camera_offset;

			// we still clamp the camera's position cause while the actual camera respects the bounds,
			//  its actual global position does not
			GlobalManager.GlobalCamera.GlobalPosition = GlobalManager.GlobalCamera.GlobalPosition.Clamp(FieldManager.Instance.TopLeft.GlobalPosition + GlobalManager.SCREEN_CENTER_GLOBAL,
					FieldManager.Instance.BottomRight.GlobalPosition - GlobalManager.SCREEN_CENTER_GLOBAL);
		}
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("change_camera"))
			SetFreeCameraMode(!Enabled);

		if (Enabled)
		{
			// do not quit camera mode if present in either storage mode or furniture menu
			if (!CanvasManager.Menus.IsBusy && (@event.IsActionPressed("escape") || @event.IsActionPressed("cancel")))
			{
				SetFreeCameraMode(false);
				return;
			}

			// spectating aphids in the resort
			if (is_focusing_aphids)
			{
				if (!IsInstanceValid(focused_object))
				{
					focused_object = ResortManager.CurrentResort.AphidsOnResort[0];
					focused_aphid_index = 0;
				}
				else
				{
					if (@event.IsActionPressed("left"))
						focused_aphid_index = focused_aphid_index == 0 ? 
							ResortManager.CurrentResort.AphidsOnResort.Count - 1 : focused_aphid_index - 1 ;
					else if (@event.IsActionPressed("right"))
						focused_aphid_index = focused_aphid_index == ResortManager.CurrentResort.AphidsOnResort.Count - 1 ? 
							0 : focused_aphid_index + 1 ;
					focused_object = ResortManager.CurrentResort.AphidsOnResort[focused_aphid_index];
				}
			}
		}
	}

	public static void SetFreeCameraMode(bool _state)
	{
		if (Instance.disable_transition.IsValid() || CanvasManager.Instance.IsInFocus || CanvasManager.Menus.IsBusy || DialogManager.IsActive)
			return;

		Instance.Enabled = _state;
		Player.Instance.SetDisabled(Instance.Enabled, true);
		Instance.focused_object = null;

		if (Instance.Enabled)
		{
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
			Instance.zoomLevelSlider.SetValueNoSignal(2);
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
			Instance.animator.Play(_state ? "open" : "close");
	}
		
}
