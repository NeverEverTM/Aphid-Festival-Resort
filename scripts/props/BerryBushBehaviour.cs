using Godot;

public partial class BerryBushBehaviour : Sprite2D, Player.IPlayerInteractable, ResortManager.IStructureData
{
	[Export] private Texture2D[] berryTextures = new Texture2D[2];
	[Export] private Node2D interactionArea;
	[Export] private AnimationPlayer player;
	[Export] private GpuParticles2D particles;
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
			interactionArea.SetMeta("tag", Player.InteractableTag);
			player.Play("grow");
		};
		berry_timer.Start(TIMER_BASE);
    }

	public void Interact()
	{
		if (isfinished && PlayerInventory.CanStoreItem())
		{
			isfinished = false;
			PlayerInventory.StoreItem("berry");
			PlayerInventory.StoreItem("berry");
			Texture = berryTextures[0];
			player.Play("harvest");
			particles.Emitting = true;
			SoundManager.CreateSound2D(SoundManager.GetAudioStream("ui/leaves"), new AudioStreamPlayer2D()
			{
				Bus = "Sounds",
				PitchScale = 3
			}, GlobalPosition);
			interactionArea.RemoveMeta("tag");
			berry_timer.Start(TIMER_BASE);
		}
	}
	public void SetData(string _data)
	{
		berry_timer.Stop();
		berry_timer.Start(float.Parse(_data));
	}

	public string GetData()
	{
		return berry_timer.TimeLeft.ToString();
	}
}
