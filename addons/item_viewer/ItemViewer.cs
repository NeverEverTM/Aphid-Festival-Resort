#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class ItemViewer : Window
{
    [Export] private LineEdit search_bar;
    [Export] private OptionButton sorting_options;
    [Export] private Container item_grid;
    [Export] private Control panelsNode;
    [ExportGroup("Information Panel")]
    [Export] private TextureRect icon;
    [Export] private Label name, recipeLabel;
    [Export] private RichTextLabel item_stats, food_stats;
    [Export] private Container recipe_grid;
    [Export] private Control updateButton, deleteButton, addRecipeButton;

    internal static ItemViewer Instance;
    internal const string ITEM_SLOT_PREFAB = "uid://c140r8dv4t86f", RECIPE_SLOT_PREFAB = "uid://2k0vsdw6oh8t", ITEM_TR_PATH = "res://databases/translations/items.csv";
    private const string ICON_BERRY = "[img width=50]res://sprites/ui/berries.tres[/img]",
        ICON_HUNGER = "[img width=50]res://sprites/icons/leaf.tres[/img]",
        ICON_THIRST = "[img width=50]res://sprites/icons/water.tres[/img]";
    private string[] ICONS =
    [
        "[img width=50]res://sprites/icons/speed.tres[/img]",
        "[img width=50]res://sprites/icons/strength.tres[/img]",
        "[img width=50]res://sprites/icons/intelligence.tres[/img]",
        "[img width=50]res://sprites/icons/stamina.tres[/img]"
    ];
    internal Dictionary<string, ItemData> ITEM_DATABASE = [];
    private Dictionary<string, string[]> ITEM_TRANSLATIONS = [];
    internal Dictionary<string, RecipeData> RECIPE_DATABASE = [];
    private string selected_item_id;

    private enum SortingMode { Name, Cost, Tier, Type }
    internal enum LocaleEditor { English, Spanish }
    private int current = 0;

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
        for (int i = 0; i < Enum.GetValues<SortingMode>().Length; i++)
            sorting_options.AddItem(((SortingMode)i).ToString());

        // data loading
        ITEM_TRANSLATIONS = ItemGUIInit.FETCH_TRANSLATION_DATABASE("uid://6s5xx5hshyku");
        LOAD_ALL_DATA();
        CLEAR_ITEM();
        CREATE_GRID([.. ITEM_DATABASE.Values]);

        // events
        sorting_options.ItemSelected += (_) => SEARCH_ITEM_ID(search_bar.Text);
    }
    internal void SUMMON_ITEM_CREATOR(bool _showSelectedItem)
    {
        ItemCreator gui = (ResourceLoader.Load("uid://bafd4ijwyt0v5") as PackedScene).Instantiate().Duplicate() as ItemCreator;
        AddChild(gui);

        gui.INITIALIZE_INSTANCE(_showSelectedItem ? selected_item_id : string.Empty);
    }
    internal void SUMMON_RECIPE_CREATOR()
    {
        RecipeCreator gui = (ResourceLoader.Load("uid://bmg1lo68dy4xl") as PackedScene).Instantiate().Duplicate() as RecipeCreator;
        AddChild(gui);

        gui.INITIALIZE_INSTANCE(selected_item_id);
    }
    internal void SHOW_TAB(int _index)
    {
        if (current == _index)
            return;

        panelsNode.GetChild<Control>((int)current).Hide();
        panelsNode.GetChild<Control>(_index).Show();
        current = _index;
    }

    // MARK: Processing
    internal static void REFRESH(string _id = "")
    {
        Instance.UNLOAD_ALL_DATA();
        Instance.LOAD_ALL_DATA();
        Instance.CREATE_GRID([.. Instance.ITEM_DATABASE.Values]);

        Instance.CLEAR_ITEM();
        if (!string.IsNullOrWhiteSpace(_id) && Instance.ITEM_DATABASE.ContainsKey(_id))
            Instance.SELECT_ITEM(_id);

        Instance.SEARCH_ITEM_ID(Instance.search_bar.Text);
    }
    internal static void REFRESH()
    {
        REFRESH(Instance.selected_item_id);
    }
    internal void LOAD_ALL_DATA()
    {
        ITEM_DATABASE = LOAD_ITEM_DATABASE();
        RECIPE_DATABASE = LOAD_RECIPE_DATABASE();
    }
    internal void UNLOAD_ALL_DATA()
    {
        ITEM_DATABASE.Clear();
        RECIPE_DATABASE.Clear();
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

        bool _check = true;
        foreach (var _pair in _list)
        {
            for (int r = 0; r < _pair.Value.Combinations.Count; r++)
            {
                if (!IS_RECIPE_MATCH_VALID(_pair.Value.Combinations[r], out RecipeData _existing, true, _pair.Key))
                {
                    _check = false;
                    GD.Print($"{_existing.Owner.ID} has the same recipe as {_pair.Key}");
                }
            }
        }
        if (_check)
            GD.Print("Succesfully verified all recipes");

        return _list;
    }
    internal void CREATE_GRID(List<ItemData> _data)
    {
        CLEAR_GRID(item_grid);
        for (int i = 0; i < _data.Count; i++)
        {
            var _slot = ResourceLoader.Load<PackedScene>(ITEM_SLOT_PREFAB).Instantiate() as BaseButton;
            string _id = _data[i].ID;
            _slot.Name = _id;
            _slot.Pressed += () => SELECT_ITEM(_id);

            // icon
            _slot.GetChild<TextureRect>(0).Texture = GET_ITEM_ICON(_data[i].ID, _data[i].Type);
            //name
            _slot.GetChild<Label>(1).Text = _data[i].ID;
            // color
            if (_data[i].Type == ItemData.ItemType.Food)
                _slot.SelfModulate = new Color(0xfdde36ff);
            else if (_data[i].Type == ItemData.ItemType.Structure)
                _slot.SelfModulate = new Color(0x00eac7ff);

            item_grid.AddChild(_slot);
        }
    }
    internal static void CLEAR_GRID(Container _grid)
    {
        for (int i = 0; i < _grid.GetChildCount(); i++)
        {
            var _node = _grid.GetChild(i);
            if (!_node.IsQueuedForDeletion())
                _node.QueueFree();
        }
    }
    internal void SEARCH_ITEM_ID(string _search)
    {
        List<ItemData> _newData = [.. ITEM_DATABASE.Values];

        for (int i = _newData.Count - 1; i >= 0; i--)
        {
            if (!_newData[i].ID.Contains(_search))
                _newData.RemoveAt(i);
        }

        CREATE_GRID(SORT_LIST(_newData));
    }
    internal List<ItemData> SORT_LIST(List<ItemData> _list)
    {
        return (SortingMode)sorting_options.GetSelectedId() switch
        {
            SortingMode.Name => [.. _list.OrderBy((d) => { return d.ID; })],
            SortingMode.Cost => [.. _list.OrderBy((d) => { return d.Cost; })],
            SortingMode.Tier => [.. _list.OrderBy((d) => { return d.TierRequirement; })],
            SortingMode.Type => [.. _list.OrderBy((d) => { return d.Type; })],
            _ => _list,
        };
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

    internal void SELECT_ITEM(string _id)
    {
        ItemData _data = ITEM_DATABASE[_id];
        name.Text = GET_ITEM_NAME(_id);
        selected_item_id = _id;

        icon.Texture = GET_ITEM_ICON(_data.ID, _data.Type);

        item_stats.Text =
                $"Category: {_data.Category}\n" +
                $"Shop: {_data.Shop}\n" +
                $"{ICON_BERRY}[color=gold]{_data.Cost}[/color]({_data.Cost / 2}) (Tier {_data.TierRequirement})";

        SET_FOOD(_id);
        SET_RECIPES(_id);

        addRecipeButton.Visible = _data.Type != ItemData.ItemType.Structure;
        updateButton.Show();
        deleteButton.Show();
    }
    private void SET_FOOD(string _id)
    {
        if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres"))
        {
            FoodData _food_data = ResourceLoader.Load<FoodData>(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres");
            food_stats.Text = $"{ICON_HUNGER}{_food_data.FoodValue} {ICON_THIRST}{_food_data.DrinkValue} ([color=gold]{_food_data.Flavor}[/color])\n";

            if (_food_data.Skills?.Count > 0)
            {
                foreach (var _pair in _food_data.Skills)
                    food_stats.Text += ICONS[(int)_pair.Key] + _pair.Value;
            }
            else
                food_stats.Text += "No Bonus";
        }
        else
            food_stats.Text = "No Food Data";
    }
    internal void SET_RECIPES(string _id)
    {
        CLEAR_GRID(recipe_grid);
        if (!GET_ITEM_RECIPE(_id, out RecipeData _recipes))
        {
            recipeLabel.Show();
            return;
        }

        if (_recipes.Combinations == null || _recipes.Combinations.Count == 0)
            recipeLabel.Show();
        else for (int i = 0; i < _recipes.Combinations.Count; i++)
        {
            var _slot = ResourceLoader.Load<PackedScene>(RECIPE_SLOT_PREFAB).Instantiate();

            // icon
            _slot.GetChild<TextureRect>(1).Texture = GET_ITEM_ICON(_recipes.Combinations[i][0].Item.ID, _recipes.Combinations[i][0].Item.Type);
            if (_recipes.Combinations[i].Count > 1)
                _slot.GetChild<TextureRect>(2).Texture = GET_ITEM_ICON(_recipes.Combinations[i][1].Item.ID, _recipes.Combinations[i][1].Item.Type);
            else
            {
                _slot.GetChild(2).QueueFree();
                _slot.GetChild(3).QueueFree();
            }
            // button
            _slot.GetChild<Button>(4).Pressed += () => RecipeCreator.DELETE_RECIPE(selected_item_id, _slot.GetIndex());

            recipe_grid.AddChild(_slot);
            recipeLabel.Hide();
        }
    }
    internal void CLEAR_ITEM()
    {
        name.Text = item_stats.Text = food_stats.Text = string.Empty;
        icon.Texture = null;
        selected_item_id = string.Empty;
        CLEAR_GRID(recipe_grid);
        updateButton.Hide();
        deleteButton.Hide();
        addRecipeButton.Hide();
        recipeLabel.Hide();
    }
    private void CONFIRM_ITEM_DELETION()
    {
        if (string.IsNullOrWhiteSpace(selected_item_id))
            return;
        ConfirmationDialog _dialog = new();
        _dialog.CloseRequested += _dialog.QueueFree;
        _dialog.Canceled += _dialog.QueueFree;
        _dialog.Confirmed += () =>
        {
            string _id = selected_item_id;

            AcceptDialog _popup = new();
            _popup.CloseRequested += _popup.QueueFree;
            _popup.Confirmed += _popup.QueueFree;
            Error _error = DELETE_ITEM(selected_item_id);
            if (_error != Error.Ok)
                 _popup.DialogText = $"Unable to delete {_id}. Error Code: {_error}";
            else
                _popup.DialogText = $"{_id} was deleted.";

            AddChild(_popup);
            _popup.PopupCentered();
            _dialog.QueueFree();
        };
        _dialog.DialogText = $"Are you sure you want to delete {selected_item_id}?";
        AddChild(_dialog);
        _dialog.PopupCentered();
    }
    internal static Error DELETE_ITEM(string _id)
    {
        if (string.IsNullOrWhiteSpace(_id) || !FileAccess.FileExists(GlobalManager.ABSOLUTE_ITEMS_DB_PATH + _id + ".tres"))
            return Error.FileNotFound;

        var _errorMain = DirAccess.RemoveAbsolute(GlobalManager.ABSOLUTE_ITEMS_DB_PATH + _id + ".tres");
        if (_errorMain != Error.Ok)
        {
            GD.PrintErr("Unable to delete item. ", _errorMain);
            return _errorMain;
        }

        if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres"))
        {
            var _errorFood = DirAccess.RemoveAbsolute(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres");
            if (_errorFood != Error.Ok)
            {
                GD.PrintErr("Unable to delete food. ", _errorFood);
                return _errorFood;
            }
        }

        if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _id + ".tres"))
        {
            var _errorRecipe = DirAccess.RemoveAbsolute(GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _id + ".tres");
            if (_errorRecipe != Error.Ok)
            {
                GD.PrintErr("Unable to delete recipe. ", _errorRecipe);
                return _errorRecipe;
            }
        }

        REFRESH();
        return Error.Ok;
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
    internal static void SET_ITEM_NAME(string _id, string _text, LocaleEditor _locale = LocaleEditor.English)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_name"))
            Instance.ITEM_TRANSLATIONS[_id + "_name"][0] = _text;
        else
            Instance.ITEM_TRANSLATIONS.Add(_id + "_name", [_text, _text]);
    }
    internal static void SET_ITEM_DESC(string _id, string _text, LocaleEditor _locale = LocaleEditor.English)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_desc"))
            Instance.ITEM_TRANSLATIONS[_id + "_desc"][0] = _text;
        else
            Instance.ITEM_TRANSLATIONS.Add(_id + "_desc", [_text, _text]);
    }
    internal static string GET_ITEM_NAME(string _id)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_name"))
            return Instance.ITEM_TRANSLATIONS[_id + "_name"][0];
        else
            return _id + "_name";
    }
    internal static string GET_ITEM_DESC(string _id)
    {
        if (Instance.ITEM_TRANSLATIONS.ContainsKey(_id + "_desc"))
            return Instance.ITEM_TRANSLATIONS[_id + "_desc"][0];
        else
            return _id + "_desc";
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
        foreach (var _pair in Instance.RECIPE_DATABASE)
        {
            for (int r = 0; r < _pair.Value.Combinations.Count; r++)
            {
                int _count = 0;
                for (int i = 0; i < _recipe.Count; i++)
                {
                    if (_pair.Value.Combinations[r].Contains(_recipe[i]))
                        _count++;
                }
                if (_count == _recipe.Count)
                {
                    if (_exemptItself && _pair.Key == _idItself)
                        continue;

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