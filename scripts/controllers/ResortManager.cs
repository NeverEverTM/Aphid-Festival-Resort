using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ResortManager : Node2D
{
	/// <summary>
	/// The name of this resort.
	/// </summary>
	[Export] public string Resort;
	[Export] public Node2D EntityRoot, StructureRoot, SpawnPoint;
	private static PackedScene aphidEntity;

	/// <summary>
	/// Access local aphids within this resort. To access aphids across all resorts, use GameManager.Aphids instead.
	/// </summary>
	public readonly List<Aphid> Aphids = [];
	private SaveSystem.SaveModule<Savefile> SaveModule;
	/// <summary>
	/// Currently running instance/singleton of a resort. This instance will reference the last resort it had to load.
	/// </summary>
	public static ResortManager Current { get; private set; }
	public static Savefile Data { get; set; }

	public record Savefile
	{
		public Item[] Items { get; set; }
		public Structure[] Structures { get; set; }
		public struct Item
		{
			public int PositionX { get; set; }
			public int PositionY { get; set; }
			public string Id { get; set; }
			public string Data { get; set; }
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
		public void Set(Savefile _data)
		{
			Data = _data;
			if (!GameManager.IsNewGame)
				Load();
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
					PositionY = (int)_item.GlobalPosition.Y,
					Data = (_item is IMetadata) ? (_item as IMetadata).GetData() : null
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
					Data = (_item is IMetadata) ? (_item as IMetadata).GetData() : null
				};
			}

			return Data;
		}
		public Savefile Default() => new();
	}

	public override async void _EnterTree()
	{
		Current = this;
		if (!IsInstanceValid(aphidEntity))
			aphidEntity = await GlobalManager.PRELOAD_RESOURCE(GlobalManager.APHID_ENTITY) as PackedScene;

		// save data setup
		SaveModule = new(Resort + "-resort", new ResortDataModule(), 1000)
		{
			Extension = SaveSystem.SAVEFILE_EXTENSION,
			RelativePath = SaveSystem.PROFILE_RESORTS_DIR
		};
		SaveSystem.AddSaveModule(SaveModule);
	}
	public override void _ExitTree()
	{
		SaveSystem.RemoveSaveModule(SaveModule);
	}

	/// <summary>
	/// Loads current resort into the game.
	/// </summary>
	private static void Load()
	{
		// load items
		for (int i = 0; i < Data.Items?.Length; i++)
			CreateItem(Data.Items[i].Id, new(Data.Items[i].PositionX, Data.Items[i].PositionY), Data.Items[i].Data);

		// clean structure root from default objects
		for (int i = 0; i < Current.StructureRoot.GetChildCount(); i++)
			Current.StructureRoot.GetChild(i).QueueFree();

		// spawn structures
		for (int i = 0; i < Data.Structures?.Length; i++)
			CreateStructure(Data.Structures[i].Id, new(Data.Structures[i].PositionX, Data.Structures[i].PositionY), Data.Structures[i].Data);

		// spawn aphids
		foreach (KeyValuePair<Guid, AphidInstance> _pair in GameManager.Aphids)
		{
			string _resort = _pair.Value.Status.HomeResort;
			if (!string.IsNullOrEmpty(_resort) && _resort == Current.Resort
					&& _pair.Value.Status.Mode == AphidData.EntityStatus.Active)
				SpawnAphid(_pair.Value);
		}
	}

	// =========| Object Creation |===============
	public static Aphid SpawnAphid(AphidInstance _instance)
	{
		Aphid _aphid = aphidEntity.Instantiate() as Aphid;

		_aphid.Instance = _instance;
		_aphid.GlobalPosition = new(_instance.Status.PositionX, _instance.Status.PositionY);
		_instance.Entity = _aphid;

		Current.AddChild(_aphid);
		Current.Aphids.Add(_aphid);

		if (GameManager.APPLY_OUTOFBOUND_PATCH)
		{
			if (GameManager.IsOutOfBounds(_aphid.GlobalPosition) || GameManager.IsInsideGeometry(_aphid.GlobalPosition))
			{
				// an offset of -1000 is done here for the real center of the resort, though this could change in the future
				_aphid.GlobalPosition = _aphid.GlobalPosition * 0.1f + new Vector2(-1000, 0);
				Logger.Print(Logger.LogPriority.Info, $"ResortManager: Applied OUTOFBOUND patch to {_aphid.Instance.Genes.Name}");
			}
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
	public static Node2D CreateItem(string _id, Vector2 _position, string _data = null)
	{
		if (_id == null)
		{
			Logger.Print(Logger.LogPriority.Error, "ResortManager: Item name is null!");
			return null;
		}
		Node2D _item;
		string _path = $"{GlobalManager.ABSOLUTE_ITEMS_DB_PATH}/{_id}.tscn";
		if (ResourceLoader.Exists(_path))
			// this is used if an item has a more complex structure or contains extra data, thus needing an unique node
			_item = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		else
		{
			_item = ResourceLoader.Load<PackedScene>(GlobalManager.ITEM_ENTITY).Instantiate() as Node2D;
			(_item.GetChild(0) as Sprite2D).Texture = GlobalManager.GetIcon(_id);
		}

		_item.SetMeta(StringNames.PickupMeta, true);
		_item.SetMeta(StringNames.IdMeta, _id);
		_item.SetMeta(StringNames.TagMeta, GlobalManager.G_ITEMS[_id].tag);
		_item.GlobalPosition = _position;
		Current.EntityRoot.AddChild(_item);
		
		try
		{
			if (_data != null && _item is IMetadata)
				(_item as IMetadata).SetData(_data);
		}
		catch (Exception _error)
		{
			Logger.Print(Logger.LogPriority.Error, "ResortManager: Failed to execute item fn of " + _id, _error);
		}

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
			if (_data != null && _structure is IMetadata)
				(_structure as IMetadata).SetData(_data);
		}
		catch (Exception _error)
		{
			Logger.Print(Logger.LogPriority.Error, "ResortManager: Failed to execute structure fn of " + _id, _error);
		}
		return _structure;
	}

	/// <summary>
	/// Allows to gather/set metadata to an object. Used for structures and items.
	/// </summary>
	public interface IMetadata
	{
		public void SetData(string _data);
		public string GetData();
	}
}