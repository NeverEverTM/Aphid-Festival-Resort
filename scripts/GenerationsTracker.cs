using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public partial class GenerationsTracker : Control, SaveSystem.ISaveData
{
    public string CLASS_ID => "generations_data";
    public static Savefile Data = new();

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
    [Export] private RichTextLabel descriptionLabel;
    [Export] private PackedScene aphidSlot;
    [Export] private Container aphidContainer;
    private bool done;

    public override void _EnterTree()
    {
        SaveSystem.AddToProfileData(this);
        descriptionLabel.Text = $"[color='black']{Tr("generations_description")}[/color]";
    }
    public override void _Process(double delta)
    {
        if (Visible)
        {
            if (!done)
            {
                done = true;
                descriptionLabel.Text = $"[color='black']{Tr("generations_description")}[/color]";
                SetAphidGenerations();
            }
        }
        else
            done = false;
    }

    private void SetAphidGenerations()
    {
        for (int i = 0; i < aphidContainer.GetChildCount(); i++)
            aphidContainer.GetChild(i).QueueFree();

        // Current Generation
        foreach (var _pair in SaveSystem.AphidsOnResort)
            GenerateAphidSlot(_pair.Key, _pair.Value.Genes, _pair.Value.Status.IsAdult, true);

        // Past Generations
        foreach (var _pair in Data.Generations)
            GenerateAphidSlot(_pair.Key, _pair.Value);
    }
    private void GenerateAphidSlot(Guid _key, AphidData.Genes _value, bool _isAdult = true, bool _current = false)
    {
        TextureButton _slot = aphidSlot.Instantiate() as TextureButton;
        aphidContainer.AddChild(_slot);
        _slot.Pressed += () => SetAphidInfo(_key, _current);

        // =====| Set Skin |======
        string _path = GameManager.SkinsPath + "/";
        // Antenna
        SetSkinPiece(_slot.GetChild(0), _path +
            (_isAdult ? $"{_value.AntennaType}/antenna_idle.png" : $"{_value.AntennaType}/antenna_baby_idle.png"),
            _value.AntennaColor);
        // Body
        SetSkinPiece(_slot.GetChild(2), _path + 
            (_isAdult ? $"{_value.BodyType}/body_idle.png" : $"{_value.BodyType}/body_baby_idle.png"), 
            _value.BodyColor);
        // Legs
        string _pathLegs = _path + (_isAdult ? $"{_value.LegType}/legs_idle.png" : 
        $"{_value.LegType}/legs_baby_idle.png");
        SetSkinPiece(_slot.GetChild(4), _pathLegs, _value.LegColor);
        SetSkinPiece(_slot.GetChild(1), _pathLegs, _value.LegColor);
        // Eyes
        SetSkinPiece(_slot.GetChild(3), _path +
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
        AphidData.Genes _genes = _current ? SaveSystem.AphidsOnResort[_key].Genes : Data.Generations[_key];
        string _parents = Tr("generations_parent");

        if (_genes.Mother == _genes.Father)
            _parents += $" {_genes.Mother}";
        else
            _parents += $" {_genes.Mother} & {_genes.Father}";

        descriptionLabel.Text = $"[color='red']{_genes.Name}[/color]\n" +
        $"[color='black']{Tr("generations_owner")} {_genes.Owner}[/color]\n" +
        $"[color='black']{_parents}[/color]";
    }
}