using System;
using System.Linq;
using System.Text.Json;
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
		if (DirAccess.GetDirectoriesAt(SaveSystem.ProfilesDir).Length > 0)
		{
			// create load game button
			MainMenu.Instance.CreateMenuAction(loadGameCategory, OpenLoadMenu);
			// create continue button
			if (!string.IsNullOrEmpty(OptionsManager.Settings.Data.LastPlayedResort))
				MainMenu.Instance.CreateMenuAction("continue", ContinueGame);
		}
	}

	private void OpenLoadMenu()
	{
		MainMenu.Instance.SetMenu(this);

		// Create savefiles
		fileNames = DirAccess.Open(SaveSystem.ProfilesDir).GetDirectories();
		for (int i = 0; i < fileNames.Length; i++)
		{
			string _path = SaveSystem.ProfilesDir + $"/{fileNames[i]}/{GameData.Instance.GetId()}.json",
			_profile = fileNames[i];
			GameData.Savefile _data = null; 

			// Get the metdata of a savefile
			if (FileAccess.FileExists(_path))
			{
				using var _stream = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
				_data = JsonSerializer.Deserialize<GameData.Savefile>(_stream.GetPascalString());
			}

			_data ??= new();
			Control _slot = savefile_slot.Instantiate() as Control;

			(_slot.FindChild("name_label") as RichTextLabel).Text = _profile;
			(_slot.FindChild("time_label") as Label).Text = TimeSpan.FromSeconds(_data.Playtime).ToString(@"hh\:mm\:ss");
			(_slot.FindChild("aphid_label") as Label).Text = _data.AphidCount.ToString("000");

			(_slot.FindChild("load_button") as BaseButton).Pressed += () => PlayFile(_profile);
			(_slot.FindChild("delete_button") as BaseButton).Pressed += () => DeleteFile(_profile, _slot);

			container.AddChild(_slot);
		}
		
		menuPlayer.Play("open");
	}
	private void ContinueGame()
	{
		if (string.IsNullOrWhiteSpace(OptionsManager.Settings.Data.LastPlayedResort) || !DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			MainMenu.Instance.RemoveMenuAction("continue");
			GameManager.CreatePopup("Could not find valid profile to continue", this);
			return;
		}
		SaveSystem.SelectProfile(OptionsManager.Settings.Data.LastPlayedResort);

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
