using Godot;

public partial class AphidHatch : RigidBody2D
{
	[Export] private Color[] colors = new Color[4];
	private Timer hatch = new();

    public override void _Ready()
    {
		AddChild(hatch);
        hatch.Timeout += () => 
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
		};
		hatch.Start(5);
    }
}
