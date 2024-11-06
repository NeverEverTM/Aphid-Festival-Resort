using System;
using Godot;

public partial class ResortGUI : CanvasLayer
{
	public static ResortGUI Instance { get; private set; }
	/// <summary>
	/// Used as a buffer for IsFreeCamera
	/// </summary>
	public bool IsFreeCamera, WasFreeCamera, EnableMouseCameraControl;

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

		if (IsInstanceValid(GameManager.GlobalCamera))
		{
			GameManager.GlobalCamera.LimitTop = (int)TopLeft.GlobalPosition.Y;
			GameManager.GlobalCamera.LimitBottom = (int)BottomRight.GlobalPosition.Y;
			GameManager.GlobalCamera.LimitLeft = (int)TopLeft.GlobalPosition.X;
			GameManager.GlobalCamera.LimitRight = (int)BottomRight.GlobalPosition.X;
		}

		build_button.Pressed += () => CanvasManager.Menus.OpenMenu(new MenuUtil.MenuInstance("build",
			build_menu.menu_player, build_menu.OnOpenMenu, build_menu.OnCloseMenu, false));
		show_storage_button.Pressed += () =>
		{
			if (!build_menu.isStorageOpen)
				build_menu.menu_player.Play("open_bar");
			else
				build_menu.menu_player.Play("close_bar");
			build_menu.isStorageOpen = !build_menu.isStorageOpen;
		};
	}
	public override void _ExitTree()
	{
		Instance = null;
	}
	public override void _PhysicsProcess(double delta)
	{
		WasFreeCamera = IsFreeCamera;

		if (Input.IsActionJustPressed("change_camera"))
			SetFreeCameraMode(!IsFreeCamera);

		if (IsFreeCamera)
		{
			if (EnableMouseCameraControl)
			{
				Vector2 _movement = GameManager.Utils.GetMouseToWorldPosition() - GameManager.GlobalCamera.GlobalPosition;
				if (Math.Abs(_movement.X) > GameManager.QuarterScreen.X * 0.8f || Math.Abs(_movement.Y) > GameManager.QuarterScreen.Y * 0.8f)
					GameManager.GlobalCamera.GlobalPosition += _movement.Normalized() * 8;
			}

			GameManager.GlobalCamera.GlobalPosition += Input.GetVector("left", "right", "up", "down") * 8;
			GameManager.GlobalCamera.GlobalPosition = GameManager.GlobalCamera.GlobalPosition.Clamp(TopLeft.GlobalPosition + GameManager.QuarterScreen, BottomRight.GlobalPosition - GameManager.QuarterScreen);
			if (Input.IsActionJustPressed("escape") || Input.IsActionJustPressed("cancel"))
				SetFreeCameraMode(false);
		}
	}

	public static void SetFreeCameraMode(bool _state)
	{
		if (Instance.quitTween.IsValid() || CanvasManager.Instance.IsInFocus || CanvasManager.Menus.IsInMenu || DialogManager.IsActive)
			return;

		Instance.IsFreeCamera = _state;
		Player.Instance.SetDisabled(Instance.IsFreeCamera);

		if (Instance.IsFreeCamera)
		{
			// Set camera free from player anchor
			Player.Instance.RemoveChild(GameManager.GlobalCamera);
			Instance.GetTree().Root.AddChild(GameManager.GlobalCamera);
			GameManager.GlobalCamera.GlobalPosition = Player.Instance.GlobalPosition;

			// Show free camera hud and hide other elements
			SetFreeCameraHud(true);
			CanvasManager.SetHudElements(false);
		}
		else
		{
			Vector2 _prev_g_position = GameManager.GlobalCamera.GlobalPosition;

			// Move Camera back to player
			Instance.GetTree().Root.RemoveChild(GameManager.GlobalCamera);
			Player.Instance.AddChild(GameManager.GlobalCamera);

			// sweep back to player
			GameManager.GlobalCamera.GlobalPosition = _prev_g_position;
			Instance.quitTween = GameManager.GlobalCamera.CreateTween();
			Instance.quitTween.SetEase(Tween.EaseType.Out);
			Instance.quitTween.SetTrans(Tween.TransitionType.Circ);
			Instance.quitTween.TweenProperty(GameManager.GlobalCamera, "position", new Vector2(0, -20), 0.2f);

			// Hide free camera hud
			SetFreeCameraHud(false);
			CanvasManager.SetHudElements(true);
		}
	}
	public static void SetFreeCameraHud(bool _state) =>
		Instance.freeCameraHUD.Play(_state ? "open" : "close");
}
