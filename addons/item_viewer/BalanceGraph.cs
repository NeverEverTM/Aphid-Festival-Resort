#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Tool]
public partial class BalanceGraph : Control, ItemMasterDB.ITab
{
    [Export] private PackedScene slot;
    [Export] private Container slotContainer;
    [Export] private LineEdit search_bar;
    [Export] private OptionButton sorting_options;
    [ExportGroup("Graph")]
    [Export] private Control graph_node;

    // Sweet, Sour, Salty, Bitter, Vile, Bland, Neutral 
    private string[] type_colors =
    [
        new("gold"),
        new("green"),
        new("orange"),
        new("blue"),
        new("purple"),
        new("gray"),
        new("white")
    ];

    public enum SortingMode
    {
        Hunger, Thirst, Flavor, Tier, Cost,  BothNeeds
    }

    public void START()
    {
        sorting_options.ItemSelected += (_) => SEARCH_ITEM_ID(search_bar.Text);
        for (int i = 0; i < Enum.GetValues<SortingMode>().Length; i++)
            sorting_options.AddItem(((SortingMode)i).ToString());
    }
    public void ON_LOAD_TAB()
    {
        SEARCH_ITEM_ID(string.Empty);
    }

    internal void CREATE_FOOD_GRID(List<FoodData> _list)
    {
        ItemMasterDB.CLEAR_GRID(slotContainer);
        ItemMasterDB.CLEAR_GRID(graph_node);

        for (int i = 0; i < _list.Count; i++)
        {
            var _foodSlot = slot.Instantiate();
            _foodSlot.GetChild<TextureRect>(0).Texture = ItemMasterDB.GET_ITEM_ICON(_list[i].Item.ID, ItemData.ItemType.Food);
            _foodSlot.GetChild<RichTextLabel>(1).Text = ItemMasterDB.ICON_HUNGER + _list[i].FoodValue.ToString();
            _foodSlot.GetChild<RichTextLabel>(2).Text = ItemMasterDB.ICON_THIRST + _list[i].DrinkValue.ToString();
            _foodSlot.GetChild<RichTextLabel>(3).Text = $"[color={type_colors[(int)_list[i].Flavor]}]{_list[i].Flavor}[/color]";
            _foodSlot.GetChild<RichTextLabel>(4).Text = $"Tier {_list[i].Item.TierRequirement}";
            _foodSlot.GetChild<RichTextLabel>(5).Text = $"{ItemMasterDB.ICON_BERRY} {_list[i].Item.Cost}";

            StringBuilder _skills = new();
            if (_list[i].Skills?.Count > 0)
            {
                foreach (var _pair in _list[i].Skills)
                {
                    _skills.Append(ItemMasterDB.ICONS[(int)_pair.Key] + _pair.Value);
                    _skills.Append(' ');
                }
            }
            else
                _skills.Append("No Bonus");
            _foodSlot.GetChild<RichTextLabel>(6).Text = _skills.ToString();

            TextureRect _iconGraph = new()
            {
                Texture = _foodSlot.GetChild<TextureRect>(0).Texture,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Size = new(18, 18),
                Position = GET_GRAPH_POSITION(_list[i], i)
            };

            graph_node.AddChild(_iconGraph);

            slotContainer.AddChild(_foodSlot);
        }
    }
    internal Vector2 GET_GRAPH_POSITION(FoodData _data, int _index)
    {
        return (SortingMode)sorting_options.GetSelectedId() switch
        {
            SortingMode.Hunger => new(_index * 10, _data.FoodValue),
            SortingMode.Thirst => new(_index * 10, _data.DrinkValue),
            SortingMode.BothNeeds => new(_data.FoodValue * 4, _data.DrinkValue * 4),
            SortingMode.Cost => new(_index * 10, _data.Item.Cost),
            SortingMode.Tier => new(_index * 10, _data.Item.TierRequirement),
            SortingMode.Flavor => new(_index * 4, (int)_data.Flavor * 10),
            _ => new()
        };
    }

    internal void SEARCH_ITEM_ID(string _search)
    {
        List<FoodData> _newData = [.. ItemMasterDB.Instance.FOOD_DATABASE.Values];

        for (int i = _newData.Count - 1; i >= 0; i--)
        {
            if (!_newData[i].Item.ID.Contains(_search))
                _newData.RemoveAt(i);
        }

        CREATE_FOOD_GRID(SORT_LIST(_newData));
    }
    internal List<FoodData> SORT_LIST(List<FoodData> _list)
    {
        return (SortingMode)sorting_options.GetSelectedId() switch
        {
            SortingMode.Hunger => [.. _list.OrderBy((d) => { return d.FoodValue; })],
            SortingMode.Thirst => [.. _list.OrderBy((d) => { return d.DrinkValue; })],
            SortingMode.BothNeeds => [.. _list.OrderBy((d) => { return d.FoodValue + d.DrinkValue; })],
            SortingMode.Cost => [.. _list.OrderBy((d) => { return d.Item.Cost; })],
            SortingMode.Tier => [.. _list.OrderBy((d) => { return d.Item.TierRequirement; })],
            SortingMode.Flavor => [.. _list.OrderBy((d) => { return d.Flavor; })],
            _ => _list,
        };
    }
}
#endif