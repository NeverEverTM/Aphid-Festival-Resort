using Godot;
using System;
using System.Threading.Tasks;

public partial class GenerationsPanel : Control
{
    public static GenerationsPanel Instance { get; private set; }

    [Export] public AnimationPlayer animPlayer;
    [Export] private RichTextLabel descriptionLabel;
    [Export] private Label warning;
    [Export] private Control iconNode;
    [Export] private Container aphidContainer, pageContainer1, pageContainer2;
    [Export] private TextureButton albumButton, aphidAlbumButton, pageLeft, pageRight;
    [Export] private TextureRect fullPhotoDisplay;

    private const string PHOTO_SLOT_SCENE = "uid://s63wnc34rtmn",
        DEFAULT_PARENT = "Crystal",
        DEFAULT_UNKNOWN_STAT = "[color='darkgray']???[/color]";

    private AphidInstance current_aphid;
    private Guid current_key;
    private int current_page = -1;
    private string[] current_photos;
    private bool is_full_photo_shown, is_album_open, is_menu_open, is_busy;
    public MenuInstance Menu { get; set; }

    // TODO: move album to its own submenu

    public override void _EnterTree()
    {
        Instance = this;
        Menu = new("generations", animPlayer,
        Open: _ => CreateInterface(),
        TryClose: _ =>
        {
            if (is_busy || animPlayer.IsPlaying())
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
            return true;
        },
        Close: _ => ClearInterface());

        pageLeft.Pressed += () => _ = ShowPage(current_page - 1, true);
        pageRight.Pressed += () => _ = ShowPage(current_page + 1, true);
        aphidAlbumButton.Pressed += () => DisplayAlbum(!is_album_open, true);
        albumButton.Pressed += () => DisplayAlbum(!is_album_open, false);
    }
    public override void _ExitTree()
    {
        Menu = null;
    }
    public static void InputAction_OpenGenerations() =>
        _ = CanvasManager.Menus.SetTo(Instance.Menu);

    private void ClearInterface()
    {
        RemoveAphidInfo();

        for (int i = 0; i < aphidContainer.GetChildCount(); i++)
            aphidContainer.GetChild(i).QueueFree();
    }
    private void CreateInterface()
    {
        ClearInterface();
        descriptionLabel.Text = Tr("generations_description");

        // Current Generation
        foreach (var _pair in GameManager.Aphids)
            aphidContainer.AddChild(CanvasManager.CreateAphidSlot(_pair.Key, false, SetAphidInfo));

        // Past Generations
        foreach (var _pair in GameManager.AphidArchive)
            aphidContainer.AddChild(CanvasManager.CreateAphidSlot(_pair.Key, false, SetAphidInfo));
    }
    public override void _Input(InputEvent @event)
    {
        if (!Menu.IsOpen)
            return;

        if (@event.IsActionPressed(InputNames.OpenGenerations))
            InputAction_OpenGenerations();
        else if (is_album_open)
        {
            if (@event.IsActionPressed(InputNames.Right))
                _ = ShowPage(current_page + 1, true);
            else
                if (@event.IsActionPressed(InputNames.Left))
                _ = ShowPage(current_page - 1, true);
        }
    }

    private void SetAphidInfo(Guid _key)
    {
        if (is_busy || animPlayer.IsPlaying())
            return;
        // set interface and data
            RemoveAphidInfo();
        current_key = _key;
        AphidData.Genes _genes;
        bool _current;
        if (GameManager.Aphids.TryGetValue(_key, out AphidInstance _aphid))
        {
            current_aphid = _aphid;
            _genes = current_aphid.Genes;
            _current = true;
        }
        else
        {
            _genes = GameManager.AphidArchive[_key];
            _current = false;
        }
        iconNode.AddChild(CanvasManager.CreateAphidSlot(_key, true));

        // get parents and display either a single/couple aphid
        string _parents = DEFAULT_PARENT;
        if (GameManager.Aphids.TryGetValue(_genes.Mother, out AphidInstance _mother))
            _parents = _mother.Genes.Name;
        else if (GameManager.AphidArchive.TryGetValue(_genes.Mother, out var _motherExtra))
            _parents = _motherExtra?.Name;

        if (!_genes.Mother.Equals(_genes.Father))
        {
            if (GameManager.Aphids.TryGetValue(_genes.Father, out AphidInstance _father))
                _parents += $" & {_father?.Genes.Name}";
            else if (GameManager.AphidArchive.TryGetValue(_genes.Father, out var _fatherExtra))
                _parents += $" & {_fatherExtra?.Name}";
        }

        // show stats based on bondship, dead aphids ignore this and will show all of them
        float _bondship = current_aphid != null ? current_aphid.Status.Bondship : 100;
        string _unknown = DEFAULT_UNKNOWN_STAT;
        string[] _elements = [
            $"[color='gold']{_genes.Name}[/color]",
            $"{Tr("generations_owner")} {_genes.Owner}",
            $"{Tr("generations_parent")} {_parents}",
            $"[color=lightgreen]{Tr("generations_favorite")}[/color] {(!_current || _bondship > 70 ? Tr("food_flavor_" + (int)_genes.FoodPreference) : _unknown)}",
            !_current || _bondship > 15 ? Tr("trait_" + _genes.Traits[0]) : _unknown,
            !_current || _bondship > 30 ? Tr("trait_" + _genes.Traits[1]) : _unknown,
            !_current || _bondship > 55 ? Tr("trait_" + _genes.Traits[2]) : _unknown,
            _genes.Traits.Count > 3 ? // dont show this trait if the aphid does not have a fourth one
                (!_current || _bondship > 90 ? Tr("trait_" + _genes.Traits[3]) : _unknown)
                : string.Empty
        ];
        descriptionLabel.Text = _elements.Join("\n");

        aphidAlbumButton.Show();
    }
    private void RemoveAphidInfo()
    {
        if (iconNode.GetChildCount() > 0)
            iconNode.GetChild(0).QueueFree();
        descriptionLabel.Text = string.Empty;
        aphidAlbumButton.Hide();
        current_aphid = null;
        current_key = Guid.Empty;
        current_page = -1;
        current_photos = null;
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
            string _path = SaveSystem.ProfilePath + SaveSystem.PROFILE_ALBUM_DIR;

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
            SoundManager.CreateSound("ui/album_open", false);
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
        warning.Text = "generations_no_photo";
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

        try
        {
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
            for (int i = _page * 12, eol = Math.Min(i + 12, current_photos.Length), s = 0; i < eol; i++, s++)
                await CreatePhotoSlot(current_photos[i], s < 6 ? pageContainer1 : pageContainer2);
        }
        catch (Exception _error)
        {
            DebugLogger.Print(DebugLogger.LogPriority.Error, "GenerationsTracker: Error on creating photo album.", _error);
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
            if (is_busy || is_full_photo_shown)
                return;
            else
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