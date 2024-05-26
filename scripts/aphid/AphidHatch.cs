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

		hatch.Timeout += () =>
		{
			if (IsNatural)
				HatchNatural();
			else
				HatchDefault();
		};
		hatch.Start(5);
	}

	private void HatchDefault()
	{
		ResortManager.CreateNewAphid(new(GlobalPosition.X, GlobalPosition.Y),
			new()
			{
				AntennaColor = colors[0],
				EyeColor = colors[1],
				BodyColor = colors[2],
				LegColor = colors[3]
			}
		);
		QueueFree();
	}
	private void HatchNatural()
	{
		ResortManager.CreateNewAphid(new(GlobalPosition.X, GlobalPosition.Y), naturalGenes);
		QueueFree();
	}
}
