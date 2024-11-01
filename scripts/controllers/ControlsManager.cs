using Godot;
using System.IO;
/// <summary>
/// Manager in charge of controlling custom input binding.
/// To access saved configurations, check OptionsManager instead
/// </summary>
public partial class ControlsManager : Node
{
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

		return _displayAction;
	}
	public static void ResetToDefault()
	{
		File.Delete(ProjectSettings.GlobalizePath(OptionsManager.ConfigOverridePath));
		InputMap.LoadFromProjectSettings();
		OptionsManager.Data = new();

		foreach (string _action in InputMap.GetActions())
		{
			var _events = InputMap.ActionGetEvents(_action);
			if (_events.Count > 0)
			{
				string _displayAction = _events[0].AsText();
				if (_displayAction.Contains(" (Physical)"))
					_displayAction = _displayAction.Replace(" (Physical)", "");
				OptionsManager.Data.InputBinds[_action] = _displayAction;
			}
			else
				Logger.Print(Logger.LogPriority.Warning, "ControlsMenu: ", $"{_action} is not a valid input action");
		}
	}
	public static void BindAction(InputEvent _event, string _action)
	{
		InputMap.ActionEraseEvents(_action);
		InputMap.ActionAddEvent(_action, _event);
		string _displayAction = _event.AsText();
		OptionsManager.Data.InputBinds[_action] = _displayAction;
		ProjectSettings.SetSetting("input/" + _action, new Godot.Collections.Dictionary<string, Variant> {
			{"deadzone", "0.5"},
			{"events", Variant.CreateFrom(new InputEvent[] { _event })}
		});
	}
}
