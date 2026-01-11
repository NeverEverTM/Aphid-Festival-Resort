using Godot;
using System;

public partial class CameraManager : Camera2D
{
    public static CameraManager Instance { get; private set; }
    public static Node2D FocusedObject { get; private set; }
    public static Aphid FocusedAphid { get; private set; }

    /// <summary>
    /// The size of the current viewport
    /// </summary>
    public static Vector2 SCREEN_SIZE_CANVAS { get; private set; }
    /// <summary>
    /// The center of the viewport, offset starting from the top-left.
    /// </summary>
    public static Vector2 SCREEN_CENTER_CANVAS { get; private set; }
    /// <summary>
    /// Same as the CANVAS version, but translated to global position measure. 
    /// </summary>
    public static Vector2 SCREEN_CENTER_GLOBAL { get; private set; }

    public static float SCREEN_RENDER_DISTANCE { get; private set; }
    public static float SCREEN_RENDER_DISTANCE_SQR { get; private set; }

    public bool EnableFreeRoam { get; set; }
    public bool EnableMouseFollow { get; set; }

    public const float DEFAULT_CAMERA_ZOOM = 2;

    [Export] public GpuParticles2D cameraParticles;

    public override void _EnterTree()
    {
        Instance = this;
        GetViewport().SizeChanged += UpdateViewportSizeTracking;
        UpdateViewportSizeTracking();
    }
    public override void _ExitTree()
    {
        Instance = null;
        FocusedObject = FocusedAphid = null;
        EnableFreeRoam = EnableMouseFollow = false;
        GetViewport().SizeChanged -= UpdateViewportSizeTracking;
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(FieldManager.Instance))
            return;

        if (!IsInstanceValid(FocusedObject))
        {
            if (EnableFreeRoam)
                ProcessFreeCamera();
        }
        else
            Instance.GlobalPosition = FocusedObject.GlobalPosition;

        // we still clamp the camera's position because, while the actual camera respects the bounds,
        //  its actual global position does not
        Instance.GlobalPosition = Instance.GlobalPosition.Clamp(FieldManager.Instance.TopLeft.GlobalPosition + SCREEN_CENTER_GLOBAL,
            FieldManager.Instance.BottomRight.GlobalPosition - SCREEN_CENTER_GLOBAL);
    }
    private void ProcessFreeCamera()
    {
        // for moving buildings and stuff (yes, its intentional that it stacks with arrow movement)
        if (EnableMouseFollow)
        {
            Vector2 _movement = GetMouseToWorldPosition() - Instance.GetScreenCenterPosition();
            if (Math.Abs(_movement.X) > SCREEN_CENTER_GLOBAL.X * 0.8f || Math.Abs(_movement.Y) > SCREEN_CENTER_GLOBAL.Y * 0.8f)
                Instance.GlobalPosition += _movement.Normalized() * 8;
        }

        Instance.GlobalPosition += Input.GetVector(InputNames.Left, InputNames.Right, InputNames.Up, InputNames.Down)
                 * (Input.IsActionPressed(InputNames.Run) ? 16 : 8);
    }
    public static void ForceCameraPosition(Vector2 _position)
    {
        Instance.GlobalPosition = _position;
        Instance.ForceUpdateScroll();
        Instance.ResetSmoothing();

        if (!FieldManager.Instance.IsInside)
        {
            Instance.cameraParticles.Visible = true;
            // force particle spawn at new location
            Instance.cameraParticles.Amount = Instance.cameraParticles.Amount;
        }
        else
            Instance.cameraParticles.Visible = false;
    }
    /// <summary>
    /// Call this function to force an instant snap to target position.
    /// </summary>
    public static void ForceCameraPosition() =>
        ForceCameraPosition(Instance.GetTargetPosition());
    // Sets current focus target for the camera. If is an aphid, it fills the current focused aphid too.
    public static void Focus(Node2D _focusObject)
    {
        if (!IsInstanceValid(_focusObject))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "CameraManager: Tried to focus non-valid object.");
            return;
        }
        FocusedObject = _focusObject;
        if (FocusedObject is Aphid)
            FocusedAphid = FocusedObject as Aphid;
        Instance.GlobalPosition = _focusObject.GlobalPosition;
    }
    public static void UnFocus()
    {
        FocusedObject = FocusedAphid = null;
    }
    public static float GetSquaredDistanceTo(Vector2 _position) =>
        Instance.GlobalPosition.DistanceSquaredTo(_position);
    public static float GetDistanceTo(Vector2 _position) =>
        Instance.GlobalPosition.DistanceTo(_position);
    public static void UpdateViewportSizeTracking()
    {
        SCREEN_SIZE_CANVAS = Instance.GetViewport().GetVisibleRect().Size;
        SCREEN_CENTER_CANVAS = SCREEN_SIZE_CANVAS / 2;
        SCREEN_CENTER_GLOBAL = SCREEN_CENTER_CANVAS / Instance.Zoom;
        SCREEN_RENDER_DISTANCE = 700 * Instance.Zoom.X;
        SCREEN_RENDER_DISTANCE_SQR = SCREEN_RENDER_DISTANCE * SCREEN_RENDER_DISTANCE;
    }

    public static void SetCameraZoom(float _amount, bool _addInstead = false)
    {
        float _total = Instance.Zoom.X;
        if (_addInstead)
            _total += _amount;
        else
            _total = _amount;

        _total = Math.Clamp(_total, 1.25f, 5f);
        Instance.Zoom = new(_total, _total);
        UpdateViewportSizeTracking();
    }
    public static Tween TweenCamera(double _duration, float _zoom = DEFAULT_CAMERA_ZOOM)
    {
        // update the tween to avoid a weird zoom delay effect near the borders of the map
        Tween _zoomTween = Instance.CreateTween(), _updateTween = Instance.CreateTween().SetLoops();
        _updateTween.TweenCallback(Callable.From(UpdateViewportSizeTracking)).SetDelay(0.01f);
        _zoomTween.SetEase(Tween.EaseType.Out);
        _zoomTween.SetTrans(Tween.TransitionType.Circ);
        _zoomTween.TweenProperty(Instance, "zoom", new Vector2(_zoom, _zoom), _duration).FromCurrent();
        _zoomTween.Finished += () =>
        {
            _updateTween.Kill();
            SetCameraZoom(_zoom);
        };
        return _zoomTween;
    }

    /// <returns>The mouse position translated to global position/returns>
    public static Vector2 GetMouseToWorldPosition() => Instance.GetGlobalMousePosition();
    /// <returns>The mouse position in canvas coordinates</returns>
    public static Vector2 GetMouseToCanvasPosition() => Instance.GetLocalMousePosition();
    /// <returns>A global position translated to canvas coordinates</returns>
    public static Vector2 GetWorldToCanvasPosition(Vector2 _position) => (_position - Instance.GetScreenCenterPosition()) * Instance.Zoom.X + SCREEN_CENTER_CANVAS;
}
