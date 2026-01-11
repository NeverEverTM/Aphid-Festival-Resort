using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class LoadGameMenu : Control
{
	[Export] private AnimationPlayer anim_player;
	[Export] private Container container;
	[Export] private ScrollContainer scroll;
	[Export] private PackedScene savefile_slot;
	private string[] file_names;
	private List<SaveSlot> loaded_savefiles = [];

	private struct SaveSlot
	{
		public Control slot;
		public TimeSpan lastTimePlayed;
		public string name;

		public SaveSlot(string name, TimeSpan lastTimePlayed, Control slot)
		{
			this.name = name;
			this.slot = slot;
			this.lastTimePlayed = lastTimePlayed;
		}
	}

	private MenuInstance menu;

	public override void _EnterTree()
	{
		menu = new(Enum.GetName(StartMenu.WheelCategories.LoadGame), anim_player, (_) =>
		{
			scroll.ScrollVertical = 0;
			GenerateSaveSlots();
		}, null, (_) => scroll.ScrollVertical = 0, null, true);

		for (int i = 0; i < container.GetChildCount(); i++)
			container.GetChild(i).QueueFree();

		if (DirAccess.GetDirectoriesAt(SaveSystem.PROFILES_DIR).Length > 0)
			StartMenu.CreateWheelAction(StartMenu.WheelCategories.LoadGame, menu);
	}

	private void GenerateSaveSlots()
	{
		file_names = DirAccess.Open(SaveSystem.PROFILES_DIR).GetDirectories();
		SaveSystem.SaveModule<GameManager.GameData> _module = new(GameManager.ID, new GameManager.GameDataModule(), 0);

		for (int i = 0; i < file_names.Length; i++)
		{
			if (loaded_savefiles.Exists(s => s.name == file_names[i]))
				continue;
			loaded_savefiles.Add(CreateSaveSlot(file_names[i], ref _module));
		}

		loaded_savefiles = [.. loaded_savefiles.OrderBy(slot => slot.lastTimePlayed)];

		for (int i = 0; i < loaded_savefiles.Count; i++)
			container.MoveChild(loaded_savefiles[i].slot, i);
	}
	private SaveSlot CreateSaveSlot(string _profile, ref SaveSystem.SaveModule<GameManager.GameData> _module)
	{
		// Get the metdata of a savefile
		uint _version = 0;

		// this whole "exists" check is to know wheter we should display info or state that is missing instead
		// it checks if a directory has "main.data", if not, check if it has a pre-1.3 savefile instead, if neither then it does not exist
		_module.RootPath = Path.Combine(SaveSystem.PROFILES_DIR, _profile);
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

		// start generating the saveslot node
		Control _slot = savefile_slot.Instantiate() as Control;

		(_slot.FindChild("name_label") as RichTextLabel).Text = " " + _profile; // add a space for correct text spacing
		(_slot.FindChild("time_label") as Label).Text =
				_exists ? TimeSpan.FromSeconds(_data.Playtime).ToString(@"hh\:mm\:ss") : "???";
		(_slot.FindChild("aphid_label") as Label).Text =
				_exists ? _data.AphidCount.ToString("000") : "???";

		string _lastPlayedText = "???";
		TimeSpan _lastPlayedTime = new((long)(Time.GetUnixTimeFromSystem() - _data.LastTimeLoaded));

		// sets the proper string for when the game was last played
		if (_exists && _data.LastTimeLoaded != 0)
		{
			if (_lastPlayedTime.TotalDays <= 1)
				_lastPlayedText = Tr("date_today");
			else if (_lastPlayedTime.TotalDays <= 2)
				_lastPlayedText = Tr("date_yesterday");
			else
				_lastPlayedText = string.Format(Tr("date_daysago"), (int)_lastPlayedTime.TotalDays);
		}

		(_slot.FindChild("last_played_label") as Label).Text = $"{Tr("load_game_last_played")} {_lastPlayedText}";

		// =====| button functionality |==========
		var _button = _slot.FindChild("load_button") as BaseButton;
		_button.FocusEntered += () => SoundManager.CreateSound("ui/button_switch");
		_button.Pressed += () =>
		{
			if (_version < GlobalManager.GAME_VERSION)
				ConfirmationPopup.Create(() => PlayFile(_profile, _data.LastRoom), null,
						ConfirmationPopup.ConfirmationEnum.Fast, "warning_incompatible_version");
			else
				PlayFile(_profile, _data.LastRoom);
		};
		(_slot.FindChild("delete_button") as BaseButton).Pressed += () => DeleteFile(_profile, _slot);

		container.AddChild(_slot);
		return new(_profile, _lastPlayedTime, _slot);
	}

	private static void PlayFile(string _profile, string _room = "")
	{
		SaveSystem.SelectProfile(_profile);
		MainMenu.LoadResort(_room);
	}
	private void DeleteFile(string _profile, Node _slot)
	{
		ConfirmationPopup.Create(() =>
		{
			MainMenu.DeleteResort(_profile);
			loaded_savefiles.Remove(loaded_savefiles.Find((s) => s.slot.Equals(_slot)));
			_slot.QueueFree();

			// there is no more savefiles, reset to default installation state
			if (DirAccess.Open(SaveSystem.PROFILES_DIR).GetDirectories().Length == 0)
			{
				StartMenu.RemoveWheelAction(StartMenu.WheelCategories.LoadGame);
				StartMenu.RemoveWheelAction(StartMenu.WheelCategories.Continue);
				StartMenu.Instance.GoBack();
			}

		}, null, ConfirmationPopup.ConfirmationEnum.Safe);
	}
}
