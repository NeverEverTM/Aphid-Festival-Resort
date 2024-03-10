using Godot;

public partial class AphidHatch : RigidBody2D
{
	public bool IsNatural;
	private Timer hatch = new();

	// Default Hatch Params
	[Export] private int[] parts_ids = new int[4];
	[Export] private Color[] colors = new Color[4];

	// Natural Hatch Params
	public AphidData.Genes naturalGenes;

	public override void _Ready()
	{
		AddChild(hatch);
		if (IsNatural)
			hatch.Timeout += HatchNatural;
		else
			hatch.Timeout += HatchDefault;
		hatch.Start(5);
	}

	private void HatchDefault()
	{
		var _aphid = ResortManager.Instance.aphidPrefab.Instantiate() as Aphid;
		_aphid.GlobalPosition = new(GlobalPosition.X, GlobalPosition.Y);
		_aphid.Instance = new()
		{
			Entity = _aphid,
			Genes = new()
			{
				AntennaColor = colors[0],
				EyeColor = colors[1],
				BodyColor = colors[2],
				LegColor = colors[3]
			}
		};
		SaveSystem.AddAphidInstance(_aphid.Instance);
		ResortManager.Instance.AddChild(_aphid);
		QueueFree();
	}
	private void HatchNatural()
	{
		var _aphid = ResortManager.Instance.aphidPrefab.Instantiate() as Aphid;
		_aphid.GlobalPosition = new(GlobalPosition.X, GlobalPosition.Y);
		_aphid.Instance = new()
		{
			Entity = _aphid,
			Genes = naturalGenes
		};
		SaveSystem.AddAphidInstance(_aphid.Instance);
		ResortManager.Instance.AddChild(_aphid);
		QueueFree();
	}
}
