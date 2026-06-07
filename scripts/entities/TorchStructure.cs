using System.Threading.Tasks;
using Godot;

public partial class TorchStructure : LightEntity
{
	[Export] private AnimatedSprite2D torch, flame;

	public override void _Ready()
	{
		torch.Play(StringNames.DefaultAnim);
		torch.Frame = RNG.RandiRange(0, 2);
		torch.FrameChanged += () =>
		{
			flame.Offset = new(torch.Frame == 1 ? -1 : (torch.Frame == 3 ? 1 : 0), 0);
		};
	}

	public override async void LightIn(RoomInstance.TimeArgs _args)
	{
		if (_args.Initialized)
			await Task.Delay(100 * RNG.RandiRange(0,30));
		base.LightIn(_args);
		if (_args.Initialized && SceneManager.CurrentlyInGame)
            SoundManager.CreateSound2D("misc/fire_match", GlobalPosition);
		flame.Show();
	}
	public override async void LightOut(RoomInstance.TimeArgs _args)
	{
		if (_args.Initialized)
			await Task.Delay(100 * RNG.RandiRange(0,30));
		base.LightOut(_args);
		flame.Hide();
	}
}
