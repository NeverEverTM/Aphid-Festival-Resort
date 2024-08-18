using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public partial class GenerationsTracker : Control, SaveSystem.ISaveData
{
    public static Savefile Data = new();
    public static GenerationsTracker Instance;

    public string GetId() => "generations_data";
    public string GetDataPath() => SaveSystem.aphidsFolder;
    public Task LoadData(string _json)
    {
        Data = JsonSerializer.Deserialize<Savefile>(_json);
        if (Data == null)
        {
            GD.PrintErr("Generations are empty.");
            Data = new();
        }
        return Task.CompletedTask;
    }
    public string SaveData()
    {
        return JsonSerializer.Serialize(Data);
    }
    public Task SetData()
    {
        Data = new();
        return Task.CompletedTask;
    }

    public class Savefile
    {
        public Dictionary<Guid, AphidData.Genes> Generations { get; set; }
        public void AddAphid(AphidInstance _instance)
        {
            Generations.Add(new(_instance.ID), _instance.Genes);
        }

        public Savefile()
        {
            Generations = new();
        }
    }

    // Class
    [Export] public AnimationPlayer animPlayer;
    [Export] private RichTextLabel descriptionLabel;
    [Export] private PackedScene aphidSlot;
    [Export] private Container aphidContainer;

    private Guid currentKey;
    private Aphid currentAphid;
    private TextureRect currentTracker;
    public MenuUtil.MenuInstance Menu { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
        SaveSystem.AddToProfileData(this);
        descriptionLabel.Text = $"[color='black']{Tr("generations_description")}[/color]";
        Menu = new("generations", Instance.animPlayer, () =>
        {
            Instance.descriptionLabel.Text = $"[color='black']{Instance.Tr("generations_description")}[/color]";
            Instance.SetAphidGenerations();
            if (IsInstanceValid(Instance.currentTracker))
                Instance.currentTracker.QueueFree();
            Instance.currentKey = Guid.Empty;
            Instance.currentTracker = null;
            Instance.currentAphid = null;
        }, null, false);
    }

    public override void _Process(double delta)
    {
        if (IsInstanceValid(currentTracker))
        {
            Vector2 _aphidPos = GameManager.Utils.GetWorldToCanvasPosition(currentAphid.GlobalPosition);

            // if is on screen, point at it
            if (_aphidPos.X > 0 && _aphidPos.X < GameManager.ScreenSize.X 
                    && _aphidPos.Y > 0 && _aphidPos.Y < GameManager.ScreenSize.Y)
                currentTracker.GlobalPosition = _aphidPos + new Vector2(-16, -36);
            else // if is far away, point at its direction
                currentTracker.GlobalPosition = GameManager.ScreenCenter + GameManager.ScreenCenter.DirectionTo(_aphidPos) * 100;
        }
    
        if (Visible && Input.IsActionJustPressed("open_generations"))
            CanvasManager.Menus.GoBackInMenu();
    }

    private void SetAphidGenerations()
    {
        for (int i = 0; i < aphidContainer.GetChildCount(); i++)
            aphidContainer.GetChild(i).QueueFree();

        // Current Generation
        foreach (var _pair in SaveSystem.Aphids)
            GenerateAphidSlot(_pair.Key, _pair.Value.Genes, _pair.Value.Status.IsAdult, true);

        // Past Generations
        foreach (var _pair in Data.Generations)
            GenerateAphidSlot(_pair.Key, _pair.Value);
    }
    private void GenerateAphidSlot(Guid _key, AphidData.Genes _value, bool _isAdult = true, bool _current = false)
    {
        TextureButton _slot = aphidSlot.Instantiate() as TextureButton;
        Control _skin = _slot.GetChild(0) as Control;
        aphidContainer.AddChild(_slot);
        _slot.Pressed += () => SetAphidInfo(_key, _current);

        if (!_current) // For the now dead
            _slot.SelfModulate = new Color("gold");

        // =====| Set Skin |======
        string _path = GameManager.SkinsPath + "/";
        // Antenna
        SetSkinPiece(_skin.GetChild(0), _path +
            (_isAdult ? $"{_value.AntennaType}/antenna_idle.png" : $"{_value.AntennaType}/antenna_baby_idle.png"),
            _value.AntennaColor);
        // Body
        SetSkinPiece(_skin.GetChild(2), _path +
            (_isAdult ? $"{_value.BodyType}/body_idle.png" : $"{_value.BodyType}/body_baby_idle.png"),
            _value.BodyColor);
        // Legs
        string _pathLegs = _path + (_isAdult ? $"{_value.LegType}/legs_idle.png" :
        $"{_value.LegType}/legs_baby_idle.png");
        SetSkinPiece(_skin.GetChild(4), _pathLegs, _value.LegColor);
        SetSkinPiece(_skin.GetChild(1), _pathLegs, _value.LegColor);
        // Eyes
        SetSkinPiece(_skin.GetChild(3), _path +
            (_isAdult ? $"{_value.EyeType}/eyes_idle.png" : $"{_value.EyeType}/eyes_baby_idle.png"),
            _value.EyeColor);
    }
    private static void SetSkinPiece(Node _node, string _path, Color _color)
    {
        TextureRect _piece = _node as TextureRect;
        _piece.Texture = ResourceLoader.Load<Resource>(_path) as Texture2D;
        _piece.SelfModulate = _color;
    }
    private void SetAphidInfo(Guid _key, bool _current = false)
    {
        // Track aphid if is alive
        if (_current && currentKey == _key)
        {
            TrackAphid(_key);
            return;
        }
        currentKey = _key;
        //SoundManager.CreateSound();
        
        // Set Aphid Info
        AphidData.Genes _genes = _current ? SaveSystem.Aphids[_key].Genes : Data.Generations[_key];
        string _parents = Tr("generations_parent");

        if (_genes.Mother == _genes.Father)
            _parents += $" {_genes.Mother}";
        else
            _parents += $" {_genes.Mother} & {_genes.Father}";

        descriptionLabel.Text = $"[color='red']{_genes.Name}[/color]\n" +
        $"[color='black']{Tr("generations_owner")} {_genes.Owner}[/color]\n" +
        $"[color='black']{_parents}[/color]";
    }
    private void TrackAphid(Guid _key)
    {
        currentAphid = SaveSystem.Aphids[_key].Entity;
        SoundManager.CreateSound(currentAphid.AudioDynamic_Idle);
        
        // create tracker
        TextureRect _track = new()
        {
            Texture = GameManager.GetIcon("aphid_adult"),
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
    }
}