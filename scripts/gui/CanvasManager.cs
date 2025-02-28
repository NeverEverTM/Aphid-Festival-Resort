using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CanvasManager : CanvasLayer
{
	[Export] private Control hud_element;
	[Export] private Label currency_text, weather_text;
	[Export] private Texture2D[] weather_sprites;
	[Export] private TextureRect weather_bg;
	[Export] private AnimationPlayer weather_player;
	[Export] private TextureButton screenshot_button, menu_button, inventoryButton;
	[Export] private Texture2D[] inventory_button_sprites = new Texture2D[2];
	[Export] private RichTextLabel inventory_control_label;
	[Export] private Container prompt_grid;
	[Export] private PackedScene prompt_element;

	private readonly Dictionary<string, Control> prompt_list = new();
	public const string Prompt_Drop = "prompt_drop";
	public const string Prompt_Talk = "prompt_talk";
	public const string Prompt_Pet = "prompt_pet";
	public const string Prompt_Harvest = "prompt_harvest";
	public const string Prompt_Pickup = "prompt_pickup";
	public const string Prompt_Interact = "prompt_interact";

	public static CanvasManager Instance { get; private set; }
	public static MenuUtil Menus { get; private set; }

	public static AudioStream AudioSell { get; set; }
	public static AudioStream AudioStore { get; set; }

	// Focus
	public Control CurrentFocus { get; private set; }
	public bool IsInFocus { get; private set; }
	public override void _EnterTree()
	{
		Instance = this;
		Menus = new();
		screenshot_button.Pressed += TakeScreenshot;
		menu_button.Pressed += () => PauseMenu.Instance.SetPauseMenu(true);
		inventoryButton.Pressed += () => Player.Instance.inventory.SetTo(!Player.Instance.inventory.Visible);
		inventory_control_label.Text = ControlsManager.GetActionName(InputNames.OpenInventory);

		Menus.OnSwitch += (_, _currentMenu) =>
		{
			if (_currentMenu?.ID == "pause")
				return;

			if (Menus.IsBusy)
			{
				SetHudElements(false);
				ClearControlPrompts();
			}
			else
				SetHudElements(true);
		};
	}
    public override void _Input(InputEvent @event)
	{
		inventory_control_label.Text = ControlsManager.GetActionName(InputNames.OpenInventory);

		if (@event.IsActionPressed(InputNames.TakeScreenshot))
			TakeScreenshot();

		if (@event.IsActionPressed("cancel") || @event.IsActionPressed("escape"))
			Menus.GoBackInMenu();
	}

	public static async void TakeScreenshot()
	{
		Instance.Hide();
		await Task.Delay(1);
		try
		{
			var capture = Instance.GetViewport().GetTexture().GetImage();
			var _time = Time.GetDatetimeStringFromSystem().Replace(":", "-");
			var filename = SaveSystem.ProfilePath + SaveSystem.ProfileAlbumDir + $"/Screenshot-{_time}.png";

			if (FileAccess.FileExists(filename))
				filename += "(Extra)";

			capture.SavePng(filename);
			SoundManager.CreateSound("ui/camera-flash");
		}
		catch (Exception _err)
		{
			Logger.Print(Logger.LogPriority.Warning, _err, "Failed to take screenshot");
		}

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
		if (Player.Data.Currency >= 10000)
			Instance.currency_text.Text = (Player.Data.Currency / 1000).ToString("00K");
		else
			Instance.currency_text.Text = Player.Data.Currency.ToString("000");
	}
	public static void UpdateInventoryIcon(bool _state) =>
		Instance.inventoryButton.TextureNormal = Instance.inventory_button_sprites[_state ? 0 : 1];
	/// <summary>
	/// Adds a control prompt ui element to point out interactables nearby and possible interactions.
	/// </summary>
	/// <param name="_tr_key">A translation key for the text that indicates what the respective input action does.</param>
	/// 	/// <param name="_id">The id of this prompt, used to remove this component if provided in RemoveControlPrompt.</param>
	/// <param name="_action_key">A key to indicate which input action should show as.</param>
	public static void AddControlPrompt(string _tr_key, string _id, string _action_key)
	{
		if (Instance.prompt_list.ContainsKey(_id))
			return;

		Control _node = Instance.prompt_element.Instantiate<Control>();
		_node.Modulate = new(1, 1, 1, 0);
		(_node.GetChild(0) as RichTextLabel).Text = ControlsManager.GetActionName(_action_key);
		(_node.GetChild(1) as RichTextLabel).Text = _tr_key;
		Tween tween = _node.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Linear);
		tween.TweenProperty(_node, "modulate", new Color(1, 1, 1, 1), 0.2f);
		Instance.prompt_grid.AddChild(_node);
		Instance.prompt_list.Add(_id, _node);
	}
	public static bool HasControlPrompt(string _id) => Instance.prompt_list.ContainsKey(_id);
	public static void RemoveControlPrompt(string _id)
	{
		if (!Instance.prompt_list.ContainsKey(_id))
			return;

		Instance.prompt_list[_id].QueueFree();
		Instance.prompt_list.Remove(_id);
	}
	public static void ClearControlPrompts()
	{
		foreach(var _pair in Instance.prompt_list)
			_pair.Value.QueueFree();

		Instance.prompt_list.Clear();
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