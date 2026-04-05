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
	[Export] private TextureButton screenshot_button;
	[Export] private Container prompt_grid;
	[Export] private PackedScene prompt_element;
	[ExportGroup("Weather")]
	[Export] private AnimationPlayer weather_player;
	[Export] private Label weather_text;
	[Export] private Texture2D[] weather_sprites;
	[Export] private TextureRect weather_bg;
    [Export] private Color[] weather_popup_colors =
    [
        new(0.22f, 0.608f, 0.898f), // Morning
			new(0.984f, 0.796f, 0.039f), // Noon
			new(0.987f, 0.371f, 0), // Afternoon
			new(0.8f, 0.1f, 0.8f), // Sunset
			new(0.435f, 0.33f, 0.823f) // Night
	];

	private Timer vanish_screenshot_timer;

	public enum ControlPrompt
	{
		None = -1,
		Interact,
		OpenMenu,
		TalkToNPC,
		PetAphid,
		HarvestAphid,
		ShowInfo,
		CloseInfo,
		PickupItem,
		DropItem,
		SellBuilding,
		StoreBuilding,
		AlignToGridBuilding
	}
	private struct ControlPromptData(string ID, string TranslationKey, string ActionKey)
	{
		// The identifier for this control prompt, use the same one to override others when needed.
		public string ID = ID;
		/// <summary>
		/// <summary>
		/// The key to the translated text to show.
		/// </summary>
		public string TranslationKey = TranslationKey;
		/// The keybind that this control prompt shows.
		/// </summary>
		public string ActionKey = ActionKey;
	}
	private record ControlPromptRuntimeData(Control Node, int Priority)
	{
		public Control Node = Node;
		public int Priority = Priority;
	}
	private readonly Dictionary<ControlPrompt, ControlPromptData> available_prompts = new()
	{
		{ ControlPrompt.Interact, new("interact", "interact", InputNames.Interact ) },
		{ ControlPrompt.OpenMenu, new("interact", "open_menu", InputNames.Interact ) },
		{ ControlPrompt.TalkToNPC, new("interact", "talk", InputNames.Interact ) },
		{ ControlPrompt.PetAphid, new("interact", "pet", InputNames.Interact ) },
		{ ControlPrompt.HarvestAphid, new("interact", "harvest", InputNames.Interact ) },
		{ ControlPrompt.ShowInfo, new("info", "show_info", InputNames.ShowInfo ) },
		{ ControlPrompt.CloseInfo, new("info", "close_info", InputNames.ShowInfo ) },
		{ ControlPrompt.PickupItem, new("pickup", "pickup", InputNames.Pickup ) },
		{ ControlPrompt.DropItem, new("pickup", "drop", InputNames.Pickup ) },
		{ ControlPrompt.SellBuilding, new("sell", "sell", InputNames.Sell ) },
		{ ControlPrompt.StoreBuilding, new("store", "store", InputNames.Store ) },
		{ ControlPrompt.AlignToGridBuilding, new("align_to_grid", "align_to_grid", InputNames.AlignToGrid ) },
	};
	private readonly Dictionary<string, ControlPromptRuntimeData> prompt_list = [];

	public override void _EnterTree()
	{
		Instance = this;
		screenshot_button.Pressed += TakeScreenshot;

		// events
		SceneManager.AddEventListener((_) => UpdateCurrency(), SceneManager.EventEnum.OnPostLoad);
		RoomInstance.Instance.AddEventListener(StartWeatherPopup, RoomInstance.TimeEvents.OnHourChange);
	}
	public override void _ExitTree()
	{
		Menus = new();
		Instance = null;
	}
	
	public static async Task INSTANTIATE_CANVAS()
	{
        CanvasLayer _canvas = (await GlobalManager.PRELOAD_RESOURCE(GlobalManager.CANVAS_PREFAB, true) as PackedScene).Instantiate() as CanvasLayer;
        GlobalManager.Instance.GetTree().CurrentScene.AddChild(_canvas);
	}

	public override void _Input(InputEvent @event)
	{
		if (GlobalManager.IsBusy || CutsceneManager.IsActive)
			return;

		if (@event.IsActionPressed(InputNames.TakeScreenshot))
			TakeScreenshot();

		if (Menus.IsActive && (@event.IsActionPressed(InputNames.Cancel) || @event.IsActionPressed(InputNames.Escape)))
		{
			_ = Menus.GoBack();
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
			FreeCameraManager.SetHUDTo(false, true);
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
			DebugLogger.Print(DebugLogger.LogPriority.Warning, _err, "Failed to take screenshot");
			SoundManager.CreateSound("ui/button-fail");
		}

		if (_is_free_camera)
		{
			FreeCameraManager.SetHUDTo(true, true);
			AphidInfo.Instance.Show();
		}
		Instance.photo_display.Show();
		Instance.Show();
	}
	public static void SetHUDTo(bool _state)
	{
		if (_state == Instance.hud_element.Visible)
			return;

		Instance.hud_element.Visible = _state;
	}
	public static void UpdateCurrency()
	{
		if (Instance == null)
		{
			GD.PrintErr("CanvasManager is null");
			return;
		}

		if (Player.Data.Currency >= 10000)
			Instance.currency_text.Text = (Player.Data.Currency / 1000).ToString("00K");
		else
			Instance.currency_text.Text = Player.Data.Currency.ToString("000");
	}

	// opens weather overlay and creates timer to hide it automatically
	public void StartWeatherPopup(RoomInstance.TimeArgs _args)
	{
		OpenWeather(RoomInstance.TimeArgs.TimeOfDay);
		Timer _timer = new()
		{
			OneShot = true
		};
		_timer.Timeout += () =>
		{
			CloseWeather();
			_timer.QueueFree();
		};
		AddChild(_timer);
		_timer.Start(5);
	}
	public void OpenWeather(RoomInstance.DayHourMode _hour)
	{
		weather_bg.SelfModulate = weather_popup_colors[(int)_hour];
		if (_hour == RoomInstance.DayHourMode.Night)
			weather_bg.Texture = weather_sprites[1];
		else
			weather_bg.Texture = weather_sprites[0];

		var _date = Time.GetDatetimeDictFromSystem();
		weather_text.Text = ((int)_date["hour"]).ToString("00") + ":" + ((int)_date["minute"]).ToString("00");
		weather_player.Play(StringNames.OpenAnim);
	}
	public void CloseWeather()
	{
		if (weather_bg.Visible)
			weather_player.Play(StringNames.CloseAnim);
	}

	/// <summary>
	/// Adds a control prompt for possible interactions.
	/// </summary>
	public static void AddControlPrompt(ControlPrompt _prompt, int _priority = 0)
	{
		if (Instance == null)
			return;
		ControlPromptData _promptData = Instance.available_prompts[_prompt];
		
		if (Instance.prompt_list.TryGetValue(_promptData.ID, out ControlPromptRuntimeData value))
		{
			if (value.Priority >= _priority)
				return;
			else
			{
				value.Node.QueueFree();
				Instance.prompt_list.Remove(_promptData.ID);
			}
		}

		Instance.prompt_list.Add(_promptData.ID, new(Instance.CreateControlPromptNode(_promptData.TranslationKey, _promptData.ActionKey), _priority));
	}
	public static bool HasControlPrompt(string _id) => Instance.prompt_list.ContainsKey(_id);
	private Control CreateControlPromptNode(string _trKey, string _actionKey)
	{
		Control _node = prompt_element.Instantiate<Control>();
		_node.Modulate = new(1, 1, 1, 0);
		_node.GetChild<RichTextLabel>(0).Text = ControlsManager.GetLocalizedActionName(_actionKey);
		_node.GetChild<RichTextLabel>(1).Text = "prompt_" + _trKey;
		Tween tween = _node.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Linear);
		tween.TweenProperty(_node, "modulate", new Color(1, 1, 1, 1), 0.2f);
		prompt_grid.AddChild(_node);
		return _node;
	}
	/// <summary>
	/// Removes a prompt from the interface. The enum may differ but most prompts share a common ID, meaning selecting any alts will remove that ID from the list.
	/// </summary>
	/// <param name="_prompt">The prompt to remove, check the common ID in available_prompts to see which you can use.</param>
	public static void RemoveControlPrompt(ControlPrompt _prompt)
	{
		if (Instance == null)
			return;
		ControlPromptData _promptData = Instance.available_prompts[_prompt];
		if (!Instance.prompt_list.TryGetValue(_promptData.ID, out ControlPromptRuntimeData value))
			return;
		value.Node.QueueFree();
		Instance.prompt_list.Remove(_promptData.ID);
	}
	public static void ClearControlPrompts()
	{
		if (Instance == null)
			return;
		foreach (var _pair in Instance.prompt_list)
			_pair.Value.Node.QueueFree();

		Instance.prompt_list.Clear();
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
					SoundManager.CreateSound("aphid/idle").Bus = "EchoUI";
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