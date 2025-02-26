using Godot;

public partial class KitchenInterface : Control, MenuTrigger.ITrigger
{
	[Export] private AnimationPlayer animPlayer;
	[Export] private PackedScene invItemContainer;

	[Export] private BaseButton ingredient1Button, ingredient2Button, resultButton;
	[Export] private CheckButton redoRecipe;
	[Export] private TextureRect ingredient1Icon, ingredient2Icon, resultIcon, portrait;
	[Export] private Texture2D[] portraitImages;
	[Export] private Label resultName;
	[Export] private RichTextLabel dialogBox;
	[Export] private Container inventoryGrid;

	private string ingredient1, ingredient2;
	private GlobalManager.Recipe resultRecipe;
	private const string MISTAKE_RECIPE = "mistake", BIG_MISTAKE_RECIPE = "big_mistake", UNKNOWN_RECIPE = "unknown";

	public MenuUtil.MenuInstance Menu { get; set; }
	public void SetMenu()
	{
		if (CanvasManager.Menus.CurrentMenu != Menu)
			CanvasManager.Menus.OpenMenu(Menu);
	}

	public override void _Ready()
	{
		ingredient1Button.Pressed += () => SetIngredientSlot(null, 0);
		ingredient2Button.Pressed += () => SetIngredientSlot(null, 1);
		resultButton.Pressed += OnResultPressed;
		redoRecipe.Toggled += OnRedoPressed;
		Menu = new("kitchen", animPlayer, () =>
		{
			CreateInventory();
			SetIngredientSlot(null, 0);
			SetIngredientSlot(null, 1);
			SetResultSlot(null);
			ingredient1Button.GrabFocus();
			dialogBox.Text = "kitchen_desc";
			portrait.Texture = portraitImages[0];
			redoRecipe.SetPressedNoSignal(false);
			ingredient1Button.Disabled = ingredient2Button.Disabled = false;
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
			(_item.GetChild(0) as TextureRect).Texture = GlobalManager.GetIcon(_item_name);

			// press function
			_item.Pressed += () => AddIngredient(_item_name);
			inventoryGrid.AddChild(_item);
		}
	}
	
	private void SetIngredientSlot(string _item_name, int _index)
	{
		bool _isNull = _item_name == null;
		if (!_isNull && GlobalManager.G_ITEMS[_item_name].tag != "food")
			return; // if is not a food item, dont bother

		ref TextureRect _icon = ref (_index == 0) ? ref ingredient1Icon : ref ingredient2Icon;
		ref string _ingredient = ref (_index == 0) ? ref ingredient1 : ref ingredient2;

		if (!_isNull)
			SoundManager.CreateSound("ui/button_select");
		else if (_ingredient != null)
			SoundManager.CreateSound("ui/switch");
		_icon.Texture = !_isNull ? GlobalManager.GetIcon(_item_name) : null;
		_ingredient = _item_name;
		DisplayResult();
	}
	private void SetResultSlot(string _result)
	{
		resultIcon.Texture = _result != null ? GlobalManager.GetIcon(_result) : null;
		resultName.Text = _result != null ? _result + "_name" : "---";
		if (_result == null)
			resultRecipe = default;
	}

	private void AddIngredient(string _item_name)
	{
		if (redoRecipe.ButtonPressed)
			return;

		if (ingredient1 == null)
			SetIngredientSlot(_item_name, 0);
		else if (ingredient2 == null)
			SetIngredientSlot(_item_name, 1);
	}
	private void OnRedoPressed(bool _toggle)
	{
		// mistakes count as recipes, so we except them out
		if (_toggle)
		{
			// Cancel toggle if not result is being displayed or the recipe is still unknown
			if (resultRecipe.Result == null || !Player.Data.RecipesDiscovered.Contains(resultRecipe.Result))
			{
				redoRecipe.SetPressedNoSignal(false);
				return;
			}
			PlayAnim("lock");
			// play sound
		}

		// deactivate buttons on redo mode
		ingredient1Button.Disabled = _toggle;
		ingredient2Button.Disabled = _toggle;
	}
	private void OnResultPressed()
	{
		// No ingredients (sanity check)
		if (ingredient1 == null && ingredient2 == null)
			return;

		// check if you have the ingredients, and remove if so
		if ((ingredient1 != null & !Player.Data.Inventory.Contains(ingredient1))
				|| (ingredient2 != null && !Player.Data.Inventory.Contains(ingredient2)))
			return;

		Player.Data.Inventory.Remove(ingredient1);
		Player.Data.Inventory.Remove(ingredient2);

		// store item
		if (PlayerInventory.StoreItem(resultRecipe.Result))
			CreateInventory();
		else // cant fit it, drop it in the floor
			ResortManager.CreateItem(resultRecipe.Result, Player.Instance.GlobalPosition);

		PlayAnim("cook");
		if (!Player.Data.RecipesDiscovered.Contains(resultRecipe.Result))
			Player.Data.RecipesDiscovered.Add(resultRecipe.Result);

		SoundManager.CreateSound("ui/steam_sizzle");
		// set interface to clear, dont clear if redo is active
		if (!redoRecipe.ButtonPressed)
		{
			if (GlobalManager.G_FOOD[resultRecipe.Result].type == AphidData.FoodType.Vile)
			{
				dialogBox.Text = "kitchen_fail";
				SoundManager.CreateSound("ui/kitchen_fail");
				portrait.Texture = portraitImages[1];
			}
			else
			{
				dialogBox.Text = "kitchen_success";
				SoundManager.CreateSound("ui/kitchen_success");
				portrait.Texture = portraitImages[2];
			}
			SetIngredientSlot(null, 0);
			SetIngredientSlot(null, 1);
			SetResultSlot(null);
		}
		else
			SoundManager.CreateSound("ui/button_select");
	}

	private void DisplayResult()
	{
		// No ingredients, sanity check
		if (ingredient1 == null && ingredient2 == null)
		{
			SetResultSlot(null);
			return;
		}

		// get result and set result interface to done
		string _recipe = GetRecipeFromMatch();
		if (_recipe == MISTAKE_RECIPE || _recipe == BIG_MISTAKE_RECIPE || !Player.Data.RecipesDiscovered.Contains(_recipe))
			SetResultSlot(UNKNOWN_RECIPE);
		else
			SetResultSlot(_recipe);
	}
	/// <summary>
	/// Gets the recipe by matching ingredients against the recipe table.
	/// </summary>
	private string GetRecipeFromMatch()
	{
		// mistakes and big mistakes used as ingredients yields bad results
		if (ingredient1 == MISTAKE_RECIPE || ingredient2 == MISTAKE_RECIPE || ingredient1 == BIG_MISTAKE_RECIPE || ingredient2 == BIG_MISTAKE_RECIPE)
		{
			resultRecipe = new(BIG_MISTAKE_RECIPE, ingredient1, ingredient2);
			return BIG_MISTAKE_RECIPE;
		}

		bool _single = ingredient1 == null || ingredient2 == null; // we assume ATLEAST ONE INGREDIENT EXISTS

		// look for a recipe that matches all ingredients
		resultRecipe = GlobalManager.G_RECIPES.Find((GlobalManager.Recipe _r) =>
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

		if (resultRecipe.Result == null)
			resultRecipe = new(MISTAKE_RECIPE, ingredient1, ingredient2);

		return resultRecipe.Result;
	}
	private void PlayAnim(string _anim)
	{
		if (animPlayer.CurrentAnimation == "close")
			return;
		animPlayer.Play(_anim);
	}
}
