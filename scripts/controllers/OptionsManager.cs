using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Main manager for option configuration, save and load.
/// This class holds to all saved video, audio, input and language configurations.
/// </summary>
public partial class OptionsManager : Node, SaveSystem.ISaveData
{
    public string GetId() => "options_config";
    public const string ConfigOverridePath = "user://settings.godot";
    public static Savefile Data { get; set; }

    public override void _EnterTree()
    {
        SaveSystem.AddToGlobalData(this);
    }

    public class Savefile
    {
        public string LastPlayedResort { get; set; }

        public float VolumeMusic { get; set; }
        public float VolumeSound { get; set; }
        public int WindowMode { get; set; }
        public int Language { get; set; }
        public Dictionary<string, string> InputBinds { get; set; }
        public bool ResetBinds { get; set; }

        public Savefile()
        {
            VolumeMusic = 0.5f;
            VolumeSound = 0.5f;
            WindowMode = (int)DisplayServer.WindowMode.Maximized;
            Language = 0;
            InputBinds = new();
            ResetBinds = false;
        }
    }

    string SaveSystem.ISaveData.SaveData()
    {
        ProjectSettings.SaveCustom(ConfigOverridePath);
        return JsonSerializer.Serialize(Data);
    }

    public Task LoadData(string _json)
    {
        Data = JsonSerializer.Deserialize<Savefile>(_json);

        if (Data.ResetBinds)
			ControlsManager.ResetToDefault();

        AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb(Data.VolumeMusic));
        AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb(Data.VolumeSound));
        DisplayServer.WindowSetMode((DisplayServer.WindowMode)Data.WindowMode);
        TranslationServer.SetLocale(Data.Language == 1 ? "es_ES" : "en_US");

        return Task.CompletedTask;
    }
    public Task SetData()
    {
        Data = new();
        ControlsManager.ResetToDefault();
        return Task.CompletedTask;
    }
}
