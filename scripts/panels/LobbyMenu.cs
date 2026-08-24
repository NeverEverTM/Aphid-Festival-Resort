using Godot;
using System;
using System.Linq;
using System.Text;

public partial class LobbyMenu : Control
{
    [Export] private InteractableArea2D interactArea;
    [Export] private AnimationPlayer animator;
    [Export] private Button[] categoryButtons;
    [Export] private Control[] categoryNodes;
    [Export] private Label currencyLabel;
    [ExportGroup("Aphid")]
    [Export] private BaseButton sellAphidButton;
    [Export] private RichTextLabel aphidCostLabel, aphidNameLabel, noAphidLabel;
    [Export] private GridContainer aphidContainer;
    [Export] private Control aphidNode, displayContents;
    [ExportGroup("Upgrades")]
    [Export] private Container upgradeSlotsGrid;
    [Export] private RichTextLabel upgradeNameLabel, upgradeDescLabel;
    [Export] private Button upgradeBuyButton;
    [Export] private PackedScene upgradeSlot;
    [ExportGroup("Stats")]
    [Export] private RichTextLabel statsDisplay, pronounsLabel, nameLabel;
    [Export] private Label pronounsDisplay, nameDisplay;

    private enum CategoriesEnum { Aphid, Upgrades, Stats }
    private CategoriesEnum current_category;
    private Guid current_key;
    private Control current_slot;
    private MenuInstance menu;
    private UpgradeHub.IUpgradeModuleCore current_upgrade;

    public override void _EnterTree()
    {
        // create menu
        menu = new("lobby", animator, Open:(_) =>
        {
            UpdateAphidPanel();
            UpdateStatsPanel();
            UpdateUpgradesPanel();
        }, null, null
        ,Dispose: () =>
        {
            ClearAphidPanel();
            ClearUpdatePanel();
            ClearStatsPanel();
        });

        // setup all category buttons
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            int index = i;
            categoryButtons[i].Text = "lobby_category_" + ((CategoriesEnum)index).ToString().ToLower();
            categoryButtons[i].Pressed += () => SetCategory((CategoriesEnum)index);
        }

        // set default values
        SetCategory(CategoriesEnum.Aphid, true);
        sellAphidButton.Pressed += () => ConfirmationPopup.Create(SellAphid, null,
            ConfirmationPopup.ConfirmationEnum.Fast);
        upgradeBuyButton.Pressed += PurchaseUpgrade;
        interactArea.OnInteractOnly.Add(SetMenu);
        
        SceneManager.AddEventListener((_) => {
            UpdateCurrencyLabel(new() { Currency = Player.Data.Currency });
            Player.Instance.AddEventListener(UpdateCurrencyLabel, Player.CurrencyEvents.OnPostCalculation);
        }, SceneManager.EventEnum.OnPostLoad);
    }

    public void SetMenu() =>
        _ = CanvasManager.Menus.SetTo(menu);
    private void SetCategory(CategoriesEnum _category, bool _force = false)
    {
        if (_category.Equals(current_category) && !_force)
            return;
        int _current = (int)current_category, _new = (int)_category;

        categoryNodes[_current].Hide();
        current_category = _category;
        categoryNodes[_new].Show();
        if (!_force)
            SoundManager.CreateSound("ui/button_switch");
    }
    private void UpdateCurrencyLabel(Player.CurrencyArgs _args)
    {
        currencyLabel.Text = _args.Currency.ToString();
    }

    // MARK: Aphid Category
    private void UpdateAphidPanel()
    {
        ClearAphidPanel();

        foreach (var _pair in GameManager.Aphids)
        {
            var _pair_clone = _pair;
            aphidContainer.AddChild(CanvasManager.CreateAphidSlot(_pair_clone.Key, false, SetAphidIcon));
        }

        if (GameManager.Aphids.Count == 0)
            noAphidLabel.Show();
        else
        {
            noAphidLabel.Hide();
            aphidContainer.GetChild<Control>(0).GrabFocus();
            SetAphidIcon(GameManager.Aphids.First().Value.GUID);
        }
    }
    private void ClearAphidPanel()
    {
        if (aphidNode.GetChildCount() > 0)
            aphidNode.GetChild(0).QueueFree();

        for (int i = 0; i < aphidContainer.GetChildCount(); i++)
            aphidContainer.GetChild(i).QueueFree();

        displayContents.Hide();
        current_key = Guid.Empty;
    }
    private void SetAphidIcon(Guid _key)
    {
        if (aphidNode.GetChildCount() > 0)
            aphidNode.GetChild(0).QueueFree();

        var _aphid = GameManager.Aphids[_key];

        current_key = _key;

        // set interface
        aphidCostLabel.Text = StringNames.BerryIcon + " " + GetAphidValue(_aphid).ToString();
        aphidNameLabel.Text = _aphid.Genes.Name;
        aphidNode.AddChild(CanvasManager.CreateAphidSlot(_key, true));
        displayContents.Show();
    }
    private void SellAphid()
    {
        int _value = GetAphidValue(GameManager.Aphids[current_key]);
        Player.AddCurrency(_value);
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

    // MARK: Stats Category
    private void UpdateStatsPanel()
    {
        nameLabel.Text = $"[bgcolor=coral]{Tr("new_game_player_name")}[/bgcolor]";
        nameDisplay.Text = Player.Data.Name;
        pronounsLabel.Text = $"[bgcolor=coral]{Tr("new_game_pronouns")}[/bgcolor]";
        pronounsDisplay.Text = Player.Data.Pronouns.Join("/");
        double _playtime = GameManager.Data.Playtime + (Time.GetUnixTimeFromSystem() - GameManager.Data.LastTimeLoaded);

        string[] _stats = [
        $"[color=gold]{Tr("lobby_stats_totalaphids")}:[/color] {GameManager.Aphids.Count + GameManager.AphidArchive.Count}",
        $"[color=gold]{Tr("lobby_stats_aphidcount")}:[/color] {GameManager.Aphids.Count}",
        $"[color=gold]{Tr("lobby_stats_aphidssold")}:[/color] {GameManager.Data.AphidsSold}",
        $"[color=gold]{Tr("lobby_stats_savefilesboot")}:[/color] {GameManager.Data.SavefileBoots}",
        $"[color=gold]{Tr("lobby_stats_itemsbought")}:[/color] {GameManager.Data.ItemsBought}",
        $"[color=gold]{Tr("lobby_stats_itemssold")}:[/color] {GameManager.Data.ItemsSold}",
        $"[color=gold]{Tr("lobby_stats_totalplaytime")}:[/color] {TimeSpan.FromSeconds(_playtime).ToString(@"hh\:mm\:ss")}",
        ];
        statsDisplay.Text = new StringBuilder().AppendJoin("\n", _stats).ToString();
    }
    private void ClearStatsPanel()
    {
        statsDisplay.Text = string.Empty;
        nameLabel.Text = string.Empty;
        nameDisplay.Text = string.Empty;
        pronounsLabel.Text = string.Empty;
        pronounsDisplay.Text = string.Empty;
    }

    // MARK: Upgrades Category
    private void UpdateUpgradesPanel()
    {
        var _currentUpgrade = current_upgrade;
        ClearUpdatePanel();

        for (int i = 0; i < UpgradeHub.G_UPGRADES.Count; i++)
            CreateUpgradeSlot(UpgradeHub.G_UPGRADES[i]);

        if (_currentUpgrade != null)
        {
            current_upgrade = _currentUpgrade;
            ShowUpgrade(current_upgrade);
        }
    }
    private void ClearUpdatePanel()
    {
        current_upgrade = null;
        upgradeNameLabel.Text = string.Empty;
        upgradeDescLabel.Text = string.Empty;
        upgradeBuyButton.Hide();

        for (int i = 0; i < upgradeSlotsGrid.GetChildCount(); i++)
            upgradeSlotsGrid.GetChild(i).QueueFree();
    }
    private void CreateUpgradeSlot(UpgradeHub.IUpgradeModuleCore _upgrade)
    {
        Button _slot = upgradeSlot.Instantiate<Button>();
        UpgradeHub.UpgradeModule _playerUpgrade = GameManager.GetPlayerUpgradeElseEmpty(_upgrade.ID);

        _slot.GetChild<RichTextLabel>(1).Text = $"lobby_upgrade_{_upgrade.ID}_name";

        // cost
        if (_playerUpgrade.Level < _upgrade.MaxLevel)
        {
            int _cost = _upgrade.Costs[_playerUpgrade.Level];
            _slot.GetChild<RichTextLabel>(2).Text = $"{StringNames.BerryIcon} {_cost}";
        }
        else
            _slot.GetChild<RichTextLabel>(2).QueueFree();

        // level
        RichTextLabel _levelLabel = _slot.GetChild<RichTextLabel>(3);
        for (int level = 1; level <= _upgrade.MaxLevel; level++)
        {
            if (level <= _playerUpgrade.Level)
                _levelLabel.Text += StringNames.StarIcon;
            else
                _levelLabel.Text += StringNames.EmptyStarIcon;
        }

        Color _origColor = _slot.GetChild<Control>(0).SelfModulate;

        _slot.Pressed += () =>
        {
            ShowUpgrade(_upgrade);
        };
        _slot.MouseEntered += () =>
        {
            _slot.SelfModulate = new(0.5f,0.5f,0.5f);
            _slot.GetChild<Control>(0).SelfModulate = new Color(_origColor.R * 0.75f, _origColor.G * 0.75f, _origColor.B * 0.75f);
        };
        _slot.MouseExited += () =>
        {
            _slot.SelfModulate = new(1,1,1);
            _slot.GetChild<Control>(0).SelfModulate = _origColor;
        };

        upgradeSlotsGrid.AddChild(_slot);
    }
    private void ShowUpgrade(UpgradeHub.IUpgradeModuleCore _upgrade, bool _force = false)
    {
        if (_upgrade == null || _upgrade == current_upgrade && !_force)
            return;

        UpgradeHub.UpgradeModule _playerUpgrade = GameManager.GetPlayerUpgradeElseEmpty(_upgrade.ID);
        upgradeNameLabel.Text = Tr($"lobby_upgrade_{_upgrade.ID}_name");

        // check that we dont have already max level, if we do, dont show information about next level
        if (_playerUpgrade.Level < _upgrade.MaxLevel)
        {
            upgradeNameLabel.AppendText("\n");
            // cost for next level
            int _cost = _upgrade.Costs[_playerUpgrade.Level];
            upgradeNameLabel.AppendText($"{Tr("lobby_upgrade_cost")}: {StringNames.BerryIcon} {_cost}");

            // tier for next level
            if (GameManager.GetPlayerUpgradeElseEmpty("membership_tier").Level < _upgrade.MinimumTiersRequired[_playerUpgrade.Level])
            {
                upgradeNameLabel.AppendText($" [color=coral][shake]{Tr("lobby_upgrade_requires_tier")} {_upgrade.MinimumTiersRequired[_playerUpgrade.Level]}[/shake][/color]");
                upgradeBuyButton.Hide();
            }
            else
            {
                if (_playerUpgrade.Level == 0)
                    upgradeBuyButton.Text = "lobby_upgrade_buy";
                else
                    upgradeBuyButton.Text = "lobby_upgrade_upgrade";
                upgradeBuyButton.Show();
            }
            upgradeNameLabel.AppendText("\n");
        }
        else
            upgradeBuyButton.Hide();

        upgradeDescLabel.Text = $"lobby_upgrade_{_upgrade.ID}_desc";
        current_upgrade = _upgrade;
    }
    private void PurchaseUpgrade()
    {
        UpgradeHub.UpgradeModule _playerUpgrade = GameManager.GetPlayerUpgradeElseEmpty(current_upgrade.ID);
        // we check if we have the money, and the tier needed
        if ((Player.Data.Currency - current_upgrade.Costs[_playerUpgrade.Level]) >= 0
            && GameManager.GetPlayerUpgradeElseEmpty("membership_tier").Level >= current_upgrade.MinimumTiersRequired[_playerUpgrade.Level])
        {
            Player.RemoveCurrency(current_upgrade.Costs[_playerUpgrade.Level]);

            if (GameManager.HasPlayerUpgrade(current_upgrade.ID))
                GameManager.Upgrades[current_upgrade.ID].Level++;
            else
                GameManager.Upgrades.Add(current_upgrade.ID, new(1));
            current_upgrade.OnGameLoad(GameManager.Upgrades[current_upgrade.ID].Level);
            UpdateUpgradesPanel();
            ShowUpgrade(current_upgrade, true);
		    SoundManager.CreateSound("ui/kaching");
        }
        else
            SoundManager.CreateSound("ui/button_fail");
    }
}