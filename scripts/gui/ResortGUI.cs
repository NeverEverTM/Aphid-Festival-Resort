using System;
using Godot;

public partial class ResortGUI : CanvasLayer
{
	public static ResortGUI Instance { get; private set; }
	/// <summary>
	/// Used as a buffer for IsFreeCamera
	/// </summary>
	public bool IsFreeCamera, WasFreeCamera, EnableMouseCameraControl;
	private bool WasInMenu;

	[Export] private AnimationPlayer freeCameraHUD;
	[Export] private TextureButton build_button, show_storage_button;
	[Export] private BuildMenu build_menu;
	[Export] private Node2D TopLeft, BottomRight;

	private Tween quitTween;

	public override void _Ready()
	{
		Instance = this;
		quitTween = CreateTween();
		quitTween.Kill();

		GlobalManager.GlobalCamera.LimitTop = (int)TopLeft.GlobalPosition.Y;
		GlobalManager.GlobalCamera.LimitBottom = (int)BottomRight.GlobalPosition.Y;
		GlobalManager.GlobalCamera.LimitLeft = (int)TopLeft.GlobalPosition.X;
		GlobalManager.GlobalCamera.LimitRight = (int)BottomRight.GlobalPosition.X;

		build_button.Pressed += () => CanvasManager.Menus.OpenMenu(new MenuUtil.MenuInstance("build",
			build_menu.menu_player, build_menu.OnOpenMenu, build_menu.OnCloseMenu, false));
		show_storage_button.Pressed += build_menu.SetStorage;
	}
	public override void _ExitTree()
	{
		Instance = null;
	}
	public override void _PhysicsProcess(double delta)
	{
		WasFreeCamera = IsFreeCamera;

		if (IsFreeCamera)
		{
			if (EnableMouseCameraControl)
			{
				Vector2 _movement = GlobalManager.Utils.GetMouseToWorldPosition() - GlobalManager.GlobalCamera.GlobalPosition;
				if (Math.Abs(_movement.X) > GlobalManager.QuarterScreen.X * 0.8f || Math.Abs(_movement.Y) > GlobalManager.QuarterScreen.Y * 0.8f)
					GlobalManager.GlobalCamera.GlobalPosition += _movement.Normalized() * 8;
			}

			GlobalManager.GlobalCamera.GlobalPosition += Input.GetVector("left", "right", "up", "down") * 8;
			GlobalManager.GlobalCamera.GlobalPosition = GlobalManager.GlobalCamera.GlobalPosition.Clamp(TopLeft.GlobalPosition + GlobalManager.QuarterScreen, BottomRight.GlobalPosition - GlobalManager.QuarterScreen);

		}
	}
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("change_camera"))
			SetFreeCameraMode(!IsFreeCamera);

		if (IsFreeCamera)
		{
			if (!WasInMenu && (@event.IsActionPressed("escape") || @event.IsActionPressed("cancel")))
				SetFreeCameraMode(false);

			WasInMenu = CanvasManager.Menus.IsBusy;
		}

	}

	public static void SetFreeCameraMode(bool _state)
	{
		if (Instance.quitTween.IsValid() || CanvasManager.Instance.IsInFocus || CanvasManager.Menus.IsBusy || DialogManager.IsActive)
			return;

		Instance.IsFreeCamera = _state;
		Player.Instance.SetDisabled(Instance.IsFreeCamera, true);

		if (Instance.IsFreeCamera)
		{
			// Set camera free from player anchor
			Player.Instance.RemoveChild(GlobalManager.GlobalCamera);
			Instance.GetTree().Root.AddChild(GlobalManager.GlobalCamera);
			GlobalManager.GlobalCamera.GlobalPosition = Player.Instance.GlobalPosition;

			// Show free camera hud and hide other elements
			SetFreeCameraHud(true);
			CanvasManager.SetHudElements(false);
			CanvasManager.ClearControlPrompts();
		}
		else
		{
			Vector2 _prev_g_position = GlobalManager.GlobalCamera.GlobalPosition;

			// Move Camera back to player
			Instance.GetTree().Root.RemoveChild(GlobalManager.GlobalCamera);
			Player.Instance.AddChild(GlobalManager.GlobalCamera);

			// sweep back to player
			GlobalManager.GlobalCamera.GlobalPosition = _prev_g_position;
			Instance.quitTween = GlobalManager.GlobalCamera.CreateTween();
			Instance.quitTween.SetEase(Tween.EaseType.Out);
			Instance.quitTween.SetTrans(Tween.TransitionType.Circ);
			Instance.quitTween.TweenProperty(GlobalManager.GlobalCamera, "position", new Vector2(0, -20), 0.2f);

			// Hide free camera hud
			SetFreeCameraHud(false);
			CanvasManager.SetHudElements(true);
		}
	}
	public static void SetFreeCameraHud(bool _state) =>
		Instance.freeCameraHUD.Play(_state ? "open" : "close");
}
