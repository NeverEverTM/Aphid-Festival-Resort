#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class ItemViewer : Control, ItemMasterDB.ITab
{
    internal static ItemViewer Instance { get; private set; }

    [Export] private LineEdit search_bar;
    [Export] private OptionButton sorting_options, type_sorting_options;
    [Export] private Container item_grid;
    [ExportGroup("Information Panel")]
    [Export] private TextureRect icon;
    [Export] private Label name, recipeLabel;
    [Export] private RichTextLabel item_stats, food_stats;
    [Export] private Container recipe_grid;
    [Export] private Control updateButton, deleteButton, addRecipeButton;

    private enum SortingMode { Name, Cost, Tier, ShopOrder }
    internal string selected_item_id;

    public void START()
    {
        Instance = this;

        for (int i = 0; i < Enum.GetValues<SortingMode>().Length; i++)
            sorting_options.AddItem(((SortingMode)i).ToString());
        for (int i = 0; i < Enum.GetValues<ItemData.ItemType>().Length; i++)
            type_sorting_options.AddItem(((ItemData.ItemType)i).ToString());
        
        sorting_options.ItemSelected += (_) => SEARCH_ITEM_ID(search_bar.Text);
        type_sorting_options.ItemSelected += (_) => SEARCH_ITEM_ID(search_bar.Text);
        CLEAR_ITEM();
        CREATE_ITEM_GRID([.. ItemMasterDB.Instance.ITEM_DATABASE.Values]);
    }
    public void ON_LOAD_TAB()
    {
        return;
    }

    internal void REFRESH_LIST(string _id = "")
    {
        CREATE_ITEM_GRID([.. ItemMasterDB.Instance.ITEM_DATABASE.Values]);

        Instance.CLEAR_ITEM();
        if (!string.IsNullOrWhiteSpace(_id) && ItemMasterDB.Instance.ITEM_DATABASE.ContainsKey(_id))
            Instance.SELECT_ITEM(_id);

        Instance.SEARCH_ITEM_ID(Instance.search_bar.Text);
    }
    internal void REFRESH()
    {
        ItemMasterDB.Instance.UNLOAD_ALL_DATA();
        ItemMasterDB.Instance.LOAD_ALL_DATA();
        REFRESH_LIST(Instance.selected_item_id);
    }

    internal void CREATE_ITEM_GRID(List<ItemData> _data)
    {
        ItemMasterDB.CLEAR_GRID(item_grid);
        for (int i = 0; i < _data.Count; i++)
        {
            var _slot = ResourceLoader.Load<PackedScene>(ItemMasterDB.ITEM_SLOT_PREFAB).Instantiate() as BaseButton;
            string _id = _data[i].ID;
            _slot.Name = _id;
            _slot.Pressed += () => SELECT_ITEM(_id);

            // icon
            _slot.GetChild<TextureRect>(0).Texture = ItemMasterDB.GET_ITEM_ICON(_data[i].ID, _data[i].Type);
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
    internal void SEARCH_ITEM_ID(string _search)
    {
        List<ItemData> _newData = [.. ItemMasterDB.Instance.ITEM_DATABASE.Values];

        for (int i = _newData.Count - 1; i >= 0; i--)
        {
            if (!_newData[i].ID.Contains(_search))
                _newData.RemoveAt(i);
        }

        CREATE_ITEM_GRID(SORT_LIST(_newData));
    }
    internal List<ItemData> SORT_LIST(List<ItemData> _list)
    {
        if (type_sorting_options.GetSelectedId() != 0)
            _list = [.. _list.Where((d) => { return (int)d.Type == (type_sorting_options.GetSelectedId() - 1); })];

        return (SortingMode)sorting_options.GetSelectedId() switch
        {
            SortingMode.Name => [.. _list.OrderBy((d) => { return d.ID; })],
            SortingMode.Cost => [.. _list.OrderBy((d) => { return d.Cost; })],
            SortingMode.Tier => [.. _list.OrderBy((d) => { return d.TierRequirement; })],
            SortingMode.ShopOrder =>  [.. 
                _list.Where((d) => d.Shop != ItemData.ShopOwner.None)
                    .Where((d) => d.ShopOrderPriority >= 0)
                    .OrderBy((d) => d.ShopOrderPriority)],
            _ => _list,
        };
    }

    internal void SELECT_ITEM(string _id)
    {
        ItemData _data = ItemMasterDB.Instance.ITEM_DATABASE[_id];
        name.Text = ItemMasterDB.GET_ITEM_NAME(_id, ItemMasterDB.LocaleEditor.English);
        selected_item_id = _id;

        icon.Texture = ItemMasterDB.GET_ITEM_ICON(_data.ID, _data.Type);

        item_stats.Text =
                $"Category: {_data.Category}\n" +
                $"Shop: {_data.Shop}([color=gold]{_data.ShopOrderPriority}[/color])\n" +
                $"{ItemMasterDB.ICON_BERRY}[color=gold]{_data.Cost}[/color]({_data.Cost / 2}) (Tier {_data.TierRequirement})";

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
            food_stats.Text = $"{ItemMasterDB.ICON_HUNGER}{_food_data.FoodValue} {ItemMasterDB.ICON_THIRST}{_food_data.DrinkValue} ([color=gold]{_food_data.Flavor}[/color])\n";

            if (_food_data.Skills?.Count > 0)
            {
                foreach (var _pair in _food_data.Skills)
                    food_stats.Text += ItemMasterDB.ICONS[(int)_pair.Key] + _pair.Value;
            }
            else
                food_stats.Text += "No Bonus";
        }
        else
            food_stats.Text = "No Food Data";
    }
    internal void SET_RECIPES(string _id)
    {
        ItemMasterDB.CLEAR_GRID(recipe_grid);
        if (!ItemMasterDB.GET_ITEM_RECIPE(_id, out RecipeData _recipes))
        {
            recipeLabel.Show();
            return;
        }

        if (_recipes.Combinations == null || _recipes.Combinations.Count == 0)
            recipeLabel.Show();
        else for (int i = 0; i < _recipes.Combinations.Count; i++)
            {
                var _slot = ResourceLoader.Load<PackedScene>(ItemMasterDB.RECIPE_SLOT_PREFAB).Instantiate();

                // icon
                _slot.GetChild<TextureRect>(1).Texture = ItemMasterDB.GET_ITEM_ICON(_recipes.Combinations[i][0].Item.ID, _recipes.Combinations[i][0].Item.Type);
                if (_recipes.Combinations[i].Count > 1)
                    _slot.GetChild<TextureRect>(2).Texture = ItemMasterDB.GET_ITEM_ICON(_recipes.Combinations[i][1].Item.ID, _recipes.Combinations[i][1].Item.Type);
                else
                {
                    _slot.GetChild<Control>(1).SetPosition(new(100, 10));
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
        ItemMasterDB.CLEAR_GRID(recipe_grid);
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

        Instance.REFRESH();
        return Error.Ok;
    }
}
#endif
