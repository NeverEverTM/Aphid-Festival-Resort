using Godot;
using System;

public partial class TileMapPlaceableProperty : TileMapLayer
{
    [Export] private BuildMenu.PlaceableArea placeableProperty;

    public override void _EnterTree()
    {
        SetMeta(StringNames.PlaceableAreaMeta, (int)placeableProperty);
    }
}
