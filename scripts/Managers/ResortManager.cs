using Godot;

public partial class ResortManager : Node2D
{
	[Export] public PackedScene aphidPrefab;

	public static ResortManager Instance;
	public static bool IsNewGame;

    public override void _EnterTree() =>
		Instance = this;

    public override async void _Ready()
	{
		if (!IsNewGame)
			await SaveSystem.LoadProfile();
		else
			IsNewGame = false;
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
		Node2D _item = ResourceLoader.Load<PackedScene>($"{GameManager.ItemPath}/{_item_name}.tscn").Instantiate() as Node2D;
		_item.SetMeta("id", _item_name);
		Instance.AddChild(_item);
		return _item;
	}
}
