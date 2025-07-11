using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CanvasManager : CanvasLayer
{
	public static CanvasManager Instance { get; private set; }
	public static MenuHandler Menus { get; private set; } = new();
	public const string APHID_SLOT_PREFAB = "uid://d7m5e6tlxyve";

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
	private Timer vanish_screenshot_timer;

	public override void _EnterTree()
	{
		Instance = this;
		screenshot_button.Pressed += TakeScreenshot;
		menu_button.Pressed += () => _ = PauseMenu.Instance.SetPauseMenu(true);
		Menus.OnSwitch.Add(OnSwitchMenu);
	}
	public override void _ExitTree()
	{
		Menus = new();
		Instance = null;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed(InputNames.TakeScreenshot))
			TakeScreenshot();

		if (Menus.IsActive && (@event.IsActionPressed(InputNames.Cancel) || @event.IsActionPressed(InputNames.Escape)))
		{
			Task.Run(Menus.GoBack);
			GetViewport().SetInputAsHandled();
		}
	}
	private void OnSwitchMenu(MenuInstance _lastMenu, MenuInstance _currentMenu)
	{
		if (_currentMenu?.Name == "pause")
			return;

		if (Menus.IsActive)
		{
			SetHudElements(false);
			ClearControlPrompts();
		}
		// dont set them back in if we are in free camera mode
		else if (!IsInstanceValid(FreeCameraManager.Instance) || !FreeCameraManager.Enabled)
			SetHudElements(true);
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

			string _filename = SaveSystem.ProfilePath + SaveSystem.PROFILE_ALBUM_DIR;
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
	/// <param name="_id">The id of this prompt, used to remove this component if provided in RemoveControlPrompt.</param>
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

	/// <summary>
	/// Generates a TextureButton that displays an aphid's current skin.
	/// </summary>
	/// <param name="_key">The GUID key linked to this aphid</param>
	/// <param name="_AsIcon">Special bool that sets this slot as merely cosmetic. OnPressed function will do nothing if true.</param>
	/// <param name="_onPressed">The function to run when pressed. Can be left null. Parameters are: KEY, IS_ADULT and IS_CURRENT_GENERATION.</param>
	/// <returns></returns>
	public static GlowButton CreateAphidSlot(Guid _key, bool _AsIcon, Action<Guid> _onPressed = null)
	{
		GlowButton _slot = (ResourceLoader.Load(APHID_SLOT_PREFAB) as PackedScene).Instantiate() as GlowButton;
		Control _skin = _slot.GetChild(0) as Control;
		bool _isFromCurrentGeneration = false, _isAdult = false;
		AphidData.Genes _genes = null;

		// get relevant values such as current age and generation
		if (GameManager.Aphids.TryGetValue(_key, out AphidInstance _aphid))
		{
			_genes = _aphid.Genes;
			_isFromCurrentGeneration = true;
			_isAdult = _aphid.Status.IsAdult;
		}
		else
		{
			_genes = GameManager.AphidArchive[_key];
			_isFromCurrentGeneration = false;
			_isAdult = true;
		}

		if (_AsIcon)
		{
			_slot.Disabled = true;
			_slot.MouseFilter = Control.MouseFilterEnum.Ignore;
			_slot.FocusMode = Control.FocusModeEnum.None;
			_slot.MouseDefaultCursorShape = Control.CursorShape.Arrow;
			_slot.HoverColor = _slot.PressedColor = new Color("white");
		}
		else
		{
			_slot.Pressed += () =>
			{
				_onPressed?.Invoke(_key);
				if (_isFromCurrentGeneration) // alive aphids
					SoundManager.CreateSound(Aphid.GetIdleAudio(_isAdult)).Bus = "UI";
				else // dead aphids
					SoundManager.CreateSound(Aphid.Audio_Idle).Bus = "EchoUI";
			};
		}

		if (!_isAdult) // baby aphids
			(_slot.GetChild(0) as Control).SetPosition(new(8, 0));

		if (!_isFromCurrentGeneration) // dead aphids
			_slot.SelfModulate = new Color("gold");

		TextureRect[] _pieces = [
			_skin.GetChild(0) as TextureRect,
			_skin.GetChild(1) as TextureRect,
			_skin.GetChild(2) as TextureRect,
			_skin.GetChild(3) as TextureRect,
			_skin.GetChild(4) as TextureRect
		];
		_pieces[0].Texture = AphidSkin.GetSkinPiece(_genes.AntennaType, "antenna", "idle", _isAdult);
		_pieces[0].Modulate = _genes.AntennaColor;
		_pieces[1].Texture = AphidSkin.GetSkinPiece(_genes.LegType, "legs", "idle", _isAdult);
		_pieces[1].Modulate = _genes.LegColor;
		_pieces[2].Texture = AphidSkin.GetSkinPiece(_genes.BodyType, "body", "idle", _isAdult);
		_pieces[2].Modulate = _genes.BodyColor;
		_pieces[3].Texture = AphidSkin.GetSkinPiece(_genes.EyeType, "eyes", "idle", _isAdult);
		_pieces[3].Modulate = _genes.EyeColor;
		_pieces[4].Texture = AphidSkin.GetSkinPiece(_genes.LegType, "legs", "idle", _isAdult);
		_pieces[4].Modulate = _genes.LegColor;

		return _slot;
	}
}