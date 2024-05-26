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

		OptionsManager.Data ??= new();

		VisibilityChanged += () =>
		{
			if (!Visible)
				SaveSystem.SaveGlobalData();
			else
			{
				musicSlider.GrabFocus();
				musicSlider.Value = OptionsManager.Data.VolumeMusic;
				soundSlider.Value = OptionsManager.Data.VolumeSound;
				if (OptionsManager.Data.WindowMode == (int)DisplayServer.WindowMode.Fullscreen)
					windowMode.Select(1);
				else
					windowMode.Select(0);
				language.Select(OptionsManager.Data.Language);
			}
		};

		saveFolderButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath("user://"));
	}

	public override void _Process(double delta)
	{
		if (saveFolderButton.IsHovered())
			saveFolderButton.SelfModulate = new Color("cyan");
		else
			saveFolderButton.SelfModulate = new Color("white");
	}

	private void OnMusicSlider(double value)
	{
		AudioServer.SetBusVolumeDb(1, Mathf.LinearToDb((float)value));
		OptionsManager.Data.VolumeMusic = (float)value;
	}
	private void OnSoundSlider(double value)
	{
		AudioServer.SetBusVolumeDb(2, Mathf.LinearToDb((float)value));
		OptionsManager.Data.VolumeSound = (float)value;
	}
	private void OnWindowMode(long _index)
	{
		if (_index == 0)
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
		else
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		OptionsManager.Data.WindowMode = (int)DisplayServer.WindowGetMode();
	}
	private void OnLanguageSelected(long _index)
	{
		if (_index == 1)
			TranslationServer.SetLocale("es_ES");
		else
			TranslationServer.SetLocale("en_US");
		OptionsManager.Data.Language = (int)_index;
	}
}
