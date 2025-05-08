using Godot;

public partial class AphidHatch : Area2D
{
	public bool IsNatural;
	private Timer hatch;

	// These are used for generic egg items
	[Export] private int[] parts_ids = new int[4];
	[Export] private Color[] colors = new Color[4];

	public AphidData.Genes given_genes = null;

	public override void _Ready()
	{
		hatch = new();
		hatch.Timeout += () =>
		{
			if (given_genes == null) // only generate new info if is a generic bought egg
			{
				given_genes = new()
				{
					AntennaColor = colors[0],
					EyeColor = colors[1],
					BodyColor = colors[2],
					LegColor = colors[3],
					AntennaType = parts_ids[0],
					EyeType = parts_ids[1],
					BodyType = parts_ids[2],
					LegType = parts_ids[3]
				};
				given_genes.GenerateNewAphid();
			}
			ResortManager.CreateAphid(new(GlobalPosition.X, GlobalPosition.Y), given_genes);
			QueueFree();
		};
		AddChild(hatch);
		hatch.Start(5);
	}
}
