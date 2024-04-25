// Dummy aphid behaviour for the main menu, we dont want em doing delicate stuff
public partial class FakeAphid : Aphid
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		OnIdleLoop((float)delta);
		ZIndex = (int)GlobalPosition.Y;
	}
}