using Godot;

public partial class BerryBushBehaviour : Sprite2D, SaveSystem.IGenericDataModule
{
	[Export] private Texture2D[] berryTextures = new Texture2D[2];
	[Export] private InteractableArea2D interactionArea;
	[Export] private AnimationPlayer player;

	private float berry_timer;
	private double last_time_loaded;
	private bool isfinished;

    public override void _EnterTree()
    {
        interactionArea.OnInteractOnly.Add(Interact);
    }

	public void Interact()
	{
		if (isfinished && !PlayerInventory.WouldInventoryBeFullWith(2))
		{
			interactionArea.RemoveMeta(StringNames.TagMeta);
			PlayerInventory.StoreItem("berry", 2);
			Default(); // resets timer

			// animation and sounds
			player.Play("harvest");
			Texture = berryTextures[0];
			SoundManager.CreateSound2D("ui/leaves", GlobalPosition).PitchScale = 3;
			GlobalManager.EmitParticles("leaves_bush", GlobalPosition + new Vector2(0,-5), false);
		}
	}
    
	public override void _Process(double delta)
    {
		if (berry_timer > 0)
        	berry_timer -= (float)delta;
		else if (!isfinished)
		{
			interactionArea.SetMeta(StringNames.TagMeta, (int)StringNames.GlobalTags.Interactable);
			Texture = berryTextures[1];
			player.Play("grow");
			isfinished = true;
		}
    }
    
	public void Set(string _data)
	{
		string[] _values = _data.Split(',');

		if (_values.Length == 1) // patch
			berry_timer = float.Parse(_data);
		else
		{
			double _timeElapsed = GameManager.Data.Playtime - double.Parse(_values[1]);
			berry_timer = float.Parse(_values[0]) - (float)_timeElapsed;
		}
	}
	public string Get()
	{
		return $"{berry_timer},{GameManager.Data.Playtime}";
	}
	public void Default()
    {
        berry_timer = 60 * GlobalManager.RNG.RandiRange(3,5);
		isfinished = false;
    }
}
