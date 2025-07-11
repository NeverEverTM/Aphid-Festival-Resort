using Godot;
using System.Threading.Tasks;

public partial class FadeLoadScreen : LoadScreen
{
    [Export] private TextureRect screen;
    [Export] private Curve curve;

    private float progress;
    private bool state;

    public override void _EnterTree()
    {
        SetProcess(false);
    }

    public override async Task Start()
    {
        progress = 0;
        SetProcess(true);
        while (!IsDone)
        {
            await Task.Delay(1);
        }
    }

    public override async Task Finish()
    {
        IsDone = false;
        state = true;
        progress = 0;
        while (!IsDone)
        {
            await Task.Delay(1);
        }
        QueueFree();
    }

    public override void _Process(double delta)
    {
        if (IsDone)
			return;

		// timer shenanigans
		if (progress < 2)
			progress += (float)delta * 3;	
		else
			IsDone = true;

        screen.Modulate = new(0,0,0,
            state ? 
            1 - curve.Sample(progress / 2) :
            curve.Sample(progress / 2));
    }
}
