using Godot;

public partial class KitchenInterface : Control
{
	[Export] private AnimationPlayer animPlayer;
	[Export] private InteractableArea2D interactArea2D;
	[Export] private BaseButton ingredient1Button, ingredient2Button, resultButton;
	[Export] private CheckButton redoRecipe;
	[Export] private TextureRect ingredient1Icon, ingredient2Icon, resultIcon, portrait;
	[Export] private Label resultName;
	[Export] private RichTextLabel dialogBox;
	[Export] private Container inventoryGrid;
	[Export] private Texture2D[] portraitImages;
	[Export] private Color slotColor;

	private PackedScene item_container;
	private string ingredient1, ingredient2;
	private RecipeData current_recipe;
	private const string MISTAKE_RECIPE = "mistake", BIG_MISTAKE_RECIPE = "big_mistake", UNKNOWN_RECIPE = "unknown";

	public MenuInstance Menu { get; set; }

	public override void _Ready()
	{
		item_container = ResourceLoader.Load("uid://cn7d8wjyx78a3") as PackedScene;
		ingredient1Button.Pressed += () => SetIngredientSlot(null, 0);
		ingredient2Button.Pressed += () => SetIngredientSlot(null, 1);
		resultButton.Pressed += OnResultPressed;
		redoRecipe.Toggled += OnRedoPressed;
		Menu = new("kitchen", animPlayer, 
		Open: (_) =>
		{
			SoundManager.CreateSound("ui/kitchen_open");
			CreateInventory();
		},
		TryClose: null,
		Close: null,
		Dispose: ClearInterface);
		interactArea2D.OnInteractOnly.Add(SetMenu);
		ClearInterface(); // To make sure is empty
	}

	public void SetMenu()
	{
		if (CanvasManager.Menus.Current != Menu)
			_ = CanvasManager.Menus.SetTo(Menu);
	}

	private void ClearInterface()
	{
		CleanInventory();
		SetIngredientSlot(null, 0);
		SetIngredientSlot(null, 1);
		SetResultSlot(null);
		ingredient1Button.GrabFocus();
		dialogBox.Text = "kitchen_desc";
		portrait.Texture = portraitImages[0];
		redoRecipe.SetPressedNoSignal(false);
		ingredient1Button.Disabled = ingredient2Button.Disabled = false;
	}
	private void CleanInventory()
	{
		for (int i = 0; i < inventoryGrid.GetChildCount(); i++)
			inventoryGrid.GetChild(i).QueueFree();
	}
	private void CreateInventory()
	{
		CleanInventory();
		for (int i = 0; i < PlayerInventory.MAX_CAPACITY; i++)
		{
			TextureButton _item = item_container.Instantiate() as TextureButton;
			(_item.GetChild(0) as Control).SelfModulate = slotColor;

			if (Player.Data.Inventory.Count <= i)
			{
				inventoryGrid.AddChild(_item);
				continue;
			}

			// set metadata
			var _item_name = Player.Data.Inventory[i];
			_item.SetMeta(StringNames.IdMeta, _item_name);
			_item.TooltipText = GlobalManager.Utils.GetTooltipText(_item_name);

			// check for available icon
			(_item.GetChild(1) as TextureRect).Texture = GlobalManager.GetIcon(_item_name);

			// press function
			_item.Pressed += () => AddIngredient(_item_name);
			inventoryGrid.AddChild(_item);
		}
	}

	private void SetIngredientSlot(string _item_name, int _index)
	{
		bool _isNull = _item_name == null;
		if (!_isNull && GlobalManager.G_ITEMS[_item_name].Tag != StringNames.GlobalTags.Food)
		{
			SoundManager.CreateSound("ui/button_fail");
			return; // if is not a food item, dont bother
		}

		ref TextureRect _icon = ref (_index == 0) ? ref ingredient1Icon : ref ingredient2Icon;
		ref string _ingredient = ref (_index == 0) ? ref ingredient1 : ref ingredient2;

		if (!_isNull)
			SoundManager.CreateSound("ui/button_select");
		else if (_ingredient != null)
			SoundManager.CreateSound("ui/button_switch");
		_icon.Texture = !_isNull ? GlobalManager.GetIcon(_item_name) : null;
		_ingredient = _item_name;
		DisplayResult();
	}
	private void SetResultSlot(string _result)
	{
		resultIcon.Texture = _result != null ? GlobalManager.GetIcon(_result) : null;
		resultName.Text = _result != null ? _result + "_name" : "---";
		if (_result == null)
			current_recipe = default;
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
			if (current_recipe == null || !Player.Data.RecipesDiscovered.Contains(current_recipe.Owner.Item.ID))
			{
				redoRecipe.SetPressedNoSignal(false);
				return;
			}
			PlayAnim("lock");
			SoundManager.CreateSound("ui/lock");
		}
		else
			SoundManager.CreateSound("ui/button_select");

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
		if (ingredient1 != ingredient2) // do not remove duplicates
			Player.Data.Inventory.Remove(ingredient2);

		// store item
		if (PlayerInventory.StoreItem(current_recipe.Owner.Item.ID))
			CreateInventory();
		else // cant fit it, drop it in the floor
			ResortManager.CreateItem(current_recipe.Owner.Item.ID, Player.Instance.GlobalPosition);

		PlayAnim("cook");
		if (!Player.Data.RecipesDiscovered.Contains(current_recipe.Owner.Item.ID))
			Player.Data.RecipesDiscovered.Add(current_recipe.Owner.Item.ID);

		SoundManager.CreateSound("ui/steam_sizzle");
		// set interface to clear, dont clear if redo is active
		if (!redoRecipe.ButtonPressed)
		{
			if (GlobalManager.G_FOOD[current_recipe.Owner.Item.ID].Type == AphidData.FoodType.Vile)
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
			current_recipe = new(BIG_MISTAKE_RECIPE, ingredient1, ingredient2);
			return BIG_MISTAKE_RECIPE;
		}

		bool _single = ingredient1 == null || ingredient2 == null; // we assume ATLEAST ONE INGREDIENT EXISTS

		// look for a recipe that matches all ingredients
		current_recipe = GlobalManager.G_RECIPES.Find(_recipe =>
		{
			for (int i = 0; i < _recipe.Combinations.Count; i++)
			{
				if (_recipe.Combinations[i][0].Item.ID == ingredient1 || _recipe.Combinations[i][0].Item.ID == ingredient2)
				{
					// single item recipes
					if (_recipe.Combinations[i].Count == 1)
					{
						// check if there was only one ingredient to check
						if (_single)
							return true;
						// else if both ingredients are the same then go ahead anyways
						if (ingredient1 == ingredient2)
							return true;
					}
					else if (_recipe.Combinations[i][1].Item.ID == ingredient1 || _recipe.Combinations[i][1].Item.ID == ingredient2)
						return true;
				}
			}
			return false;
		});

		current_recipe ??= new(MISTAKE_RECIPE, ingredient1, ingredient2);

		return current_recipe.Owner.Item.ID;
	}
	private void PlayAnim(string _anim)
	{
		if (animPlayer.CurrentAnimation == "close")
			return;
		animPlayer.Play(_anim);
	}
}