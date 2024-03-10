using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class LoadScreen : CanvasLayer
{
	[Export] private PackedScene leaf;

	// Process Params
	private readonly List<Vector2> virtualNodes = new();
	private readonly List<Leaf> leafNodes = new();
	private Vector2 windowSize;
	public bool IsDone;

	// Customizable Params
	private int spacing = 280, scale = 10;
	private float[] points = new float[]
	{
		0,
		0.75f,
		1
	};

	public async Task CreateLeaves()
	{
		windowSize = GetViewport().GetVisibleRect().Size;
		// Generate grid of virtual nodes
		for (int x = 0; x < windowSize.X + spacing; x += spacing)
		{
			for (int y = 0; y < windowSize.Y + spacing; y += spacing)
				virtualNodes.Add(new(x, y));
		}

		// Spawn all the leaves above the screen and prepare their positions
		for (int i = 0; i < virtualNodes.Count; i++)
		{
            Control _leaf = leaf.Instantiate() as Control;
            _leaf.GlobalPosition = GameManager.Utils.GetRandomVector_X(0, (int)windowSize.X, -600);
			_leaf.Scale = new(scale,scale);
			_leaf.ZIndex = GameManager.RNG.RandiRange(0, 1000);
            AddChild(_leaf);
			leafNodes.Add(new()
			{
				entity = _leaf,
				position_start = _leaf.GlobalPosition,
				position_final = virtualNodes[i],
				rotation_final = GameManager.RNG.RandiRange(-5, 5)
			});
		}

		while(true)
		{
			await Task.Delay(1);
			if (IsDone)
				break;
		}
	}
	public async Task SweepLeaves()
	{
		windowSize = GetViewport().GetVisibleRect().Size;
		for (int i = 0; i < leafNodes.Count; i++)
		{
			var _leaf = leafNodes[i];
			_leaf.position_start = _leaf.entity.GlobalPosition;
			_leaf.position_final = GameManager.Utils.GetRandomVector_Y(0, windowSize.Y, -600);
			_leaf.rotation_final = GameManager.RNG.RandiRange(-5, 5);
			leafNodes[i] = _leaf;
		}
		IsDone = false;
		progress = 0;
		timer = 2;

		while(true)
		{
			await Task.Delay(1);
			if (IsDone)
				break;
		}
		QueueFree();
	}

	private float progress = 0, timer = 2;
    public override void _Process(double delta)
    {
		// timer shenanigans
		if (timer <= 0)
		{
			IsDone = true;
			return;
		}
		float _delta = (float)delta;
		timer -= _delta;
		progress += _delta * timer / 2;

		// get lerp between points
		var lerp = Mathf.Lerp(
			Mathf.Lerp(points[0], points[1], progress),
			Mathf.Lerp(points[1], points[2], progress),
			progress
		);

		// Lerp leaf nodes
        for (int i = 0; i < leafNodes.Count; i++)
		{
			var _leaf = leafNodes[i];
			_leaf.entity.Rotation = Mathf.Lerp(0, _leaf.rotation_final, progress);
			_leaf.entity.GlobalPosition = _leaf.position_start.Lerp(_leaf.position_final, lerp);
		}
    }

	private struct Leaf
	{
		public Control entity;
		public Vector2 position_start;
		public Vector2 position_final;
		public int rotation_final;
	}
}
