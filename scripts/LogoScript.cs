using Godot;
using System;

public partial class LogoScript : TextureRect
{
	[Export] private TextureButton aphidButton, secretButton;

    public override void _Ready()
    {
        aphidButton.Pressed += () => SoundManager.CreateSound(Aphid.Audio_Idle_Baby);
		secretButton.Pressed += () => 
		{
			Cheats.LikeForRealsiesYouWantThisSinceYourGameMayGetFuckedUpBeyondRepair = true;
			SoundManager.CreateSound(Aphid.Audio_Boing);
		};
    }
}
