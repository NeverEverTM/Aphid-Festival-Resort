using Godot;

public partial class HelpPanel : Control
{
	[Export] private RichTextLabel[] helpDescriptions;
	public override void _Ready()
	{
		VisibilityChanged += () =>
		{
			if (Visible)
				UpdateHelpDescriptions();
		};

		UpdateHelpDescriptions();
	}

	private void UpdateHelpDescriptions()
	{
		helpDescriptions[0].Text = string.Format(Tr("help_desc_0"), new string[]
		{
			ControlsManager.GetActionName(InputNames.OpenInventory),
			ControlsManager.GetActionName(InputNames.Pickup),
		});
		helpDescriptions[1].Text = string.Format(Tr("help_desc_1"),
			ControlsManager.GetActionName(InputNames.Interact));
		helpDescriptions[2].Text = Tr("help_desc_2");
		helpDescriptions[3].Text = Tr("help_desc_3");
		helpDescriptions[4].Text = Tr("help_desc_4");
		helpDescriptions[5].Text = string.Format(Tr("help_desc_5"),
			ControlsManager.GetActionName(InputNames.OpenGenerations));
		helpDescriptions[6].Text = Tr("help_desc_6");
		helpDescriptions[7].Text = string.Format(Tr("help_desc_7"), new string[]
		{
			ControlsManager.GetActionName(InputNames.ChangeCamera),
			ControlsManager.GetActionName(InputNames.TakeScreenshot),
		});
	}
}
