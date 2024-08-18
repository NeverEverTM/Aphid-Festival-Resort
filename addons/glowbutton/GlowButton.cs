#if TOOLS
using Godot;

[Tool]
public partial class GlowButton : EditorPlugin
{
	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
        Script _script = GD.Load<Script>("res://addons/glowbutton/ButtonBehaviour.cs");
        Texture2D _texture = GD.Load<Texture2D>("res://icon.svg");
        AddCustomType("GlowButton", "TextureButton", _script, _texture);
        GD.PrintRich("[color=green]Added GlowButton plugin[/color]");
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
        RemoveCustomType("GlowButton");
        GD.PrintRich("[color=red]Removed GlowButton plugin[/color]");
	}
}
#endif
