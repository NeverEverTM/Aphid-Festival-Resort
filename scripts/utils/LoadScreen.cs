using System.Threading.Tasks;
using Godot;

public abstract partial class LoadScreen : CanvasLayer
{
	[Export] protected float timer = 2;

	public abstract Task RunIN();
	public abstract Task RunOUT();
	protected async Task StartAnim()
    {
        float progress = 0;
        double _anchor = Time.GetUnixTimeFromSystem();
        while (progress < timer)
        {
            progress += (float)(Time.GetUnixTimeFromSystem() - _anchor);
            _anchor = Time.GetUnixTimeFromSystem();
            await Tick(progress);
            await Task.Delay(1);
        }
        await Task.Delay(1);
    }
	protected abstract Task Tick(float _progress);
}