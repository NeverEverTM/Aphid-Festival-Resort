using Godot;
using System.Threading.Tasks;

public partial class FadeLoadScreen : LoadScreen
{
    [Export] private TextureRect screen;
    [Export] private Curve curve;

    private bool state;

    public override async Task RunIN()
    {
        await StartAnim();
    }

    public override async Task RunOUT()
    {
        state = true;
        await StartAnim();
        QueueFree();
    }

    protected override Task Tick(float progress)
    {
        // timer shenanigans
        screen.Modulate = new(0, 0, 0,
            state ?
            1 - curve.Sample(progress) :
            curve.Sample(progress));
        return Task.CompletedTask;
    }
}
