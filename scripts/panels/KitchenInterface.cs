using Godot;

public partial class KitchenInterface : Control
{
	[Export] private AnimationPlayer animPlayer;
	[Export] private PackedScene invItemContainer;

	[Export] private BaseButton ingredient1Button, ingredient2Button, resultButton;
	[Export] private CheckButton redoRecipe;
	[Export] private TextureRect ingredient1Icon, ingredient2Icon, resultIcon;
	[Export] private Label resultName;
	[Export] private Container inventoryGrid;
	private string ingredient1, ingredient2, result;
	private GameManager.Recipe resultRecipe;
	private const string mistake = "mistake", bigMistake = "big_mistake";

	public MenuUtil.MenuInstance Menu { get; set; }

	public override void _Ready()
	{
		ingredient1Button.Pressed += () => SetIngredient1Slot();
		ingredient2Button.Pressed += () => SetIngredient2Slot();
		resultButton.Pressed += OnResultPressed;
		redoRecipe.Toggled += OnRedoPressed;
		Menu = new("kitchen", animPlayer, () =>
		{
			CreateInventory();

			SetIngredient1Slot();
			SetIngredient2Slot();
			SetResultSlot(false);
			ingredient1Button.GrabFocus();
		}, null, false);
	}
	private void CreateInventory()
	{
		for (int i = 0; i < inventoryGrid.GetChildCount(); i++)
			inventoryGrid.GetChild(i).QueueFree();

		for (int i = 0; i < Player.Data.Inventory.Count; i++)
		{
			TextureButton _item = invItemContainer.Instantiate() as TextureButton;
			var _item_name = Player.Data.Inventory[i];
			_item.SetMeta("id", _item_name);

			// check for available icon
			(_item.GetChild(0) as TextureRect).Texture = GameManager.GetIcon(_item_name);

			// press function
			_item.Pressed += () => OnInvSlotPressed(_item_name);
			inventoryGrid.AddChild(_item);
		}
	}

	private void SetIngredient1Slot(string _item_name = null)
	{
		if (_item_name != null)
		{
			ingredient1Icon.Texture = GameManager.GetIcon(_item_name);
			ingredient1 = _item_name;
		}
		else
		{
			ingredient1Icon.Texture = null;
			ingredient1 = null;
		}
	}
	private void SetIngredient2Slot(string _item_name = null)
	{
		if (_item_name != null)
		{
			ingredient2Icon.Texture = GameManager.GetIcon(_item_name);
			ingredient2 = _item_name;
		}
		else
		{
			ingredient2Icon.Texture = null;
			ingredient2 = null;
		}
	}
	private void SetResultSlot(bool _state)
	{
		if (_state)
		{
			resultIcon.Texture = GameManager.GetIcon(result);
			resultName.Text = Tr(result + "_name");
			(resultName.GetParent() as Control).Show();
		}
		else
		{
			resultIcon.Texture = null;
			result = null;
			resultRecipe = default;
			(resultName.GetParent() as Control).Hide();
		}
	}

	private void OnInvSlotPressed(string _item_name)
	{
		if (ingredient1 == null)
			SetIngredient1Slot(_item_name);
		else if (ingredient2 == null)
			SetIngredient2Slot(_item_name);
	}
	private void OnRedoPressed(bool _toggle)
	{
		if (_toggle)
		{
			if (result != null)
			{
				// hand it to the player as this means it has used ingredients already
				if (PlayerInventory.StoreItem(result))
					CreateInventory();
				else
					ResortManager.CreateItem(result, Player.Instance.GlobalPosition);

				// set ingredients slot
				SetIngredient1Slot(resultRecipe.Ingredient1);
				if (resultRecipe.Ingredient2 != string.Empty)
					SetIngredient2Slot(resultRecipe.Ingredient2);
				else
					SetIngredient2Slot();
			}
		}
		else
			SetResultSlot(false);

		// deactivate buttons on redo mode
		ingredient1Button.Disabled = _toggle;
		ingredient2Button.Disabled = _toggle;
	}
	private void OnResultPressed()
	{
		if (result == null)
		{
			// No ingredients
			if (ingredient1 == null && ingredient2 == null)
				return;

			// Missing ingredients (can happen after turning off redo)
			if ((ingredient1 != null & !Player.Data.Inventory.Contains(ingredient1))
			|| (ingredient2 != null && !Player.Data.Inventory.Contains(ingredient2)))
				return;
			// get result and set result interface to done
			result = GetRecipeResult();
			SetResultSlot(true);

			// remove items from player & update inventory, unless we are redoing
			if (!redoRecipe.ButtonPressed)
			{
				Player.Data.Inventory.Remove(ingredient1);
				Player.Data.Inventory.Remove(ingredient2);
				CreateInventory();
				SetIngredient1Slot();
				SetIngredient2Slot();
			}
		}
		else
		{
			// (ONLY ON REDO) check if you have the needed ingredients and remove them if yes
			if (redoRecipe.ButtonPressed)
			{
				// No ingredients (sanity check)
				if (ingredient1 == null && ingredient2 == null)
					return;

				// mistakes count as recipes, so we except them out
				if (result == mistake || result == bigMistake)
					return;

				// check if you have the ingredients, and remove if so
				if ((ingredient1 != null & !Player.Data.Inventory.Contains(ingredient1))
				|| (ingredient2 != null && !Player.Data.Inventory.Contains(ingredient2)))
					return;
				Player.Data.Inventory.Remove(resultRecipe.Ingredient1);
				Player.Data.Inventory.Remove(resultRecipe.Ingredient2);
			}

			// store item and set interface to clear, dont clear if redo is active
			if (PlayerInventory.StoreItem(result))
			{
				if (!redoRecipe.ButtonPressed)
					SetResultSlot(false);
				CreateInventory();
			}
		}
	}
	private string GetRecipeResult()
	{
		// produce a mistake if you are using one
		if (ingredient1 == mistake || ingredient2 == mistake)
		{
			if (ingredient1 == ingredient2)
				return bigMistake; // produce a bigger mistake if both are mistakes
			else
				return mistake;
		}

		// return big mistake regardless if used in recipe
		if (ingredient1 == bigMistake || ingredient2 == bigMistake)
			return bigMistake;

		bool _single = ingredient1 == null || ingredient2 == null; // we assume ATLEAST ONE INGREDIENT EXISTS

		// look for recipe that matches all ingredients
		GameManager.Recipe _recipe = GameManager.G_RECIPES.Find((GameManager.Recipe _r) =>
		{
			if (_r.Ingredient1 == ingredient1 || _r.Ingredient1 == ingredient2)
			{
				if (_r.Ingredient2 == ingredient1 || _r.Ingredient2 == ingredient2)
					return true;

				// single item recipes
				if (_r.Ingredient2 == "")
				{
					// check if there was only one ingredient to check
					if (_single)
						return true;
					// else if both ingredients are the same then go ahead anyways
					if (ingredient1 == ingredient2)
						return true;
				}
			}
			return false;
		});

		resultRecipe = _recipe;
		return _recipe.Result == null ? mistake : _recipe.Result;
	}
}
