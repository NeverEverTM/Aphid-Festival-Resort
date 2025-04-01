using Godot;

public partial class Grass : Sprite2D
{
    // Grass grows, bird flies, sun shines, and brother, I hurt people. [BOINK] 
    // I'm a force of nature. [BONK] if you were from where I was from, you'd be fucking dead.
    // [AAAAAAAH] Woooh!
    [Export] private Area2D area;
    [Export] private double first = 0.1, second = 0.1, third = 0.05;
    private Tween swayTween;
    public override void _Ready()
    {
        area.BodyEntered += (_body) =>
        {
            if (_body.HasMeta(StringNames.TagMeta) && 
                    (_body.GetMeta(StringNames.TagMeta).ToString() == "player" ||
                    _body.GetMeta(StringNames.TagMeta).ToString() == Aphid.Tag))

                Sway(-6, first).Finished += () => Sway(4, second).Finished += ()=> Sway(0, third);
        };
        Timer _newTimer = new();
        _newTimer.Timeout += () => 
        {
            if (swayTween.IsValid())
                return;

            Sway(GD.RandRange(0,3.0), GD.RandRange(0.5,1)).Finished += 
                    () => Sway(0, GD.RandRange(0.5,1));
        };
        AddChild(_newTimer);
        _newTimer.Start(GD.RandRange(1,2));
        
        swayTween = CreateTween();
        swayTween.Kill();
    }
    private Tween Sway(double to, double duration)
    {
        swayTween.Kill();

        swayTween = CreateTween();
        swayTween.SetTrans(Tween.TransitionType.Bounce);
        swayTween.TweenProperty(this, "skew", Mathf.DegToRad(to), duration).FromCurrent();
        return swayTween;
    }
}
