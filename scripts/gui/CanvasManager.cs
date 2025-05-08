using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CanvasManager : CanvasLayer
{
	[Export] private Control hud_element;
	[Export] private TextureRect photo_display;
	[Export] private AnimationPlayer photo_anim_player;
	[Export] private Label currency_text;
	[Export] private TextureButton screenshot_button, menu_button;
	[Export] private Container prompt_grid;
	[Export] private PackedScene prompt_element;
	[ExportGroup("Weather")]
	[Export] private AnimationPlayer weather_player;
	[Export] private Label weather_text;
	[Export] private Texture2D[] weather_sprites;
	[Export] private TextureRect weather_bg;

	public readonly Dictionary<string, Control> PromptList = [];

	public static CanvasManager Instance { get; private set; }
	public static MenuUtil Menus { get; private set; } = new();

	private Timer vanish_screenshot_timer;

	public override void _EnterTree()
	{
		Instance = this;
		Menus = new();
		screenshot_button.Pressed += TakeScreenshot;
		menu_button.Pressed += () => PauseMenu.Instance.SetPauseMenu(true);

		Menus.OnSwitch += (_, _currentMenu) =>
		{
			if (_currentMenu?.Name == "pause")
				return;

			if (Menus.IsBusy)
			{
				SetHudElements(false);
				ClearControlPrompts();
			}
			// dont set them back in if we are in free camera mode
			else if (!IsInstanceValid(FreeCameraManager.Instance) || !FreeCameraManager.Enabled)
				SetHudElements(true);
		};
	}
    public override void _ExitTree()
    {
       	Instance = null;
		Menus = new();
    }

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed(InputNames.TakeScreenshot))
			TakeScreenshot();

		if (Menus.IsBusy && (@event.IsActionPressed(InputNames.Cancel) || @event.IsActionPressed(InputNames.Escape)))
		{
			Menus.GoBack();
			GetViewport().SetInputAsHandled();
		}
	}

	public static async void TakeScreenshot()
	{
		bool _is_free_camera = IsInstanceValid(FreeCameraManager.Instance) && FreeCameraManager.Enabled;

		Instance.Hide();
		Instance.photo_display.Hide();
		if (_is_free_camera)
		{
			FreeCameraManager.SetFreeCameraHud(false, true);
			AphidInfo.Instance.Hide();
		}

		await Task.Delay(1);
		try
		{
			Image _capture = Instance.GetViewport().GetTexture().GetImage();

			string _filename = SaveSystem.ProfilePath + SaveSystem.ProfileAlbumDir;
			if (_is_free_camera && IsInstanceValid(CameraManager.FocusedAphid))
			{
				_filename += $"/{CameraManager.FocusedAphid.Instance.ID}/";
				if (!DirAccess.DirExistsAbsolute(_filename))
					DirAccess.MakeDirAbsolute(_filename);
			}
			_filename += $"screenshot-{Time.GetDatetimeStringFromSystem().Replace(":", "-")}.png";
			if (FileAccess.FileExists(_filename))
				_filename.Replace(".png", Time.GetTicksMsec() + ".png");

			_capture.SavePng(_filename);
			SoundManager.CreateSound("ui/camera-flash");
			Instance.photo_display.Texture = ImageTexture.CreateFromImage(_capture);
			Instance.photo_anim_player.Play("popup");

			if (Instance.vanish_screenshot_timer != null)
			{
				Instance.vanish_screenshot_timer.Stop();
				Instance.vanish_screenshot_timer.QueueFree();
			}
			Instance.vanish_screenshot_timer = new()
			{
				OneShot = true,
			};
			Instance.vanish_screenshot_timer.Timeout += () => Instance.photo_anim_player.Play("vanish");
			Instance.AddChild(Instance.vanish_screenshot_timer);
			Instance.vanish_screenshot_timer.Start(3);
		}
		catch (Exception _err)
		{
			Logger.Print(Logger.LogPriority.Warning, _err, "Failed to take screenshot");
			SoundManager.CreateSound("ui/button-fail");
		}

		if (_is_free_camera)
		{
			FreeCameraManager.SetFreeCameraHud(true, true);
			AphidInfo.Instance.Show();
		}
		Instance.photo_display.Show();
		Instance.Show();
	}
	public static void SetHudElements(bool _state)
	{
		if (_state == Instance.hud_element.Visible)
			return;

		if (_state)
			Instance.hud_element.Show();
		else
			Instance.hud_element.Hide();
	}
	public static void UpdateCurrency()
	{
		if (Instance == null)
			return;

		if (Player.Data.Currency >= 10000)
			Instance.currency_text.Text = (Player.Data.Currency / 1000).ToString("00K");
		else
			Instance.currency_text.Text = Player.Data.Currency.ToString("000");
	}
		
	/// <summary>
	/// Adds a control prompt ui element to point out interactables nearby and possible interactions.
	/// </summary>
	/// <param name="_tr_key">A translation key for the text that indicates what the respective input action does.</param>
	/// 	/// <param name="_id">The id of this prompt, used to remove this component if provided in RemoveControlPrompt.</param>
	/// <param name="_action_key">A key to indicate which input action should show as.</param>
	public static void AddControlPrompt(string _tr_key, string _id, string _action_key)
	{
		if (Instance == null || Instance.PromptList.ContainsKey(_id))
			return;

		Control _node = Instance.prompt_element.Instantiate<Control>();
		_node.Modulate = new(1, 1, 1, 0);
		(_node.GetChild(0) as RichTextLabel).Text = ControlsManager.GetActionName(_action_key);
		(_node.GetChild(1) as RichTextLabel).Text = "prompt_" + _tr_key;
		Tween tween = _node.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Linear);
		tween.TweenProperty(_node, "modulate", new Color(1, 1, 1, 1), 0.2f);
		Instance.prompt_grid.AddChild(_node);
		Instance.PromptList.Add(_id, _node);
	}
	public static bool HasControlPrompt(string _id) => Instance.PromptList.ContainsKey(_id);
	public static void RemoveControlPrompt(string _id)
	{
		if (Instance == null || !Instance.PromptList.TryGetValue(_id, out Control value))
			return;
        value.QueueFree();
		Instance.PromptList.Remove(_id);
	}
	public static void ClearControlPrompts()
	{
		if (Instance == null)
			return;
		foreach (var _pair in Instance.PromptList)
			_pair.Value.QueueFree();

		Instance.PromptList.Clear();
	}

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
}