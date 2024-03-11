using System.Threading.Tasks;
using Godot;

public partial class ResortManager : Node2D
{
	[Export] public PackedScene aphidPrefab;
	[Export] public Node2D ItemGroundRoot;

	public static ResortManager Instance;
	public static SaveData Data = new(); 
	public static bool IsNewGame;

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

    public override void _EnterTree() =>
		Instance = this;

    public override async void _Ready()
	{
		if (!IsNewGame)
			await SaveSystem.LoadProfile();
		else
		{
			IsNewGame = false;
			while (GameManager.IsBusy)
				await Task.Delay(1);
			await Task.Delay(100);
			await DialogManager.OpenDialog(new string[] { "welcome_0", "welcome_1" });
		}
	}

	// Spawning Commands, Exlcusive for the Resort since they control sensitive data
	public static Aphid CreateAphid(AphidInstance _instance)
	{
		var _aphid = Instance.aphidPrefab.Instantiate() as Aphid;

		_aphid.Instance = _instance;
		_instance.Entity = _aphid;

		_aphid.GlobalPosition = new(_instance.Status.PositionX, _instance.Status.PositionY);

		Instance.AddChild(_aphid);
		return _aphid;
	}

	public static Node2D CreateItem(string _item_name)
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

		Instance.ItemGroundRoot.AddChild(_item);
		return _item;
	}
}
