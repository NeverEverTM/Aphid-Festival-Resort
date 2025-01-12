using Godot;
using System;
using System.Collections.Generic;

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
    [Export] private PackedScene aphidSlot;
    [Export] private Container aphidContainer;

    private Guid currentKey;
    private Aphid currentAphid;
    private TextureRect currentTracker;
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
        Menu = new("generations", animPlayer, () =>
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
            if (IsInstanceValid(currentTracker))
                currentTracker.QueueFree();

            if (iconNode.GetChildCount() > 0)
                iconNode.GetChild(0).QueueFree();
            currentKey = Guid.Empty;
            currentTracker = null;
            currentAphid = null;
        }, null, false);
    }
    public override void _Process(double delta)
    {
        if (IsInstanceValid(currentTracker))
        {
            Vector2 _aphidPos = GlobalManager.Utils.GetWorldToCanvasPosition(currentAphid.GlobalPosition);

            // if is on screen, point at it
            if (_aphidPos.X > 0 && _aphidPos.X < GlobalManager.ScreenSize.X
                    && _aphidPos.Y > 0 && _aphidPos.Y < GlobalManager.ScreenSize.Y)
                currentTracker.GlobalPosition = _aphidPos + new Vector2(-16, -36);
            else // if is far away, point at its direction
                currentTracker.GlobalPosition = GlobalManager.ScreenCenter + GlobalManager.ScreenCenter.DirectionTo(_aphidPos) * 100;
        }
    }

    private void GenerateAphidSlot(Guid _key, AphidData.Genes _value, bool _isAdult = true, bool _current = false, bool _AsIcon = false)
    {
        TextureButton _slot = aphidSlot.Instantiate() as TextureButton;
        Control _skin = _slot.GetChild(0) as Control;
        if (_AsIcon)
        {
            if (iconNode.GetChildCount() > 0)
                iconNode.GetChild(0).QueueFree();
            iconNode.AddChild(_slot);
            _slot.Disabled = true;
        }
        else
        {
            aphidContainer.AddChild(_slot);
            _slot.Pressed += () => SetAphidInfo(_key, _isAdult, _current);
        }

        if (!_current) // For the now dead
            _slot.SelfModulate = new Color("gold");

        // =====| Set Skin |======
        TextureRect[] _pieces = new TextureRect[]{
            _skin.GetChild(0) as TextureRect,
            _skin.GetChild(1) as TextureRect,
            _skin.GetChild(2) as TextureRect,
            _skin.GetChild(3) as TextureRect,
            _skin.GetChild(4) as TextureRect

        };
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
        if (_current && currentKey == _key)
        {
            TrackAphid(_key);
            return;
        }
        currentKey = _key;

        // Set Aphid Info
        AphidData.Genes _genes = _current ? GameManager.Aphids[_key].Genes : Data.Archive[_key];
        GenerateAphidSlot(_key, _genes, _isAdult, _current, true);
        string _parents = _genes.Mother;

        if (_genes.Mother != _genes.Father)
            _parents += $" & {_genes.Father}";

        descriptionLabel.Text = $"[color='cyan']{_genes.Name}[/color]\n" +
        $"[color='black']{Tr("generations_owner")}[/color] {_genes.Owner}\n" +
        $"[color='black']{Tr("generations_parent")}[/color] {_parents}";
    }
    private void TrackAphid(Guid _key)
    {
        currentAphid = GameManager.Aphids[_key].Entity;
        SoundManager.CreateSound(currentAphid.AudioDynamic_Idle);

        // create tracker
        TextureRect _track = new()
        {
            Texture = GlobalManager.GetIcon(currentAphid.Instance.Status.IsAdult ?
                    "aphid_adult" : "aphid_child"),
            Scale = new(2, 2),
            ZIndex = -1000
        };
        if (IsInstanceValid(currentTracker))
            currentTracker.QueueFree();
        currentTracker = _track;

        Timer _vanish = new();
        _vanish.Timeout += () => _track.QueueFree();
        _track.AddChild(_vanish);

        CanvasManager.Instance.AddChild(_track);
        _vanish.Start(10);

        // icon should vanish as the timer hits zero
        // icon should vanish when approaching targeted aphid
        // able to deselect from aphid to aphid
    }
}