using System.Threading.Tasks;
using Godot;

public partial class AphidSkin : Node2D
{
	[Export] public Sprite2D eyes, antenna, body, back_legs, front_legs;
	private AphidInstance Instance;

	/// <summary>
	/// Used to keep track of eye expressions after blink.
	/// So if aphid is happy and blinks, it doesnt reset back to idle.
	/// </summary>
	public string lastEyeExpression = "idle", currentEyeExpression;
	/// <summary>
	/// False : Facing Right - True : Facing Left
	/// </summary>
	public bool IsFlipped;
	public bool OverrideMovementAnim;

	private int walk_shutter;
	private const int walk_shutter_speed = 8;
	private bool legsStep;
	private Vector2 front_legs_position, back_legs_position;

    public override void _Process(double delta)
    {
        TickFlip((float)delta);
    }

	public void SetInstance(AphidInstance _instance)
	{
		Instance = _instance;
		front_legs_position = front_legs.Position;
		back_legs_position = back_legs.Position;
	}

	// ==================| SKINS |======================
	/// <summary>
	/// Sets all skin sprites to the given action id
	/// </summary>
	/// <param name="_action"></param>
	public void SetTo(string _action)
	{
		SetEyesSkin(_action);
		SetAntennaSkin(_action);
		SetBodySkin(_action);
		SetLegsSkin(_action);
	}

	public static Texture2D GetSkinSprite(int _id, string _piece, string _action = "idle", bool _isAdult = true)
	{
		string _skinName = _id + "/" + (_isAdult ?
				$"{_piece}_{_action}" :
				$"{_piece}_baby_{_action}");
		return GlobalManager.GetSkin(_skinName);
	}
	public void SetEyesSkin(string _action)
	{
		eyes.Texture = GetSkinSprite(Instance.Genes.EyeType, "eyes", _action, Instance.Status.IsAdult);
		eyes.SelfModulate = Instance.Genes.EyeColor;
		lastEyeExpression = currentEyeExpression;
		currentEyeExpression = _action;
	}
	public void SetAntennaSkin(string _action)
	{
		antenna.Texture = GetSkinSprite(Instance.Genes.AntennaType, "antenna", _action, Instance.Status.IsAdult);
		antenna.SelfModulate = Instance.Genes.AntennaColor;
	}
	public void SetBodySkin(string _action)
	{
		body.Texture = GetSkinSprite(Instance.Genes.BodyType, "body", _action, Instance.Status.IsAdult); ;
		body.SelfModulate = Instance.Genes.BodyColor;
	}
	public void SetLegsSkin(string _action)
	{
		Texture2D _legsTexture = GetSkinSprite(Instance.Genes.LegType, "legs", _action, Instance.Status.IsAdult);
		front_legs.SelfModulate = back_legs.SelfModulate = Instance.Genes.LegColor;
		front_legs.Texture = back_legs.Texture = _legsTexture;
	}

	// ===============| FLIP DIRECTION |=================
	public void SetFlipDirection(Vector2 _direction, bool _setAsCurrent = false)
	{
		// False : Facing Right - True : Facing Left
		if (_direction.X < 0)
			IsFlipped = true;
		else if (_direction.X > 0)
			IsFlipped = false;

		if (_setAsCurrent)
			Scale = new(IsFlipped ? 1 : -1, Scale.Y);
	}
	public void TickFlip(float _delta)
	{
		if (IsFlipped)
			Scale = new(Mathf.Lerp(Scale.X, 1, _delta * 3), Scale.Y);
		else
			Scale = new(Mathf.Lerp(Scale.X, -1, _delta * 3), Scale.Y);
	}
	// ===============| ANIMATIONS |=================
	public Tween CreateAnimationTween(Tween.EaseType _ease, Tween.TransitionType _trans, string _property, Variant _finalVar, float _duration)
	{
		Tween tween = CreateTween();
		tween.SetEase(_ease);
		tween.SetTrans(_trans);
		tween.TweenProperty(this, _property, _finalVar, _duration);
		return tween;
	}

	public virtual void DoWalkAnim()
	{
		// Motion Framerate
		if (walk_shutter > 0)
		{
			walk_shutter--;
			return;
		}
		walk_shutter = walk_shutter_speed;
		legsStep = !legsStep;

		// Switch between back and front legs to make the walking motion
		front_legs.Position = front_legs_position + (legsStep ? new Vector2(0, -1) : Vector2.Zero);
		back_legs.Position = back_legs_position + (legsStep ? Vector2.Zero : new Vector2(0, -1));

		if (SceneManager.CurrentlyInGame)
			SoundManager.CreateSound2D("aphid/step", GlobalPosition).VolumeDb = -10;
	}
	/// <summary>
	/// Properly handles walking during movement. 
	/// if you want to play the animation by itself, set OverrideMovementAnim to true and use DoWalkAnim instead.
	/// </summary>
	public void StartWalk(Vector2 _currentWalkingDirection)
	{
		if (OverrideMovementAnim)
			return;

		if (_currentWalkingDirection.IsEqualApprox(Vector2.Zero))
		{
			// Reset back to idle standing
			front_legs.Position = front_legs_position;
			back_legs.Position = back_legs_position;
		}
		else
			DoWalkAnim();
	}

	/// <summary>
	/// A jump animation for the aphid.
	/// </summary>
	/// <param name="_dropIntensity"></param>
	public void DoJump(float _dropIntensity = 1)
	{
		Tween _jump = CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Cubic, "position", new Vector2(0, -20), 0.15f);
		_jump.TweenInterval(0.05);
		_jump.TweenSubtween(CreateAnimationTween(Tween.EaseType.In, Tween.TransitionType.Cubic, "position", new Vector2(0, 0), 0.25f * _dropIntensity));
	}
	/// <summary>
	/// A smaller jump animation, a hop for the aphid.
	/// </summary>
	/// <param name="_playSound"></param>
	public void DoHop(bool _playSound = true)
	{
		Tween tween = CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "position", new Vector2(0, -5), 0.15f);
		tween.Finished += () =>  CreateAnimationTween(Tween.EaseType.In, Tween.TransitionType.Bounce, "position", new Vector2(0, 0), 0.15f);
		if (_playSound)
			SoundManager.CreateSound2D("aphid/jump", GlobalPosition, true);
	}
	public async Task DoDance()
	{
		for (int i = 0; i < 3; i++)
		{
			SetFlipDirection(IsFlipped ? Vector2.Right : Vector2.Left);
			DoHop();
			await Task.Delay(400);
		}
		int _ticks = 60;
		OverrideMovementAnim = true;
		while(_ticks > 0)
		{
			DoWalkAnim();
			_ticks--;
			await Task.Delay(16);
		}
		OverrideMovementAnim = false;
	}
	public Tween DoSquish(float _durationMultiplier = 1f, bool _playSound = true)
	{
		// start of animation
		Tween _squish = CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "scale", new Vector2(IsFlipped ? 1 : -1, 0.5f), 0.15f * _durationMultiplier);
		_squish.SetParallel();
		_squish.TweenSubtween(CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "position", new Vector2(0, 2.2f), 0.2f * _durationMultiplier));
		_squish.SetParallel(false);

		// end of animation
		_squish.TweenSubtween(CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "scale", new Vector2(IsFlipped ? 1 : -1, 1), 0.6f * _durationMultiplier));
		_squish.SetParallel();
		_squish.TweenSubtween(CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "position", new Vector2(0, 0), 0.5f * _durationMultiplier));

		if (_playSound)
			SoundManager.CreateSound2D("aphid/boing", GlobalPosition, true);
		return _squish;
	}
}
