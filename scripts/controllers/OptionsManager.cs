using Godot;

/// <summary>
/// Main manager for option configuration, save and load.
/// This class holds to all saved video, audio, input and language configurations.
/// </summary>
public static class OptionsManager
{
    public static Data Settings = new("settings"){
        Extension = ".cfg",
        RelativePath = "config/",
        Data = new()
    };
    public class Data : SaveSystem.SaveData<Savefile>
    {
        public Data(string ID) : base(ID) { }

        public override Savefile Load(bool loadToClass = true)
        {
            Savefile _save = base.Load(loadToClass);
            if (Data.ResetBinds)
                ControlsManager.ResetToDefault();

            AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb(Data.VolumeMusic));
            AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb(Data.VolumeSound));
            DisplayServer.WindowSetMode((DisplayServer.WindowMode)Data.WindowMode);
            TranslationServer.SetLocale(Data.Language == 1 ? "es_ES" : "en_US");
            return _save;
        }

        public override Savefile GetDefault()
        {
            return new();
        }
    }
    public class Savefile
    {
        public string LastPlayedResort { get; set; }

        public float VolumeMusic { get; set; }
        public float VolumeSound { get; set; }
        public int WindowMode { get; set; }
        public int Language { get; set; }
        public bool ResetBinds { get; set; }

        public Savefile()
        {
            VolumeMusic = 0.5f;
            VolumeSound = 0.5f;
            WindowMode = (int)DisplayServer.WindowMode.Maximized;
            Language = 0;
            ResetBinds = false;
        }
    }
}
