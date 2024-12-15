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

            // Volume
            AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Data.VolumeMaster));
            AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb(Data.VolumeMusic));
            AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb(Data.VolumeSound));
            AudioServer.SetBusVolumeDb(3, Mathf.LinearToDb(Data.VolumeAmbience));
            AudioServer.SetBusVolumeDb(4, Mathf.LinearToDb(Data.VolumeUI));

            // Display
            DisplayServer.WindowSetMode(Data.DisplayMode);
            TranslationServer.SetLocale(Data.Locale);
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

        public float VolumeMaster { get; set; }
        public float VolumeMusic { get; set; }
        public float VolumeSound { get; set; }
        public float VolumeAmbience { get; set; }
        public float VolumeUI { get; set; }

        public DisplayServer.WindowMode DisplayMode { get; set; }
        public string Locale { get; set; }

        public bool SettingAutoRun { get; set; }
        public bool SettingCameraSmoothing { get; set; }
        public bool ResetBinds { get; set; }

        public Savefile()
        {
            VolumeMaster = VolumeMusic = VolumeSound = VolumeUI = VolumeAmbience = 0.4f;
            DisplayMode = DisplayServer.WindowMode.Maximized;
            Locale = "en_US";
            ResetBinds = false;
        }
    }
}
