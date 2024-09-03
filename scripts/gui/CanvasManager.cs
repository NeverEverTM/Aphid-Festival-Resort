using System;
using System.Threading.Tasks;
using Godot;

public partial class CanvasManager : CanvasLayer
{
	[Export] private Label currency_text, weather_text;
	[Export] private Texture2D[] weather_sprites;
	[Export] private TextureRect weather_bg;
	[Export] private AnimationPlayer weather_player;
	[Export] private BaseButton screenshot_button, menu_button;

	public static CanvasManager Instance { get; private set; }
	public static MenuUtil Menus { get; private set; }

	public static AudioStream Audio_SellSound { get; set; }
	public static AudioStream Audio_StoreSound { get; set; }

	// Focus
	public Control CurrentFocus { get; private set; }
	public bool IsInFocus { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
		Menus = new();
		screenshot_button.Pressed += TakeScreenshot;
		menu_button.Pressed += () => PauseMenu.Instance.SetPauseMenu(true);
		UpdateCurrency();
	}
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("take_screenshot"))
			TakeScreenshot();

		if (Input.IsActionJustPressed("cancel") || Input.IsActionJustPressed("escape"))
		{
			// Go back in a menu if pause menu itself isnt open
			if (!GetTree().Paused)
				Menus.GoBackInMenu();
		}
	}

	public static async void TakeScreenshot()
	{
		Instance.Hide();
		await Task.Delay(1);
		try
		{
			var capture = Instance.GetViewport().GetTexture().GetImage();
			var _time = Time.GetDatetimeStringFromSystem().Replace(":", "-");
			var filename = SaveSystem.ProfilePath + SaveSystem.screenshotsFolder + $"/Screenshot-{_time}.png";

			if (FileAccess.FileExists(filename))
				filename += "(Extra)";	

			capture.SavePng(filename);
			SoundManager.CreateSound(ResourceLoader.Load<AudioStream>(GameManager.SFXPath + "/ui/camera-flash.wav"));
		}
		catch (Exception _err)
		{
			Logger.Print(Logger.LogPriority.Warning, _err, "Failed to take screenshot");
		}

		Instance.Show();
	}

	public static void SetCurrencyElement(bool _state)
	{
		if (_state)
			Instance.currency_text.GetParent<Control>().Show();
		else
			Instance.currency_text.GetParent<Control>().Hide();
	}
	public static void UpdateCurrency() =>
		Instance.currency_text.Text = Player.Data.Currency.ToString("000");

	public static void OpenWeather(Color _color)
	{
		Instance.weather_bg.SelfModulate = _color;
		if (FieldManager.TimeOfDay == FieldManager.DayHours.Night)
			Instance.weather_bg.Texture = Instance.weather_sprites[1];
		else
			Instance.weather_bg.Texture = Instance.weather_sprites[0];

		var _date = Time.GetDatetimeDictFromSystem();
		Instance.weather_text.Text = ((int)_date["hour"]).ToString("00") + ":" + ((int)_date["minute"]).ToString("00");
		Instance.weather_player.Play("open");
	}
	public static void CloseWeather()
	{
		if (Instance.weather_bg.Visible)
			Instance.weather_player.Play("close");
	}

	// ======| Focus Related (For TextEdit and such) |=========
	public static void SetFocus(Control _focus)
	{
		if (IsInstanceValid(Instance.CurrentFocus))
		{
			Instance.CurrentFocus.ReleaseFocus();
			Instance.CurrentFocus = null;
		}

		if (!_focus.HasFocus())
			_focus.GrabFocus();
		Instance.CurrentFocus = _focus;

		if (!Instance.IsInFocus)
			Instance.IsInFocus = true;
	}
	public static void RemoveFocus()
	{
		if (!Instance.IsInFocus)
			return;

		Instance.CurrentFocus?.ReleaseFocus();
		Instance.CurrentFocus = null;
		Instance.IsInFocus = false;
	}
	public static bool IsFocus(Control _focus) => 
		_focus.Equals(Instance.CurrentFocus);
}