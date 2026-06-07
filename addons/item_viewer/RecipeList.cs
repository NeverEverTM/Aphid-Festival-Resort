#if TOOLS
using Godot;
using System;

[Tool]
public partial class RecipeList : Control, ItemMasterDB.ITab
{
    public void START()
    {
        return;
    }
    public void ON_LOAD_TAB()
    {
        //VALIDATE_ALL_RECIPES();
    }
    
    internal static void VALIDATE_ALL_RECIPES()
    {
        bool _check = true;
        foreach (var _pair in ItemMasterDB.Instance.RECIPE_DATABASE)
        {
            for (int r = 0; r < _pair.Value.Combinations.Count; r++)
            {
                if (!ItemMasterDB.IS_RECIPE_MATCH_VALID(_pair.Value.Combinations[r], out RecipeData _existing, true, _pair.Key))
                {
                    _check = false;
                    GD.PrintErr($"{_existing.Owner.ID} has the same recipe as {_pair.Key}");
                }
            }
        }
        if (_check)
            GD.Print("Succesfully verified all recipes");
    }
}
#endif