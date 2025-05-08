using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class GenerationsTracker : Control, SaveSystem.IDataModule<GenerationsTracker.Savefile>
{
    private static SaveSystem.SaveModule<Savefile> SaveModule;
    public static Savefile Data { get; set; }

    [Serializable]
    public struct Savefile
    {
        public Dictionary<Guid, AphidData.Genes> Archive { get; set; }

        public readonly bool Add(AphidInstance _instance)
        {
            if (Archive.ContainsKey(new Guid(_instance.ID)))
                return false;
            Archive.Add(new(_instance.ID), _instance.Genes);
            return true;
        }

        public Savefile()
        {
            Archive = [];
        }
    }

    public void Set(Savefile _data)
    {
        Data = _data;
    }
    public Savefile Get()
    {
        return Data;
    }
    public Savefile Default() => new();

    [Export] public AnimationPlayer animPlayer;
    [Export] private RichTextLabel descriptionLabel;
    [Export] private Label warning;
    [Export] private Control iconNode;
    [Export] private Container aphidContainer, pageContainer1, pageContainer2;
    [Export] private TextureButton albumButton, aphidAlbumButton, pageLeft, pageRight;
    [Export] private TextureRect fullPhotoDisplay;
    private const string APHID_SLOT_SCENE = "uid://d7m5e6tlxyve", PHOTO_SLOT_SCENE = "uid://s63wnc34rtmn";

    private Aphid current_aphid;
    private Guid current_key;
    private int current_page = -1;
    private string[] current_photos;
    private bool is_full_photo_shown, is_album_open, is_menu_open, is_busy;
    public static MenuUtil.MenuInstance Menu { get; set; }

    public override void _EnterTree()
    {
        SaveModule = new("generations", this)
        {
            Extension = SaveSystem.SAVEFILE_EXTENSION,
            RelativePath = SaveSystem.PROFILEAPHIDS_DIR
        };
        SaveSystem.ProfileClassData.Add(SaveModule);
    }
    public override void _Ready()
    {
        Menu = new("generations", animPlayer,
        Open: _ =>
        {
            RemoveAphidInfo(false);
            descriptionLabel.Text = Tr("generations_description");

            // set window
            for (int i = 0; i < aphidContainer.GetChildCount(); i++)
                aphidContainer.GetChild(i).QueueFree();

            // Current Generation
            foreach (var _pair in GameManager.Aphids)
                CreateAphidSlot(_pair.Key, _pair.Value.Genes, _pair.Value.Status.IsAdult, true);

            // Past Generations
            foreach (var _pair in Data.Archive)
                CreateAphidSlot(_pair.Key, _pair.Value);
        },
        Close: _ =>
        {
            if (is_busy)
                return false;
            else if (is_full_photo_shown)
            {
                is_full_photo_shown = false;
                animPlayer.Play("close_photo");
                return false;
            }
            else if (is_album_open)
            {
                DisplayAlbum(false);
                return false;
            }
            RemoveAphidInfo(false);
            return true;
        });

        pageLeft.Pressed += () => _ = ShowPage(current_page - 1, true);
        pageRight.Pressed += () => _ = ShowPage(current_page + 1, true);
        aphidAlbumButton.Pressed += () => DisplayAlbum(!is_album_open, true);
        albumButton.Pressed += () => DisplayAlbum(!is_album_open, false);
    }
    public override void _Input(InputEvent @event)
    {
        if (Menu.IsOpen)
        {
            if (@event.IsActionPressed(InputNames.OpenGenerations))
            {
                CanvasManager.Menus.OpenMenu(Menu);
                AcceptEvent();
            }
            if (@event.IsActionPressed(InputNames.Right))
                _ = ShowPage(current_page + 1, true);
            else
            if (@event.IsActionPressed(InputNames.Left))
                _ = ShowPage(current_page - 1, true);
        }
    }

    private void CreateAphidSlot(Guid _key, AphidData.Genes _value, bool _isAdult = true, bool _current = false, bool _AsIcon = false)
    {
        TextureButton _slot = (ResourceLoader.Load(APHID_SLOT_SCENE) as PackedScene).Instantiate() as TextureButton;
        Control _skin = _slot.GetChild(0) as Control;
        if (_AsIcon)
        {
            iconNode.AddChild(_slot);
            _slot.Disabled = true;
            _slot.FocusMode = FocusModeEnum.None;
        }
        else
        {
            aphidContainer.AddChild(_slot);
            _slot.Pressed += () => SetAphidInfo(_key, _isAdult, _current);
        }

        if (!_isAdult)
            (_slot.GetChild(0) as Control).SetPosition(new(8, 0));

        if (!_current) // For the now dead
            _slot.SelfModulate = new Color("gold");

        // =====| Set Skin |======
        TextureRect[] _pieces = [
            _skin.GetChild(0) as TextureRect,
            _skin.GetChild(1) as TextureRect,
            _skin.GetChild(2) as TextureRect,
            _skin.GetChild(3) as TextureRect,
            _skin.GetChild(4) as TextureRect
        ];
        _pieces[0].Texture = AphidSkin.GetSkinPiece(_value.AntennaType, "antenna", "idle", _isAdult);
        _pieces[0].Modulate = _value.AntennaColor;
        _pieces[1].Texture = AphidSkin.GetSkinPiece(_value.LegType, "legs", "idle", _isAdult);
        _pieces[1].Modulate = _value.LegColor;
        _pieces[2].Texture = AphidSkin.GetSkinPiece(_value.BodyType, "body", "idle", _isAdult);
        _pieces[2].Modulate = _value.BodyColor;
        _pieces[3].Texture = AphidSkin.GetSkinPiece(_value.EyeType, "eyes", "idle", _isAdult);
        _pieces[3].Modulate = _value.EyeColor;
        _pieces[4].Texture = AphidSkin.GetSkinPiece(_value.LegType, "legs", "idle", _isAdult);
        _pieces[4].Modulate = _value.LegColor;
    }
    private void SetAphidInfo(Guid _key, bool _isAdult = false, bool _current = false)
    {
        RemoveAphidInfo();
        current_aphid = null;
        current_key = _key;
        if (_current)
        {
            if (!GameManager.Aphids.TryGetValue(_key, out AphidInstance _aphid))
            {
                Logger.Print(Logger.LogPriority.Warning, "GenerationsTracker: This aphid is no longer available.");
                _current = false;
            }
            else
                current_aphid = _aphid.Entity;
        }

        // Set Aphid Info
        AphidData.Genes _genes = _current ? current_aphid.Instance.Genes : Data.Archive[_key];
        CreateAphidSlot(_key, _genes, _isAdult, _current, true);
        string _parents = _genes.Mother;

        if (_genes.Mother != _genes.Father)
            _parents += $" & {_genes.Father}";

        var _bondship = current_aphid?.Instance.Status.Bondship;
        var _unknown = "[color='darkgray']???[/color]";
        string[] _elements = [
            $"[color='gold']{_genes.Name}[/color]",
            $"{Tr("generations_owner")} {_genes.Owner}",
            $"{Tr("generations_parent")} {_parents}",
            $"[color=lightgreen]{Tr("generations_favorite")}[/color] {(!_current || _bondship > 70 ? Tr("food_flavor_" + (int)_genes.FoodPreference) : _unknown)}",
            !_current || _bondship > 15 ? Tr("trait_" + _genes.Traits[0]) : _unknown,
            !_current || _bondship > 30 ? Tr("trait_" + _genes.Traits[1]) : _unknown,
            !_current || _bondship > 55 ? Tr("trait_" + _genes.Traits[2]) : _unknown
        ];

        descriptionLabel.Text = string.Empty; ;
        for (int i = 0; i < _elements.Length; i++)
        {
            descriptionLabel.AppendText(_elements[i]);
            if (i != _elements.Length - 1)
                descriptionLabel.AppendText("\n");
        }
        if (_genes.Traits.Count > 3)
            descriptionLabel.AppendText("\n" + (!_current || _bondship > 90 ? Tr("trait_" + _genes.Traits[3]) : _unknown));

        aphidAlbumButton.Show();
        SoundManager.CreateSound(current_aphid?.AudioDynamic_Idle);
    }
    private void RemoveAphidInfo(bool _audioConfirmation = true)
    {
        if (iconNode.GetChildCount() > 0)
            iconNode.GetChild(0).QueueFree();
        descriptionLabel.Text = string.Empty;
        aphidAlbumButton.Hide();
        current_aphid = null;
        current_key = Guid.Empty;
        current_page = -1;
        current_photos = null;

        if (_audioConfirmation)
            SoundManager.CreateSound("ui/button_switch");
    }
    /// <param name="_current">If true, display current aphid album, otherwise, the global one</param>
    private async void DisplayAlbum(bool _state, bool _current = true)
    {
        if (is_busy)
            return;
        is_busy = true;

        is_album_open = _state;
        current_page = -1;
        current_photos = [];

        if (_state)
        {
            warning.Hide();
            // either show aphid specific screenshots or global resort screenshots
            string _path = SaveSystem.ProfilePath + SaveSystem.ProfileAlbumDir;

            if (_current)
                _path += current_key.ToString() + "/";

            if (DirAccess.DirExistsAbsolute(_path))
            {
                var _files = DirAccess.GetFilesAt(_path);

                if (_files.Length == 0)
                    await DisplayNoPhoto();
                else
                {
                    for (int i = 0; i < _files.Length; i++)
                        _files[i] = _path + _files[i];
                    current_photos = _files;

                    await ShowPage(0, false, true);
                }
            }
            else
                await DisplayNoPhoto();

            // open the album anyway if we dont have photos
            animPlayer.Play("open_album");
        }
        else
            animPlayer.Play("close_album");

        while (animPlayer.IsPlaying())
            await Task.Delay(1);

        is_busy = false;
    }
    private async Task DisplayNoPhoto()
    {
        current_page = 0;
        warning.Text = "warning_no_photo";
        warning.Show();
        await CleanPage();
    }
    private Task CleanPage()
    {
        // delete previous page contents
        for (int i = 0; i < pageContainer1.GetChildCount(); i++)
            pageContainer1.GetChild(i).QueueFree();

        for (int i = 0; i < pageContainer2.GetChildCount(); i++)
            pageContainer2.GetChild(i).QueueFree();
        return Task.CompletedTask;
    }
    private async Task ShowPage(int _page, bool _playPageAnim, bool _forceRefresh = false)
    {
        if (is_busy && !_forceRefresh)
            return;
        is_busy = true;

        // page clamp
        int _max = current_photos.Length / 12 + 1;
        _page = _page < 0 ? _max - 1 : _page;
        _page = _page >= _max ? 0 : _page;
        if (!_forceRefresh && _page == current_page)
        {
            is_busy = false;
            return;
        }
        current_page = _page;

        if (_playPageAnim)
        {
            animPlayer.Play("close_page");
            while (animPlayer.IsPlaying())
                await Task.Delay(1);
        }

        await CleanPage();

        // generate current page of contents
        // each "page" is the sum of both visible pages, containing 6 photos each, thus, we get 12 items
        try
        {
            for (int i = _page * 12, eol = Math.Min(i + 12, current_photos.Length), s = 0; i < eol; i++, s++)
                await CreatePhotoSlot(current_photos[i], s < 6 ? pageContainer1 : pageContainer2);
        }
        catch (Exception _error)
        {
            Logger.Print(Logger.LogPriority.Error, "GenerationsTracker: Error on creating photo album", _error);
        }
        if (_playPageAnim)
        {
            animPlayer.Play("open_page");
            while (animPlayer.IsPlaying())
                await Task.Delay(1);
        }
        if (!_forceRefresh)
            is_busy = false;
    }
    private async Task CreatePhotoSlot(string _photo_path, Control _parent)
    {
        var _photo = Image.LoadFromFile(_photo_path);
        BaseButton _slot = (ResourceLoader.Load(PHOTO_SLOT_SCENE) as PackedScene).Instantiate() as BaseButton;
        var _frame = _slot.GetChild(1) as TextureRect;
        _frame.Texture = ImageTexture.CreateFromImage(_photo);

        _slot.Pressed += () =>
        {
            if (!is_full_photo_shown)
            {
                is_full_photo_shown = true;
                fullPhotoDisplay.Texture = _frame.Texture;
                animPlayer.Play("show_photo");
            }
        };

        _parent.AddChild(_slot);
        await Task.Delay(1);
    }
}