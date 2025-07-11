using Godot;

public partial class StartMenu : Control
{
	[Export] private TextureButton aphidButton, secretButton, githubButton, itchioButton;
	[Export] private TextureRect titleAphid, titleFestival;
	[Export] private RichTextLabel startText;
	[Export] private Material trans_rights;
	public bool IsReady;
	private int isASecreeeeet;

	public void SetPanel()
	{
		aphidButton.Pressed += () =>
		{
			CreateBoingTween(titleAphid);
			SoundManager.CreateSound("aphid/baby_idle");
		};
		secretButton.Pressed += () =>
		{
			CreateBoingTween(titleFestival);
			if (DebugConsole.IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt)
				DebugConsole.LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair = true;
			SoundManager.CreateSound("aphid/boing");

			if (isASecreeeeet < 7)
				isASecreeeeet++;
			else
				titleFestival.Material = trans_rights;
		};

		string _translation = string.Format(Tr("press_start"),
			ControlsManager.GetActionName(InputNames.Interact));
		startText.Text = $"[wave amp=50.0 freq=5.0 connected=1][center]{_translation}[/center][/wave]";

		githubButton.Pressed += () =>
			OS.ShellOpen("https://github.com/NeverEverTM/Aphid-Festival");
		itchioButton.Pressed += () =>
			OS.ShellOpen("https://neverevertm.itch.io/aphid-festival-resort");
	}
	private void CreateBoingTween(Control _element)
	{
		Tween _tween = _element.CreateTween();
		_tween.SetTrans(Tween.TransitionType.Spring);
		_tween.TweenProperty(_element, "scale", Vector2.One * 1.1f, 0.2).FromCurrent();
		_tween.TweenProperty(_element, "scale", Vector2.One * 0.95f, 0.1).FromCurrent();
		_tween.TweenProperty(_element, "scale", Vector2.One, 0.1).FromCurrent();
		_tween.Play();
		_tween.Finished += () => _tween.Kill();
	}
	public void ReadyUp()
	{
		startText.Hide();

		if (MainMenu.Instance.currentCategory == "continue" && !MainMenu.Instance.menuActions.ContainsKey("continue"))
			MainMenu.Instance.currentCategory = "new_game"; // Hotfix

		MainMenu.Instance.SetButtonWheel(() => MainMenu.Instance.menuActions[MainMenu.Instance.currentCategory](), MainMenu.Instance.SwitchCategories);
		SoundManager.CreateSound(Aphid.Audio_Idle);
		IsReady = true;
	}
}
