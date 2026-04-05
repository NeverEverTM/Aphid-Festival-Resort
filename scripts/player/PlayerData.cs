using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    internal static Savefile Data;
    internal SaveSystem.SaveModule<Savefile> SaveModule = new("player", new PlayerDataModule(), 1000)
    {
        Extension = SaveSystem.SAVEFILE_EXTENSION,
    };

    // MARK: SaveData Implementation
    public record Savefile
    {
        public string Name { get; set; }
        public string[] Pronouns { get; set; }
        public int MembershipTier { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }

        public List<string> Inventory { get; set; } = [];
        public List<string> Storage { get; set; } = [];
        public List<string> RecipesDiscovered { get; set; } = [];
        public int Currency { get; set; } = 30;

        public Savefile()
        {
            Name = NewGameMenu.NewName;
            Pronouns = NewGameMenu.NewPronouns;
        }
    }
    public class PlayerDataModule : SaveSystem.IDataModule<Savefile>
    {
        public void Set(Savefile _data)
        {
            Data = _data;

            Data.Name ??= NewGameMenu.NewName;
            Data.Pronouns ??= NewGameMenu.NewPronouns;
        }
        public Savefile Get()
        {
            if (!SceneManager.IsBusy)
            {
                Data.PositionX = Instance.GlobalPosition.X;
                Data.PositionY = Instance.GlobalPosition.Y;
            }
            else
            {
                Data.PositionX = Instance.LastPosition.X;
                Data.PositionY = Instance.LastPosition.Y;
            }
            return Data;
        }
        public Savefile Default() => new();
        public void Dispose()
        {
            Data = null;
        }
    }

    private static int VAR_AMOUNT_CURRENCY { get; set; }
    public enum CurrencySource { GeneralGain, AphidGain, JobGain, InventorySell }

    /// <summary>
    /// Adds berry currency to the player and triggers related events.
    /// </summary>
    /// <param name="_amount"></param>
    /// <param name="_source"></param>
    public static void AddCurrency(int _amount, CurrencySource _source = CurrencySource.GeneralGain)
    {
        VAR_AMOUNT_CURRENCY = _amount;
        CurrencyArgs _args = new()
        {
            Current = Data.Currency,
            Source = _source
        };

        if (VAR_AMOUNT_CURRENCY < 0)
            GlobalManager.Utils.InvokeEventListeners(Instance.CurrencyEventsList[CurrencyEvents.OnCurrencyLose], _args);
        else if (VAR_AMOUNT_CURRENCY > 0)
            GlobalManager.Utils.InvokeEventListeners(Instance.CurrencyEventsList[CurrencyEvents.OnCurrencyGain], _args);

        GlobalManager.Utils.InvokeEventListeners(Instance.CurrencyEventsList[CurrencyEvents.OnCurrencyChange], _args);
        Data.Currency = Mathf.Max(Data.Currency + VAR_AMOUNT_CURRENCY, 0);
        CanvasManager.UpdateCurrency();
    }
    /// <summary>
    /// Syntax sugar for AddCurrency(-cost).
    /// </summary>
    /// <param name="_amount"></param>
    /// <param name="_source"></param>
    public static void RemoveCurrency(int _amount, CurrencySource _source = CurrencySource.GeneralGain)
    {
        AddCurrency(-_amount, _source);
    }
}
