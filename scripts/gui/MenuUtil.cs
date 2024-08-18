using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class MenuUtil
{
	public MenuInstance CurrentMenu { get; private set; }
	public bool IsInMenu { get; private set; }
	public readonly List<MenuInstance> MenuList = new();

	public delegate void MenuEvent(bool _menuState, MenuInstance _menuInstance);
	public event MenuEvent OnSwitch;

	public record class MenuInstance
	{
		public string ID { get; private set; }
		public AnimationPlayer MenuPlayer { get; set; }
		public Action Open;
		public Action<MenuInstance> Close;
		public bool IsASubMenu { get; set; }

		public MenuInstance(string ID, AnimationPlayer _menu, Action _openAction, Action<MenuInstance> _closeAction, bool _isASub)
		{
			this.ID = ID;
			MenuPlayer = _menu;
			Open = _openAction;
			Close = _closeAction;
			IsASubMenu = _isASub;
		}
	}

	public bool OpenMenu(MenuInstance _menu)
	{
		// if is the same menu we have, close it then and go to previous one
		if (_menu.Equals(CurrentMenu))
		{
			GoBackInMenu();
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
	private void SwitchTo(MenuInstance _menu = null)
	{
		// Close last menu
		if (CurrentMenu != null)
		{
			CurrentMenu.Close?.Invoke(_menu);
			CurrentMenu.MenuPlayer.Play("close");
			if (_menu == null || !_menu.IsASubMenu)
				MenuList.Clear();
		}

		// Open new menu
		if (_menu != null)
		{
			_menu.Open?.Invoke();
			_menu.MenuPlayer.Play("open");
			MenuList.Add(_menu);
		}

		// Set menu state
		CurrentMenu = _menu;
		IsInMenu = MenuList.Count > 0;
		OnSwitch?.Invoke(IsInMenu, CurrentMenu);
	}
	public void GoBackInMenu()
	{
		if (MenuList.Count > 1)
		{
			MenuList.RemoveAt(MenuList.Count - 1);
			SwitchTo(MenuList.Last());
		}
		else if (MenuList.Count == 1)
			SwitchTo();
	}
}