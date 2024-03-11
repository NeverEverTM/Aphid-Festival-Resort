using Godot;
using System.Text.Json;
using System.Threading.Tasks;

public class PlayerData : SaveSystem.ISaveData
{
	public string Name { get; set; }
	public int Level { get; set; }
	public string Room { get; set; }

	public float PositionX { get; set; }
	public float PositionY { get; set; }

	public System.Collections.Generic.List<string> Inventory { get; set; }

	private const string playerData = "/player.data";

	public PlayerData()
	{
		Inventory = new();
		Room = "resort_golden_grounds";
	}

	public Task SaveData(string _path)
	{
		using var _file = FileAccess.Open(_path + playerData, FileAccess.ModeFlags.Write);
		var _jsonPlayer = JsonSerializer.Serialize(Player.Data);
		_file.StorePascalString(_jsonPlayer);

		return Task.CompletedTask;
	}

	public Task LoadData(string _path)
	{
		using var _file = FileAccess.Open(_path + playerData, FileAccess.ModeFlags.Read);
		var _data = _file.GetPascalString();

		if (_data == string.Empty)
		{
			Player.Data = new();
			GD.PrintErr("This player data was empty!");
			return Task.CompletedTask;
		}

		Player.Data = JsonSerializer.Deserialize<PlayerData>(_data);
		return Task.CompletedTask;
	}
}