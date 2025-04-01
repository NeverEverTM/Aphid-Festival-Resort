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
    /// The center of the viweport, offset starting from the top-left.
    /// </summary>
    public static Vector2 SCREEN_CENTER_CANVAS { get; private set; }
    /// <summary>
    /// Same as the CANVAS version, but translated to global position measure. 
    /// </summary>
    public static Vector2 SCREEN_CENTER_GLOBAL { get; private set; }

    public bool EnableFreeRoam { get; set; }
    public bool EnableMouseFollow { get; set; }

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

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(FocusedObject))
        {
            if (EnableFreeRoam)
                ProcessCameraMovement();
        }
        else
            Instance.GlobalPosition = FocusedObject.GlobalPosition;
        // we still clamp the camera's position because, while the actual camera respects the bounds,
        //  its actual global position does not
        Instance.GlobalPosition = Instance.GlobalPosition.Clamp(FieldManager.Instance.TopLeft.GlobalPosition + SCREEN_CENTER_GLOBAL,
                FieldManager.Instance.BottomRight.GlobalPosition - SCREEN_CENTER_GLOBAL);
    }
    private void ProcessCameraMovement()
    {
        // for moving buildings and stuff (yes, its intentional that it stacks with arrow movement)
        if (EnableMouseFollow)
        {
            Vector2 _movement = GetMouseToWorldPosition() - Instance.GlobalPosition;
            if (Math.Abs(_movement.X) > SCREEN_CENTER_GLOBAL.X * 0.8f || Math.Abs(_movement.Y) > SCREEN_CENTER_GLOBAL.Y * 0.8f)
                Instance.GlobalPosition += _movement.Normalized() * 8;
        }

        Instance.GlobalPosition += Input.GetVector(InputNames.Left, InputNames.Right, InputNames.Up, InputNames.Down) * 8;
    }
    public static void Focus(Node2D _focusObject)
    {
        FocusedObject = _focusObject;
        if (FocusedObject is Aphid)
            FocusedAphid = FocusedObject as Aphid;
    }
    public static void UnFocus()
    {
        FocusedObject = FocusedAphid = null;
    }

    private static void UpdateViewportSizeTracking()
    {
        SCREEN_SIZE_CANVAS = Instance.GetViewport().GetVisibleRect().Size;
        SCREEN_CENTER_CANVAS = SCREEN_SIZE_CANVAS / 2;
        SCREEN_CENTER_GLOBAL = SCREEN_CENTER_CANVAS / Instance.Zoom;
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

    /// <returns>The mouse position translated to global position/returns>
    public static Vector2 GetMouseToWorldPosition() => Instance.GlobalPosition + (Instance.GetViewport().GetMousePosition() - SCREEN_CENTER_CANVAS) * 1 / Instance.Zoom;
    public static Vector2 GetMouseToCanvasCenter() => Instance.GetViewport().GetMousePosition() - SCREEN_CENTER_CANVAS;
    /// <summary>
    /// Translates world position to canvas coordinates.
    /// </summary>
    /// <returns>A 2D Vector of an objects position translated to canvas coordinates</returns>
    public static Vector2 GetWorldToCanvasPosition(Vector2 _position) => SCREEN_CENTER_CANVAS + (_position - Instance.GlobalPosition) * Instance.Zoom;

}
