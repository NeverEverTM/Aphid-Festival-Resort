using System;
using Godot;

public partial class ImageSignStructure : Sprite2D, SaveSystem.IGenericDataModule
{
    [Export] private TextureRect billboard;
    [Export] private InteractableArea2D interactArea2D;
    [Export] private Texture2D emptyTexture;
    [Export] private PackedScene customSignMenu;

    private string path;
    private CustomSignMenu menu;

    public override void _EnterTree()
    {
        billboard.Texture = emptyTexture;
        interactArea2D.OnInteractOnly.Add(Interact);
    }

    public void Interact()
    {
        if (IsInstanceValid(menu))
            return;

        menu = customSignMenu.Instantiate() as CustomSignMenu;
        CanvasManager.Instance.AddChild(menu);
        menu.type = CustomSignMenu.SignType.Image;
        menu.OnSelect += SetImage;

        _ = CanvasManager.Menus.SetTo(menu.GetMenuInstance());
    }

    public void Set(string _data)
    {
        SetImage(_data);
    }
    public string Get()
    {
        return path;
    }

    public void SetImage(string _path)
    {
        if (CanvasManager.Menus.Current?.Name == "custom_sign")
            _ = CanvasManager.Menus.GoBack();
        path = _path;
        if (!FileAccess.FileExists(_path))
        {
            DebugLogger.Print(DebugLogger.LogPriority.Warning, "ImageSign: Image did not exist at path " + _path);
            return;
        }

        try
        {
            path = _path;
            Image _image = Image.LoadFromFile(_path);
            billboard.Texture = ImageTexture.CreateFromImage(_image);
        }
        catch(Exception _error)
        {
            billboard.Texture = emptyTexture;
            DebugLogger.Print(DebugLogger.LogPriority.Error, "ImageSign: Unable to load image", _error);
        }
    }
}
