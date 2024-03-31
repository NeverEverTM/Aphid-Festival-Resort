using Godot;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	public class SaveFile
	{
		public string Name { get; set; }
		public int Level { get; set; }
		public string Room { get; set; }

		public float PositionX { get; set; }
		public float PositionY { get; set; }

		public List<string> Inventory { get; set; }
		public int Currency { get; set; }

		public SaveFile()
		{
			Room = "resort_golden_grounds";
		}
	}

	public static SaveFile savedata;
	public static SaveFileController savecontroller = new();

	public class SaveFileController : SaveSystem.ISaveData
	{
		private const string playerData = "/player.data";
		public Task SaveData(string _path)
		{
			using var _file = FileAccess.Open(_path + playerData, FileAccess.ModeFlags.Write);
			var _jsonPlayer = JsonSerializer.Serialize(savedata);
			_file.StorePascalString(_jsonPlayer);

			return Task.CompletedTask;
		}

		public Task LoadData(string _path)
		{
			string _fullPath = _path + playerData;
			if (!FileAccess.FileExists(_fullPath)) // Does it exist?
			{
                savedata = new()
                {
                    Inventory = new()
                };
                GD.PrintErr("This player data does not exist!");
				return Task.CompletedTask;
			}

			using var _file = FileAccess.Open(_fullPath, FileAccess.ModeFlags.Read);
			var _data = _file.GetPascalString();

			if (_data == string.Empty) // Is it empty?
			{
				savedata = new();
				GD.PrintErr("This player data was empty!");
				return Task.CompletedTask;
			}

			// Deseralize and load data into memory
			savedata = JsonSerializer.Deserialize<SaveFile>(_data);
			Instance.GlobalPosition = new Vector2(savedata.PositionX, savedata.PositionY);
			for (int i = 0; i < savedata.Inventory.Count; i++)
				Instance.CreateInvItem(i);
			return Task.CompletedTask;
		}
	}
}