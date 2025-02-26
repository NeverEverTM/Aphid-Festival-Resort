using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }
	public static SaveSystem.SaveModule<GameData> ProfileSaveModule { get; set; }
	public const string ID = "main";
	public static bool IsNewGame { get; set; }

	// SaveData
	public static GameData ProfileData { get; private set; }
	public static Dictionary<Guid, AphidInstance> Aphids { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

    public static async void StartGame()
    {
		ProfileData = new();
		Aphids = new();
		SaveSystem.ProfileClassData.Add(ProfileSaveModule);
        // On New game, put intro cutscene, otherwise just load normally
		if (!IsNewGame)
			await SaveSystem.LoadProfile();
		else
		{
			IsNewGame = false;
			PlayerInventory.StoreItem("aphid_egg");
			PlayerInventory.StoreItem("aphid_egg");
			Player.Instance.GlobalPosition = ResortManager.CurrentResort.SpawnPoint.GlobalPosition;
			await SaveSystem.SaveProfile();
			await DialogManager.Instance.OpenDialogBox("intro_welcome");
		}
    }

    public override void _Process(double delta)
	{
		if (!GlobalManager.IsBusy)
			ProfileData.Playtime += (float)delta;
	}

	public static Guid AddAphid(AphidInstance _buddy)
	{
		Guid _guid = Guid.NewGuid();
		_buddy.ID = _guid.ToString();
		Aphids.Add(_guid, _buddy);
		return _guid;
	}
	public static void RemoveAphid(Guid _guid)
	{
		if (!Aphids.ContainsKey(_guid))
		{
			Logger.Print(Logger.LogPriority.Error, $"GameManager: Cannot delete <{_guid}> as it does not exist.");
			return;
		}
		Aphids.Remove(_guid);
	}
	public static void ReplaceAphid(Guid _guid, AphidInstance _buddy) =>
		Aphids[_guid] = _buddy;

	public class SaveModule : SaveSystem.IDataModule<GameData>
	{
		public void Set(GameData _data)
		{
			ProfileData = _data;
		}
		public GameData Get()
		{
			ProfileData.Version = GlobalManager.GAME_VERSION;
			ProfileData.AphidCount = Aphids.Count;
			return ProfileData;
		}
		public GameData Default() => new();
	}
	public record GameData
	{
		public int AphidCount { get; set; }
		public float Playtime { get; set; }
		public uint Version { get; set; }

		public GameData()
		{
			AphidCount = 0;
			Playtime = 0;
			Version = GlobalManager.GAME_VERSION;
		}
	}
}
