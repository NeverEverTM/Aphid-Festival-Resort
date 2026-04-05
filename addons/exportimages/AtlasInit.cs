#if TOOLS
using Godot;

[Tool]
public partial class AtlasInit : EditorPlugin
{
    public override void _EnterTree()
    {
        AddToolMenuItem("Generate Atlas & Textures...", Callable.From(SUMMON_GUI));
        GD.PrintRich("[color=cyan]To start exporting images, go to Project > Tools and click 'Generate Atlas & Textures...'.[/color]");
    }
    public override void _ExitTree()
    {
        RemoveToolMenuItem("Generate Atlas & Textures...");
    }
    
    internal void SUMMON_GUI()
    {
        AtlasGui gui = (ResourceLoader.Load("uid://d2x6cpn14j87m") as PackedScene).Instantiate().Duplicate() as AtlasGui;
        AddChild(gui);
        gui.Start();
    }
}
#endif