using Godot;

public partial class Cheats : Node
{
	public static bool IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt,
	LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair,
	DidntSayIDidntWarnYouBeforeHand;
	private bool IsDebugBuild;

	public override void _Ready()
	{
		CheckForDebug();
	}
	private void CheckForDebug()
	{
		if (IsOnDebugModeAndThereforeExemptFromAnyRightOfComplainForFaultyProductAndPossibilityOfACaseOfCourt)
		{
			if (LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair)
			{
				if (DidntSayIDidntWarnYouBeforeHand)
				{
					if (!IsDebugBuild)
					{
						IsDebugBuild = true;
						SoundManager.CreateSound(Aphid.Audio_Jump);
					}
				}
				else
				{
					DidntSayIDidntWarnYouBeforeHand = true;
					GD.Print("DEBUG MODE ENGAGED: PRESS DEBUG KEY AGAIN TO CONFIRM");
				}
			}
		}

#if DEBUG
		IsDebugBuild = true;
#endif
	}

	public override void _Input(InputEvent @event)
	{
		if (IsDebugBuild)
			ProcessDebugActions();
		else
		{
			if (Input.IsActionJustPressed("debug_0"))
				CheckForDebug();
			else if (DidntSayIDidntWarnYouBeforeHand)
				DidntSayIDidntWarnYouBeforeHand = false;
		}
	}

	private void ProcessDebugActions()
	{
		if (Input.IsActionJustPressed("debug_0"))
		{
			AphidData.Genes _genes = new();
			_genes.RandomizeColors();
			_genes.Name += ResortManager.Instance.AphidsOnResort.Count;
			ResortManager.CreateNewAphid(GameManager.Utils.GetMouseToWorldPosition(), _genes);
		}
	}
}
