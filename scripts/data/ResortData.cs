using Godot;
using System.Text.Json;
using System.Threading.Tasks;

public partial class ResortManager : Node2D, SaveSystem.ISaveData
{
	// SaveData Parameters
	public string GetId() => Resort + "-resort_data";
	public string GetDataPath() => SaveSystem.ProfileResortsDir;
	public static Savefile Data = new();

	public record class Savefile
	{
		public Item[] Items { get; set; }
		public Structure[] Structures { get; set; }
		public record struct Item
		{
			public int PositionX { get; set; }
			public int PositionY { get; set; }
			public string Id { get; set; }
		}
		public record struct Structure
		{
			public int PositionX { get; set; }
			public int PositionY { get; set; }
			public string Id { get; set; }
			public string Data { get; set; }
		}
	}
	public string SaveData()
	{
		// Save items in the ground
		Data.Items = new Savefile.Item[Instance.EntityRoot.GetChildCount()];
		for (int i = 0; i < Instance.EntityRoot.GetChildCount(); i++)
		{
			Node2D _item = Instance.EntityRoot.GetChild(i) as Node2D;

			if (!_item.HasMeta("id"))
			{
				Logger.Print(Logger.LogPriority.Error, $"ResortManager: The object {_item.Name}({_item.GetClass()}) did not have a valid id.");
				continue;
			}

			Data.Items[i] = new()
			{
				Id = _item.GetMeta("id").ToString(),
				PositionX = (int)_item.GlobalPosition.X,
				PositionY = (int)_item.GlobalPosition.Y
			};
		}

		Data.Structures = new Savefile.Structure[Instance.StructureRoot.GetChildCount()];
		for (int i = 0; i < Instance.StructureRoot.GetChildCount(); i++)
		{
			Node2D _item = Instance.StructureRoot.GetChild(i) as Node2D;
			Data.Structures[i] = new()
			{
				Id = _item.GetMeta("id").ToString(),
				PositionX = (int)_item.GlobalPosition.X,
				PositionY = (int)_item.GlobalPosition.Y
			};
		}

		return JsonSerializer.Serialize(Data);
	}
	public Task LoadData(string _json)
	{
		Data = JsonSerializer.Deserialize<Savefile>(_json);

		// load items
		for (int i = 0; i < Data.Items.Length; i++)
			CreateItem(Data.Items[i].Id, new(Data.Items[i].PositionX, Data.Items[i].PositionY));

		// clean structure root from default objects
		if (!IsNewGame)
		{
			for (int i = 0; i < Instance.StructureRoot.GetChildCount(); i++)
				Instance.StructureRoot.GetChild(i).QueueFree();
		}

		// spawn structures
		for (int i = 0; i < Data.Structures.Length; i++)
			CreateStructure(Data.Structures[i].Id, new(Data.Structures[i].PositionX, Data.Structures[i].PositionY));

		return Task.CompletedTask;
	}
	public Task SetData()
	{
		Data = new();
		return Task.CompletedTask;
	}
}
