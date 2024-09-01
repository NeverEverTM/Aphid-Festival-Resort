using Godot;

public partial class StartMenu : Control
{
	[Export] private TextureButton aphidButton, secretButton, githubButton, itchioButton;
	[Export] private RichTextLabel startText;
	public bool IsReady;

	public void SetPanel()
	{
		aphidButton.Pressed += () => SoundManager.CreateSound(Aphid.Audio_Idle_Baby);
		secretButton.Pressed += () =>
		{
			DebugConsole.LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair = true;
			SoundManager.CreateSound(Aphid.Audio_Boing);
		};

		string _translation = string.Format(Tr("press_start"), 
			Tr(InputMap.ActionGetEvents("interact")[0].AsText().ToLower().Replace(" ", "")));
		startText.Text = $"[wave amp=50.0 freq=5.0 connected=1][center]{_translation}[/center][/wave]";

		githubButton.Pressed += () =>
			OS.ShellOpen("https://github.com/NeverEverTM/Aphid-Festival");
		itchioButton.Pressed += () =>
			OS.ShellOpen("https://neverevertm.itch.io/aphid-festival-resort");
	}
	public void ReadyUp()
	{
		startText.Hide();
		MainMenu.Instance.SetButtonWheel(() => MainMenu.Instance.MenuActions[MainMenu.Instance.currentCategory](), MainMenu.Instance.SwitchCategories);
		SoundManager.CreateSound(Aphid.Audio_Idle);
		IsReady = true;
	}
}
