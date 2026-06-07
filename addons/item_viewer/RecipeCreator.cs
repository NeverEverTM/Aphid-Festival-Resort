#if TOOLS
using Godot;
using System.Collections.Generic;

[Tool]
public partial class RecipeCreator : Window
{
    [Export] private Container itemGrid;
    [Export] private Control slotsNode, plusLabel;

    private TextureRect[] slots = new TextureRect[3];
    private ItemData[] ingredients = new ItemData[3];

    private enum Slot { FirstIngredient, SecondIngredient, Result }

    internal void INITIALIZE_INSTANCE(string _id)
    {
        CloseRequested += QueueFree;

        SEARCH_ITEM_ID(string.Empty);
        for (int i = 0; i < slotsNode.GetChildCount(); i++)
            slots[i] = slotsNode.GetChild<TextureRect>(i);
        for (int i = 0; i < ingredients.Length; i++)
            EMPTY_SLOT(i);

        if (!string.IsNullOrWhiteSpace(_id))
            SET_SLOT(_id, Slot.Result);
    }

    private void CREATE_GRID(List<ItemData> _data)
    {
        CLEAR_GRID(itemGrid);
        for (int i = 0; i < _data.Count; i++)
        {
            if (_data[i].Type != ItemData.ItemType.Food)
                continue;

            var _slot = ResourceLoader.Load<PackedScene>(ItemMasterDB.ITEM_SLOT_PREFAB).Instantiate() as BaseButton;
            string _id = _data[i].ID;
            _slot.Name = _id;
            _slot.Pressed += () => FILL_SLOTS(_id);

            // icon
            _slot.GetChild<TextureRect>(0).Texture = ItemMasterDB.GET_ITEM_ICON(_data[i].ID, _data[i].Type);
            //name
            _slot.GetChild<Label>(1).Text = _data[i].ID;

            itemGrid.AddChild(_slot);
        }
    }
    private static void CLEAR_GRID(Container _grid)
    {
        for (int i = 0; i < _grid.GetChildCount(); i++)
        {
            var _node = _grid.GetChild(i);
            if (!_node.IsQueuedForDeletion())
                _node.QueueFree();
        }
    }
    private void SEARCH_ITEM_ID(string _search)
    {
        List<ItemData> _newData = [.. ItemMasterDB.Instance.ITEM_DATABASE.Values];

        for (int i = _newData.Count - 1; i >= 0; i--)
        {
            if (!_newData[i].ID.Contains(_search))
                _newData.RemoveAt(i);
        }

        CREATE_GRID(_newData);
    }

    private bool SET_SLOT(string _itemID, Slot _slot)
    {
        int _index = (int)_slot;
        var _data = ItemMasterDB.GET_ITEM_DATA(_itemID);

        if (_slot != Slot.Result && _data.Type == ItemData.ItemType.Item)
            return false;

        ingredients[_index] = _data;
        slots[_index].Texture = ItemMasterDB.GET_ITEM_ICON(ingredients[_index].ID, ingredients[_index].Type);

        if (_slot != Slot.Result)
            slots[_index].GetChild<Control>(0).Show();

        if (ingredients[0] != null && ingredients[1] != null)
            plusLabel.Show();
        return true;
    }
    private void FILL_SLOTS(string _id)
    {
        for (int i = 0; i < ingredients.Length; i++)
        {
            if (ingredients[i] != null || !SET_SLOT(_id, (Slot)i))
                continue;
            break;
        }
    }
    private void EMPTY_SLOT(int _index)
    {
        ingredients[_index] = null;
        slots[_index].Texture = null;
        slots[_index].GetChild<Control>(0).Hide();

        if (ingredients[0] == null || ingredients[1] == null)
            plusLabel.Hide();
    }
    internal void CREATE_RECIPE()
    {
        if (ingredients[0] == null && ingredients[1] == null)
            return;

        Godot.Collections.Array<FoodData> _recipe = [];

        if (ingredients[(int)Slot.FirstIngredient] != null)
            _recipe.Add(ResourceLoader.Load<FoodData>(ItemMasterDB.GET_FOOD_FILEPATH(ingredients[(int)Slot.FirstIngredient].ID)));

        if (ingredients[(int)Slot.SecondIngredient] != null)
            _recipe.Add(ResourceLoader.Load<FoodData>(ItemMasterDB.GET_FOOD_FILEPATH(ingredients[(int)Slot.SecondIngredient].ID)));

        string _id = ingredients[(int)Slot.Result].ID;
        if (!ItemMasterDB.IS_RECIPE_MATCH_VALID(_recipe, out RecipeData _existing))
        {
            THROW_INVALID_WARNING(_existing.Owner.ID);
            return;
        }

        if (!ItemMasterDB.GET_ITEM_RECIPE(_id, out RecipeData _data))
            _data = new() { Combinations = [] };

        _data.Owner = ResourceLoader.Load<ItemData>(ItemMasterDB.GET_ITEM_FILEPATH(_id));
        _data.Combinations.Add(_recipe);

        ItemMasterDB.SET_ITEM_RECIPE(_id, _data);
        ItemViewer.Instance.SELECT_ITEM(_id);

        AcceptDialog _popup = new();
        _popup.CloseRequested += _popup.QueueFree;
        _popup.Confirmed += _popup.QueueFree;
        _popup.DialogText = $"{_id} was given a recipe with the following ingredients: ";

        if (_recipe.Count == 1)
            _popup.DialogText += _recipe[0].Item.ID;

        if (_recipe.Count == 2)
            _popup.DialogText += _recipe[0].Item.ID + " and " + _recipe[1].Item.ID;

        _popup.DialogText += "!";

        ItemMasterDB.Instance.AddChild(_popup);
        _popup.PopupCentered();
        QueueFree();
    }
    internal static void DELETE_RECIPE(string _id, int _index)
    {
        var _recipes = ItemMasterDB.GET_ITEM_RECIPE(_id);

        _recipes.Combinations.RemoveAt(_index);
        if (_recipes.Combinations.Count == 0)
        {
            if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _id + ".tres"))
            {
                var _errorRecipe = DirAccess.RemoveAbsolute(GlobalManager.ABSOLUTE_RECIPES_DB_PATH + _id + ".tres");
                if (_errorRecipe != Error.Ok)
                    GD.PrintErr($"Unable to delete recipe of {_id}. ", _errorRecipe);
            }
        }
        else
            ItemMasterDB.SET_ITEM_RECIPE(_id, _recipes);

        ItemViewer.Instance.SELECT_ITEM(_id);
    }
    private void THROW_INVALID_WARNING(string _existingID)
    {
        AcceptDialog _popup = new();
        _popup.CloseRequested += _popup.QueueFree;
        _popup.Confirmed += _popup.QueueFree;
        _popup.DialogText = $"This combination already exists for {_existingID}!";
        AddChild(_popup);
        _popup.PopupCentered();
    }
}
#endif