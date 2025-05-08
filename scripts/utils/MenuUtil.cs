using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class MenuUtil
{
	public MenuInstance CurrentMenu { get; private set; }
	public bool IsBusy { get; private set; }
	public readonly List<MenuInstance> MenuList = [];

	public delegate void MenuEvent(MenuInstance _last, MenuInstance _current);
	/// <summary>
	/// Parameters: Last Menu and Current Menu
	/// </summary>
	public event MenuEvent OnSwitch;

	public record class MenuInstance(string Name, AnimationPlayer MenuPlayer,
			Action<MenuInstance> Open, Func<MenuInstance, bool> Close, bool IsASubMenu = false)
	{
		/// <summary>
		/// Identification name for this menu
		/// </summary>
		public string Name { get; private set; } = Name;
		public bool IsOpen { get; set; }
		/// <summary>
		/// Whether or not this menu is a child of the current menu, if so add on-top, otherwise, set this as the parent (one parent alllowed at a time)
		/// </summary>
		public bool IsASubMenu { get; set; } = IsASubMenu;
		public AnimationPlayer MenuPlayer { get; set; } = MenuPlayer;

		/// <summary>
		/// Closes the menu, it is given which menu is the next one and indicates whether or not this action can be done
		/// </summary>
		public Func<MenuInstance, bool> Close = Close;
		/// <summary>
		/// Opens the menu, it is given which menu was the last one
		/// </summary>
		public Action<MenuInstance> Open = Open;
	}

	public bool OpenMenu(MenuInstance _menu)
	{
		if (_menu == null)
		{
			GoBack();
			return false;
		}
		// if is the same menu we have, close it and go to previous one
		if (_menu.Name.Equals(CurrentMenu?.Name))
		{
			GoBack();
			return false;
		}
		// if menu isnt already in the list, open it up
		else if (!MenuList.Contains(_menu))
		{
			SwitchTo(_menu);
			return true;
		}
		return false;
	}
	/// <summary>
	/// Switch to the given menu and updates the state, a value of 'null' just closes the menu without complications
	/// </summary>
	/// <param name="_menu">Next menu to display</param>
	private bool SwitchTo(MenuInstance _menu)
	{
		var _lastMenu = _menu;
		// Close last menu
		if (CurrentMenu != null)
		{
			// cancel close if this menu is currently not allowed to
			if (CurrentMenu.Close != null && !CurrentMenu.Close.Invoke(_menu))
				return false;
			CurrentMenu.IsOpen = false;
			CurrentMenu.MenuPlayer.Play(StringNames.CloseAnim);
			if (_menu == null || !_menu.IsASubMenu)
				MenuList.Clear();
		}

		// Open new menu
		if (_menu != null)
		{
			_menu.Open?.Invoke(CurrentMenu);
			_menu.MenuPlayer.Play(StringNames.OpenAnim);
			_menu.IsOpen = true;
			MenuList.Add(_menu);
		}

		// Set menu state
		CurrentMenu = _menu;
		IsBusy = MenuList.Count > 0;
		OnSwitch?.Invoke(_lastMenu, CurrentMenu);
		return true;
	}
	public void GoBack()
	{
		if (MenuList.Count > 1)
		{
			if (SwitchTo(MenuList[^2]))
				MenuList.RemoveAt(MenuList.Count - 1);
		}
		else if (MenuList.Count == 1)
			SwitchTo(null);
	}
}