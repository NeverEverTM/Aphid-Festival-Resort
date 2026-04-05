using System.Threading.Tasks;
using Godot;

public partial class GameOver : Control, CutsceneManager.ICutscene
{
    private static GameOver Instance;
    [Export] private AnimatedSprite2D sprite;
    [Export] private Label label;
    [Export] private TextureRect bg, doneSymbol;

    public string ID { get; set; } = "game_over";
    public bool OneShot { get; set; } = false;
    public bool IsDone { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
    }
    public override void _ExitTree()
    {
        Instance = null;
    }

    public static async Task DisplayDialog(string _dialog_key)
    {
        Instance.label.VisibleCharacters = 0;
        Instance.label.Text = _dialog_key;
        for(int i = 0; i < Instance.Tr(_dialog_key).Length; i++)
        {
            Instance.label.VisibleCharacters++;
            SoundManager.CreateSound("dialog/dev_idle");
            await Task.Delay(18);
        }
        Instance.doneSymbol.Visible = true;
        while(true)
        {
            await Task.Delay(1);
            if (Input.IsAnythingPressed()) 
                break;
        }
        Instance.doneSymbol.Visible = false;
    }

    public static async void OhNo()
    {
        PauseMenu.Instance.ProcessMode = ProcessModeEnum.Disabled;
        Instance.GetTree().Paused = true;
        Player.Instance.SetDisabled(true);
        SoundManager.PauseSong();
        Instance.bg.SelfModulate = new(0, 0, 0, 1);
        Instance.label.VisibleCharacters = 0;
        Instance.Visible = true;
        Instance.bg.Visible = true;
        Instance.sprite.Visible = true;
        Instance.label.Visible = true;
        Instance.sprite.Play("death");
        SoundManager.CreateSound("misc/game_over");
        await Task.Delay(3500);

        await DisplayDialog("game_over_0");
        await DisplayDialog("game_over_1");
        await DisplayDialog("game_over_2");

        SoundManager.CreateSound("ui/button_select");
        Instance.sprite.Visible = false;
        Instance.label.Visible = false;

        Instance.GetTree().Paused = false;
        await SceneManager.Load("golden_resort", new("golden_resort", -1, Vector2.Zero, Vector2.Zero));

        if (FreeCameraManager.Enabled)
            FreeCameraManager.SetTo(false);

        Player.Instance.GlobalPosition = ResortManager.Current.SpawnPoint.GlobalPosition;
        CameraManager.Instance.ForceUpdateScroll();
        CameraManager.Instance.ResetSmoothing();
        PlayerInventory.StoreItem("aphid_egg");
        Player.Instance.SetPlayerAnim(StringNames.IdleAnim);

        await Task.Delay(4000);
        while (Instance.bg.SelfModulate.A > 0)
        {
            await Task.Delay(5);
            Instance.bg.SelfModulate = new(Instance.bg.SelfModulate, Instance.bg.SelfModulate.A - 0.01f);
        }
        PauseMenu.Instance.ProcessMode = ProcessModeEnum.Always;
        Player.Instance.SetDisabled(false);
        SoundManager.ResumeSong();
        Instance.bg.Visible = false;
        Instance.Visible = false;
    }

    public async Task Start()
    {
        PauseMenu.Instance.ProcessMode = ProcessModeEnum.Disabled;
        Instance.GetTree().Paused = true;
        Player.Instance.SetDisabled(true);
        SoundManager.PauseSong();
        Instance.bg.SelfModulate = new(0, 0, 0, 1);
        Instance.label.VisibleCharacters = 0;
        Instance.Visible = true;
        Instance.bg.Visible = true;
        Instance.sprite.Visible = true;
        Instance.label.Visible = true;
        Instance.sprite.Play("death");
        SoundManager.CreateSound("misc/game_over");
        await Task.Delay(3500);

        await DisplayDialog("game_over_0");
        await DisplayDialog("game_over_1");
        await DisplayDialog("game_over_2");

        SoundManager.CreateSound("ui/button_select");
        Instance.sprite.Visible = false;
        Instance.label.Visible = false;

        Instance.GetTree().Paused = false;

        if (FreeCameraManager.Enabled)
            FreeCameraManager.SetTo(false);

        Player.Instance.GlobalPosition = ResortManager.Current.SpawnPoint.GlobalPosition;
        CameraManager.ForceCameraPosition();
        PlayerInventory.StoreItem("aphid_egg");
        Player.Instance.SetPlayerAnim("idle");

        await Task.Delay(4000);
        while (Instance.bg.SelfModulate.A > 0)
        {
            await Task.Delay(5);
            Instance.bg.SelfModulate = new(Instance.bg.SelfModulate, Instance.bg.SelfModulate.A - 0.01f);
        }
    }

    public Task Finish(bool _skipped)
    {
        if (_skipped)
        {
            if (!Player.Data.Inventory.Contains("aphid_egg"))
                PlayerInventory.StoreItem("aphid_egg");
        }
        PauseMenu.Instance.ProcessMode = ProcessModeEnum.Always;
        Player.Instance.SetDisabled(false);
        SoundManager.ResumeSong();
        Instance.bg.Visible = false;
        Instance.Visible = false;
        
        return Task.CompletedTask;
    }
}
