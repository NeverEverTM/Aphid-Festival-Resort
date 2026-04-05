using System;
using System.Collections.Generic;
using Godot;

public partial class ResortManager : Node2D
{
	/// <summary>
	/// The name of this resort.
	/// </summary>
	[Export] public string Resort;
	[Export] public Node2D EntityRoot, StructureRoot, SpawnPoint;
	[ExportGroup("Inmutables")]
	[Export] private PackedScene aphidEntity;

	/// <summary>
	/// Access local aphids within this resort. To access aphids across all resorts, use GameManager.Aphids instead.
	/// </summary>
	public readonly List<Aphid> Aphids = [];
	public SaveSystem.SaveModule<Savefile> SaveModule;
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
					DebugLogger.Print(DebugLogger.LogPriority.Error, $"ResortManager: The object {_item.Name}({_item.GetClass()}) did not have a valid id.");
					continue;
				}

				Data.Items[i] = new()
				{
					Id = _item.GetMeta(StringNames.IdMeta).ToString(),
					PositionX = (int)_item.GlobalPosition.X,
					PositionY = (int)_item.GlobalPosition.Y,
					Data = (_item is SaveSystem.IGenericDataModule) ? (_item as SaveSystem.IGenericDataModule).Get() : null
				};
			}

			Data.Structures = new Savefile.Structure[Current.StructureRoot.GetChildCount()];
			for (int i = 0; i < Current.StructureRoot.GetChildCount(); i++)
			{
				Node2D _item = Current.StructureRoot.GetChild(i) as Node2D;

				if (!_item.HasMeta(StringNames.IdMeta))
				{
					DebugLogger.Print(DebugLogger.LogPriority.Error, $"ResortManager: The object {_item.Name}({_item.GetClass()}) did not have a valid id.");
					continue;
				}

				Data.Structures[i] = new()
				{
					Id = _item.GetMeta(StringNames.IdMeta).ToString(),
					PositionX = (int)_item.GlobalPosition.X,
					PositionY = (int)_item.GlobalPosition.Y,
					Data = (_item is SaveSystem.IGenericDataModule) ? (_item as SaveSystem.IGenericDataModule).Get() : null
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
		SaveModule = new(Resort + "-resort", new ResortDataModule(), 1337)
		{
			Extension = SaveSystem.SAVEFILE_EXTENSION,
			RelativePath = SaveSystem.PROFILE_RESORTS_DIR,
			DisposeMode = SaveSystem.SaveMetadata.DisposeMethod.OnRoomTransition 
		};
		SaveModule.AddEventListener(OnLoadFinish, SaveSystem.SaveEventsEnum.OnLoadFinish);
		SaveSystem.AddSaveModule(SaveModule);
	}

	private void OnLoadFinish(SaveSystem.SaveEventArgs _)
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
					&& _pair.Value.Status.Mode != AphidData.EntityStatusType.Busy)
				SpawnAphid(_pair.Value);
		}
	}

	// =========| Object Creation |===============
	public static Aphid SpawnAphid(AphidInstance _instance)
	{
		_instance.Status.Mode = AphidData.EntityStatusType.Active;
		Aphid _aphid = Current.aphidEntity.Instantiate() as Aphid;

		_aphid.Instance = _instance;
		_aphid.GlobalPosition = new(_instance.Status.PositionX, _instance.Status.PositionY);
		_instance.Entity = _aphid;

		Current.AddChild(_aphid);
		Current.Aphids.Add(_aphid);

		if (GameManager.APPLY_OUTOFBOUND_PATCH)
		{
			if (GameManager.IsOutOfBounds(_aphid.GlobalPosition) || GameManager.IsInsideGeometry(_aphid.GlobalPosition))
			{
				_aphid.GlobalPosition *= 0.1f;
				DebugLogger.Print(DebugLogger.LogPriority.Info, $"ResortManager: Applied OUTOFBOUND patch to {_aphid.Instance.Genes.Name}");
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
			DebugLogger.Print(DebugLogger.LogPriority.Error, "ResortManager: Item name is null!");
			return null;
		}

		Node2D _item;
		string _path = GlobalManager.ABSOLUTE_ITEMS_DB_PATH + _id + ".tscn";

		if (ResourceLoader.Exists(_path)) // has a template already made
			_item = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		else // create a new item from scratch
		{
			_item = ResourceLoader.Load<PackedScene>(GlobalManager.ITEM_ENTITY).Instantiate() as Node2D;
			(_item.GetChild(0) as Sprite2D).Texture = GlobalManager.GetIcon(_id);
		}

		ItemData _itemData = GlobalManager.G_ITEMS[_id];
		_item.SetMeta(StringNames.PickupMeta, true);
		_item.SetMeta(StringNames.IdMeta, _id);
		_item.SetMeta(StringNames.TagMeta, (int)(GlobalManager.G_FOOD.ContainsKey(_id) ? 
				StringNames.GlobalTags.Food : StringNames.GlobalTags.Item));
		_item.GlobalPosition = _position;
		Current.EntityRoot.AddChild(_item);

		try
		{
			if (_item is SaveSystem.IGenericDataModule)
			{
				var _itemMetadata = _item as SaveSystem.IGenericDataModule;
				if (!string.IsNullOrWhiteSpace(_data))
					_itemMetadata.Set(_data);
				else
					_itemMetadata.Default();
			}
		}
		catch (Exception _error)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "ResortManager: Failed to execute item fn of " + _id, _error);
		}

		return _item;
	}
	public static Node2D CreateStructure(string _id, Vector2 _position, string _data = null)
	{
		string _path = GlobalManager.ABSOLUTE_STRUCTURES_DB_PATH + $"/{_id}.tscn";
		if (!ResourceLoader.Exists(_path))
		{
			if (string.IsNullOrWhiteSpace(_id))
				DebugLogger.Print(DebugLogger.LogPriority.Error, $"ResortManager: Invalid string id!");
			else
				DebugLogger.Print(DebugLogger.LogPriority.Error, $"ResortManager: {_id} is not a valid id.");
			return null;
		}

		Node2D _structure = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		_structure.GlobalPosition = _position;
		_structure.SetMeta(StringNames.IdMeta, _id);
		Current.StructureRoot.AddChild(_structure);

		try
		{
			if (_structure is SaveSystem.IGenericDataModule)
			{
				var _structureMetadata = _structure as SaveSystem.IGenericDataModule;
				if (!string.IsNullOrWhiteSpace(_data))
					_structureMetadata.Set(_data);
				else
					_structureMetadata.Default();
			}
		}
		catch (Exception _error)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "ResortManager: Failed to execute structure fn of " + _id, _error);
		}
		return _structure;
	}
}