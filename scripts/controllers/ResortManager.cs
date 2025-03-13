using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ResortManager : Node2D, SaveSystem.IDataModule<ResortManager.Savefile>
{
	// Default Parameters
	[Export] public string Resort;
	[Export] public Node2D EntityRoot, StructureRoot, SpawnPoint;

	public static ResortManager CurrentResort { get; private set; }

	/// <summary>
	/// Access local aphids within this resort. To access aphids across all resorts, use SaveSystem.Aphids instead.
	/// </summary>
	public readonly List<Aphid> AphidsOnResort = new();
	public Savefile Data { get; set; }

	private SaveSystem.SaveModule<Savefile> ResortSaveModule;
	public record Savefile
	{
		public Item[] Items { get; set; }
		public Structure[] Structures { get; set; }
		public struct Item
		{
			public int PositionX { get; set; }
			public int PositionY { get; set; }
			public string Id { get; set; }
		}
		public struct Structure
		{
			public int PositionX { get; set; }
			public int PositionY { get; set; }
			public string Id { get; set; }
			public string Data { get; set; }
		}
	}

	public interface IStructureData
	{
		public void SetData(string _data);
		public string GetData();
	}

	public void Set(Savefile _data)
    {
        Data = _data;

		// load items
		for (int i = 0; i < Data.Items?.Length; i++)
			CreateItem(Data.Items[i].Id, new(Data.Items[i].PositionX, Data.Items[i].PositionY));

		// clean structure root from default objects
		if (!GameManager.IsNewGame)
		{
			for (int i = 0; i < CurrentResort.StructureRoot.GetChildCount(); i++)
				CurrentResort.StructureRoot.GetChild(i).QueueFree();
		}

		// spawn structures
		for (int i = 0; i < Data.Structures?.Length; i++)
			CreateStructure(Data.Structures[i].Id, new(Data.Structures[i].PositionX, Data.Structures[i].PositionY), Data.Structures[i].Data);
    }
    public Savefile Get()
    {
		// Save items in the ground
		Data.Items = new Savefile.Item[CurrentResort.EntityRoot.GetChildCount()];
		for (int i = 0; i < CurrentResort.EntityRoot.GetChildCount(); i++)
		{
			Node2D _item = CurrentResort.EntityRoot.GetChild(i) as Node2D;

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

		Data.Structures = new Savefile.Structure[CurrentResort.StructureRoot.GetChildCount()];
		for (int i = 0; i < CurrentResort.StructureRoot.GetChildCount(); i++)
		{
			Node2D _item = CurrentResort.StructureRoot.GetChild(i) as Node2D;
			Data.Structures[i] = new()
			{
				Id = _item.GetMeta("id").ToString(),
				PositionX = (int)_item.GlobalPosition.X,
				PositionY = (int)_item.GlobalPosition.Y,
				Data = (_item is IStructureData) ? (_item as IStructureData).GetData() : null
			};
		}

        return Data;
    }
	public Savefile Default() => new();

	public override void _EnterTree()
	{
		CurrentResort = this;
		
		// save data setup
		ResortSaveModule = new(Resort + "-resort", this)
		{
			Extension = SaveSystem.SAVEFILE_EXTENSION,
			RelativePath = SaveSystem.PROFILERESORTS_DIR
		};
		SaveSystem.ProfileClassData.Add(ResortSaveModule);

		// music loop handling
		static void FinishedSignal(GlobalManager.SceneName _)
		{
			SoundManager.MusicPlayer.Finished -= CheckSongToPlay;
			GlobalManager.OnPreLoadScene -= FinishedSignal;
		};
		GlobalManager.OnPreLoadScene += FinishedSignal;

		if (IsInstanceValid(SoundManager.MusicPlayer))
		{
			SoundManager.MusicPlayer.Finished += CheckSongToPlay;
			CheckSongToPlay();
		}
	}
    public override void _Ready()
    {
        GameManager.StartGame();
    }
    public static async void CheckSongToPlay()
	{
		await Task.Delay(1000);
		string[] _raw_files = DirAccess.GetFilesAt(GlobalManager.RES_SFX_PATH + "music");

		if (FieldManager.TimeOfDay == FieldManager.DayHours.Night)
			_raw_files = _raw_files.Where(e => e.StartsWith("night")).ToArray();
		else
			_raw_files = _raw_files.Where(e => e.StartsWith("day")).ToArray();

		// for some reason, the exported project cannot get access to "music.mp3" files
		// using DirAccess.GetFilesAt(), it will only find "music.mp3.imported" ones and return those
		// however for some WEIRD reason, if you just reference it anyways by trimming the ".import"
		// it will find the supposedly non-existent .mp3 file
		// UPDATE: this is a thing for EVERY GODDAMN IMPORTED ITEM, who the fuck made this engine!
		// more in global manager's game initalization
		string _file = _raw_files[GlobalManager.RNG.RandiRange(0, _raw_files.Length - 1)].TrimSuffix(".import");
		SoundManager.PlaySong($"music/{_file}");
	}

	// =========| Object Creation |===============
	public static Aphid SpawnAphid(AphidInstance _instance)
	{
		Aphid _aphid = (ResourceLoader.Load(GlobalManager.APHID_ENTITY_PATH) as PackedScene).Instantiate() as Aphid;

		_aphid.Instance = _instance;
		_aphid.GlobalPosition = new(_instance.Status.PositionX, _instance.Status.PositionY);
		_instance.Entity = _aphid;

		CurrentResort.AddChild(_aphid);
		CurrentResort.AphidsOnResort.Add(_aphid);
		return _aphid;
	}
	/// <summary>
	/// Creates a new aphid and adds it to the current savefile.
	/// </summary>
	/// <returns>The newly created aphid.</returns>
	public static Aphid CreateAphid(Vector2 _position, AphidData.Genes _genes)
	{
		Aphid _aphid = (ResourceLoader.Load(GlobalManager.APHID_ENTITY_PATH) as PackedScene).Instantiate() as Aphid;

		_aphid.Instance = new()
		{
			Entity = _aphid,
			Genes = _genes
		};
		_aphid.GlobalPosition = _position;

		GameManager.AddAphid(_aphid.Instance);
		CurrentResort.AddChild(_aphid);
		CurrentResort.AphidsOnResort.Add(_aphid);
		return _aphid;
	}
	public static Node2D CreateItem(string _item_name, Vector2 _position)
	{
		if (_item_name == null)
		{
			Logger.Print(Logger.LogPriority.Error, "ResortManager: Item name is null!");
			return null;
		}
		Node2D _item;
		string _path = $"{GlobalManager.ItemPath}/{_item_name}.tscn";
		if (ResourceLoader.Exists(_path))
			// this is used if an item has a more complex structure or contains extra data, thus needing an unique node
			_item = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		else
		{
			_item = ResourceLoader.Load<PackedScene>(GlobalManager.ItemEntity).Instantiate() as Node2D;
			(_item.GetChild(0) as Sprite2D).Texture = GlobalManager.GetIcon(_item_name);
		}

		_item.SetMeta(GlobalManager.StringNames.PickupMeta, true);
		_item.SetMeta(GlobalManager.StringNames.IdMeta, _item_name);
		_item.SetMeta(GlobalManager.StringNames.TagMeta, GlobalManager.G_ITEMS[_item_name].tag);
		_item.GlobalPosition = _position;

		CurrentResort.EntityRoot.AddChild(_item);
		return _item;
	}
	public static void CreateStructure(string _id, Vector2 _position, string _data = null)
	{
		string _path = GlobalManager.StructuresPath + $"/{_id}.tscn";
		if (!ResourceLoader.Exists(_path))
		{
			Logger.Print(Logger.LogPriority.Error, $"ResortManager: {_id} is not a valid id.");
			return;
		}

		Node2D _item = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		_item.GlobalPosition = _position;
		CurrentResort.StructureRoot.AddChild(_item);
		if (_data != null && _item is IStructureData)
			(_item as IStructureData).SetData(_data);
	}
}