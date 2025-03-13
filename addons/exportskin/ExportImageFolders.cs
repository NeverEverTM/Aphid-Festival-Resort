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
    FileDialog popup;
    List<SkinFolder> img_folders;
    const string atlas_path = GlobalManager.RES_SKINS_PATH + "skins-atlas.tres";
    int DANGER;
    
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
        popup = new()
        {
            FileMode = FileDialog.FileModeEnum.OpenDir,
            Access = FileDialog.AccessEnum.Filesystem
        };
        popup.DirSelected += ON_DIR_SELECT;
        AddChild(popup);
        img_folders = [];

        // gathers the relevant folders from which export the skin folders
        AddToolMenuItem("Select Image Folder...", Callable.From(() => popup.PopupCentered()));
        // generates the relevant textures
        AddToolMenuItem("Generate Atlas & Textures...", Callable.From(EXECUTE_WRAP));
        GD.Print("To start exporting images, go to Project > Tools and click 'Select Image Folder...' " +
                "and then once you have selected all of your skin folders, click on 'Generate Atlas & Textures...'.");
    }
    public override void _ExitTree()
    {
        RemoveToolMenuItem("Select Image Folder...");
        RemoveToolMenuItem("Generate Atlas & Textures...");
        popup.Free();
        img_folders = null;
        GC.Collect(2);
        GD.Print("Removed atlas export. Deleted tracked paths.");
    }
    public void EXECUTE_WRAP()
    {
        if (img_folders.Count == 0)
        {
            GD.PrintErr("No folders have been selected");
            return;
        }
        try
        {
            for (int i = 0; i < img_folders.Count; i++)
            {
                GD.PrintRich($"[color=cyan]Exporting folder <{img_folders[i].name}> at <{img_folders[i].path}>...[/color]");
                GET_IMAGE_DATA(img_folders[i].path, i);
            }
            GENERATE_ATLAS();
            for (int i = 0; i < img_folders.Count; i++)
                GENERATE_SKIN_FOLDER(img_folders[i].name, i);
        }
        catch (Exception _err)
        {
            GD.PrintErr(_err);
            return;
        }
        GD.PrintRich($"[color=green]Imported folders succesfully. You will need to refresh the filesystem in order to see the results, you can simply click in and out of the editor application.[/color]");
    }
    public void ON_DIR_SELECT(string _path)
    {
        string _dir_name = Path.GetFileName(_path);
        if (!img_folders.Exists((e) => e.name == _dir_name))
        {
            var _files = FILE_LOOKUP_RECURSIVE(_path + "/", true);
            img_folders.Add(new(_dir_name, _path + "/", _files.Length));
            Array.ForEach(_files, GD.Print);
            GD.PrintRich($"[color=cyan]Saved <{_dir_name}> as path. Amount of files was {img_folders[^1].max_files}[/color]");
        }
        else
            GD.Print($"<{_dir_name}> is already a tracked folder!");
    }
    public string[] FILE_LOOKUP_RECURSIVE(string _path, bool _root, string _subfolder_concat = "")
    {
        DANGER++;
        if (DANGER > 1000)
            throw new OverflowException();

        // finds all files within a root directory, filenames include subfolders as part of their name
        var _directories = DirAccess.GetDirectoriesAt(_path + _subfolder_concat);
        var _files = DirAccess.GetFilesAt(_path + _subfolder_concat);

        // if isnt root, append the subfolders to the filename
        if (!_root)
        {
            for(int i = 0; i < _files.Length; i++)
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
    public void GET_IMAGE_DATA(string _path, int _dir_index)
    {
        var _files = FILE_LOOKUP_RECURSIVE(_path, true);

        foreach (var _file in _files)
        {
            var _skin_image = Image.LoadFromFile(_path + _file);
            if (_skin_image.GetFormat() != Image.Format.Rgba8)
            {
                GD.PrintErr($"Error on exporting file <{_file}>. Image format is not Rgba8.");
                return;
            }
            img_folders[_dir_index].images.Add(new(_file, _skin_image));
            GD.Print($"Image: <{_file}> loaded to <{_path}>.");
        }
    }
    public void GENERATE_ATLAS()
    {
        GD.PrintRich("[color=cyan]Creating spriteatlas...[/color]");
        // create atlas by joining all previous images into one spritesheet
        var _image = Image.CreateEmpty(img_folders[0].images.Count * 64, img_folders.Count * 64, false, Image.Format.Rgba8);
        
        for (int i = 0; i < img_folders.Count; i++)
        {
            for (int a = 0;a < img_folders[i].images.Count; a++)
                _image.BlitRect(img_folders[i].images[a].data, new(0, 0, 64, 64), new(a * 64, i * 64));
        }

       // check if it was saved correctly
        var _error_atlas = ResourceSaver.Save(ImageTexture.CreateFromImage(_image), 
                atlas_path);
        if (_error_atlas == Error.Ok)
            GD.Print($"Atlas saved at <{atlas_path}>");
        else
        {
            GD.PrintErr($"Failed to save atlas. Following error: {_error_atlas}");
            return;
        }
    }
    public void GENERATE_SKIN_FOLDER(string _dir_name, int _dir_index)
    {
        Regex _regex = MyRegex();
        // generate atlas textures 
        GD.PrintRich($"[color=cyan]Creating folder and generating textures for <{_dir_name}>...[/color]");
        using var _skinFolder = DirAccess.Open(GlobalManager.RES_SKINS_PATH);
        if (!_skinFolder.DirExists(_dir_name))
        {
            _skinFolder.MakeDir(_dir_name);
            GD.Print($"Created skin directory for <{_dir_name}>");
        }
        // set index point to the start of 
        for (int i = 0; i < img_folders[_dir_index].images.Count; i++)
        {
            AtlasTexture _texture = new()
            {
                Atlas = ResourceLoader.Load(atlas_path) as ImageTexture,
                Region = new Rect2(i * 64, _dir_index * 64, 64, 64)
            };
            // check if it was saved correctly
            var _error_texture = ResourceSaver.Save(_texture, GlobalManager.RES_SKINS_PATH + 
                    _dir_name + "/" + _regex.Match(img_folders[_dir_index].images[i].name).Value + ".tres");
            
            if (_error_texture == Error.Ok)
                GD.Print($"Created resource <{img_folders[_dir_index].images[i].name}>");
            else
            {
                GD.PrintErr($"Failed to save resource. Following error: {_error_texture}");
                return;
            }
        }
    }
}
#endif