using Godot;

public partial class BerryBushBehaviour : Sprite2D, Player.IPlayerInteractable, ResortManager.IStructureData
{
	[Export] private Texture2D[] berryTextures = new Texture2D[2];
	private Timer berry_timer;
	private bool isfinished;
	private const int TIMER_BASE = 60 * 4;

    public override void _Ready()
    {
        berry_timer = new()
        {
            OneShot = true
        };
        AddChild(berry_timer);
		berry_timer.Timeout += () => {
			Texture = berryTextures[1];
			isfinished = true;
		};
		berry_timer.Start(TIMER_BASE);
    }

	public void Interact()
	{
		GD.Print("Im interactable");
		if (isfinished)
		{
			isfinished = false;
			Texture = berryTextures[0];
			PlayerInventory.StoreItem("berry");
			PlayerInventory.StoreItem("berry");
			berry_timer.Start(TIMER_BASE);
		}
	}
	public void SetData(string _data)
	{
		berry_timer.Start(float.Parse(_data));
		GD.Print("berry timer is now " + berry_timer.WaitTime);
	}

	public string GetData()
	{
		GD.Print("we returned berry time");
		return berry_timer.TimeLeft.ToString();
	}
}
