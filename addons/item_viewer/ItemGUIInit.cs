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
		ItemViewer.ITEM_TRANSLATIONS.Clear();
		RemoveToolMenuItem("View Item Database...");
	}

	internal void SUMMON_ITEM_VIEWER()
	{
		ItemViewer gui = (ResourceLoader.Load("uid://f7gdm4yd2spb") as PackedScene).Instantiate().Duplicate() as ItemViewer;
		AddChild(gui);
		gui.INITIALIZE_INSTANCE();
	}

	internal static string[][] FETCH_CSV_DATABASE(string _path)
	{
		using FileAccess _file = FileAccess.Open(_path, FileAccess.ModeFlags.Read);
		List<string[]> _document = [];

		while (_file.GetPosition() < _file.GetLength())
			_document.Add(_file.GetCsvLine());

		return [.. _document];
	}
	internal static Dictionary<string, string> FETCH_TRANSLATION_DATABASE(string _path)
	{
		string[][] _document = FETCH_CSV_DATABASE(_path);
		Dictionary<string, string> _translationDocument = [];
		for (int i = 0; i < _document.Length; i++)
		{
			_translationDocument.Add(_document[i][0], _document[i][1]);
		}
		return _translationDocument;
	}
}
#endif
