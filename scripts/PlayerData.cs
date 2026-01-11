using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerData : Player
{
    public enum ResortUpgradesEnum
    {
        PlayerLevel,
        JobBoardLevel,
        GOLDEN_AutoCareService,
        GOLDEN_EastWing,
    }
    // To be implemented later on
    // GOLDEN_DrinkService
    // GOLDEN_MinigameLevel
    // GOLDEN_HatsShop

    public struct ResortUpgrade
    {
        public ResortUpgradesEnum ID;
        public int Level;
    }

    public enum CurrencyEventsEnum { OnGain, OnLose, OnChange }
    public enum CurrencySourceEnum { GeneralGain, AphidGain, JobGain, InventorySell }
    public class CurrencyEventArgs : EventArgs
    {
        public CurrencySourceEnum Source;
        public int Amount { get => PlayerData.Amount; set => PlayerData.Amount = value; }
        public int Current;
    }
    protected static List<Action<CurrencyEventArgs>> OnGain = [], OnLose = [], OnChange = [];

    public static void AddEventListener(Action<CurrencyEventArgs> _action, CurrencyEventsEnum _event)
    {
        switch (_event)
        {
            case CurrencyEventsEnum.OnGain:
                OnGain.Add(_action);
                break;
            case CurrencyEventsEnum.OnLose:
                OnLose.Add(_action);
                break;
            case CurrencyEventsEnum.OnChange:
                OnChange.Add(_action);
                break;
        }
    }
    public static void RemoveEventListener(Action<CurrencyEventArgs> _action, CurrencyEventsEnum _event)
    {
        switch (_event)
        {
            case CurrencyEventsEnum.OnGain:
                OnGain.Remove(_action);
                break;
            case CurrencyEventsEnum.OnLose:
                OnLose.Remove(_action);
                break;
            case CurrencyEventsEnum.OnChange:
                OnChange.Remove(_action);
                break;
        }
    }

    protected static int Amount;
    public static void AddCurrency(int _amount, CurrencySourceEnum _source = CurrencySourceEnum.GeneralGain)
    {
        CurrencyEventArgs _args = new()
        {
            Current = Data.Currency,
            Amount = _amount,
            Source = _source
        };

        if (_amount < 0)
            GlobalManager.Utils.InvokeEventListeners(ref OnLose, _args);
        else
            GlobalManager.Utils.InvokeEventListeners(ref OnGain, _args);

        GlobalManager.Utils.InvokeEventListeners(ref OnChange, _args);
        Data.Currency = Mathf.Max(Data.Currency + _amount, 0);
        CanvasManager.UpdateCurrency();
    }

    // MARK: SaveData Implementation
    public record Savefile
    {
        public string Name { get; set; } = "Mello";
        public string[] Pronouns { get; set; } = ["They", "them"];
        public int Level { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }

        public List<string> Inventory { get; set; } = [];
        public List<string> Storage { get; set; } = [];
        public List<string> RecipesDiscovered { get; set; } = [];
        public List<ResortUpgrade> Upgrades { get; set; } = [];
        public int Currency { get; set; } = 30;

        public Savefile()
        {
            Name = NewGameMenu.NewName;
            Pronouns = NewGameMenu.NewPronouns;
            OnGain.Clear();
            OnLose.Clear();
            OnChange.Clear();
        }
    }
    public class PlayerDataModule : SaveSystem.IDataModule<Savefile>
    {
        public void Set(Savefile _data)
        {
            Data = _data;
            CanvasManager.UpdateCurrency();

            if (!GameManager.IsNewGame)
            {
                Vector2 _position = new(Data.PositionX, Data.PositionY);
                if (GameManager.APPLY_OUTOFBOUND_PATCH && (GameManager.IsOutOfBounds(_position) || GameManager.IsInsideGeometry(_position)))
                    Instance.GlobalPosition = FieldManager.Instance.Doors[0].GlobalPosition + (-FieldManager.Instance.Doors[0].entryDirection) * 5;
                else
                    Instance.GlobalPosition = _position;
                CameraManager.Focus(Instance);
                CameraManager.ForceCameraPosition(Instance.GlobalPosition);
            }

            _data.Name ??= "Mello";
            _data.Pronouns ??= ["They", "them"];
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
    }
}
