using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ResortManager : Node2D
{
	// Default Parameters
	[Export] public string Resort;
	[Export] public Node2D EntityRoot, StructureRoot, SpawnPoint;

	/// <summary>
	/// Access local aphids within this resort. To access aphids across all resorts, use GameManager.Aphids instead.
	/// </summary>
	public readonly List<Aphid> Aphids = [];
	private SaveSystem.SaveModule<Savefile> SaveModule;
	public static ResortManager Current { get; private set; }

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
	public class ResortDataModule : SaveSystem.IDataModule<Savefile>
	{
		public Savefile Data { get; set; }
		public void Set(Savefile _data)
		{
			Data = _data;

			// load items
			for (int i = 0; i < Data.Items?.Length; i++)
				CreateItem(Data.Items[i].Id, new(Data.Items[i].PositionX, Data.Items[i].PositionY));

			// clean structure root from default objects
			if (!GameManager.IsNewGame)
			{
				for (int i = 0; i < Current.StructureRoot.GetChildCount(); i++)
					Current.StructureRoot.GetChild(i).QueueFree();
			}

			// spawn structures
			for (int i = 0; i < Data.Structures?.Length; i++)
				CreateStructure(Data.Structures[i].Id, new(Data.Structures[i].PositionX, Data.Structures[i].PositionY), Data.Structures[i].Data);
		}
		public Savefile Get()
		{
			// Save items in the ground
			Data.Items = new Savefile.Item[Current.EntityRoot.GetChildCount()];
			for (int i = 0; i < Current.EntityRoot.GetChildCount(); i++)
			{
				Node2D _item = Current.EntityRoot.GetChild(i) as Node2D;

				if (!_item.HasMeta(StringNames.IdMeta))
				{
					Logger.Print(Logger.LogPriority.Error, $"ResortManager: The object {_item.Name}({_item.GetClass()}) did not have a valid id.");
					continue;
				}

				Data.Items[i] = new()
				{
					Id = _item.GetMeta(StringNames.IdMeta).ToString(),
					PositionX = (int)_item.GlobalPosition.X,
					PositionY = (int)_item.GlobalPosition.Y
				};
			}

			Data.Structures = new Savefile.Structure[Current.StructureRoot.GetChildCount()];
			for (int i = 0; i < Current.StructureRoot.GetChildCount(); i++)
			{
				Node2D _item = Current.StructureRoot.GetChild(i) as Node2D;

				if (!_item.HasMeta(StringNames.IdMeta))
				{
					Logger.Print(Logger.LogPriority.Error, $"ResortManager: The object {_item.Name}({_item.GetClass()}) did not have a valid id.");
					continue;
				}

				Data.Structures[i] = new()
				{
					Id = _item.GetMeta(StringNames.IdMeta).ToString(),
					PositionX = (int)_item.GlobalPosition.X,
					PositionY = (int)_item.GlobalPosition.Y,
					Data = (_item is IStructureData) ? (_item as IStructureData).GetData() : null
				};
			}

			return Data;
		}
		public Savefile Default() => new();
	}

    public override void _EnterTree()
	{
		Current = this;

		// save data setup
		SaveModule = new(Resort + "-resort", new ResortDataModule(), 1000)
		{
			Extension = SaveSystem.SAVEFILE_EXTENSION,
			RelativePath = SaveSystem.PROFILERESORTS_DIR
		};
		SaveSystem.ProfileClassData.Add(SaveModule);

		// music loop handling
		static void FinishedSignal(GlobalManager.SceneName _)
		{
			SoundManager.MusicPlayer.Finished -= CheckSongToPlay;
			GlobalManager.OnPreLoadScene -= FinishedSignal;
		}
		;
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
		string[] _raw_files = DirAccess.GetFilesAt(GlobalManager.ABSOLUTE_SFX_PATH + "music");

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
		Aphid _aphid = (ResourceLoader.Load(GlobalManager.APHID_ENTITY) as PackedScene).Instantiate() as Aphid;

		_aphid.Instance = _instance;
		_aphid.GlobalPosition = new(_instance.Status.PositionX, _instance.Status.PositionY);
		_instance.Entity = _aphid;

		Current.AddChild(_aphid);
		Current.Aphids.Add(_aphid);

		if (GameManager.APPLY_OUTOFBOUND_PATCH)
		{
			float _x = _aphid.GlobalPosition.X, _y = _aphid.GlobalPosition.Y,
				_xtp = FieldManager.Instance.TopLeft.GlobalPosition.X, _ytp = FieldManager.Instance.TopLeft.GlobalPosition.Y,
				_xbr = FieldManager.Instance.BottomRight.GlobalPosition.X, _ybr = FieldManager.Instance.BottomRight.GlobalPosition.Y;
			if (_x < _xtp || _x > _xbr ||
					_y < _ytp || _y > _ybr )
				_aphid.GlobalPosition = new();
			else
			{
				PhysicsRayQueryParameters2D _query = new()
				{
					HitFromInside = false,
					From = _aphid.GlobalPosition
				};
				Vector2[] _list = [
					new(-20, -20), new(0, -20), new(20, -20),
					new(-20, 0), /* Center */ new(20, 0),
					new(-20, 20), new(0, 20), new(20, 20)
				];

				for (int i = 0; i < 8; i++)
				{
					_query.To = _aphid.GlobalPosition + _list[i];
					var _hit = GlobalManager.Utils.Raycast(_query);
					if (_hit.Count == 0 || !_hit.ContainsKey("collision"))
						continue;
					var _collision = _hit["collision"].ToString();
					if (!_collision.Contains("ground") || !_collision.Contains("wall"))
						continue;

					_aphid.GlobalPosition = new();
					Logger.Print(Logger.LogPriority.Info, $"ResortManager: Applied OUTOFBOUND patch to {_aphid.Instance.Genes.Name}");
				}
			}
			Logger.Print(Logger.LogPriority.Info, "ResortManager: OUTOFBOUND patch finalized.");
		}

		_aphid.SetReady();
		return _aphid;
	}
	/// <summary>
	/// Creates a new aphid and adds it to the current savefile.
	/// </summary>
	/// <returns>The newly created aphid.</returns>
	public static Aphid CreateAphid(Vector2 _position, AphidData.Genes _genes)
	{
		Aphid _aphid = (ResourceLoader.Load(GlobalManager.APHID_ENTITY) as PackedScene).Instantiate() as Aphid;

		_aphid.Instance = new(Guid.NewGuid())
		{
			Entity = _aphid,
			Genes = _genes
		};
		_aphid.GlobalPosition = _position;
		_aphid.Instance.Status.HomeResort = Current.Resort;

		GameManager.AddAphid(_aphid.Instance);
		Current.AddChild(_aphid);
		Current.Aphids.Add(_aphid);
		_aphid.SetReady();
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
		string _path = $"{GlobalManager.ABSOLUTE_ITEMS_DB_PATH}/{_item_name}.tscn";
		if (ResourceLoader.Exists(_path))
			// this is used if an item has a more complex structure or contains extra data, thus needing an unique node
			_item = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		else
		{
			_item = ResourceLoader.Load<PackedScene>(GlobalManager.ITEM_ENTITY).Instantiate() as Node2D;
			(_item.GetChild(0) as Sprite2D).Texture = GlobalManager.GetIcon(_item_name);
		}

		_item.SetMeta(StringNames.PickupMeta, true);
		_item.SetMeta(StringNames.IdMeta, _item_name);
		_item.SetMeta(StringNames.TagMeta, GlobalManager.G_ITEMS[_item_name].tag);
		_item.GlobalPosition = _position;

		Current.EntityRoot.AddChild(_item);
		return _item;
	}
	public static Node2D CreateStructure(string _id, Vector2 _position, string _data = null)
	{
		string _path = GlobalManager.ABSOLUTE_STRUCTURES_DB_PATH + $"/{_id}.tscn";
		if (!ResourceLoader.Exists(_path))
		{
			if (string.IsNullOrEmpty(_id))
				Logger.Print(Logger.LogPriority.Error, $"ResortManager: This id does not exist!");
			else
				Logger.Print(Logger.LogPriority.Error, $"ResortManager: {_id} is not a valid id.");
			return null;
		}

		Node2D _structure = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		_structure.GlobalPosition = _position;
		_structure.SetMeta(StringNames.IdMeta, _id);
		Current.StructureRoot.AddChild(_structure);

		try
		{
			if (_data != null && _structure is IStructureData)
				(_structure as IStructureData).SetData(_data);
		}
		catch (Exception _error)
		{
			Logger.Print(Logger.LogPriority.Error, "ResortManager: Failed to execute structure fn of " + _id, _error);
		}
		return _structure;
	}

	public interface IStructureData
	{
		public void SetData(string _data);
		public string GetData();
	}
}