using Godot;

public partial class TVScreenBehaviour : Sprite2D, SaveSystem.IDataModule
{
	[Export] private InteractableArea2D changeChannelButton, powerButton;
	[Export] private Light2D light;
	[Export] private Node2D contentAnchor;

	private bool active;
	private int channelIndex;
	private Node2D channelContent;
	private readonly ITVChannel[] catalog = [
		new AphidKidzChannel(),
		new ProphecyChannel(),
		new HolidayChannel(),
		new VideoPlayerChannel(),
	];

	public override void _EnterTree()
	{
		changeChannelButton.OnInteractOnly.Add(SwitchChannels);
		powerButton.OnInteractOnly.Add(SetActiveState);
	}
	public override void _PhysicsProcess(double delta)
	{
		if (active)
			catalog[channelIndex].Process((float)delta);
	}

	// TV Controls
	private void SwitchChannels()
	{
		if (!active)
			return;
		channelIndex++;
		if (channelIndex >= catalog.Length)
			channelIndex = 0;
		ViewChannel();
	}
	private void SetActiveState()
	{
		active = !active;
		light.Enabled = active;
		ViewChannel();
	}
	private void ViewChannel()
    {
		channelContent?.QueueFree();
		string _path = active ? catalog[channelIndex].ChannelContentPath : "uid://dka1ip2cyn76r";
		Color _screenColor = active ? catalog[channelIndex].ScreenColor : new();
		light.Color = _screenColor;
        channelContent = ResourceLoader.Load<PackedScene>(_path).Instantiate() as Node2D;
		contentAnchor.AddChild(channelContent);

		if (active)
			catalog[channelIndex].Start(channelContent);
    }

	// Save Data
	public void Set(string _data)
	{
		string[] _dataList = _data.Split(',');
		if (_dataList.Length <= 1)
			return;
		channelIndex = int.Parse(_dataList[0]);
		active = bool.Parse(_dataList[1]);

		if (active)
			ViewChannel();
	}
	public string Get()
	{
		return $"{channelIndex},{active}";
	}

	// Channels
	public interface ITVChannel
	{
		public Color ScreenColor { get; }
		public string ChannelContentPath { get; }

		public void Start(Node2D content);
		public void Process(float delta);
	}

	public class AphidKidzChannel : ITVChannel
	{
		public Color ScreenColor { get; } = new("green");
		public string ChannelContentPath { get => "uid://btetrcmn3t8t"; }

		public void Start(Node2D content)
        {
            return;
        }
        public void Process(float delta)
        {
            return;
        }
    }
	public class VideoPlayerChannel : ITVChannel
	{
		public Color ScreenColor { get; } = new("darkblue");
		public string ChannelContentPath { get => "uid://dgvc22xpcl31v"; }

		private int maxDistanceSquared = 400 * 400;
		private VideoStreamPlayer videoplayer;
		private Node2D node;

		public void Start(Node2D content)
        {
			node = content;
           	videoplayer = content.GetChild<VideoStreamPlayer>(0);
        }
		public void Process(float delta)
		{
			float _distance = node.GlobalPosition.DistanceSquaredTo(Player.Instance.GlobalPosition);
			videoplayer.VolumeDb = -20 - (80 * (_distance / maxDistanceSquared));
		}
	}
	public class ProphecyChannel : ITVChannel
	{
		public Color ScreenColor { get; } = new("darkblue");
		public string ChannelContentPath { get => "uid://d2xlcl4arbiqg"; }

		public void Start(Node2D content)
        {
            return;
        }

        public void Process(float delta)
        {
            return;
        }
    }
	public class HolidayChannel : ITVChannel
	{
		public Color ScreenColor { get; } = new("coral");
		public string ChannelContentPath { get => "uid://cnaebt5rh285e"; }

		public void Start(Node2D content)
        {
            return;
        }

        public void Process(float delta)
        {
            return;
        }  
    }
}
