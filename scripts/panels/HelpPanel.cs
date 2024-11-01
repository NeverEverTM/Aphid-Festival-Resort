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
			ControlsManager.GetUserReadableText(InputMap.ActionGetEvents("open_inventory")[0].AsText()),
			ControlsManager.GetUserReadableText(InputMap.ActionGetEvents("pickup")[0].AsText()),
				});
		helpDescriptions[1].Text = string.Format(Tr("help_desc_1"),
			ControlsManager.GetUserReadableText(InputMap.ActionGetEvents("interact")[0].AsText()));
		helpDescriptions[2].Text = Tr("help_desc_2");
		helpDescriptions[3].Text = Tr("help_desc_3");
		helpDescriptions[4].Text = Tr("help_desc_4");
		helpDescriptions[5].Text = string.Format(Tr("help_desc_5"),
			ControlsManager.GetUserReadableText(InputMap.ActionGetEvents("open_generations")[0].AsText()));
		helpDescriptions[6].Text = Tr("help_desc_6");
		helpDescriptions[7].Text = string.Format(Tr("help_desc_7"), new string[]
		{
			ControlsManager.GetUserReadableText(InputMap.ActionGetEvents("change_camera")[0].AsText()),
			ControlsManager.GetUserReadableText(InputMap.ActionGetEvents("take_screenshot")[0].AsText()),
		});
	}
}
