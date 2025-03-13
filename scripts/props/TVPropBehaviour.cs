using Godot;

public partial class TVPropBehaviour : Sprite2D
{
	[Export] private Texture2D[] backgrounds;
	[Export] private Sprite2D viewport_display, background_display;
	[Export] private Light2D light;
	[Export] private AudioStreamPlayer2D stereo;

	private bool is_connected = false;

	private void Interact()
	{
		if (is_connected)
		{
			background_display.Texture = backgrounds[0];
			viewport_display.Hide();
			light.Enabled = false;
			stereo.Stop();
		}
		else
		{
			RandomNumberGenerator _rng = new();
			background_display.Texture = backgrounds[_rng.RandiRange(1, backgrounds.Length - 1)];
			viewport_display.Show();
			light.Enabled = true;
			stereo.Play();
		}
		is_connected = !is_connected;
	}
}
