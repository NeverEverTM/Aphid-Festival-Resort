using Godot;

public partial class BerryBushBehaviour : Sprite2D, Player.IPlayerInteractable, ResortManager.IStructureData
{
	[Export] private Texture2D[] berryTextures = new Texture2D[2];
	[Export] private Node2D interactionArea;
	[Export] private AnimationPlayer player;
	[Export] private GpuParticles2D particles;
	private float berry_timer;
	private bool isfinished;
	private const int TIMER_BASE = 60 * 4;

    public override void _Ready()
    {
		berry_timer = TIMER_BASE;
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
			interactionArea.RemoveMeta(StringNames.TagMeta);
			berry_timer = TIMER_BASE;
		}
	}
    public override void _Process(double delta)
    {
		if (berry_timer > 0)
        	berry_timer -= (float)delta;
		else if (!isfinished)
		{
			isfinished = true;
			Texture = berryTextures[1];
			interactionArea.SetMeta(StringNames.TagMeta, Player.InteractableTag);
			player.Play("grow");
		}
    }
    public void SetData(string _data)
	{
		berry_timer = float.Parse(_data);
	}

	public string GetData()
	{
		return berry_timer.ToString();
	}
}
