using Godot;

public partial class OptionsMenu : Control
{
	[Export] private Slider musicSlider, soundSlider;
	[Export] private TextureButton saveFolderButton;
	[Export] private OptionButton windowMode, language;

	public override void _Ready()
	{
		musicSlider.ValueChanged += OnMusicSlider;
		soundSlider.ValueChanged += OnSoundSlider;
		windowMode.ItemSelected += OnWindowMode;
		language.ItemSelected += OnLanguageSelected;

		OptionsManager.Settings.Data ??= new();

		VisibilityChanged += () =>
		{
			if (!Visible)
				OptionsManager.Settings.Save();
			else
			{
				musicSlider.GrabFocus();
				musicSlider.Value = OptionsManager.Settings.Data.VolumeMusic;
				soundSlider.Value = OptionsManager.Settings.Data.VolumeSound;
				if (OptionsManager.Settings.Data.WindowMode == (int)DisplayServer.WindowMode.ExclusiveFullscreen)
					windowMode.Select(1);
				else
					windowMode.Select(0);
				language.Select(OptionsManager.Settings.Data.Language);
			}
		};

		saveFolderButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath("user://"));
	}

	private void OnMusicSlider(double value)
	{
		AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb((float)value));
		OptionsManager.Settings.Data.VolumeMusic = (float)value;
	}
	private void OnSoundSlider(double value)
	{
		AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb((float)value));
		OptionsManager.Settings.Data.VolumeSound = (float)value;
		SoundManager.CreateSound(Aphid.Audio_Step);
	}
	private void OnWindowMode(long _index)
	{
		if (_index == 0)
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
		else
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
		OptionsManager.Settings.Data.WindowMode = (int)DisplayServer.WindowGetMode();
	}
	private void OnLanguageSelected(long _index)
	{
		if (_index == 1)
			TranslationServer.SetLocale("es_ES");
		else
			TranslationServer.SetLocale("en_US");
		OptionsManager.Settings.Data.Language = (int)_index;
	}
}
