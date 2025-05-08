#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

[Tool]
public partial class ExportImageFolders : EditorPlugin
{
    // Atlas Variables
    private List<SkinFolder> img_folders;
    private string atlas_path = GlobalManager.ABSOLUTE_SPRITES_PATH + "atlases/atlas.res";
    private string output_path = GlobalManager.ABSOLUTE_SKINS_PATH;
    private int image_size = 64;

    // GUI Elements
    private AcceptDialog gui;
    private LineEdit lineEdit;
    private Label output_label, atlas_label;
    private Container container;

    [GeneratedRegex(@"[\w\. ]+(?=[\.])")]
    private static partial Regex MyRegex();

    private struct SkinFolder(string name, string path, int max_files)
    {
        public string name = name;
        public string path = path;
        public int max_files = max_files;
        public List<ImageFile> images = [];
    }
    private struct ImageFile(string name, Image data)
    {
        public string name = name;
        public Image data = data;
    }

    public override void _EnterTree()
    {
        img_folders = [];

        // create gui and link actions to buttons
        gui = (ResourceLoader.Load(GlobalManager.ABSOLUTE_SCENES_PATH + "/debug/atlas_gui.tscn") as PackedScene).Instantiate() as AcceptDialog;
        AddChild(gui);
        gui.OkButtonText = "Generate";
        gui.Confirmed += EXECUTE_WRAP;

        // folder paths
        (gui.FindChild("reset") as BaseButton).Pressed += () =>
        {
            for (int i = 0; i < container.GetChildCount(); i++)
                container.GetChild(i).QueueFree();
            img_folders = [];
        };
        EditorFileDialog _folder_popup = new()
        {
            FileMode = EditorFileDialog.FileModeEnum.OpenDir,
            Access = EditorFileDialog.AccessEnum.Filesystem,
            Title = "Choose a folder to load contents from",
            Mode = Window.ModeEnum.Maximized
        };
        _folder_popup.DirSelected += ON_DIR_SELECT;
        gui.AddChild(_folder_popup);
        (gui.FindChild("get_folder") as BaseButton).Pressed += () => _folder_popup.PopupCentered();
        container = gui.FindChild("path_grid") as BoxContainer;

        // image size
        lineEdit = gui.FindChild("line_edit") as LineEdit;
        lineEdit.Text = image_size.ToString();
        lineEdit.TextSubmitted += (_text) => SET_IMAGE_SIZE(_text);
        lineEdit.FocusExited += () => SET_IMAGE_SIZE(lineEdit.Text);

        //output
        output_label = gui.FindChild("output_text") as Label;
        EditorFileDialog _output_file = new()
        {
            FileMode = EditorFileDialog.FileModeEnum.OpenDir,
            Access = EditorFileDialog.AccessEnum.Filesystem,
            Title = "Choose a folder for the output folders",
            Mode = Window.ModeEnum.Maximized
        };
        _output_file.DirSelected += (_path) =>
        {
            output_path = _path;
            output_label.Text = _path;
        };
        gui.AddChild(_output_file);
        (gui.FindChild("get_output_path") as BaseButton).Pressed += () => _output_file.PopupCentered();
        output_label.Text = output_path;

        // atlas
        atlas_label = gui.FindChild("atlas_text") as Label;
        EditorFileDialog _atlas_file = new()
        {
            FileMode = EditorFileDialog.FileModeEnum.SaveFile,
            Access = EditorFileDialog.AccessEnum.Filesystem,
            Title = "Choose a folder for the atlas spritesheet",
            Mode = Window.ModeEnum.Maximized
        };
        _atlas_file.FileSelected += (_path) =>
        {
            atlas_path = _path;
            atlas_label.Text = _path;
        };
        gui.AddChild(_atlas_file);
        _atlas_file.CurrentPath = atlas_path;
        (gui.FindChild("get_atlas_path") as BaseButton).Pressed += () => _atlas_file.PopupCentered();
        atlas_label.Text = atlas_path;

        AddToolMenuItem("Generate Atlas & Textures...", Callable.From(() => gui.PopupCentered()));
        GD.PrintRich("[color=cyan]To start exporting images, go to Project > Tools and click 'Generate Atlas & Textures...'.[/color]");
    }
    public override void _ExitTree()
    {
        RemoveToolMenuItem("Generate Atlas & Textures...");
        gui.Free();

        atlas_path = GlobalManager.ABSOLUTE_SPRITES_PATH + "atlases/";
        output_path = GlobalManager.ABSOLUTE_SKINS_PATH;
        image_size = 64;
        img_folders = null;
        GC.Collect(2);
        GD.PrintRich("[color=red]Removed atlas export. Deleted tracked paths.[/color]");
    }
    internal void THROW_POPUP(string _text, bool _bootBack = false, string _title = "Error!")
    {
        var _dialog = new AcceptDialog()
        {
            DialogText = _text,
            Title = _title
        };
        if (_bootBack)
            _dialog.Confirmed += () => gui.PopupCentered();
        
        if (gui.Visible)
            gui.AddChild(_dialog);
        else
            AddChild(_dialog);
            
        _dialog.PopupCentered();
    }
    internal bool SET_IMAGE_SIZE(string _text)
    {
        var _last = image_size;
        if (!int.TryParse(_text, out image_size))
        {
            image_size = _last;
            lineEdit.Text = image_size.ToString();
            THROW_POPUP("Invalid image size!");
            return false;
        }
        return true;
    }
    internal void ON_DIR_SELECT(string _path)
    {
        string _dir_name = Path.GetFileName(_path);
        if (!img_folders.Exists((e) => e.name == _dir_name))
        {
            var _file_path = _path + "/";
            var _files = FILE_LOOKUP_RECURSIVE(_file_path, true);
            img_folders.Add(new(_dir_name, _file_path, _files.Length));
            Array.ForEach(_files, GD.Print);
            gui.AddChild(new AcceptDialog() { DialogText = $"Saved <{_dir_name}> as path. Amount of files was {img_folders[^1].max_files}" });
            container.AddChild(new Label() { Text = _file_path });
        }
        else
            THROW_POPUP($"<{_dir_name}> is already a tracked folder!");
    }
    internal void EXECUTE_WRAP()
    {
        if (img_folders.Count == 0)
        {
            THROW_POPUP("No folders have been selected.", true);
            return;
        }
        if (!SET_IMAGE_SIZE(lineEdit.Text))
            return;

        try
        {
            for (int i = 0; i < img_folders.Count; i++)
            {
                GD.PrintRich($"[color=cyan]Exporting folder <{img_folders[i].name}> at <{img_folders[i].path}>...[/color]");
                GET_IMAGE_DATA(img_folders[i].path, i);
            }
            if (!GENERATE_ATLAS())
                return;
            for (int i = 0; i < img_folders.Count; i++)
            {
                if (!GENERATE_TEXTURE_FOLDERS(img_folders[i].name, i))
                    return;
            }
        }
        catch (Exception _err)
        {
            GD.PrintErr(_err);
            return;
        }
        THROW_POPUP("Imported files succesfully! Click in and out of the editor application to refresh the filesystem and see the results.", false, "Success!");
    }
    internal static string[] FILE_LOOKUP_RECURSIVE(string _path, bool _root, string _subfolder_concat = "")
    {
        // finds all files within a root directory, filenames include subfolders as part of their name
        var _directories = DirAccess.GetDirectoriesAt(_path + _subfolder_concat);
        var _files = DirAccess.GetFilesAt(_path + _subfolder_concat);

        // if isnt root, append the subfolders to the filename
        if (!_root)
        {
            for (int i = 0; i < _files.Length; i++)
                _files[i] = Path.Combine(_subfolder_concat, _files[i]);
        }
        // ignore is used to avoid exporting non-related assets
        _files = [.. _files.Where((s) => !s.StartsWith("ignore"))];

        for (int i = 0; i < _directories.Length; i++)
        {
            _files = [.. _files, .. FILE_LOOKUP_RECURSIVE(_path, false,
                    _root ? _directories[i] + "/" : Path.Combine(_subfolder_concat, _directories[i]))];
        }

        return _files;
    }
    internal void GET_IMAGE_DATA(string _path, int _dir_index)
    {
        var _files = FILE_LOOKUP_RECURSIVE(_path, true);

        foreach (var _file in _files)
        {
            var _skin_image = Image.LoadFromFile(_path + _file);
            if (_skin_image.GetFormat() != Image.Format.Rgba8)
            {
                GD.PrintErr($"Error on exporting file <{_file}>. Image format is not Rgba8.");
                continue;
            }
            if (_skin_image.GetWidth() != image_size || _skin_image.GetHeight() != image_size)
            {
                GD.PrintErr($"Error on exporting file <{_file}>. Image's size does not match current set image size.");
                continue;
            }

            img_folders[_dir_index].images.Add(new(_file, _skin_image));
            GD.Print($"Image: <{_file}> loaded to <{_path}>.");
        }
    }
    internal bool GENERATE_ATLAS()
    {
        GD.PrintRich("[color=cyan]Creating spriteatlas...[/color]");
        // create atlas by joining all previous images into one spritesheet
        var _image = Image.CreateEmpty(img_folders[0].images.Count * image_size, img_folders.Count * image_size, false, Image.Format.Rgba8);

        for (int i = 0; i < img_folders.Count; i++)
        {
            for (int a = 0; a < img_folders[i].images.Count; a++)
                _image.BlitRect(img_folders[i].images[a].data, new(0, 0, image_size, image_size), new(a * image_size, i * image_size));
        }

        // check if it was saved correctly
        var _texture = ImageTexture.CreateFromImage(_image);
        _image.TakeOverPath(atlas_path);
        var _error_atlas = ResourceSaver.Save(_texture, atlas_path);
        if (_error_atlas == Error.Ok)
            GD.Print($"Atlas saved at <{atlas_path}>");
        else
        {
            THROW_POPUP($"Failed to save atlas. Following error: {_error_atlas}");
            return false;
        }
        return true;
    }
    internal bool GENERATE_TEXTURE_FOLDERS(string _dir_name, int _dir_index)
    {
        Regex _regex = MyRegex();
        // generate atlas textures 
        GD.PrintRich($"[color=cyan]Creating folder and generating textures for <{_dir_name}>...[/color]");
        using var _folder = DirAccess.Open(output_path);
        if (!_folder.DirExists(_dir_name))
        {
            _folder.MakeDir(_dir_name);
            GD.Print($"Created directory for <{_dir_name}>");
        }
        // set index point to the start of 
        for (int i = 0; i < img_folders[_dir_index].images.Count; i++)
        {
            AtlasTexture _texture = new()
            {
                Atlas = ResourceLoader.Load(atlas_path) as ImageTexture,
                Region = new Rect2(i * image_size, _dir_index * image_size, image_size, image_size)
            };

            var _path = $"{output_path}/{_dir_name}/{_regex.Match(img_folders[_dir_index].images[i].name).Value}.tres";
            _texture.TakeOverPath(_path);
            // check if it was saved correctly
            var _error_texture = ResourceSaver.Save(_texture, _path);

            if (_error_texture == Error.Ok)
                GD.Print($"Created resource <{img_folders[_dir_index].images[i].name}> at <{_path}>");
            else
            {
                THROW_POPUP($"Failed to save resource at <{_path}>. Texture Error ID: {_error_texture}");
                return false;
            }
        }
        return true;
    }
}
#endif