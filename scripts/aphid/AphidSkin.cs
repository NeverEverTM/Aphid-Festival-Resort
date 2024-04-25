using System.Threading.Tasks;
using Godot;

public partial class AphidSkin : Node2D
{
	[Export] public Sprite2D eyes, antenna, body, back_legs, front_legs;
	private AphidInstance Instance;
	private Aphid MyAphid;

	/// <summary>
	/// Used to keep track of eye expressions after blink.
	/// So if aphid is happy and blinks, it doesnt reset back to idle.
	/// </summary>
	public string lastEyeExpression = "idle", currentEyeExpression;
	public bool IsFlipped, OverrideMovementAnim;

	private int walk_shutter;
	private const int walk_shutter_speed = 8;
	private bool legsStep;
	private Vector2 front_legs_position, back_legs_position;

	public void SetInstance(AphidInstance _instance, Aphid _aphid)
	{
		Instance = _instance;
		front_legs_position = front_legs.Position;
		back_legs_position = back_legs.Position;
		MyAphid = _aphid;
	}

	// ==================| SKINS |======================
	public void SetSkin(string _action)
	{
		SetEyesSkin(_action);
		SetAntennaSkin(_action);
		SetBodySkin(_action);
		SetLegsSkin(_action);
	}
	private Texture2D GetSkinPiece(int _id, string _piece, string _action = "idle")
	{
		string _path = $"{GameManager.SkinsPath}/{_id}/";
		if (Instance.Status.IsAdult)
			_path += $"{_piece}_{_action}.png";
		else
			_path += $"{_piece}_baby_{_action}.png";

		return ResourceLoader.Load<Resource>(_path) as Texture2D;
	}
	public void SetEyesSkin(string _action)
	{
		eyes.Texture = GetSkinPiece(Instance.Genes.EyeType, "eyes", _action);
		eyes.SelfModulate = Instance.Genes.EyeColor;
		lastEyeExpression = currentEyeExpression;
		currentEyeExpression = _action;
	}
	public void SetAntennaSkin(string _action)
	{
		antenna.Texture = GetSkinPiece(Instance.Genes.AntennaType, "antenna", _action);
		antenna.SelfModulate = Instance.Genes.AntennaColor;
	}
	public void SetBodySkin(string _action)
	{
		body.Texture = GetSkinPiece(Instance.Genes.BodyType, "body", _action); ;
		body.SelfModulate = Instance.Genes.BodyColor;
	}
	public void SetLegsSkin(string _action)
	{
		Texture2D _legsTexture = GetSkinPiece(Instance.Genes.LegType, "legs", _action);
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
		// False : Facing Right - True : Facing Left
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

		MyAphid.PlaySound(Aphid.Audio_Step, true);
	}
	/// <summary>
	/// Makes sure there is actual movement active. 
	/// Use DoWalkAniminstead and set OverrideMovementAnim to true if you want to execute it standalone.
	/// </summary>
	public void StartWalking()
	{
		if (OverrideMovementAnim)
			return;

		if (MyAphid.MovementDirection == Vector2.Zero)
		{
			// Reset back to idle standing
			front_legs.Position = front_legs_position;
			back_legs.Position = back_legs_position;
			return;
		}

		DoWalkAnim();
	}

	public void DoJumpAnim()
	{
		Tween tween = CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "position", new Vector2(0, -5), 0.15f);
		tween.Finished += () => {
			CreateAnimationTween(Tween.EaseType.In, Tween.TransitionType.Bounce, "position", new Vector2(0, 0), 0.15f);
		};
		SoundManager.CreateSound2D(Aphid.Audio_Jump, MyAphid.GlobalPosition, true);
	}
	public async Task DoDanceAnim()
	{
		for (int i = 0; i < 3; i++)
		{
			SetFlipDirection(IsFlipped ? Vector2.Right : Vector2.Left);
			DoJumpAnim();
			await Task.Delay(400);
		}
		int _ticks = 60;
		while(_ticks > 0)
		{
			DoWalkAnim();
			_ticks--;
			await Task.Delay(16);
		}
	}
	public void DoSquishAnim()
	{
		// scale
		CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "scale", new Vector2(IsFlipped ? 1: -1, 0.5f), 0.15f)
		.Finished += () => {
			CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "scale", new Vector2(IsFlipped ? 1: -1, 1), 0.6f);
		};
		// position
		CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "position", new Vector2(0, 2.2f), 0.2f)
		.Finished += () => {
			CreateAnimationTween(Tween.EaseType.Out, Tween.TransitionType.Bounce, "position", new Vector2(0, 0), 0.5f);
		};
		SoundManager.CreateSound2D(Aphid.Audio_Boing, MyAphid.GlobalPosition, true);
	}
}
