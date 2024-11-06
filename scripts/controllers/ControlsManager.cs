using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Manager in charge of controlling custom input binding.
/// </summary>
public static class ControlsManager
{
	public class Data : SaveSystem.SaveDataGD<Godot.Collections.Dictionary<string, string>>
	{
		public Data(string ID) : base(ID)
		{
			RelativePath = "config/";
			Extension = ".cfg";
			Data = new();
		}
		public Godot.Collections.Dictionary<string, InputEvent> Binds = new();
		public override Task Save()
		{
			Data = new();

			foreach (KeyValuePair<string, InputEvent> _pair in Binds)
			{
				if (_pair.Key.Contains("ui"))
					continue;

				if (_pair.Value is InputEventKey)
					Data.Add(_pair.Key, "InputEventKey" + ":" + ((long)(_pair.Value as InputEventKey).Keycode).ToString());
				else if (_pair.Value is InputEventMouseButton)
					Data.Add(_pair.Key, "InputEventMouseButton" + ":" + ((long)(_pair.Value as InputEventMouseButton).ButtonIndex).ToString());
			}

			return base.Save();
		}
		public override Variant Load(bool loadToClass = true)
		{
			base.Load(loadToClass);

			foreach (KeyValuePair<string, string> _pair in Data)
			{
				string[] _keyValues = _pair.Value.Split(":");
				// dont bind non assigned variables
				if (_keyValues[1] == "0")
					continue;
				if (_keyValues[0] == "InputEventKey")
				{
					InputEventKey _key = new()
					{
						Keycode = (Key)long.Parse(_keyValues[1])
					};
					BindAction(_key, _pair.Key);
				}
				else if (_keyValues[0] == "InputEventMouseButton")
				{
					InputEventMouseButton _key = new()
					{
						ButtonIndex = (MouseButton)long.Parse(_keyValues[1])
					};
					BindAction(_key, _pair.Key);
				}
            }

			return Data;
		}
		public override Variant GetDefault()
		{
			ResetToDefault();
			return Data;
		}
		public override Variant GetVariantData()
		{
			return Data;
		}
		public override void SetVariantData(Variant Data)
		{
			this.Data = (Godot.Collections.Dictionary<string, string>)Data;
		}
	}
	public readonly static Data InputBinds = new("controls");
	/// <summary>
	/// Clean action names for user display.
	/// </summary>
	public static string GetUserReadableText(string _displayAction)
	{
		if (_displayAction.Contains(" (Physical)"))
			_displayAction = _displayAction.Replace(" (Physical)", "");

		if (_displayAction.Equals("Left Mouse Button"))
			_displayAction = $"[img]res://sprites/ui/icon_mouse_left.png[/img]";

		if (_displayAction.Equals("Right Mouse Button"))
			_displayAction = $"[img]res://sprites/ui/icon_mouse_right.png[/img]";

		if (_displayAction.Equals("Middle Mouse Button"))
			_displayAction = $"[img]res://sprites/ui/icon_mouse_middle.png[/img]";

		if (_displayAction.Equals("Mouse Thumb Button 1"))
			_displayAction = $"[img]res://sprites/ui/icon_mouse_extra1.png[/img]";

		if (_displayAction.Equals("Mouse Thumb Button 2"))
			_displayAction = $"[img]res://sprites/ui/icon_mouse_extra2.png[/img]";

		return _displayAction;
	}
	public static string GetUserReadableText(InputEvent _event)
	{
		string _action = _event.AsText();
		return GetUserReadableText(_action);
	}
	public static void ResetToDefault()
	{
		InputMap.LoadFromProjectSettings();

		foreach (string _action in InputMap.GetActions())
		{
			var _events = InputMap.ActionGetEvents(_action);
			if (_events.Count > 0)
				InputBinds.Binds[_action] = _events[0];
			else
				Logger.Print(Logger.LogPriority.Warning, "ControlsMenu: ", $"{_action} is not a valid input action");
		}
	}
	public static void BindAction(InputEvent _event, string _action)
	{
		InputMap.ActionEraseEvents(_action);
		InputMap.ActionAddEvent(_action, _event);
		InputBinds.Binds[_action] = _event;
	}
}
