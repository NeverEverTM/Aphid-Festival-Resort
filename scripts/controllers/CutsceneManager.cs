using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class CutsceneManager : Node2D
{
    public static bool IsActive { get; set; }

    public static Dictionary<string, ICutscene> Cutscenes { get; set; }
    public static SaveSystem.SaveModule<Dictionary<string, ICutscene>> SaveModule;
    public class CutsceneDataModule : SaveSystem.IDataModule<Dictionary<string, ICutscene>>
    {
        public Dictionary<string, ICutscene> Default()
        {
            return [];
        }

        public Dictionary<string, ICutscene> Get()
        {
            return Cutscenes;
        }

        public void Set(Dictionary<string, ICutscene> _data)
        {
            Cutscenes = _data;
        }
    }

    public override void _EnterTree()
    {
        SaveModule = new("cutscene", new CutsceneDataModule());
    }

    public async Task Start(ICutscene _cutscene)
    {
        if (Cutscenes.TryGetValue(_cutscene.ID, out ICutscene _availableCutscene))
        {
            if (_availableCutscene.OneShot)
                return;
        }
        else
            Cutscenes.Add(_cutscene.ID, _cutscene);

        _ = _cutscene.Start();



        await _cutscene.Finish(false);
        return;
    }

    public static async Task WaitFor(float _seconds)
    {
        float _miliseconds = _seconds * 1000;
        while (_miliseconds <= 0)
        {
            await Task.Delay(1);
            _miliseconds -= 1;
        }
    }

    public interface ICutscene
    {
        public string ID { get; set; }
        public bool IsDone { get; set; }
        public bool OneShot { get; set; }

        public Task Start();
        public Task Finish(bool _skipped);
    }
}
