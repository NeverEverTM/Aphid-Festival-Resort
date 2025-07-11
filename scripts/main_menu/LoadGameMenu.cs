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

	private const string loadGameCategory = "load_game", continueCategory = "continue";

	public void AddMenuAction()
	{
		if (DirAccess.GetDirectoriesAt(SaveSystem.PROFILES_DIR).Length > 0)
		{
			// create load game button
			if (DirAccess.Open(SaveSystem.PROFILES_DIR).GetDirectories().Length > 0)
				MainMenu.Instance.CreateMenuAction(loadGameCategory, OpenLoadMenu);
			// create continue button
			if (!string.IsNullOrEmpty(OptionsManager.Settings.LastPlayedResort))
			{
				SaveSystem.SelectProfile(OptionsManager.Settings.LastPlayedResort);
				if (DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
				{
					MainMenu.Instance.CreateMenuAction(continueCategory, ContinueGame);
					MainMenu.Instance.SetCategory(continueCategory);
				}
			}
		}
	}

	private void OpenLoadMenu()
	{
		for (int i = 0; i < container.GetChildCount(); i++)
			container.GetChild(i).QueueFree();
		MainMenu.Instance.SetMenu(this);

		Dictionary<TimeSpan, Control> _savefiles = [];

		// Create savefiles
		fileNames = DirAccess.Open(SaveSystem.PROFILES_DIR).GetDirectories();
		SaveSystem.SaveModule<GameManager.GameData> _module = new(GameManager.ID, new GameManager.GameDataModule(), 0);

		for (int i = 0; i < fileNames.Length; i++)
		{
			// Get the metdata of a savefile
			string _profile = fileNames[i];
			uint _version = 0;

			// this whole "exists" check is to know wheter we should display info or state that is missing instead
			// it checks if a directory has "main.data", if not, check if it has a pre-1.3 savefile instead, if neither then it does not exist
			_module.RootPath = Path.Combine(SaveSystem.PROFILES_DIR, fileNames[i]);
			bool _exists = false;
			if (!Godot.FileAccess.FileExists(_module.GetPath()))
			{
				string _path_old = _module.GetPath(true).Replace("main.data", "game_savedata.json");
				if (Godot.FileAccess.FileExists(_path_old))
					_exists = true;
			}
			else
				_exists = true;
			GameManager.GameData _data = _module.Load(false);
			if (_exists)
				_version = _module.GameVersion;

			Control _slot = savefile_slot.Instantiate() as Control;

			(_slot.FindChild("name_label") as RichTextLabel).Text = _profile;
			(_slot.FindChild("time_label") as Label).Text =
					_exists ? TimeSpan.FromSeconds(_data.Playtime).ToString(@"hh\:mm\:ss") : "???";
			(_slot.FindChild("aphid_label") as Label).Text =
					_exists ? _data.AphidCount.ToString("000") : "???";

			// sets the proper string for when the game was last played
			string _lastPlayedTime = "???";
			TimeSpan _lastPlayedInterval = new();

			if (_exists)
			{
				_lastPlayedInterval = DateTime.Now - GlobalManager.Utils.UnixTimeStampToDateTime(Godot.FileAccess.GetModifiedTime(_module.GetPath()));
				if (_lastPlayedInterval.TotalDays < 1)
					_lastPlayedTime = Tr("date_today");
				else if (_lastPlayedInterval.TotalDays < 2)
					_lastPlayedTime = Tr("date_yesterday");
				else
					_lastPlayedTime = string.Format(Tr("date_daysago"), (int)_lastPlayedInterval.TotalDays);
			}
			(_slot.FindChild("last_played_label") as Label).Text = Tr("load_game_last_played") + " " + _lastPlayedTime;

			// button functionality
			(_slot.FindChild("load_button") as BaseButton).Pressed += () =>
			{
				if (_version < GlobalManager.GAME_VERSION)
					ConfirmationPopup.Create(() => PlayFile(_profile, _data.LastRoom), null,
							ConfirmationPopup.ConfirmationEnum.Fast, "warning_incompatible_version");
				else
					PlayFile(_profile, _data.LastRoom);
			};
			(_slot.FindChild("delete_button") as BaseButton).Pressed += () => DeleteFile(_profile, _slot);

			_savefiles.Add(_lastPlayedInterval, _slot);
		}

		var _list = _savefiles.OrderBy(age => age.Key.TotalSeconds).ToList();
		foreach (var _pair in _list)
			container.AddChild(_pair.Value);

		menuPlayer.Play("open");
	}
	private void ContinueGame()
	{
		if (string.IsNullOrWhiteSpace(OptionsManager.Settings.LastPlayedResort) || !DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			MainMenu.Instance.RemoveMenuAction(continueCategory);
			GlobalManager.CreatePopup("Could not find valid profile to continue", this);
			return;
		}

		SaveSystem.SelectProfile(OptionsManager.Settings.LastPlayedResort);
        GameManager.GameSaveModule _module = new(GameManager.ID, new GameManager.GameDataModule(), 0)
        {
            RootPath = Path.Combine(SaveSystem.ProfilePath)
        };

		GameManager.GameData _data = _module.Load(false);
		MainMenu.LoadResort(_data.LastRoom);
	}

	private static void PlayFile(string _profile, string _room = "")
	{
		SaveSystem.SelectProfile(_profile);
		MainMenu.LoadResort(_room);
	}
	private static void DeleteFile(string _profile, Node _slot)
	{
		ConfirmationPopup.Create(() =>
		{
			MainMenu.DeleteResort(_profile);
			_slot.QueueFree();

			// there is no more savefiles, reset to default installation state
			if (DirAccess.Open(SaveSystem.PROFILES_DIR).GetDirectories().Length == 0)
			{
				MainMenu.Instance.RemoveMenuAction(loadGameCategory);
				MainMenu.Instance.RemoveMenuAction(continueCategory);
				MainMenu.Instance.CloseMenu();
			}

		}, null, ConfirmationPopup.ConfirmationEnum.Safe);
	}
}
