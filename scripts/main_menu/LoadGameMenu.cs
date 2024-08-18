using System;
using System.Text.Json;
using Godot;

public partial class LoadGameMenu : Control
{
	[Export] private AnimationPlayer menuPlayer;
	[Export] private Control container;
	[Export] private PackedScene savefile_slot;
	private string[] fileNames;
	private int lastFileNameIndex;

	private const string loadGameCategory = "load_game", continueCategory = "continue";

    public override void _Ready()
    {
        fileNames = DirAccess.Open(SaveSystem.ProfilesDirectory).GetDirectories();

		for (int i = 0; i < fileNames.Length; i++)
		{
			string _path = SaveSystem.ProfilesDirectory + $"/{fileNames[i]}/{GameData.Instance.GetId()}.json",
			_profile = fileNames[i];
			GameData.Savefile _data = null; 

			// Get the metdata of a savefile
			if (FileAccess.FileExists(_path))
			{
				using var _stream = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
				_data = JsonSerializer.Deserialize<GameData.Savefile>(_stream.GetPascalString());
			}

			_data ??= new();
			BaseButton _slot = savefile_slot.Instantiate().GetChild(0) as BaseButton;

			(_slot.GetChild(0) as RichTextLabel).Text = _profile;
			(_slot.GetChild(1) as Label).Text = TimeSpan.FromSeconds(_data.Playtime).ToString(@"hh\:mm\:ss");
			(_slot.GetChild(2) as Label).Text = _data.AphidCount.ToString("000");

			_slot.Pressed += () => PlayFile(_profile);
			(_slot.GetChild(3) as BaseButton).Pressed += () => DeleteFile(_profile, _slot.GetParent());

			container.AddChild(_slot.GetParent());
		}
    }
    public override void _Process(double delta)
    {
        if (!Visible)
			menuPlayer.Play("RESET");
    }
    public void AddMenuActions()
	{
		if (DirAccess.GetDirectoriesAt(SaveSystem.ProfilesDirectory).Length > 0)
		{
			// create load game button
			MainMenu.Instance.CreateMenuAction(loadGameCategory, OpenLoadMenu);
			// create continue button
			if (!string.IsNullOrEmpty(OptionsManager.Data.LastPlayedResort))
			{
				MainMenu.Instance.CreateMenuAction(continueCategory, ContinueGame);
				MainMenu.Instance.SetCategory(continueCategory);
			}
		}
	}

	private void OpenLoadMenu()
	{
		MainMenu.Instance.SetMenu(this);
		menuPlayer.Play("open");
	}
	private void ContinueGame()
	{
		if (string.IsNullOrWhiteSpace(OptionsManager.Data.LastPlayedResort) || !DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			MainMenu.Instance.RemoveMenuAction(continueCategory);
			GameManager.CreatePopup("Could not find valid profile to continue", this);
			return;
		}
		SaveSystem.SetProfile(OptionsManager.Data.LastPlayedResort);

		MainMenu.LoadResort();
	}

	private static void PlayFile(string _profile)
	{
		SaveSystem.SetProfile(_profile);
		MainMenu.LoadResort();
	}
	private static void DeleteFile(string _profile, Node _slot)
	{
		ConfirmationPopup.CreateConfirmation(() => 
		{
			MainMenu.DeleteResort(_profile);
			_slot.QueueFree();
		});
	}
}
