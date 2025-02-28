using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class LoadGameMenu : Control
{
	[Export] private AnimationPlayer menuPlayer;
	[Export] private Control container;
	[Export] private PackedScene savefile_slot;
	private string[] fileNames;
	private int lastFileNameIndex;

	private const string loadGameCategory = "load_game";

    public override void _Process(double delta)
    {
        if (!Visible)
			menuPlayer.Play("RESET");
    }
    public void AddMenuAction()
	{
		if (DirAccess.GetDirectoriesAt(SaveSystem.PROFILES_DIR).Length > 0)
		{
			// create load game button
			MainMenu.Instance.CreateMenuAction(loadGameCategory, OpenLoadMenu);
			// create continue button
			if (!string.IsNullOrEmpty(OptionsManager.Settings.LastPlayedResort))
				MainMenu.Instance.CreateMenuAction("continue", ContinueGame);
		}
	}

	private void OpenLoadMenu()
	{
		for(int i = 0; i < container.GetChildCount(); i++)
			container.GetChild(i).QueueFree();
		MainMenu.Instance.SetMenu(this);

		Dictionary<TimeSpan, Control> _savefiles = new();

		// Create savefiles
		fileNames = DirAccess.Open(SaveSystem.PROFILES_DIR).GetDirectories();
		for (int i = 0; i < fileNames.Length; i++)
		{
			// Get the metdata of a savefile
			string _profile = fileNames[i];
			
			GameManager.ProfileSaveModule.RootPath = Path.Combine(SaveSystem.PROFILES_DIR, fileNames[i]);
			// patch for 0.1.3v files
			if (!Godot.FileAccess.FileExists(GameManager.ProfileSaveModule.GetPath()))
			{
				string _path_old = GameManager.ProfileSaveModule.GetPath(true).Replace("main.data", "game_savedata.json");
				File.Move(_path_old, GameManager.ProfileSaveModule.GetPath(true));
			}
			GameManager.GameData _data = GameManager.ProfileSaveModule.Load(false);

			Control _slot = savefile_slot.Instantiate() as Control;

			(_slot.FindChild("name_label") as RichTextLabel).Text = _profile;
			(_slot.FindChild("time_label") as Label).Text = TimeSpan.FromSeconds(_data.Playtime).ToString(@"hh\:mm\:ss");
			(_slot.FindChild("aphid_label") as Label).Text = _data.AphidCount.ToString("000");

			// sets the proper string for when the game was last played
			TimeSpan _lastPlayedInterval = DateTime.Now - GlobalManager.Utils.UnixTimeStampToDateTime(Godot.FileAccess.GetModifiedTime(GameManager.ProfileSaveModule.GetPath()));
			var _lastPlayedTime = "???";

			if (_lastPlayedInterval.TotalDays < 1)
				_lastPlayedTime = Tr("date_today");
			else if(_lastPlayedInterval.TotalDays < 2)
				_lastPlayedTime = Tr("date_yesterday");
			else
				_lastPlayedTime = string.Format(Tr("date_daysago"), (int)_lastPlayedInterval.TotalDays);
			(_slot.FindChild("last_played_label") as Label).Text = Tr("load_game_last_played") + " " + _lastPlayedTime;

			(_slot.FindChild("load_button") as BaseButton).Pressed += () => PlayFile(_profile);
			(_slot.FindChild("delete_button") as BaseButton).Pressed += () => DeleteFile(_profile, _slot);

			_savefiles.Add(_lastPlayedInterval, _slot);
		}

		var _list = _savefiles.OrderBy(age => age.Key.TotalSeconds).ToList();
		foreach (var _pair in _list)
		{
			container.AddChild(_pair.Value);
		}
		
		menuPlayer.Play("open");
	}
	private void ContinueGame()
	{
		if (string.IsNullOrWhiteSpace(OptionsManager.Settings.LastPlayedResort) || !DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			MainMenu.Instance.RemoveMenuAction("continue");
			GlobalManager.CreatePopup("Could not find valid profile to continue", this);
			return;
		}
		SaveSystem.SelectProfile(OptionsManager.Settings.LastPlayedResort);

		MainMenu.LoadResort();
	}

	private static void PlayFile(string _profile)
	{
		SaveSystem.SelectProfile(_profile);
		MainMenu.LoadResort();
	}
	private static void DeleteFile(string _profile, Node _slot)
	{
		ConfirmationPopup.Create(() => 
		{
			MainMenu.DeleteResort(_profile);
			_slot.QueueFree();
		}, null, ConfirmationPopup.ConfirmationEnum.Safe);
	}
}
