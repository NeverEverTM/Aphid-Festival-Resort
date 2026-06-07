using System.Collections.Generic;
using Godot;

public partial class OptionsMenu : MenuControl
{
	public override string ID => "options";
    public override bool IsASubMenu => true;

	[Export] private BaseButton saveFolderButton;
	[ExportGroup("Sliders")]
	[Export] private Control[] volumeSliders;
	[Export] private Control wavinessSlider;
	[ExportGroup("MultiOption")]
	[Export] private OptionButton windowMode, language;
	[ExportGroup("CheckBox")]
	[Export] private CheckButton[] genericCheckButtons;

	private readonly List<SettingSlider> Sliders = [];
	private readonly List<SettingOptionButton> OptionButtons = [];
	private readonly List<SettingCheckButton> CheckButttons = [];

	public override void _EnterTree()
	{
		saveFolderButton.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath("user://"));
		if (!OptionsManager.SaveModule.Loaded)
			OptionsManager.SaveModule.AddEventListener((_) => InitMenu(), SaveSystem.SaveEventsEnum.OnLoadFinish);
		else
			InitMenu();
	}
	private void InitMenu()
	{
		for (int i = 0; i < volumeSliders.Length; i++)
		{
			var _index = i;
			Sliders.Add(new SliderVolume(volumeSliders[i], _index));
		}

		Sliders.Add(new SliderWaviness(wavinessSlider));

		OptionButtons.Add(new OptionButtonWindowMode(windowMode));
		OptionButtons.Add(new OptionButtonLocale(language));

		for (int i = 0; i < genericCheckButtons.Length; i++)
			CheckButttons.Add(new(genericCheckButtons[i]));
	}

	public class SettingSlider
	{
		public string Name;

		public SettingSlider(Control SliderParent)
		{
			Slider _slider = SliderParent.GetNode<Slider>("slider");
			Name = SliderParent.Name.ToString();

			SliderParent.GetNode<Label>("label").Text = "options_" + Name.ToLower();
			_slider.ValueChanged += (_value) => OnValueChanged((float)_value);
			_slider.Value = OptionsManager.Settings.FloatFlags[Name].Value;
		}

		/// <summary>
		/// Make sure to execute the base method to apply the setting to the config file properly.
		/// </summary>
		public virtual void OnValueChanged(float _value)
		{
			OptionsManager.Settings.FloatFlags[Name].Value = _value;
		}
	}
	public class SliderVolume(Control SliderParent, int BusID) : SettingSlider(SliderParent)
	{
		public int BusID = BusID;

		public override void OnValueChanged(float _value)
		{
			AudioServer.SetBusVolumeDb(BusID, Mathf.LinearToDb(_value));
			if (!GlobalManager.IsBusy)
				SoundManager.CreateSound("player/step", false).Bus = Name;
			OptionsManager.Settings.FloatFlags[Name].Value = _value;
		}
	}
	public class SliderWaviness(Control SliderParent) : SettingSlider(SliderParent)
	{
		public override void OnValueChanged(float _value)
		{
			RenderingServer.GlobalShaderParameterSet("accesibility_menubgwaviness", _value);
			base.OnValueChanged(_value);
		}
	}

	public class SettingOptionButton
	{
		public string Name;

		public SettingOptionButton(OptionButton Parent)
		{
			Name = Parent.Name.ToString();
			Parent.ItemSelected += (_value) => OnItemSelected((int)_value);
			Parent.Select(OptionsManager.Settings.IntFlags[Name].Value);
			OnItemSelected(OptionsManager.Settings.IntFlags[Name].Value);
		}

		/// <summary>
		/// Make sure to execute the base method to apply the setting to the config file properly.
		/// </summary>
		public virtual void OnItemSelected(int _index)
		{
			OptionsManager.Settings.IntFlags[Name].Value = _index;
		}
	}
	public class OptionButtonWindowMode(OptionButton Parent) : SettingOptionButton(Parent)
	{
		protected static readonly DisplayServer.WindowMode[] display =
		{
			DisplayServer.WindowMode.Maximized,
			DisplayServer.WindowMode.ExclusiveFullscreen
		};

		public override void OnItemSelected(int _index)
		{
			DisplayServer.WindowSetMode(display[_index]);
			base.OnItemSelected(_index);
		}
	}
	public class OptionButtonLocale(OptionButton Parent) : SettingOptionButton(Parent)
	{
		protected string[] locales = [
			"en_US", "es_ES"
		];
		public override void OnItemSelected(int _index)
		{
			string _locale = locales[_index];
			TranslationServer.SetLocale(_locale);
			base.OnItemSelected(_index);
		}
	}

	public class SettingCheckButton
	{
		public string Name;

		public SettingCheckButton(CheckButton Parent)
		{
			Name = Parent.Name.ToString();
			Parent.Toggled += OnToggle;
			Parent.ButtonPressed = OptionsManager.Settings.BoolFlags[Name].Value;
		}

		/// <summary>
		/// Make sure to execute the base method to apply the setting to the config file properly.
		/// </summary>
		public virtual void OnToggle(bool _value)
		{
			OptionsManager.Settings.BoolFlags[Name].Value = _value;
			if (!GlobalManager.IsBusy)
				SoundManager.CreateSound("ui/lock");
		}
	}

	// ===| Menu Interface |===
	protected override void Open(MenuInstance _last)
	{
		genericCheckButtons[0].GrabFocus();
	}
	protected override void Close(MenuInstance _next)
	{
		OptionsManager.SaveModule.Save();
	}
}
