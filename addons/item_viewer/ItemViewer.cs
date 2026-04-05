#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class ItemViewer : Window
{
    [Export] private LineEdit search_bar;
    [Export] private OptionButton sorting_options;
    [Export] private Container item_grid;
    [ExportGroup("Information Panel")]
    [Export] private TextureRect icon;
    [Export] private Label name;
    [Export] private RichTextLabel item_stats, food_stats;
    [Export] private Container recipe_grid;

    private const string ITEM_SLOT_PREFAB = "uid://c140r8dv4t86f", RECIPE_SLOT_PREFAB = "uid://2k0vsdw6oh8t";
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
    private Dictionary<string, ItemData> LOADED_DATA = [];
    internal static Dictionary<string, string> ITEM_TRANSLATIONS;
    private string selected_item_id;

    private enum SortingMode { Name, Cost, Tier }

    // MARK: Intialization
    internal void INITIALIZE_INSTANCE()
	{
		CloseRequested += QueueFree;

        // data loading
        ITEM_TRANSLATIONS = ItemGUIInit.FETCH_TRANSLATION_DATABASE("uid://6s5xx5hshyku");
        LOAD_DATA();
        CLEAR_ITEM();
        CREATE_GRID([.. LOADED_DATA.Values]);

        // events
        sorting_options.ItemSelected += (_) => SEARCH_ITEM_ID(search_bar.Text);
	}
    internal void SUMMON_ITEM_CREATOR(bool _showSelectedItem)
    {
        ItemCreator gui = (ResourceLoader.Load("uid://bafd4ijwyt0v5") as PackedScene).Instantiate().Duplicate() as ItemCreator;
		AddChild(gui);
        
		gui.INITIALIZE_INSTANCE(_showSelectedItem ? selected_item_id : string.Empty);
    } 

    // MARK: Processing
    internal void LOAD_DATA()
    {
        var _files = DirAccess.GetFilesAt(GlobalManager.ABSOLUTE_ITEMS_DB_PATH);
        for (int i = 0; i < _files.Length; i++)
        {
            if (!_files[i].EndsWith(".tres"))
                continue;
            LOADED_DATA.Add(_files[i].Replace(".tres", string.Empty), ResourceLoader.Load<ItemData>(GlobalManager.ABSOLUTE_ITEMS_DB_PATH + _files[i]));
        }
    }
    internal void CREATE_GRID(List<ItemData> _data)
    {
        CLEAR_GRID(item_grid);
        for (int i = 0; i < _data.Count; i++)
        {
            var _slot = ResourceLoader.Load<PackedScene>(ITEM_SLOT_PREFAB).Instantiate() as BaseButton;
            string _id = _data[i].ID;
            _slot.Name = _id;
            _slot.Pressed += () => SET_ITEM(_id);

            // icon
            _slot.GetChild<TextureRect>(0).Texture = ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _data[i].ID + ".tres");
            //name
            _slot.GetChild<Label>(1).Text = ITEM_TRANSLATIONS[_data[i].ID + "_name"];

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
        List<ItemData> _newData = [.. LOADED_DATA.Values];

        for (int i = _newData.Count - 1; i >= 0; i--)
        {
            if (!_newData[i].ID.StartsWith(_search))
                _newData.RemoveAt(i);
        }

        CREATE_GRID(SORT_LIST(_newData));
    }
    internal List<ItemData> SORT_LIST(List<ItemData> _list)
    {
        switch ((SortingMode)sorting_options.GetSelectedId())
        {
            case SortingMode.Name:
                return [.. _list.OrderBy((d) => { return d.ID; })];
            case SortingMode.Cost:
                return [.. _list.OrderBy((d) => { return d.Cost; })];
            case SortingMode.Tier:
                return [.. _list.OrderBy((d) => { return d.TierRequirement; })];
            default:
                return _list;
        }
    }

    internal void SET_ITEM(string _id)
    {
        var _data = LOADED_DATA[_id];
        name.Text = ITEM_TRANSLATIONS[_id + "_name"] + $" (Tier {_data.TierRequirement})";
        selected_item_id = _id;
        icon.Texture = ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".tres");

        item_stats.Text =
                $"Category: {_data.Category}\n" +
                $"Shop: {_data.Shop}\n" +
                $"{ICON_BERRY}{_data.Cost}([color=gold]{_data.Cost / 2}[/color])";

        if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres"))
        {
            FoodData _food_data = ResourceLoader.Load<FoodData>(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres");
            food_stats.Text = $"{ICON_HUNGER}{_food_data.FoodValue} {ICON_THIRST}{_food_data.DrinkValue} Flavor: {_food_data.Flavor}\n";

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

        CLEAR_GRID(recipe_grid);
        if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _id + ".tres"))
        {
            var _recipes = ResourceLoader.Load<RecipeData>(GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _id + ".tres");
            for(int i = 0; i < _recipes?.Combinations.Count; i++)
            {
                var _slot = ResourceLoader.Load<PackedScene>(RECIPE_SLOT_PREFAB).Instantiate();

                _slot.GetChild(0).GetChild<TextureRect>(0).Texture = ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _recipes.Combinations[i][0].Item.ID + ".tres");
                if (_recipes.Combinations[i].Count > 1)
                    _slot.GetChild(2).GetChild<TextureRect>(0).Texture = ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _recipes.Combinations[i][1].Item.ID + ".tres");
                else
                {
                    _slot.GetChild(1).QueueFree();
                    _slot.GetChild(2).QueueFree();
                }
                recipe_grid.AddChild(_slot);
            }
        }
    }
    internal void CLEAR_ITEM()
    {
        name.Text = item_stats.Text = food_stats.Text = string.Empty;
        icon.Texture = null;
        selected_item_id = string.Empty;
        CLEAR_GRID(recipe_grid);
    }
}
#endif