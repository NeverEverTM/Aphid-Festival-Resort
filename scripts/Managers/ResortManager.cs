using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ResortManager : Node2D
{
	[Export] public PackedScene aphidPrefab;
	[Export] public Node2D ItemGroundRoot;

	[ExportGroup("Music")]
	[Export] private AudioStream day_0;

	public static ResortManager Instance;
	public static bool IsNewGame;
	public static readonly List<Aphid> AphidsList = new();

	public SaveData Data = new(); 

	public class SaveData
	{
		public GroundItem[] Items { get; set; }
		public struct GroundItem
		{
			public Vector2 Position {get; set;}
			public string Id {get;set;} 
		}
		public Task SaveGroundItems()
		{
			int _count = Instance.ItemGroundRoot.GetChildCount();
			GroundItem[] _list = new GroundItem[_count];
			for (int i = 0; i < _count; i++)
			{
				Node2D _item = Instance.ItemGroundRoot.GetChild(i) as Node2D;
				_list[i] = new()
				{
					Id = _item.GetMeta("id").ToString(),
					Position = _item.GlobalPosition
				};
			}
			return Task.CompletedTask;
		}
	}

    public override void _EnterTree()
	{
		Instance = this;
		AphidsList.Clear();
	}

    public override async void _Ready()
	{
		if (!IsNewGame)
			await SaveSystem.LoadProfile();
		else
		{
			IsNewGame = false;
			SaveSystem.aphids.Clear();
			while (GameManager.IsBusy)
				await Task.Delay(1);
			await Task.Delay(200);
			await DialogManager.OpenDialog(new string[] { "welcome_0", "welcome_1" }, "dev");
		}
		SoundManager.PlaySong(day_0);
	}

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
		string _path = $"{GameManager.ItemPath}/{_item_name}.tscn";
		if (!ResourceLoader.Exists(_path))
		{
			GD.PrintErr($"{_item_name} is not a valid .tscn in the folder");
			return null;
		}

		Node2D _item = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		_item.SetMeta("id", _item_name);
		_item.SetMeta("tag", GameManager.G_ITEMS[_item_name].tag);
		_item.GlobalPosition = _position;
		_item.ZIndex = (int)_item.GlobalPosition.Y;

		Instance.ItemGroundRoot.AddChild(_item);
		return _item;
	}
}
