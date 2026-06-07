#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class ItemMasterDB : Window
{
    [Export] private Control panelsNode;

    internal static ItemMasterDB Instance;

    internal const string ITEM_SLOT_PREFAB = "uid://c140r8dv4t86f", RECIPE_SLOT_PREFAB = "uid://2k0vsdw6oh8t", ITEM_TR_PATH = "res://databases/translations/items.csv";
    internal const string ICON_BERRY = "[img width=50]res://sprites/ui/berries.tres[/img]",
        ICON_HUNGER = "[img width=50]res://sprites/icons/leaf.tres[/img]",
        ICON_THIRST = "[img width=50]res://sprites/icons/water.tres[/img]";
    internal static string[] ICONS =
    [
        "[img width=40]res://sprites/icons/speed.tres[/img]",
        "[img width=40]res://sprites/icons/strength.tres[/img]",
        "[img width=40]res://sprites/icons/intelligence.tres[/img]",
        "[img width=40]res://sprites/icons/stamina.tres[/img]"
    ];
    internal Dictionary<string, ItemData> ITEM_DATABASE = [];
    private Dictionary<string, string[]> ITEM_TRANSLATIONS = [];
    internal Dictionary<string, RecipeData> RECIPE_DATABASE = [];
    internal Dictionary<string, FoodData> FOOD_DATABASE = [];

    internal enum LocaleEditor { English, Spanish }
    private int current_tab_index = 0;
    private ItemCreator item_creator_instance;
    private RecipeCreator recipe_creator_instance;
    internal interface ITab
    {
        public void START();
        public void ON_LOAD_TAB();
    }

    // MARK: Intialization
    internal void INITIALIZE_INSTANCE()
    {
        Instance = this;
        CloseRequested += () =>
        {
            UNLOAD_ALL_DATA();
            SAVE_TRANSLATIONS();
            ITEM_TRANSLATIONS.Clear();
            Instance = null;
            QueueFree();
        };

        // data loading
        ITEM_TRANSLATIONS = ItemGUIInit.FETCH_TRANSLATION_DATABASE("uid://6s5xx5hshyku");
        LOAD_ALL_DATA();

        foreach (var _panel in panelsNode.GetChildren())
            (_panel as ITab).START();
    }
    internal void SUMMON_ITEM_CREATOR(bool _showSelectedItem)
    {
        if (IsInstanceValid(item_creator_instance))
        {
            item_creator_instance.GrabFocus();
            return;
        }
        item_creator_instance = (ResourceLoader.Load("uid://bafd4ijwyt0v5") as PackedScene).Instantiate().Duplicate() as ItemCreator;
        AddChild(item_creator_instance);

        item_creator_instance.INITIALIZE_INSTANCE(_showSelectedItem ? ItemViewer.Instance.selected_item_id : string.Empty);
    }
    internal void SUMMON_RECIPE_CREATOR()
    {
        if (IsInstanceValid(recipe_creator_instance))
        {
            recipe_creator_instance.GrabFocus();
            return;
        }
        recipe_creator_instance = (ResourceLoader.Load("uid://bmg1lo68dy4xl") as PackedScene).Instantiate().Duplicate() as RecipeCreator;
        AddChild(recipe_creator_instance);

        recipe_creator_instance.INITIALIZE_INSTANCE(ItemViewer.Instance.selected_item_id);
    }
    internal void SHOW_TAB(int _index)
    {
        if (current_tab_index == _index)
            return;

        panelsNode.GetChild<Control>((int)current_tab_index).Hide();
        panelsNode.GetChild<Control>(_index).Show();
        panelsNode.GetChild<ITab>(_index).ON_LOAD_TAB();
        current_tab_index = _index;
    }

    // MARK: Processing
    internal void LOAD_ALL_DATA()
    {
        ITEM_DATABASE = LOAD_ITEM_DATABASE();
        RECIPE_DATABASE = LOAD_RECIPE_DATABASE();
        FOOD_DATABASE = LOAD_FOOD_DATABASE();
    }
    internal void UNLOAD_ALL_DATA()
    {
        ITEM_DATABASE.Clear();
        RECIPE_DATABASE.Clear();
        FOOD_DATABASE.Clear();
        GC.Collect();
    }
    internal static Dictionary<string, ItemData> LOAD_ITEM_DATABASE()
    {
        var _files = DirAccess.GetFilesAt(GlobalManager.ABSOLUTE_ITEMS_DB_PATH);
        Dictionary<string, ItemData> _list = [];
        for (int i = 0; i < _files.Length; i++)
        {
            if (!_files[i].EndsWith(".tres"))
                continue;
            _list.Add(_files[i].Replace(".tres", string.Empty), ResourceLoader.Load<ItemData>(GlobalManager.ABSOLUTE_ITEMS_DB_PATH + _files[i]));
        }
        return _list;
    }
    internal static Dictionary<string, RecipeData> LOAD_RECIPE_DATABASE()
    {
        var _files = DirAccess.GetFilesAt(GlobalManager.ABSOLUTE_RECIPES_DB_PATH);
        Dictionary<string, RecipeData> _list = [];
        for (int i = 0; i < _files.Length; i++)
        {
            if (!_files[i].EndsWith(".tres"))
                continue;
            _list.Add(_files[i].Replace(".tres", string.Empty), ResourceLoader.Load<RecipeData>(GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _files[i]));
        }

        return _list;
    }
    internal static Dictionary<string, FoodData> LOAD_FOOD_DATABASE()
    {
        var _files = DirAccess.GetFilesAt(GlobalManager.ABSOLUTE_FOODS_DB_PATH);
        Dictionary<string, FoodData> _list = [];
        for (int i = 0; i < _files.Length; i++)
        {
            if (!_files[i].EndsWith(".tres"))
                continue;
            _list.Add(_files[i].Replace(".tres", string.Empty), ResourceLoader.Load<FoodData>(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _files[i]));
        }
        return _list;
    }
    internal void SAVE_TRANSLATIONS()
    {
        using var _file = FileAccess.Open(ITEM_TR_PATH, FileAccess.ModeFlags.WriteRead);

        foreach (var _pair in ITEM_TRANSLATIONS)
            _file.StoreCsvLine([_pair.Key, _pair.Value[0], _pair.Value[1]]);

        if (_file.GetPosition() < _file.GetLength())
            _file.Resize((long)_file.GetPosition());

        _file.Dispose();
    }
    internal static void CLEAR_GRID(Node _grid)
    {
        for (int i = 0; i < _grid.GetChildCount(); i++)
        {
            var _node = _grid.GetChild(i);
            if (!_node.IsQueuedForDeletion())
                _node.QueueFree();
        }
    }

    internal static Texture2D GET_ITEM_ICON(string _id, ItemData.ItemType _type)
    {
        if (_type == ItemData.ItemType.Structure)
        {
            // load from icon
            if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".png"))
                return ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".png");

            // load from node
            if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_STRUCTURES_DB_PATH + _id + ".tscn"))
            {
                Node _node = (ResourceLoader.Load(GlobalManager.ABSOLUTE_STRUCTURES_DB_PATH + _id + ".tscn") as PackedScene).Instantiate<Node>();
                Texture2D _texture;
                if (_node.IsClass("Sprite2D") && (_node as Sprite2D) != null)
                {
                    _texture = (_node as Sprite2D).Texture;
                    _node.QueueFree();
                    return _texture;
                }
                else if (_node.IsClass("AnimatedSprite2D") && (_node as AnimatedSprite2D) != null)
                {
                    _texture = (_node as AnimatedSprite2D).SpriteFrames.GetFrameTexture("default", 0);
                    _node.QueueFree();
                    return _texture;
                }
            }
        }
        else
        {
            // technically, the game allows for all image types, yet doesnt allow for capitalized extensions, damn you technology!
            if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".png"))
                return ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".png");

            if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".tres"))
                return ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".tres");
        }
        return ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + "unknown.tres");
    }
    internal static void SET_ITEM_NAME(string _id, string _text, LocaleEditor _locale)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_name"))
            Instance.ITEM_TRANSLATIONS[_id + "_name"][(int)_locale] = _text;
        else
            Instance.ITEM_TRANSLATIONS.Add(_id + "_name", [_text, _text]);
    }
    internal static void SET_ITEM_DESC(string _id, string _text, LocaleEditor _locale)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_desc"))
            Instance.ITEM_TRANSLATIONS[_id + "_desc"][(int)_locale] = _text;
        else
            Instance.ITEM_TRANSLATIONS.Add(_id + "_desc", [_text, _text]);
    }
    internal static string GET_ITEM_NAME(string _id, LocaleEditor _locale)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_name"))
            return Instance.ITEM_TRANSLATIONS[_id + "_name"][(int)_locale];
        else
        {
            GD.PrintErr("Failed to fetch name");
            return _id + "_name";
        }
    }
    internal static string GET_ITEM_DESC(string _id, LocaleEditor _locale)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_desc"))
            return Instance.ITEM_TRANSLATIONS[_id + "_desc"][(int)_locale];
        else
        {
            GD.PrintErr("Failed to fetch description");
            return _id + "_desc";
        }
    }
    internal static ItemData GET_ITEM_DATA(string _id)
    {
        return Instance.ITEM_DATABASE[_id];
    }
    internal static string GET_ITEM_FILEPATH(string _id) =>
        GlobalManager.ABSOLUTE_ITEMS_DB_PATH + _id + ".tres";
    internal static string GET_FOOD_FILEPATH(string _id) =>
        GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres";
    internal static void SET_ITEM_RECIPE(string _id, RecipeData _data)
    {
        if (!Instance.RECIPE_DATABASE.TryAdd(_id, _data))
            Instance.RECIPE_DATABASE[_id] = _data;

        string _path = GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _id + ".tres";
        _data.TakeOverPath(_path);
        ResourceSaver.Save(_data, _path);
    }
    internal static RecipeData GET_ITEM_RECIPE(string _id)
    {
        if (Instance.RECIPE_DATABASE.TryGetValue(_id, out RecipeData value))
            return value;
        else
            return null;
    }
    internal static bool GET_ITEM_RECIPE(string _id, out RecipeData _recipes)
    {
        if (Instance.RECIPE_DATABASE.TryGetValue(_id, out _recipes))
            return true;
        else
            return false;
    }
    internal static bool IS_RECIPE_MATCH_VALID(Godot.Collections.Array<FoodData> _recipe, out RecipeData _existing, bool _exemptItself = false, string _idItself = "id_goes_here")
    {
        // every recipe entry
        foreach (var _pair in Instance.RECIPE_DATABASE)
        {
            // if our given entry is the same as this entry, dont continue
            if (_exemptItself && _pair.Key == _idItself)
                continue;

            // every registered combination
            for (int r = 0; r < _pair.Value.Combinations.Count; r++)
            {
                int _count = 0;
                // for every ingredient that matches against our recipe combination, up the counter
                for (int i = 0; i < _recipe.Count; i++)
                {
                    if (_pair.Value.Combinations[r].Contains(_recipe[i]))
                        _count++;
                }

                // if all ingredients matched across both combinations, this is a repeat
                if (_count == _pair.Value.Combinations[r].Count && _count == _recipe.Count)
                {
                    _existing = _pair.Value;
                    return false;
                }
            }
        }
        _existing = null;
        return true;
    }
}
#endif