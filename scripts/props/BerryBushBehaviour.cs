using Godot;

public partial class BerryBushBehaviour : Sprite2D, Player.IInteractEvent, ResortManager.IStructureData
{
	[Export] private Texture2D[] berryTextures = new Texture2D[2];
	[Export] private Node2D interactionArea;
	[Export] private AnimationPlayer player;
	[Export] private GpuParticles2D particles;
	private float berry_timer;
	private bool isfinished;

    public override void _Ready()
    {
		berry_timer = 60 * GlobalManager.RNG.RandiRange(3,5);
    }

	public void Interact()
	{
		if (isfinished && PlayerInventory.CanStoreItem(2))
		{
			isfinished = false;
			PlayerInventory.StoreItem("berry");
			PlayerInventory.StoreItem("berry");
			Texture = berryTextures[0];
			player.Play("harvest");
			particles.Emitting = true;
			var _player = SoundManager.SFXPlayer2D.Duplicate() as AudioStreamPlayer2D;
			_player.PitchScale = 3;
			SoundManager.CreateSound2D("ui/leaves", _player, GlobalPosition);
			interactionArea.RemoveMeta(StringNames.TagMeta);
			berry_timer = 60 * GlobalManager.RNG.RandiRange(3,5);
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
			interactionArea.SetMeta(StringNames.TagMeta, StringNames.InteractableTag);
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
