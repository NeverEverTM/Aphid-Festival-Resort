using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class LeafLoadScreen : LoadScreen
{
	// Process Params
	private readonly List<Vector2> virtual_nodes = [];
	private readonly List<Leaf> leaf_nodes = [];
	private float progress;

	// Customizable Params
	[Export] public int spacing = 260, scale = 11;
	[Export] public float timer = 2;
	[Export] public Curve curve;
	[Export] public PackedScene leaf;

	private struct Leaf
	{
		public Control entity;
		public Vector2 position_start;
		public Vector2 position_final;
		public int rotation_final;
	}

	public override void _EnterTree()
	{
		SetProcess(false);
	}


	public override async Task Start()
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

		progress = 0;
		SoundManager.CreateSound("ui/leaves");
		SetProcess(true);
		while (!IsDone)
		{
			await Task.Delay(1);
		}
		await Task.Delay(1);
	}
	public override async Task Finish()
	{
		IsDone = false;
		for (int i = 0; i < leaf_nodes.Count; i++)
		{
			var _leaf = leaf_nodes[i];
			_leaf.position_start = _leaf.entity.GlobalPosition;
			_leaf.position_final = GlobalManager.Utils.GetRandomVector_Y(0, CameraManager.SCREEN_SIZE_CANVAS.Y, -600);
			_leaf.rotation_final = GD.RandRange(-3, 3);
			leaf_nodes[i] = _leaf;
		}
		progress = 0;

		while (!IsDone)
		{
			await Task.Delay(1);
		}
		QueueFree();
	}

	public override void _Process(double delta)
	{
		if (IsDone)
			return;

		// timer shenanigans
		if (progress < timer)
		{
			progress += (float)delta;
			float _lerp = curve.Sample(progress / timer);
			// Lerp leaf nodes
			for (int i = 0; i < leaf_nodes.Count; i++)
			{
				Leaf _leaf = leaf_nodes[i];
				_leaf.entity.Rotation = Mathf.Lerp(0, _leaf.rotation_final, _lerp);
				_leaf.entity.GlobalPosition = _leaf.position_start.Lerp(_leaf.position_final, _lerp);
			}
		}
		else
			IsDone = true;
	}
}

public partial class LoadScreen : CanvasLayer
{
	public bool IsDone { get; set; }
	public virtual Task Start()
	{
		return Task.CompletedTask;
	}
	public virtual Task Finish()
	{
		return Task.CompletedTask;
	}
}