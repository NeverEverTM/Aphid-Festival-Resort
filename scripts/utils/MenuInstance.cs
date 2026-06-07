using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// A menu instance for a MenuHandler. Can be created on the go using minimal setup.
/// </summary>
public class MenuInstance : IEqualityComparer<MenuInstance>
{
    /// <summary>
    /// Identification name for this menu
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Whether or not this menu is a child of the current menu, if so add on-top, otherwise, set this as the parent (one parent alllowed at a time)
    /// </summary>
    public bool IsASubMenu { get; set; }
    public bool IsOpen { get; set; }
    public AnimationPlayer MenuPlayer { get; set; }

    /// <summary>
    /// Function that disposes of menu states after closing.
    /// <para>Param: New menu that will replace this one.</para>
    /// <para>returns: Wheter or not it could be closed successfully.</para>
    /// </summary>
    public Func<MenuInstance, bool> TryClose;

    /// <summary>
    /// Function that fires before the opening menu animation plays.
    /// <para>Param: The last active menu.</para>
    /// </summary>
    public Action<MenuInstance> Open;

    /// <summary>
    /// Function that fires before the closing menu animation plays.
    /// <para>Param: The next menu in line.</para>
    /// </summary>
    public Action<MenuInstance> Close;

    /// <summary>
    /// Function that fires after the closing menu animation plays. Meant for getting rid of unneeded data.
    /// </summar>
    public Action Dispose;

    public MenuInstance(string Name, AnimationPlayer MenuPlayer, Action<MenuInstance> Open = null,
            Func<MenuInstance, bool> TryClose = null, Action<MenuInstance> Close = null, bool IsASubMenu = false)
    {
        this.Name = Name;
        this.MenuPlayer = MenuPlayer;
        this.Open = Open;
        this.TryClose = TryClose;
        this.Close = Close;
        this.IsASubMenu = IsASubMenu;
    }
    public MenuInstance(string Name, AnimationPlayer MenuPlayer, Action<MenuInstance> Open,
            Func<MenuInstance, bool> TryClose, Action<MenuInstance> Close, Action Dispose, bool IsASubMenu = false)
    {
        this.Name = Name;
        this.MenuPlayer = MenuPlayer;
        this.Open = Open;
        this.TryClose = TryClose;
        this.Close = Close;
        this.Dispose = Dispose;
        this.IsASubMenu = IsASubMenu;
    }

    public bool Equals(MenuInstance x, MenuInstance y) =>
        x.Name.Equals(y?.Name);
    public int GetHashCode(MenuInstance obj) =>
        obj.Name.GetHashCode();
}