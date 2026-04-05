using Godot;

public partial class LumiButton : NinePatchRect
{
    [ExportGroup("On Hover")]
    [Export] private Color HoverColor;
    [Export] private Texture2D HoverTexture;
    [Export] private AudioStream HoverEnterSound, HoverExitSound;

    public override void _EnterTree()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseEntered += () => 
        MouseExited += () => Modulate = new("white");
    }

    public override void _Notification(int what)
    {
        switch((long)what)
        {
            case NotificationMouseEnterSelf:
                OnMouseEnter();
            break;
            case NotificationMouseExitSelf:
                OnMouseExit();
            break;
        }
    }

    private void OnMouseEnter()
    {
        Modulate = HoverColor;
        if (HoverEnterSound != null)
            SoundManager.CreateSound(HoverEnterSound);
    }
    private void OnMouseExit()
    {
        Modulate = HoverColor;
        if (HoverExitSound != null)
            SoundManager.CreateSound(HoverExitSound);
    }
}