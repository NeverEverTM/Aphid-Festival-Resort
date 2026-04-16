#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class ItemCreator : Window
{
    [Export] private LineEdit fileNameEdit, nameEdit, costEdit, shopOrderEdit;
    [Export] private TextEdit descriptionEdit;
    [Export] private TextureRect iconTexture;
    [Export] private OptionButton tierOptions, categoryOptions, shopOptions, typeOptions;
    [Export] private Button finishButton, deleteButton;
    [ExportGroup("Food Options")]
    [Export] private Control foodPanel;
    [Export] private HSlider hungerSlider, thirstSlider;
    [Export] private OptionButton flavorOptions;
    [Export] private Control skillsNode;

    internal void INITIALIZE_INSTANCE(string _id)
    {
        CloseRequested += QueueFree;

        SET_OPTION_BUTTONS();

        if (!string.IsNullOrWhiteSpace(_id))
        {
            DISPLAY_ITEM(_id);
            finishButton.Text = "Update";
            deleteButton.Show();
        }
    }

    private void IS_VALID_FILENAME(string _text, LineEdit _source)
    {
        if (string.IsNullOrWhiteSpace(_text) || !_source.Editable)
            return;
        if (!_text.IsValidFileName())
        {
            _source.Text = string.Empty;
            _source.SelfModulate = new Color("red");
            return;
        }
        if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_ITEMS_DB_PATH + _text + ".tres"))
        {
            _source.Text = string.Empty;
            _source.SelfModulate = new Color("red");
            return;
        }
        _source.SelfModulate = new Color("white");
        UPDATE_ICON(_text);
    }
    private void IS_VALID_FILENAME(LineEdit _source) =>
        IS_VALID_FILENAME(_source.Text, _source);
    private void IS_VALID_NUMBER(string _text, LineEdit _source)
    {
        if (string.IsNullOrWhiteSpace(_text))
            return;
        if (!int.TryParse(_text, out int _value))
        {
            _source.Text = string.Empty;
            return;
        }
        if (_value <= 0)
        {
            _source.Text = string.Empty;
            return;
        }
    }
    private void IS_VALID_NUMBER(LineEdit _source) =>
        IS_VALID_NUMBER(_source.Text, _source);

    private void SET_OPTION_BUTTONS()
    {
        for (int i = 0; i < Enum.GetValues<ItemData.ItemType>().Length; i++)
            typeOptions.AddItem(((ItemData.ItemType)i).ToString());
        for (int i = -1; i < Enum.GetValues<ItemData.CategoryTags>().Length - 1; i++)
            categoryOptions.AddItem(((ItemData.CategoryTags)i).ToString());
        for (int i = 0; i < Enum.GetValues<ItemData.ShopOwner>().Length; i++)
            shopOptions.AddItem(((ItemData.ShopOwner)i).ToString());
        for (int i = 0; i < Enum.GetValues<AphidData.FoodType>().Length; i++)
            flavorOptions.AddItem(((AphidData.FoodType)i).ToString());
    }
    private void ENABLE_FOOD_OPTIONS(bool _value)
    {
        if (foodPanel.Visible == _value)
            return;

        if (_value)
        {
            Size += new Vector2I(0, 125);
            foodPanel.Show();
        }
        else
        {
            Size -= new Vector2I(0, 125);
            foodPanel.Hide();
        }
    }
    private void ON_SELECTED(int _index)
    {
        if (_index == (int)ItemData.ItemType.Food)
            ENABLE_FOOD_OPTIONS(true);
        else
            ENABLE_FOOD_OPTIONS(false);
    }

    private void DISPLAY_ITEM(string _id)
    {
        ItemData _data = ItemViewer.GET_ITEM_DATA(_id);

        fileNameEdit.Text = _id;
        fileNameEdit.Editable = false;
        fileNameEdit.Flat = true;

        nameEdit.Text = ItemViewer.GET_ITEM_NAME(_id);
        descriptionEdit.Text = ItemViewer.GET_ITEM_DESC(_id);

        tierOptions.Select(_data.TierRequirement);

        if (ResourceLoader.Exists(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".tres"))
            iconTexture.Texture = ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + _id + ".tres");
        else
            iconTexture.Texture = ResourceLoader.Load<Texture2D>(GlobalManager.ABSOLUTE_ICONS_PATH + "unknown.tres");

        categoryOptions.Select((int)_data.Category);
        shopOptions.Select((int)_data.Shop);
        costEdit.Text = _data.Cost.ToString();
        typeOptions.Select((int)_data.Type);

        if (_data.Type == ItemData.ItemType.Food)
        {
            DISPLAY_FOOD(_id);
            ENABLE_FOOD_OPTIONS(true);
        }
    }
    private void DISPLAY_FOOD(string _id)
    {
        if (!ResourceLoader.Exists(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres"))
            return;

        FoodData _food_data = ResourceLoader.Load<FoodData>(GlobalManager.ABSOLUTE_FOODS_DB_PATH + _id + ".tres");
        hungerSlider.Value = _food_data.FoodValue;
        thirstSlider.Value = _food_data.DrinkValue;
        flavorOptions.Select((int)_food_data.Flavor);

        if (_food_data.Skills?.Count > 0)
        {
            foreach (var _pair in _food_data.Skills)
            {
                skillsNode.GetNode<CheckButton>(_pair.Key.ToString().ToLower()).SetPressedNoSignal(true);
                skillsNode.GetNode<CheckButton>(_pair.Key.ToString().ToLower()).GetChild<LineEdit>(0).Text = _pair.Value.ToString();
            }
        }
    }
    private void CREATE_ITEM()
    {
        if (string.IsNullOrWhiteSpace(fileNameEdit.Text) || !fileNameEdit.Text.IsValidFileName())
            return;
        string _id = fileNameEdit.Text;

        if (!string.IsNullOrWhiteSpace(nameEdit.Text))
            ItemViewer.SET_ITEM_NAME(fileNameEdit.Text, nameEdit.Text);

        if (!string.IsNullOrWhiteSpace(descriptionEdit.Text))
            ItemViewer.SET_ITEM_DESC(fileNameEdit.Text, descriptionEdit.Text);

        if (!int.TryParse(costEdit.Text, out int _cost))
            _cost = 1;

        if (!int.TryParse(shopOrderEdit.Text, out int _priority))
            _priority = 0;

        ItemData _data = new()
        {
            ID = fileNameEdit.Text,
            Type = (ItemData.ItemType)typeOptions.GetSelectedId(),
            Cost = _cost,
            TierRequirement = tierOptions.GetSelectedId(),
            Category = (ItemData.CategoryTags)categoryOptions.GetSelectedId() - 1,
            Shop = (ItemData.ShopOwner)shopOptions.GetSelectedId(),
            ShopOrderPriority = _priority,
        };

        _data.TakeOverPath(ItemViewer.GET_ITEM_FILEPATH(_id));
        ResourceSaver.Save(_data, ItemViewer.GET_ITEM_FILEPATH(_id));
        if (_data.Type == ItemData.ItemType.Food)
            CREATE_FOOD(_id);

        bool _created = fileNameEdit.Editable;

        ItemViewer.REFRESH(_id);
        AcceptDialog _popup = new()
        {
            DialogText = $"{_id} was {(_created ? "created" : "updated")}!"
        };
        _popup.CloseRequested += _popup.QueueFree;
        _popup.Confirmed += _popup.QueueFree;
        if (_created)
            ItemViewer.Instance.AddChild(_popup);
        else
            AddChild(_popup);
        _popup.PopupCentered();
        if (_created)
            QueueFree();
    }
    private void CREATE_FOOD(string _id)
    {
        FoodData _data = new()
        {
            Item = ResourceLoader.Load<ItemData>(ItemViewer.GET_ITEM_FILEPATH(_id)),
            FoodValue = (int)hungerSlider.Value,
            DrinkValue = (int)thirstSlider.Value,
            Flavor = (AphidData.FoodType)flavorOptions.GetSelectedId()
        };

        Dictionary<string, AphidData.SkillEnum> _key = new()
        {
            { "speed", AphidData.SkillEnum.Speed },
            { "intelligence", AphidData.SkillEnum.Intelligence },
            { "strength", AphidData.SkillEnum.Strength },
            { "stamina", AphidData.SkillEnum.Stamina },
        };

        for (int i = 0; i < skillsNode.GetChildCount(); i++)
        {
            if (skillsNode.GetChild<CheckButton>(i).ButtonPressed && int.TryParse(skillsNode.GetChild(i).GetChild<LineEdit>(0).Text, out int _number))
                _data.Skills.Add(_key[skillsNode.GetChild(i).Name], _number);
        }
        _data.TakeOverPath(ItemViewer.GET_FOOD_FILEPATH(_id));
        ResourceSaver.Save(_data, ItemViewer.GET_FOOD_FILEPATH(_id));
    }
    private void CONFIRM_DELETION()
    {
        ConfirmationDialog _dialog = new();
        _dialog.CloseRequested += _dialog.QueueFree;
        _dialog.Canceled += _dialog.QueueFree;
        _dialog.Confirmed += () =>
        {
            string _id = fileNameEdit.Text;
            Error _error = ItemViewer.DELETE_ITEM(_id);
            AcceptDialog _popup = new();
            void close_all()
            {
                _popup.QueueFree();
                if (_error == Error.Ok)
                    QueueFree();
            }
            if (_error == Error.Ok)
                _popup.DialogText = $"{_id} was deleted.";
            else 
                _popup.DialogText = $"Unable to delete {_id}. Error Code: {_error}";
            _popup.CloseRequested += close_all;
            _popup.Confirmed += close_all;
            ItemViewer.Instance.AddChild(_popup);
            _popup.PopupCentered();
            _dialog.QueueFree();
        };
        _dialog.DialogText = $"Are you sure you want to delete {fileNameEdit.Text}?";
        AddChild(_dialog);
        _dialog.PopupCentered();
    }
    private void UPDATE_ICON(string _text)
    {
        iconTexture.Texture = ItemViewer.GET_ITEM_ICON(_text, (ItemData.ItemType)typeOptions.GetSelectedId());
    }
}
#endif