using Godot;

public partial class TVPropBehaviour : Sprite2D
{
	[Export] private Texture2D[] backgrounds;
	[Export] private Sprite2D viewport_display, background_display;
	[Export] private Light2D light;
	[Export] private AudioStreamPlayer2D stereo;
	[Export] private VideoStreamPlayer player;

	private bool is_connected = false;
	private int channel;

	private void Interact()
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
}
