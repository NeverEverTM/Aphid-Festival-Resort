using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class LeafLoadScreen : LoadScreen
{
	// Process Params
	private readonly List<Vector2> virtual_nodes = [];
	private readonly List<Leaf> leaf_nodes = [];

	// Customizable Params
	[Export] public int spacing = 260, scale = 11;
	
	[Export] public Curve curve;
	[Export] public PackedScene leaf;

	private struct Leaf
	{
		public Control entity;
		public Vector2 position_start;
		public Vector2 position_final;
		public int rotation_final;
	}

	public override async Task RunIN()
	{
		// Generate grid of virtual nodes
		for (int x = 0; x < CameraManager.SCREEN_SIZE_CANVAS.X + spacing; x += spacing)
		{
			for (int y = 0; y < CameraManager.SCREEN_SIZE_CANVAS.Y + spacing; y += spacing)
				virtual_nodes.Add(new(x, y));
		}

		// Spawn all the leaves above the screen and prepare their positions
		for (int i = 0; i < virtual_nodes.Count; i++)
		{
			Control _leaf = leaf.Instantiate() as Control;
			_leaf.GlobalPosition = GlobalManager.Utils.GetRandomVector_X(0, (int)CameraManager.SCREEN_SIZE_CANVAS.X, -600);
			_leaf.Scale = new(scale, scale);
			_leaf.ZIndex = GD.RandRange(0, 1000);
			AddChild(_leaf);
			leaf_nodes.Add(new()
			{
				entity = _leaf,
				position_start = _leaf.GlobalPosition,
				position_final = virtual_nodes[i],
				rotation_final = GD.RandRange(-3, 3)
			});
		}

		SoundManager.CreateSound("ui/leaves");
		await StartAnim();
	}
	public override async Task RunOUT()
	{
		for (int i = 0; i < leaf_nodes.Count; i++)
		{
			var _leaf = leaf_nodes[i];
			_leaf.position_start = _leaf.entity.GlobalPosition;
			_leaf.position_final = GlobalManager.Utils.GetRandomVector_Y(0, CameraManager.SCREEN_SIZE_CANVAS.Y, -600);
			_leaf.rotation_final = GD.RandRange(-3, 3);
			leaf_nodes[i] = _leaf;
		}
		await StartAnim();
		QueueFree();
	}

	protected override Task Tick(float _progress)
	{
		// timer shenanigans
		float _lerp = curve.Sample(_progress / timer);
		// Lerp leaf nodes
		for (int i = 0; i < leaf_nodes.Count; i++)
		{
			Leaf _leaf = leaf_nodes[i];
			_leaf.entity.Rotation = Mathf.Lerp(0, _leaf.rotation_final, _lerp);
			_leaf.entity.GlobalPosition = _leaf.position_start.Lerp(_leaf.position_final, _lerp);
		}
		return Task.CompletedTask;
	}
}