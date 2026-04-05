#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

[Tool]
public partial class AtlasGui : Window
{
    // Atlas Variables
    private List<SkinFolder> img_folders = [];
    private string atlas_path = "res://addons/exportimages/atlas.res";
    private string output_path = "res://addons/exportimages/output/";
    private int size_x = 0, size_y = 0;
    private bool is_filter_clip_enabled = true;

    // GUI Elements
    [Export] private BaseButton add_folder_button, loadall_folder_button, clear_list_button, get_atlaspath_button, get_outputpath_button, export_button;
    [Export] private Container folder_paths_container;
    [Export] private Label output_label, atlas_label;
    [Export] private LineEdit sizeXedit, sizeYedit;
    [Export] private CheckButton filterClipButton;

    [GeneratedRegex(@"[\w\. ]+(?=[\.])")]
    private static partial Regex FETCH_FILENAME();

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

    public void Start()
    {
        CloseRequested += QueueFree;
        CLEAR_FOLDER_LIST();

        add_folder_button.Pressed += START_FETCH_FOLDER_PATH;
        loadall_folder_button.Pressed += START_FETCH_FOLDER_PATH_FROMROOT;
        clear_list_button.Pressed += CLEAR_FOLDER_LIST;

        atlas_label.Text = atlas_path;
        output_label.Text = output_path;
        get_atlaspath_button.Pressed += START_FETCH_ATLAS_PATH;
        get_outputpath_button.Pressed += START_FETCH_OUTPUT_PATH;

        sizeXedit.TextSubmitted += (_text) => SET_IMAGE_SIZE(_text, ref size_x);
        sizeXedit.FocusExited += () => SET_IMAGE_SIZE(sizeXedit.Text, ref size_x);
        sizeYedit.TextSubmitted += (_text) => SET_IMAGE_SIZE(_text, ref size_y);
        sizeYedit.FocusExited += () => SET_IMAGE_SIZE(sizeYedit.Text, ref size_y);
        filterClipButton.Toggled += (_state) => is_filter_clip_enabled = _state;

        export_button.Pressed += START_EXPORT;
    }

    internal void CLEAR_FOLDER_LIST()
    {
        img_folders = [];
        for (int i = 0; i < folder_paths_container.GetChildCount(); i++)
            folder_paths_container.GetChild(i).QueueFree();
    }
    internal void START_FETCH_FOLDER_PATH()
    {
        EditorFileDialog _folder_popup = new()
        {
            FileMode = EditorFileDialog.FileModeEnum.OpenDir,
            Access = EditorFileDialog.AccessEnum.Filesystem,
            Title = "Choose a folder to load contents from...",
            Mode = ModeEnum.Windowed
        };
        _folder_popup.DirSelected += ON_DIR_SELECT;
        AddChild(_folder_popup);
        _folder_popup.PopupCentered();
    }
    internal void START_FETCH_FOLDER_PATH_FROMROOT()
    {
        EditorFileDialog _folder_popup = new()
        {
            FileMode = EditorFileDialog.FileModeEnum.OpenDir,
            Access = EditorFileDialog.AccessEnum.Filesystem,
            Title = "Choose a root to load folders from...",
            Mode = ModeEnum.Windowed
        };
        _folder_popup.DirSelected += ON_FULL_DIR_SELECT;
        AddChild(_folder_popup);
        _folder_popup.PopupCentered();
    }
    internal void START_FETCH_ATLAS_PATH()
    {
        EditorFileDialog _atlas_file = new()
        {
            FileMode = EditorFileDialog.FileModeEnum.SaveFile,
            Access = EditorFileDialog.AccessEnum.Resources,
            Title = "Choose a folder for the atlas...",
            Mode = ModeEnum.Windowed
        };
        _atlas_file.FileSelected += (_path) =>
        {
            atlas_path = _path;
            atlas_label.Text = _path;
        };
        AddChild(_atlas_file);
        _atlas_file.CurrentPath = atlas_path;
        _atlas_file.PopupCentered();
    }
    internal void START_FETCH_OUTPUT_PATH()
    {
        EditorFileDialog _output_file = new()
        {
            FileMode = EditorFileDialog.FileModeEnum.OpenDir,
            Access = EditorFileDialog.AccessEnum.Resources,
            Title = "Choose a folder for the output folders...",
            Mode = ModeEnum.Windowed
        };
        _output_file.DirSelected += (_path) =>
        {
            output_path = _path;
            output_label.Text = _path;
        };
        AddChild(_output_file);
        _output_file.PopupCentered();
    }

    internal void SET_IMAGE_SIZE(string _text, ref int _value)
    {
        if (!int.TryParse(_text, out _value) || _value <= 0)
            THROW_POPUP("Invalid image size!");
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
            folder_paths_container.AddChild(new Label() { Text = _file_path });
        }
        else
            THROW_POPUP($"<{_dir_name}> is already a tracked folder!");
    }
    internal void ON_FULL_DIR_SELECT(string _path)
    {
        var _directories = DirAccess.GetDirectoriesAt($"{_path}/");
        for (int i = 0; i < _directories.Length; i++)
            ON_DIR_SELECT($"{_path}/{_directories[i]}");
    }
    internal void THROW_POPUP(string _text, string _title = "Error!")
    {
        var _dialog = new AcceptDialog()
        {
            DialogText = _text,
            Title = _title
        };
        AddChild(_dialog);
        _dialog.PopupCentered();
    }

    // =========| MARK: Export Process
    internal void START_EXPORT()
    {
        if (img_folders.Count == 0)
        {
            THROW_POPUP("No folders have been selected!");
            return;
        }

        if (size_x <= 0 || size_y <= 0)
        {
            THROW_POPUP("Image size is invalid!");
            return;
        }

        atlas_path = atlas_label.Text;
        output_path = output_label.Text;
        is_filter_clip_enabled = filterClipButton.ButtonPressed;

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
            THROW_POPUP(_err.Message);
            return;
        }
        THROW_POPUP("Imported files succesfully! Focus in and out of the editor to refresh and see the results. You can close this window now.", "Success!");
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

        _files = [.. _files.Where((s) =>
        {
            if (!s.StartsWith("ignore")) // ignore is used to avoid exporting non-related assets
                return true;

            if (s.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        })];

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
            if (_skin_image.GetWidth() != size_x || _skin_image.GetHeight() != size_y)
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
        // create atlas by joining all previous images into one spritesheet
        GD.PrintRich("[color=cyan]Creating spriteatlas...[/color]");
        var _image = Image.CreateEmpty(img_folders[0].images.Count * size_x, img_folders.Count * size_y, false, Image.Format.Rgba8);

        for (int i = 0; i < img_folders.Count; i++)
        {
            for (int a = 0; a < img_folders[i].images.Count; a++)
                _image.BlitRect(img_folders[i].images[a].data, new(0, 0, size_x, size_y), new(a * size_x, i * size_y));
        }

        var _texture = ImageTexture.CreateFromImage(_image);
        _texture.TakeOverPath(atlas_path);
        var _error_atlas = ResourceSaver.Save(_texture, atlas_path);

        // check if it was saved correctly
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
        GD.PrintRich($"[color=cyan]Creating folder and generating textures for <{_dir_name}>...[/color]");
        Regex _regex = FETCH_FILENAME();

        // create and open output path
        using var _folder = DirAccess.Open(output_path);
        if (!_folder.DirExists(_dir_name))
        {
            _folder.MakeDir(_dir_name);
            GD.Print($"Created directory for <{_dir_name}>");
        }

        // generate all individual atlastexture's
        for (int i = 0; i < img_folders[_dir_index].images.Count; i++)
        {
            AtlasTexture _texture = new()
            {
                Atlas = ResourceLoader.Load(atlas_path) as ImageTexture,
                Region = new Rect2(i * size_x, _dir_index * size_y, size_x, size_y),
                FilterClip = is_filter_clip_enabled,
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