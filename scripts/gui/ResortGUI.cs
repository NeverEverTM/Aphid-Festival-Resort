using Godot;

public partial class ResortGUI : CanvasLayer
{
	public static ResortGUI Instance { get; private set; }
	public static bool IsFreeCamera { get; private set; }
	/// <summary>
	/// Used as a buffer for IsFreeCamera
	/// </summary>
	public static bool WasFreeCamera { get; private set; }

	[Export] private AnimationPlayer freeCameraHUD;
	private Tween quitTween;
    public override void _Ready()
    {
        Instance = this;
		IsFreeCamera = false;
		quitTween = CreateTween();
		quitTween.Kill();
    }
    public override void _ExitTree()
    {
		Instance = null;
        IsFreeCamera = WasFreeCamera = false;
    }
    public override void _PhysicsProcess(double delta)
	{
		WasFreeCamera = IsFreeCamera;

		if (IsFreeCamera)
		{
			Vector2 _position = GameManager.GlobalCamera.GlobalPosition;
			_position += ReadCameraInput();
		}

		// Exit camera mode without triggering the pause menu
    	if (IsFreeCamera && (Input.IsActionJustPressed("escape") || Input.IsActionJustPressed("cancel")))
		{
			SetFreeCameraMode(false);
			CanvasManager.Instance.GetViewport().SetInputAsHandled();
			return;
		}

		// switch camera mode
		if (Input.IsActionJustPressed("change_camera"))
			SetFreeCameraMode(!IsFreeCamera);
	}

	private static Vector2 ReadCameraInput()
	{
		var _up = Input.IsActionPressed("up");
		var _down = Input.IsActionPressed("down");
		var _left = Input.IsActionPressed("left");
		var _right = Input.IsActionPressed("right");

		return new Vector2((_left ? -1 : 0) + (_right ? 1 : 0),
			(_up ? -1 : 0) + (_down ? 1 : 0)) * 10;
	}
    public static void SetFreeCameraMode(bool _state)
	{
		if (Instance.quitTween.IsValid() || CanvasManager.Instance.IsInFocus || CanvasManager.Menus.IsInMenu || DialogManager.IsActive)
			return;

		IsFreeCamera = _state;
		Player.Instance.SetDisabled(IsFreeCamera);

		if (IsFreeCamera)
		{
			// Set camera free from player anchor
			Player.Instance.RemoveChild(GameManager.GlobalCamera);
			Instance.AddChild(GameManager.GlobalCamera);
			GameManager.GlobalCamera.GlobalPosition = Player.Instance.GlobalPosition;

			// Show free camera hud
			SetFreeCameraHud(true);
			CanvasManager.SetCurrencyElement(false);
		}
		else
		{
			Vector2 _prev_g_position = GameManager.GlobalCamera.GlobalPosition;

			// Move Camera back to player
			Instance.RemoveChild(GameManager.GlobalCamera);
			Player.Instance.AddChild(GameManager.GlobalCamera);

			// sweep back to player
			GameManager.GlobalCamera.GlobalPosition = _prev_g_position;
			Instance.quitTween = GameManager.GlobalCamera.CreateTween();
			Instance.quitTween.SetEase(Tween.EaseType.Out);
			Instance.quitTween.SetTrans(Tween.TransitionType.Circ);
			Instance.quitTween.TweenProperty(GameManager.GlobalCamera, "position", new Vector2(0, -20), 0.2f);

			// Hide free camera hud
			SetFreeCameraHud(false);
			CanvasManager.SetCurrencyElement(true);
		}
	}
	public static void SetFreeCameraHud(bool _state)
	{
		Instance.freeCameraHUD.Play(_state ? "open" : "close");
	}
}
