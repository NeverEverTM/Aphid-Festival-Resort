using System.Collections.Generic;
using System.Text.Json;
using Godot;

/// <summary>
/// Main manager for option configuration, save and load.
/// This class holds to all saved video, audio, input and language configurations.
/// </summary>
public static class OptionsManager
{
    internal static Savefile Settings { get; set; }
    internal static OptionsSaveModule SaveModule = new("settings", new OptionsDataModule())
    {
        RootPath = SaveSystem.CONFIG_DIR,
        Extension = SaveSystem.CONFIGFILE_EXTENSION,
        DisposeMode = SaveSystem.SaveMetadata.DisposeMethod.NotApplicable
    };

    internal class OptionsDataModule : SaveSystem.IDataModule<Savefile>
    {
        public void Set(Savefile _data)
        {
            Settings = _data;
        }
        public Savefile Get()
        {
            return Settings;
        }
        public Savefile Default() => new();
    }
    internal class OptionsSaveModule(string ID, SaveSystem.IDataModule<Savefile> _module, int LoadPriority = 0)
         : SaveSystem.SaveModule<Savefile>(ID, _module, LoadPriority)
    {
        public override Savefile PostLoad(string _raw_data)
        {
            if (GameVersion < 301)
            {
                var _oldSavefile = JsonSerializer.Deserialize<Savefile_LEGACY>(_raw_data);
                return _oldSavefile.GetNew();
            }

            return base.PostLoad(_raw_data);
        }
    }

    internal const string DEFAULT_LOCALE = "en_US";
    internal const DisplayServer.WindowMode DEFAULT_DISPLAY_MODE = DisplayServer.WindowMode.Maximized;

    // MARK: Data Types
    public record Savefile
    {
        public Dictionary<string, Setting<float>> FloatFlags { get; set; }
        public Dictionary<string, Setting<int>> IntFlags { get; set; }
        public Dictionary<string, Setting<bool>> BoolFlags { get; set; }

        // Non-customizable
        public string LastPlayedResort { get; set; }

        public Savefile()
        {
            FloatFlags = new()
            {
                { "VolumeMaster", new(0.6f) },
                { "VolumeMusic", new(0.4f) },
                { "VolumeSound", new(0.4f) },
                { "VolumeAmbience", new(0.4f) },
                { "VolumeUI", new(0.4f) },
                { "BackgroundWaviness", new(1.0f) },
            };
            IntFlags = new()
            {
                { "DisplayMode", new(0) },
                { "Locale", new(0) },
            };
            BoolFlags = new()
            {
                { "AutoRun", new(false) },
            };
        }
    }
    public class Setting<T>(T Value)
    {
        public T Value { get; set; } = Value;
        protected T Default { get; set; } = Value;

        public void SetToDefault() => Value = Default;
    }

    public record Savefile_LEGACY
    {
        public float VolumeMaster { get; set; }
        public float VolumeMusic { get; set; }
        public float VolumeSound { get; set; }
        public float VolumeAmbience { get; set; }
        public float VolumeUI { get; set; }

        public DisplayServer.WindowMode DisplayMode { get; set; }
        protected static Dictionary<DisplayServer.WindowMode, int> display = new()
        {
            { DisplayServer.WindowMode.Maximized, 0 },
            { DisplayServer.WindowMode.ExclusiveFullscreen, 1 }
        };
        public string Locale { get; set; }

        public bool SettingAutoRun { get; set; }

        public string LastPlayedResort { get; set; }

        public readonly Dictionary<string, int> locales = new()
        {
            { "en_US" , 0 },
            { "es_ES", 1  }
        };

        public Savefile_LEGACY()
        {
            DisplayMode = DEFAULT_DISPLAY_MODE;
            Locale = DEFAULT_LOCALE;
        }
        public Savefile GetNew()
        {
            var _savefile = new Savefile();
            _savefile.FloatFlags["VolumeMaster"].Value = VolumeMaster;
            _savefile.FloatFlags["VolumeMusic"].Value = VolumeMusic;
            _savefile.FloatFlags["VolumeSound"].Value = VolumeSound;
            _savefile.FloatFlags["VolumeAmbience"].Value = VolumeAmbience;
            _savefile.FloatFlags["VolumeUI"].Value = VolumeUI;

            _savefile.IntFlags["DisplayMode"].Value = display[DisplayMode];
            _savefile.IntFlags["Locale"].Value = locales[Locale];

            _savefile.BoolFlags["AutoRun"].Value = SettingAutoRun;

            _savefile.LastPlayedResort = LastPlayedResort;

            return _savefile;
        }
    }
}
