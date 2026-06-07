#if TOOLS
using System.Collections.Generic;
using Godot;

[Tool]
public partial class ItemGUIInit : EditorPlugin
{
	public override void _EnterTree()
	{
		AddToolMenuItem("View Item Database...", Callable.From(SUMMON_ITEM_VIEWER));
		GD.PrintRich("[color=gold]To view the item database, go to Project > Tools and click 'View Item Database...'.[/color]");
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(ItemMasterDB.Instance))
			ItemMasterDB.Instance.UNLOAD_ALL_DATA();
		ItemMasterDB.Instance = null;
		RemoveToolMenuItem("View Item Database...");
	}

	internal void SUMMON_ITEM_VIEWER()
	{
		ItemMasterDB gui = (ResourceLoader.Load("uid://f7gdm4yd2spb") as PackedScene).Instantiate().Duplicate() as ItemMasterDB;
		AddChild(gui);
		gui.INITIALIZE_INSTANCE();
	}

	internal static Dictionary<string, string[]> FETCH_TRANSLATION_DATABASE(string _path)
	{
		using FileAccess _file = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
		Dictionary<string, string[]> _document = [];

		while (_file.GetPosition() < _file.GetLength())
		{
			string[] _line = _file.GetCsvLine(); 
			_document.Add(_line[0], [ _line[1], _line[2] ]);
		}
		return _document;
	}
}
#endif
