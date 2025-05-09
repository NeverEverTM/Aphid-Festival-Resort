using System;
using Godot;

public partial class ImageSign : Sprite2D, Player.IInteractEvent, ResortManager.IStructureData
{
    [Export] private Sprite2D billboard;

    private FileDialog popup = new();
    private string path;
    private Vector2 orig_scale;
    private bool firstLoad = false;

    public override void _EnterTree()
    {
        popup = new()
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Userdata,
            Filters = ["*.png","*.jpeg"],
            Theme = ThemeDB.GetDefaultTheme()
        };
        popup.FileSelected += SetImage;
        popup.Canceled += () => Player.Instance.SetDisabled(false);
        AddChild(popup);
        orig_scale = billboard.Scale;
    }

    public void Interact()
    {
        Player.Instance.SetDisabled(true, true);
        popup.PopupCentered();
    }

    public void SetData(string _data)
    {
        firstLoad = true;
        path = _data;
        SetImage(_data);
    }
    public string GetData()
    {
        return path;
    }

    public void SetImage(string _path)
    {
        if (!FileAccess.FileExists(_path))
        {
            if (!firstLoad)
                Player.Instance.SetDisabled(false);
            return;
        }

        // we get the image variables
        try
        {
            path = _path;
            Image _image = Image.LoadFromFile(_path);
            float w = _image.GetWidth(), h = _image.GetHeight(), max_width = h * 1.45f;

            // scale is sized to fit the image in the smaller canvas
            // images below 300px get upscaled, images above 300px get downscaled
            float _height = 300f / h * orig_scale.Y;
            // width tries to stay in sync with height for vertical images,
            //  but things higher than that get downscaled
            float _width = w < max_width ? _height : 300f * 1.45f / w * orig_scale.X;
            billboard.Texture = ImageTexture.CreateFromImage(_image);
            billboard.Scale = new(_width, _height);
        }
        catch(Exception _error)
        {
            Logger.Print(Logger.LogPriority.Error, "Image Sign: Unable to load image.", _error);
        }

        if (!firstLoad)
            Player.Instance.SetDisabled(false);
        firstLoad = false;
    }
}
