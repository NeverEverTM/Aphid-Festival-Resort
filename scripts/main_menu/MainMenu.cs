using System;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Central processing script for main menu operations, including boot up and user interface.
/// </summary>
public partial class MainMenu : Node2D
{
	public static MainMenu Instance { get; private set; }

	[Export] private CanvasLayer canvas;
	[Export] private AnimationPlayer intro_animator, title_animator;
	[Export] private Node2D entity_root;
	public static bool IsReady { get; private set; }

	private bool DirectionForX, DirectionForY;
	private float MaxWanderDistanceX = 600, MaxWanderDistanceY = 450;
	private float[] babyWeight = [70, 30];

	public override void _EnterTree()
	{
		Instance = this;
	}
	public async override void _Ready()
	{
		if (!IsReady)
		{
			intro_animator.Play("start");
			while (intro_animator.IsPlaying())
			{
				if (Input.IsAnythingPressed())
					break;
				await Task.Delay(1);
			}
			intro_animator.Play("loading");
			await GlobalManager.INTIALIZE_GAME_PROCESS();

			intro_animator.Play("sweep");
		}

		SpawnBunchaOfAphidsForTheFunnies();
		DirectionForX = GlobalManager.RNG.Randf() > 0.5f;
		DirectionForY = GlobalManager.RNG.Randf() > 0.5f;
		CameraManager.ForceCameraPosition(GlobalManager.Utils.GetRandomVector(-300, 300));

		SoundManager.PlaySong("misc/title");
		title_animator.Play("slide_down");

		while (title_animator.IsPlaying())
			await Task.Delay(1);
		IsReady = true;
	}
	public override void _Process(double delta)
	{
		DoBounceAnim();
	}

	// MARK: Main Functionality
	public void ContinueGame()
	{
		if (string.IsNullOrWhiteSpace(OptionsManager.Settings.LastPlayedResort) || !DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			GlobalManager.CREATE_POPUP("Could not find valid profile to continue", this);
			return;
		}

		SaveSystem.SelectProfile(OptionsManager.Settings.LastPlayedResort);
		GameManager.GameSaveModule _module = new(GameManager.SAVEMODULE_ID, new GameManager.GameDataModule(), 0)
		{
			RootPath = System.IO.Path.Combine(SaveSystem.ProfilePath)
		};

		GameManager.GameData _data = _module.Load(false);
		LoadResort(_data.LastRoom);
	}
	public static void DeleteResort(string _profile)
	{
		SaveSystem.SelectProfile(_profile);
		if (!DirAccess.DirExistsAbsolute(SaveSystem.ProfilePath))
		{
			GlobalManager.CREATE_POPUP("warning_invalid_resort", Instance.canvas);
			return;
		}

		SaveSystem.DeleteProfile(_profile);
		SoundManager.CreateSound("aphid/hurt");
	}
	public static async void LoadResort(string _room = "")
	{
		if (string.IsNullOrEmpty(_room))
			_room = "golden_resort";

		OptionsManager.Settings.LastPlayedResort = SaveSystem.Profile;
		DebugLogger.Print(DebugLogger.LogPriority.Info, $"MainMenu: Loading the profile <{SaveSystem.Profile}>.");
		await OptionsManager.SaveModule.Save();
		await SceneManager.Switch(_room, true);
	}
	public void ExitGame()
	{
		ConfirmationPopup.Create(() => GetTree().Quit(), null,
			ConfirmationPopup.ConfirmationEnum.Fast, "confirmation_exit");
	}

	// MARK: Cosmetic Interface
	private void DoBounceAnim()
	{
		if (CameraManager.Instance.GlobalPosition.X > MaxWanderDistanceX)
			DirectionForX = true;
		else if (CameraManager.Instance.GlobalPosition.X < -MaxWanderDistanceX)
			DirectionForX = false;

		if (CameraManager.Instance.GlobalPosition.Y > MaxWanderDistanceY)
			DirectionForY = true;
		else if (CameraManager.Instance.GlobalPosition.Y < -MaxWanderDistanceY)
			DirectionForY = false;

		CameraManager.Instance.GlobalPosition += new Vector2(DirectionForX ? -1 : 1, DirectionForY ? -1 : 1);
	}
	private void SpawnBunchaOfAphidsForTheFunnies()
	{
		for (int i = 0; i < 8; i++)
		{
			AphidInstance _generic = new(Guid.Empty);
			_generic.Genes.DEBUG_Randomize(false);
			AphidFake _aphid = ResourceLoader.Load<PackedScene>("uid://2rlgwyhwp5w8").Instantiate() as AphidFake;
			_generic.Status.IsAdult = GlobalManager.Utils.GetRandomByWeight(babyWeight) == 0;
			_aphid.GlobalPosition = GlobalManager.Utils.GetRandomVector(-300, 300);
			entity_root.AddChild(_aphid);
			_aphid.SetReady(_generic);
		}
	}
}