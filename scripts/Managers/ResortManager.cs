using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using System.Text.Json;

public partial class ResortManager : Node2D, SaveSystem.ISaveData
{
	[Export] public string Resort;
	[Export] public PackedScene aphidPrefab;
	[Export] public Node2D EntityRoot;

	public static ResortManager Instance;
	public static bool IsNewGame;
	public static readonly List<Aphid> AphidsList = new();

	public string CLASS_ID => class_id_dummy;
	private string class_id_dummy;
	public static Savefile Data = new();

	public class Savefile
	{
		public GroundItem[] Items { get; set; }
		public struct GroundItem
		{
			public int PositionX { get; set; }
			public int PositionY { get; set; }
			public string Id { get; set; }
		}
	}
	public string SaveData()
	{
		// Save items in the ground
		int _count = Instance.EntityRoot.GetChildCount();
		Data.Items = new Savefile.GroundItem[_count];
		for (int i = 0; i < _count; i++)
		{
			Node2D _item = Instance.EntityRoot.GetChild(i) as Node2D;
			Data.Items[i] = new()
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

		// load items in the ground
		for (int i = 0; i < Data.Items.Length; i++)
			CreateItem(Data.Items[i].Id, new(Data.Items[i].PositionX, Data.Items[i].PositionY));

		return Task.CompletedTask;
	}
	public Task SetData()
	{
		Data = new();
		return Task.CompletedTask;
	}

	public override void _EnterTree()
	{
		Instance = this;
		AphidsList.Clear();
		class_id_dummy = Resort + "-resort_data";
		SaveSystem.AddToProfileData(Instance);
	}
	public override async void _Ready()
	{
		// On New game, put intro cutscene, otherwise just load normally
		await SaveSystem.SetProfileData();
		if (!IsNewGame)
			await SaveSystem.LoadProfileData();
		else
		{
			IsNewGame = false;
			while (GameManager.IsBusy)
				await Task.Delay(1);
			await Task.Delay(200);
			await DialogManager.OpenDialog(new string[] { "welcome_0", "welcome_1" }, "dev");
			Player.Instance.StoreItem("aphid_egg");
			Player.Instance.StoreItem("aphid_egg");
			Player.Instance.StoreItem("aphid_egg");
			await SaveSystem.SaveProfileData();
		}

		// Set Resort music loop
		SoundManager.MusicPlayer.Finished += CheckSongToPlay;
		static void FinishedSignal()
		{
			SoundManager.MusicPlayer.Finished -= CheckSongToPlay;
			GameManager.OnPreLoadScene -= FinishedSignal;
		};
		GameManager.OnPreLoadScene += FinishedSignal;
		CheckSongToPlay();
	}

	public static async void CheckSongToPlay()
	{
		await Task.Delay(1000);
		string[] _raw_files = DirAccess.GetFilesAt(GameManager.MusicPath);
		// for some reason, the exported project cannot get access to "music.mp3" files
		// using DirAccess.GetFilesAt(), it will only find "music.mp3.imported" ones and return those
		// however for some WEIRD reason, if you just reference it anyways by trimming the ".import"
		// it will find the supposedly non-existent .mp3 file
		string _file = _raw_files[GameManager.RNG.RandiRange(0, _raw_files.Length - 1)].TrimSuffix(".import");
		SoundManager.PlaySong(_file);
	}

	// =========| Object Creation |===============
	public static Aphid CreateAphid(AphidInstance _instance)
	{
		Aphid _aphid = Instance.aphidPrefab.Instantiate() as Aphid;

		_aphid.Instance = _instance;
		_aphid.GlobalPosition = new(_instance.Status.PositionX, _instance.Status.PositionY);
		_instance.Entity = _aphid;

		Instance.AddChild(_aphid);
		AphidsList.Add(_aphid);
		return _aphid;
	}
	/// <summary>
	/// Creates a new aphid and adds it to the current savefile.
	/// </summary>
	/// <returns>The newly created aphid.</returns>
	public static Aphid CreateNewAphid(Vector2 _position, AphidData.Genes _genes)
	{
		var _aphid = Instance.aphidPrefab.Instantiate() as Aphid;

		_aphid.Instance = new()
		{
			Entity = _aphid,
			Genes = _genes
		};
		_aphid.GlobalPosition = _position;

		SaveSystem.AddAphidInstance(_aphid.Instance);
		Instance.AddChild(_aphid);
		AphidsList.Add(_aphid);
		return _aphid;
	}
	public static Node2D CreateItem(string _item_name, Vector2 _position)
	{
		Node2D _item;
		string _path = $"{GameManager.ItemPath}/{_item_name}.tscn";
		if (ResourceLoader.Exists(_path))
			// this is used if an item has a more complex structure or contains extra data, thus needing an unique node
			_item = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		else
		{
			// create a new item dynamically
			_item = new RigidBody2D(){
				GravityScale = 0
			};
			_item.AddChild(new CollisionShape2D()
			{
				Shape = new CircleShape2D()
				{
					Radius = 5
				}
			});
			_item.AddChild(new Sprite2D()
			{
				Texture = GameManager.G_ICONS[GameManager.G_ICONS.ContainsKey(_item_name) ? _item_name : "missing"]
			});
		}

		_item.SetMeta("pickup", true);
		_item.SetMeta("id", _item_name);
		_item.SetMeta("tag", GameManager.G_ITEMS[_item_name].tag);
		_item.GlobalPosition = _position;
		_item.ZIndex = (int)_item.GlobalPosition.Y;

		Instance.EntityRoot.AddChild(_item);
		return _item;
	}
}
