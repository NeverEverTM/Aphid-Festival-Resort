using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

public partial class Player : CharacterBody2D, SaveSystem.ISaveData
{
	public static SaveFile Data = new();
	public string GetId() => "player_data";
	public static string NewName { get; set; }
	public static string[] NewPronouns { get; set; }

	public class SaveFile
	{
		public string Name { get; set; }
		public string[] Pronouns { get; set; }
		public int Level { get; set; }
		public string Room { get; set; }

		public float PositionX { get; set; }
		public float PositionY { get; set; }

		public List<string> Inventory { get; set; }
		public List<string> Storage { get; set; }
		public int Currency { get; set; } 
		public int InventoryMaxCapacity { get; set; }

		public SaveFile()
		{
			Room = "resort_golden_grounds";
			Inventory = new();
			Storage = new();
			Currency = 30;
			InventoryMaxCapacity = 15;
		}
		public void SetCurrency(int _amount, bool _setToValue = false)
		{
			if (_setToValue)
				Currency = _amount;
			else
				Currency += _amount;
			CanvasManager.UpdateCurrency();
		}
	}

	public string SaveData()
	{
		Data.PositionX = Instance.GlobalPosition.X;
		Data.PositionY = Instance.GlobalPosition.Y;
		return JsonSerializer.Serialize(Data);
	}
	public Task LoadData(string _json)
	{
		if (_json == string.Empty) // Is it empty?
		{
			Data = new();
			GD.PrintErr("This player data was empty!");
			return Task.CompletedTask;
		}
		Data = JsonSerializer.Deserialize<SaveFile>(_json);
		Instance.GlobalPosition = new Vector2(Data.PositionX, Data.PositionY);
		CanvasManager.UpdateCurrency();
		return Task.CompletedTask;
	}
	public Task SetData()
	{
		Data = new();
		if (NewName != null)
		{
			Data.Name = NewName;
			NewName = null;
		}
		if (NewPronouns != null)
		{
			Data.Pronouns = NewPronouns;
			NewPronouns = null;
		}
		CanvasManager.UpdateCurrency();
		return Task.CompletedTask;
	}
}