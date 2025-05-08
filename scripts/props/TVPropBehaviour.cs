using Godot;

public partial class TVPropBehaviour : Sprite2D, Player.IInteractEvent, ResortManager.IStructureData
{
	[Export] private Texture2D[] backgrounds;
	[Export] private Sprite2D viewport_display, background_display;
	[Export] private Light2D light;
	[Export] private AudioStreamPlayer2D stereo;
	[Export] private VideoStreamPlayer player;

	private int channel;

	public void SetData(string _data)
	{
		player.VolumeDb = -80; // keep it quiet during startup
		channel = int.Parse(_data) - 1;
		Interact();
	}
    public string GetData() =>
		channel.ToString();

    public void Interact()
	{
		channel++;
		if (channel == 3)
			channel = 0;

		if (channel == 0)
		{
			player.Stop();

			background_display.Texture = backgrounds[0];
			light.Enabled = false;
		}
		else if (channel == 1)
		{
			light.Enabled = true;
			light.Color = new("green");
			background_display.Texture = backgrounds[1];
			viewport_display.Show();
			stereo.Play();
		}
		else if (channel == 2)
		{
			stereo.Stop();
			viewport_display.Hide();

			player.Play();
			light.Color = new("darkblue");
		}
	}
    public override void _PhysicsProcess(double delta)
    {
        if (channel == 2)
		{
			float _distance = GlobalPosition.DistanceSquaredTo(Player.Instance.GlobalPosition);
			player.VolumeDb = -20 - (80 * (_distance / 160000));
		}    
	}
}
