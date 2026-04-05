#if TOOLS
using Godot;
using System;

[Tool]
public partial class ItemCreator : Window
{
    [Export] private LineEdit fileNameEdit, nameEdit, costEdit;
    [Export] private TextEdit descriptionEdit;
    [Export] private TextureRect iconTexture;
    [Export] private OptionButton categoryOptions, shopOptions;
    [Export] private Button finishButton;

    internal void INITIALIZE_INSTANCE(string _id)
	{
		CloseRequested += QueueFree;

        if (!string.IsNullOrWhiteSpace(_id))
        {
            nameEdit.Text = ItemViewer.ITEM_TRANSLATIONS[_id + "_name"];
            descriptionEdit.Text = ItemViewer.ITEM_TRANSLATIONS[_id + "_desc"];
            finishButton.Text = "Modify";
        }
	}
}
#endif