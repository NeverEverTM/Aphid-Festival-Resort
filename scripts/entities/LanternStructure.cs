using Godot;

public partial class LanternStructure : LightEntity
{
    [Export] private AnimatedSprite2D lantern;
    public override void LightIn(RoomInstance.TimeArgs _args)
	{
		base.LightIn(_args);
		lantern.Play("on");
        if (_args.Initialized && SceneManager.CurrentlyInGame)
            SoundManager.CreateSound2D("misc/fire_match", GlobalPosition);
	}
	public override void LightOut(RoomInstance.TimeArgs _args)
	{
		base.LightOut(_args);
		lantern.Play("default");
	}
}
