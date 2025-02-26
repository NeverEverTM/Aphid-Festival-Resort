using Godot;

/// <summary>
/// Main manager for option configuration, save and load.
/// This class holds to all saved video, audio, input and language configurations.
/// </summary>
internal static class OptionsManager
{
    public static Savefile Settings { get; set; }

    internal static SaveSystem.SaveModule<Savefile> Module = new("settings", new DataModule()){
        RelativePath = SaveSystem.CONFIG_DIR,
        Extension = SaveSystem.CONFIGFILE_EXTENSION,
    };
    internal class DataModule : SaveSystem.IDataModule<Savefile>
    {
        public void Set(Savefile _data)
        {
            Settings = _data;
            if (Settings.ResetBinds)
                ControlsManager.ResetToDefault();

            // Volume
            AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Settings.VolumeMaster));
            AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb(Settings.VolumeMusic));
            AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb(Settings.VolumeSound));
            AudioServer.SetBusVolumeDb(3, Mathf.LinearToDb(Settings.VolumeAmbience));
            AudioServer.SetBusVolumeDb(4, Mathf.LinearToDb(Settings.VolumeUI));

            // Display
            DisplayServer.WindowSetMode(Settings.DisplayMode);
            TranslationServer.SetLocale(Settings.Locale);
        }
        public Savefile Get()
        {
            return Settings;
        }
        public Savefile Default() => new();
    }
    public record Savefile
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
            VolumeMaster = 0.6f;
            VolumeMusic = VolumeSound = VolumeUI = VolumeAmbience = 0.4f;
            DisplayMode = DisplayServer.WindowMode.Maximized;
            Locale = "en_US";
            ResetBinds = false;
        }
    }
}
