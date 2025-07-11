using Godot;
using System;
using System.Linq;

public partial class LobbyMenu : Control, MenuTrigger.ITrigger
{
    private MenuInstance menu;
    [Export] private AnimationPlayer player;
    [Export] private TextureButton[] categoryButtons;
    [Export] private Control[] categoryNodes;
    [ExportGroup("Aphid")]
    [Export] private TextureButton moveAphidButton, sellAphidButton;
    [Export] private Label aphidCost, aphidName;
    [Export] private GridContainer aphidContainer;
    [Export] private Control aphidNode;
    [ExportGroup("Stats")]
    [Export] private RichTextLabel statsDisplay, pronounsLabel, nameLabel;
    [Export] private Label pronounsDisplay, nameDisplay;

    private enum Categories { Aphid, Resort, Stats }
    private Categories Category;

    private Guid current_key;
    private Control current_slot;

    private Color default_category_color;

    public override void _EnterTree()
    {
        // create menu
        menu = new("lobby", player, (_) =>
        {
            UpdateAphidPanel();
            UpdateStats();
        });

        // setup all category buttons
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            int index = i;
            (categoryButtons[i].GetChild(1) as Label).Text = "lobby_category_" + ((Categories)index).ToString().ToLower();
            categoryButtons[i].Pressed += () => SetCategory((Categories)index);
        }
        
        // set default values
        default_category_color = categoryButtons[0].GetChild<Control>(0).SelfModulate;
        SetCategory(Categories.Aphid, true);
        sellAphidButton.Pressed += () => ConfirmationPopup.Create(SellAphid, null,
            ConfirmationPopup.ConfirmationEnum.Fast);
    }

    public void SetMenu() =>
        _= CanvasManager.Menus.SetTo(menu);
    private void SetCategory(Categories _category, bool _force = false)
    {
        if (_category.Equals(Category) && !_force)
            return;
        int _current = (int)Category, _new = (int)_category;

        categoryButtons[_current].GetChild<Control>(0).SelfModulate = default_category_color;
        categoryNodes[_current].Hide();
        Category = _category;
        categoryButtons[_new].GetChild<Control>(0).SelfModulate = new Color("darkred");
        categoryNodes[_new].Show();
        if (!_force)
            SoundManager.CreateSound("ui/button_switch");
    }

    private void UpdateAphidPanel()
    {
        ClearAphidPanel();

        foreach (var _pair in GameManager.Aphids)
        {
            var _pair_clone = _pair;
            aphidContainer.AddChild(CanvasManager.CreateAphidSlot(_pair_clone.Key, false, SetAphidIcon));
        }
    }
    private void ClearAphidPanel()
    {
        if (aphidNode.GetChildCount() > 0)
            aphidNode.GetChild(0).QueueFree();

        for (int i = 0; i < aphidContainer.GetChildCount(); i++)
            aphidContainer.GetChild(i).QueueFree();

        current_key = Guid.Empty;

        aphidName.Hide();
        aphidCost.Hide();
        moveAphidButton.Hide();
        sellAphidButton.Hide();
    }
    private void SetAphidIcon(Guid _key)
    {
        if (aphidNode.GetChildCount() > 0)
            aphidNode.GetChild(0).QueueFree();

        var _aphid = GameManager.Aphids[_key];

        current_key = _key;

        // set interface
        aphidCost.Text = GetAphidValue(_aphid).ToString();
        aphidName.Text = _aphid.Genes.Name;
        aphidNode.AddChild(CanvasManager.CreateAphidSlot(_key, true));

        aphidName.Show();
        aphidCost.Show();
        moveAphidButton.Show();
        sellAphidButton.Show();
    }
    private void SellAphid()
    {
        int _value = GetAphidValue(GameManager.Aphids[current_key]);
        Player.Data.AddCurrency(_value);
        GameManager.RemoveAphid(current_key);
        GameManager.Data.AphidsSold++;
        SoundManager.CreateSound("ui/kaching");
        UpdateAphidPanel();
    }
    private static int GetAphidValue(AphidInstance _aphid)
    {
        int _value = _aphid.Status.IsAdult ? 50 : 25;

        foreach (var _skill in _aphid.Genes.Skills)
            _value += _skill.Value.Level * 2;

        return _value;
    }
    private void UpdateStats()
    {
        nameLabel.Text = $"[bgcolor=darkred]{Tr("new_game_player_name")}[/bgcolor]";
        nameDisplay.Text = Player.Data.Name;
        pronounsLabel.Text = $"[bgcolor=darkred]{Tr("new_game_pronouns")}[/bgcolor]";
        pronounsDisplay.Text = Player.Data.Pronouns.Join("/");
        string[] _stats = [
        $"[color=gold]{Tr("lobby_stats_totalaphids")}:[/color] {GameManager.Aphids.Count + GameManager.AphidArchive.Count}",
        $"[color=crimson]{Tr("lobby_stats_aphidcount")}:[/color] {GameManager.Aphids.Count}",
        $"[color=crimson]{Tr("lobby_stats_aphidssold")}:[/color] {GameManager.Data.AphidsSold}",
        $"[color=crimson]{Tr("lobby_stats_savefilesboot")}:[/color] {GameManager.Data.SavefileBoots}",
        $"[color=crimson]{Tr("lobby_stats_itemsbought")}:[/color] {GameManager.Data.ItemsBought}",
        $"[color=crimson]{Tr("lobby_stats_itemssold")}:[/color] {GameManager.Data.ItemsSold}",
        $"[color=crimson]{Tr("lobby_stats_randomnumber")}:[/color] {GD.Randi()}",
        ];
        statsDisplay.Text = _stats.Join("\n");
    }
}