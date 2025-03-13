using Godot;
using System.Collections.Generic;

/// <summary>
/// Manager in charge of controlling custom input binding.
/// </summary>
public static class ControlsManager
{
	public class DataModule : SaveSystem.IDataModule<Variant>
	{
		public void Set(Variant _data)
		{
			ResetToDefault();
			foreach (KeyValuePair<string, string> _pair in (Godot.Collections.Dictionary<string, string>)_data)
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
		}
		public Variant Get()
		{
			Godot.Collections.Dictionary<string, string> _binds = new();
			foreach (KeyValuePair<string, InputEvent> _pair in Binds)
			{
				if (_pair.Key.Contains("ui"))
					continue;

				if (_pair.Value is InputEventKey)
					_binds.Add(_pair.Key, "InputEventKey" + ":" + ((long)(_pair.Value as InputEventKey).Keycode).ToString());
				else if (_pair.Value is InputEventMouseButton)
					_binds.Add(_pair.Key, "InputEventMouseButton" + ":" + ((long)(_pair.Value as InputEventMouseButton).ButtonIndex).ToString());
			}
			return _binds;
		}
		public Variant Default()
		{
			return new Godot.Collections.Dictionary<string, string>();
		}
	}

	public const string GUI_ICONS_PATH = "res://sprites/ui/gui_icons/";

	internal static Godot.Collections.Dictionary<string, InputEvent> Binds = new();
	internal readonly static SaveSystem.SaveModuleGD InputBinds = new("controls", new DataModule())
	{
		RelativePath = SaveSystem.CONFIG_DIR,
		Extension = SaveSystem.CONFIGFILE_EXTENSION
	};

	/// <summary>
	/// Cleans action names under specific requirements.
	/// </summary>
	public static string CleanActionName(InputEvent _input) =>
		CleanActionName(_input.AsText());
	public static string CleanActionName(string _raw_action_name)
	{
		if (_raw_action_name.Contains(" (Physical)"))
			_raw_action_name = _raw_action_name.Replace(" (Physical)", "");

		return _raw_action_name;
	}
	/// <summary>
	/// Returns the human-friendly name for an action. Intended for UI display.
	/// </summary>
	public static string GetActionName(InputEvent _event)
	{
		string _action = _event.AsText();

		if (_action.Contains(" (Physical)"))
			_action = _action.Replace(" (Physical)", "");

        return _action switch
        {
            "Space" => $"[img]{GUI_ICONS_PATH}icon_space.png[/img]",
            "Left Mouse Button" => $"[img]{GUI_ICONS_PATH}icon_mouse_left.png[/img]",
            "Right Mouse Button" => $"[img]{GUI_ICONS_PATH}icon_mouse_right.png[/img]",
            "Middle Mouse Button" => $"[img]{GUI_ICONS_PATH}icon_mouse_middle.png[/img]",
            "Mouse Thumb Button 1" => $"[img]{GUI_ICONS_PATH}icon_mouse_extra1.png[/img]",
            "Mouse Thumb Button 2" => $"[img]{GUI_ICONS_PATH}icon_mouse_extra2.png[/img]",
            _ => _action,
        };
    }
	/// <summary>
	/// Returns the human-friendly name for an action. Intended for UI display.
	/// </summary>
	public static string GetActionName(string _action_name) =>
		GetActionName(Binds[_action_name]);
	public static void ResetToDefault()
	{
		InputMap.LoadFromProjectSettings();
		Binds.Clear();

		foreach (string _action in InputMap.GetActions())
		{
			var _events = InputMap.ActionGetEvents(_action);

			if (_events.Count > 0)
			{
				if (_events[0].AsText().StartsWith("ui"))
					continue;
				Binds[_action] = _events[0];
			}
		}
	}
	public static void BindAction(InputEvent _event, StringName _action)
	{
		InputMap.ActionEraseEvents(_action);
		InputMap.ActionAddEvent(_action, _event);
		Binds[_action] = _event;
	}
}
