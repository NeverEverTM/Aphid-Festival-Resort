using Godot;
using System;
using System.Linq;

public partial class CustomSignMenu : MenuControl
{
    public override string ID => "custom_sign";

    public enum SignType { Text, Image }
    public SignType type;

    internal delegate void TextSelection(string _text);
    internal event TextSelection OnSelect;

    private bool selected = false;
    
    [Export] private Control photoPanel, signPanel;
    [Export] private PackedScene photoSlot;
    [Export] private Container photoGrid;
    [Export] private TextEdit signEdit;
    [Export] private BaseButton customImagesFolderButton;

    public override void _Input(InputEvent @event)
    {
        if (signEdit.GetLineCount() >= 4 && signEdit.GetLine(4).Length >= 15 && (@event as InputEventKey).Keycode != Key.Backspace)
            AcceptEvent();
    }

    private void GeneratePhotoGrid()
    {
        for (int i = 0; i < photoGrid.GetChildCount(); i++)
            photoGrid.GetChild(i).QueueFree();

        var _customImages = DirAccess.GetFilesAt(SaveSystem.CUSTOM_IMAGES_DIR)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var _screenshotsImages =  DirAccess.GetFilesAt(SaveSystem.ProfilePath + SaveSystem.PROFILE_SCREENSHOTS_DIR)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToArray();
            
        for (int i = 0; i < _customImages.Length; i++)
            CreatePhotoSlot(SaveSystem.CUSTOM_IMAGES_DIR + _customImages[i]);

        for (int i = 0; i < _screenshotsImages.Length; i++)
            CreatePhotoSlot(SaveSystem.ProfilePath + SaveSystem.PROFILE_SCREENSHOTS_DIR + _screenshotsImages[i]);
    }
    private void CreatePhotoSlot(string _path)
    {
        var _slot = photoSlot.Instantiate() as BaseButton;
        Image _image = Image.LoadFromFile(_path);
        _slot.GetChild<TextureRect>(1).Texture = ImageTexture.CreateFromImage(_image);
        _slot.Pressed += () =>
        {
            if (selected)
                return;
            selected = true;
            OnSelect.Invoke(_path);
        };
        photoGrid.AddChild(_slot);
    }
    private void SubmitText()
    {
        OnSelect.Invoke(signEdit.Text);
    }
    private void CancelText()
    {
        if (CanvasManager.Menus.Current.Name == "custom_sign")
            _ = CanvasManager.Menus.GoBack();
    }

    // ===| Menu Interface |===
    protected override void Open(MenuInstance _last)
    {
        if (type == SignType.Image)
        {
            customImagesFolderButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath(SaveSystem.CUSTOM_IMAGES_DIR));
            try { GeneratePhotoGrid(); }
            catch { DebugLogger.Print(DebugLogger.LogPriority.Error, "ImageSign: Unable to load images"); }
            photoPanel.Show();
        }
        else
        {
            signPanel.Show();
        }
    }
    protected override void CloseDispose()
    {
        QueueFree();
    }
}
