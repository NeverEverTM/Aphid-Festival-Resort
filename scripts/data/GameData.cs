using System.Threading.Tasks;
using System.Text.Json;

public class GameData : SaveSystem.ISaveData
{
    public static GameData Instance = new();
    public static Savefile Data = new();

    public int LoadOrderPriority { get => int.MaxValue; }

    public string GetId() => "game_savedata";

    public Task LoadData(string _json)
    {
        Data = JsonSerializer.Deserialize<Savefile>(_json);
        return Task.CompletedTask;
    }

    public string SaveData()
    {
        Data.Version = GameManager.GAME_VERSION;
        Data.AphidCount = SaveSystem.Aphids.Count;
        return JsonSerializer.Serialize(Data);
    }

    public Task SetData()
    {
        Data = new();
        return Task.CompletedTask;
    }

	public class Savefile
	{
        /// <summary>
        /// Only used for display purposes.
        /// </summary>
        public int AphidCount { get; set; }
		public float Playtime { get; set; }
		public int Version { get; set; }

        public Savefile()
        {
            Version = GameManager.GAME_VERSION;
            AphidCount = SaveSystem.Aphids.Count;
        }
	}
}
