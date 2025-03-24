using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class OptionsMenu : Control
{
	[Export] private Slider masterSlider, musicSlider, soundSlider, ambienceSlider, uiSlider;
	[Export] private TextureButton saveFolderButton;
	[Export] private OptionButton windowMode, language;
	[Export] private BaseButton autoRun;

	public readonly Dictionary<string, int> locales = new()
	{
		{ "en_US" , 0 },
		{ "es_ES", 1  }
	};

	public readonly Dictionary<int, DisplayServer.WindowMode> display = new()
	{
		{ 0, DisplayServer.WindowMode.Maximized },
		{ 1, DisplayServer.WindowMode.ExclusiveFullscreen }
	};

	/* To create a setting you must:
	- Create a new serializeable variable in the Savefile config
	- Create a menu element that allows the user to modify it (aka Slider, Select, Check)
	- Create a function that sets the value both in the savefile and in-game
	*/
	public override void _Ready()
	{
		masterSlider.ValueChanged += OnMasterSlider;
		musicSlider.ValueChanged += OnMusicSlider;
		soundSlider.ValueChanged += OnSoundSlider;
		ambienceSlider.ValueChanged += OnAmbienceSlider;
		uiSlider.ValueChanged += OnUISlider;

		windowMode.ItemSelected += OnDisplaySelect;
		language.ItemSelected += OnLocaleSelect;
		autoRun.Toggled += OnAutoRunToggle;
		
		VisibilityChanged += () =>
		{
			if (!Visible)
				OptionsManager.Module.Save();
			else
			{
				masterSlider.GrabFocus();
				masterSlider.Value = OptionsManager.Settings.VolumeMaster;
				musicSlider.Value = OptionsManager.Settings.VolumeMusic;
				soundSlider.Value = OptionsManager.Settings.VolumeSound;
				ambienceSlider.Value = OptionsManager.Settings.VolumeAmbience;
				uiSlider.Value = OptionsManager.Settings.VolumeUI;

				foreach (KeyValuePair<int, DisplayServer.WindowMode> _pair in display)
				{
					if (_pair.Value.Equals(OptionsManager.Settings.DisplayMode))
					{
						windowMode.Select(_pair.Key);
						break;
					}
				}
				
				language.Select(locales[OptionsManager.Settings.Locale]);
				autoRun.SetPressedNoSignal(OptionsManager.Settings.SettingAutoRun);
			}
		};

		saveFolderButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath("user://"));
	}

	private void OnMasterSlider(double value)
	{
		AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb((float)value));
		OptionsManager.Settings.VolumeMaster = (float)value;
	}
	private void OnMusicSlider(double value)
	{
		AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb((float)value));
		OptionsManager.Settings.VolumeMusic = (float)value;
	}
	private void OnSoundSlider(double value)
	{
		AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb((float)value));
		OptionsManager.Settings.VolumeSound = (float)value;
		SoundManager.CreateSound(Aphid.Audio_Step, false, "Sounds");
	}
	private void OnAmbienceSlider(double value)
	{
		AudioServer.SetBusVolumeDb(3, Mathf.LinearToDb((float)value));
		OptionsManager.Settings.VolumeAmbience = (float)value;
		SoundManager.CreateSound(Aphid.Audio_Step, false, "Ambience");
	}
	private void OnUISlider(double value)
	{
		AudioServer.SetBusVolumeDb(4, Mathf.LinearToDb((float)value));
		OptionsManager.Settings.VolumeUI = (float)value;
		SoundManager.CreateSound(Aphid.Audio_Step);
	}
	
	private void OnDisplaySelect(long _index) 
	{
		DisplayServer.WindowSetMode(display[(int)_index]);
		OptionsManager.Settings.DisplayMode = display[(int)_index];
	}
	private void OnAutoRunToggle(bool _toggledOn)
	{
		OptionsManager.Settings.SettingAutoRun = _toggledOn;
		SoundManager.CreateSound("ui/lock");
	}
	private void OnLocaleSelect(long _index)
	{
		string _locale = locales.Keys.ToList()[(int)_index];
		TranslationServer.SetLocale(_locale);
		OptionsManager.Settings.Locale = _locale;
	}
}
