using Godot;

public partial class Grass : Sprite2D
{
    [Export] private Area2D area;
    [Export] private double first = 0.1, second = 0.1, third = 0.05;
    public override void _Ready()
    {
        area.BodyEntered += (_body) =>
        {
            if (_body.HasMeta(StringNames.TagMeta) && 
                    (_body.GetMeta(StringNames.TagMeta).ToString() == "player" ||
                    _body.GetMeta(StringNames.TagMeta).ToString() == Aphid.Tag))

                Sway(-6, first).Finished += () => Sway(4, second).Finished += ()=> Sway(0, third);
        };
    }
    private Tween Sway(int to, double duration)
    {
        Tween _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Bounce);
        _tween.TweenProperty(this, "skew", Mathf.DegToRad(to), duration).FromCurrent();
        return _tween;
    }
}
