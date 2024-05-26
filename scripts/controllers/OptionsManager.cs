using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public partial class OptionsManager : Node, SaveSystem.ISaveData
{
    public string GetId() => "options_config";
    public static Savefile Data;

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

        public Savefile()
        {
            VolumeMusic = 0.5f;
            VolumeSound = 0.5f;
            WindowMode = (int)DisplayServer.WindowMode.Maximized;
            Language = 0;
        }
    }

    string SaveSystem.ISaveData.SaveData()
    {
        return JsonSerializer.Serialize(Data);
    }

    public Task LoadData(string _json)
    {
        Data = JsonSerializer.Deserialize<Savefile>(_json);

        AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb(Data.VolumeMusic));
        AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb(Data.VolumeSound));
        DisplayServer.WindowSetMode((DisplayServer.WindowMode)Data.WindowMode);
        TranslationServer.SetLocale(Data.Language == 1 ? "es_ES" : "en_US");

        return Task.CompletedTask;
    }
    public Task SetData()
	{
		Data = new();
		return Task.CompletedTask;
	}
}
