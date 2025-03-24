using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
            Archive = new();
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
    [Export] private Control iconNode;
    [Export] private Container aphidContainer, photoContainer;
    [Export] private TextureButton albumButton, aphidAlbumButton;
    private const string APHID_SLOT_SCENE = "uid://d7m5e6tlxyve", PHOTO_SLOT_SCENE = "uid://s63wnc34rtmn";
    private static Vector2 APHID_OFFSET = new(-16, -36);

    private Aphid current_aphid;
    private TextureRect current_tracker;
    private bool is_album_open;
    private Tween vanish_tween;
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
            descriptionLabel.Text = Tr("generations_description");

            // set window
            for (int i = 0; i < aphidContainer.GetChildCount(); i++)
                aphidContainer.GetChild(i).QueueFree();

            // Current Generation
            foreach (var _pair in GameManager.Aphids)
                GenerateAphidSlot(_pair.Key, _pair.Value.Genes, _pair.Value.Status.IsAdult, true);

            // Past Generations
            foreach (var _pair in Data.Archive)
                GenerateAphidSlot(_pair.Key, _pair.Value);

            // setup variables
            if (IsInstanceValid(current_tracker))
                current_tracker.QueueFree();

            if (iconNode.GetChildCount() > 0)
                iconNode.GetChild(0).QueueFree();
            if (is_album_open)

                current_tracker = null;
            current_aphid = null;
        }, Close: _ =>
        {
            if (is_album_open)
            {
                DisplayAlbum(false);
                return false;
            }
            aphidAlbumButton.Hide();
            return true;
        });
        aphidAlbumButton.Pressed += () =>
        {
            if (DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath +
                SaveSystem.ProfileAlbumDir + current_aphid.Instance.ID))
            {
                DisplayAlbum(!is_album_open, SaveSystem.ProfilePath +
                    SaveSystem.ProfileAlbumDir + current_aphid.Instance.ID + "/");
            }
        };
        albumButton.Pressed += () => DisplayAlbum(!is_album_open, SaveSystem.ProfilePath + SaveSystem.ProfileAlbumDir);
        vanish_tween = CreateTween();
        vanish_tween.Kill();
    }
    public override void _Process(double delta)
    {
        if (IsInstanceValid(current_tracker))
        {
            Vector2 _aphidPos = GlobalManager.Utils.GetWorldToCanvasPosition(current_aphid.GlobalPosition);

            if (!vanish_tween.IsValid() && current_aphid.GlobalPosition.DistanceTo(Player.Instance.GlobalPosition) < 100)
            {
                vanish_tween = current_tracker.CreateTween();
                vanish_tween.TweenProperty(current_tracker, "self_modulate", new Color(0), 1);
                vanish_tween.Finished += () =>
                {
                    if (IsInstanceValid(current_tracker))
                        current_tracker.QueueFree();
                };
            }
            // if is on screen, point at it
            if (_aphidPos.X > 0 && _aphidPos.X < GlobalManager.SCREEN_SIZE_CANVAS.X
                    && _aphidPos.Y > 0 && _aphidPos.Y < GlobalManager.SCREEN_SIZE_CANVAS.Y)
                current_tracker.GlobalPosition = _aphidPos + APHID_OFFSET;
            else // if is far away, point at its direction
                current_tracker.GlobalPosition = GlobalManager.SCREEN_CENTER_CANVAS + GlobalManager.SCREEN_CENTER_CANVAS.DirectionTo(_aphidPos) * 100;
        }
    }

    private void GenerateAphidSlot(Guid _key, AphidData.Genes _value, bool _isAdult = true, bool _current = false, bool _AsIcon = false)
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
        // Track aphid if is alive
        if (current_aphid != null && IsInstanceValid(current_aphid))
        {
            GD.Print(current_aphid.Instance.Genes.Name);
            if (_current)
            {
                if (current_aphid.Instance.ID.Equals(_key.ToString()))
                    TrackAphid(_key);
                else
                {
                    vanish_tween.Kill();
                    current_tracker.QueueFree();
                }
            }
            else
                RemoveAphidInfo();
        }

        current_aphid = _current ? GameManager.Aphids[_key].Entity : null;

        // Set Aphid Info
        AphidData.Genes _genes = _current ? current_aphid.Instance.Genes : Data.Archive[_key];
        GenerateAphidSlot(_key, _genes, _isAdult, _current, true);
        string _parents = _genes.Mother;

        if (_genes.Mother != _genes.Father)
            _parents += $" & {_genes.Father}";

        var _bondship = current_aphid?.Instance.Status.Bondship;
        var _unknown = "[color='darkgray']???[/color]";
        string[] _elements = [
            $"[color='gold']{_genes.Name}[/color]",
            $"{Tr("generations_owner")} {_genes.Owner}",
            $"{Tr("generations_parent")} {_parents}",
            $"[color=lightgreen]{Tr("generations_favorite")}[/color] {(!_current || _bondship > 70 ? Tr("food_flavor_" + _genes.FoodPreference) : _unknown)}",
            !_current || _bondship > 15 ? Tr("trait_" + _genes.Traits[0]) : _unknown,
            !_current || _bondship > 30 ? Tr("trait_" + _genes.Traits[1]) : _unknown,
            !_current || _bondship > 55 ? Tr("trait_" + _genes.Traits[2]) : _unknown
        ];

        descriptionLabel.Clear();
        for (int i = 0; i < _elements.Length; i++)
        {
            descriptionLabel.AppendText(_elements[i]);
            if (i != _elements.Length - 1)
                descriptionLabel.AppendText("\n");
        }
        if (_genes.Traits.Count > 3)
            descriptionLabel.AppendText("\n" + (!_current || _bondship > 90 ? Tr("trait_" + _genes.Traits[3]) : _unknown));

        aphidAlbumButton.Show();
        SoundManager.CreateSound("ui/button_select");
    }
    private void RemoveAphidInfo()
    {
        if (iconNode.GetChildCount() > 0)
            iconNode.GetChild(0).QueueFree();
        descriptionLabel.Clear();
        aphidAlbumButton.Hide();
        SoundManager.CreateSound("ui/button_switch");
    }
    private void TrackAphid(Guid _key)
    {
        // if we already have a tracker delete it
        if (IsInstanceValid(current_tracker))
        {
            vanish_tween.Kill();
            current_tracker.Free();
            // if it was the same aphid marked, then simply marked it for deletion and leave
            if (_key.ToString().Equals(current_aphid?.Instance.ID))
                return;
        }

        // create tracker
        TextureRect _track = new()
        {
            Texture = GlobalManager.GetIcon(current_aphid.Instance.Status.IsAdult ?
                    "aphid_adult" : "aphid_child"),
            Scale = new(2, 2)
        };
        current_tracker = _track;

        // set it to vanish after a while
        Timer _vanishTimer = new();
        _track.AddChild(_vanishTimer);

        _vanishTimer.Timeout += () =>
        {
            vanish_tween = CreateTween();
            vanish_tween.TweenProperty(_track, "self_modulate", new Color("white"), 5);
            vanish_tween.Finished += () =>
            {
                if (IsInstanceValid(_track))
                    _track.QueueFree();
            };
        };

        // we set this as parent so it does not show up above the generations menu 
        GetParent().AddChild(_track);
        _vanishTimer.Start(30);
        CanvasManager.Menus.GoBack();
        SoundManager.CreateSound(current_aphid.AudioDynamic_Idle);
    }

    private void DisplayAlbum(bool _state, string _path = "")
    {
        is_album_open = _state;
        if (_state)
        {
            var _files = DirAccess.GetFilesAt(_path);
            for (int i = 0; i < _files.Length; i++)
            {
                var _photo = Image.LoadFromFile(_path + _files[i]);
                Control _slot = (ResourceLoader.Load(PHOTO_SLOT_SCENE) as PackedScene).Instantiate() as Control;
                (_slot.GetChild(0) as TextureRect).Texture = ImageTexture.CreateFromImage(_photo);
                photoContainer.AddChild(_slot);
            }
            animPlayer.Play("open_album");
        }
        else
        {
            for (int i = 0; i < photoContainer.GetChildCount(); i++)
                photoContainer.GetChild(i).QueueFree();
            animPlayer.Play("close_album");
        }
    }
}