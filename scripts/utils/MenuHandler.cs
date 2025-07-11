using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MenuHandler
{
	public MenuInstance Current { get; private set; }
	public bool Processing { get; private set; } = false;
	public bool IsActive { get; private set; } = false;

	public readonly List<MenuInstance> Available = [];
	public MenuInstance Pending;

	public readonly List<Action<MenuInstance, MenuInstance>> OnSwitch = [];

	public async Task<bool> SetTo(MenuInstance _menu)
	{
		if (Processing)
			return true;
		try
		{
			if (_menu == null)
			{
				await GoBack();
				return false;
			}
			// if is the same menu we have, close it and go to previous one
			else if (_menu.Equals(Current))
			{
				await GoBack();
				return false;
			}
			// if menu isnt already in the list, open it up
			else if (!Available.Contains(_menu))
			{
				Pending = _menu;
				await SwitchTo(Pending);
				return true;
			}
		}
		catch (Exception _error)
		{
			Logger.Print(Logger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error);
		}
		return false;
	}
	/// <summary>
	/// Closes the current menu. DOES not call events.
	/// </summary>
	/// <param name="_newMenu"></param>
	/// <returns></returns>
	public async Task<bool> CloseCurrent(MenuInstance _newMenu = null)
	{
		bool alreadyActive = Processing, _isCallbackDone = false;
		Processing = true;

		if (Current == null)
		{
			if (!alreadyActive) // make sure to not set it back to false if another function is managing it
				Processing = false;
			return true;
		}

		// Close last menu
		// see if closing this menu is possible
		if (Current.TryClose != null)
		{
			bool _canProceed = true;
			Callable _closeCallback = Callable.From(() =>
			{
				try
				{
					_canProceed = Current.TryClose.Invoke(_newMenu);
				}
				catch (Exception _error)
				{
					Logger.Print(Logger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error);
				}
				_isCallbackDone = true;
			});
			_closeCallback.CallDeferred();
			while (!_isCallbackDone)
				await Task.Delay(1);
			if (!_canProceed)
				return false;
		}

		_isCallbackDone = false;
		var _copyCurrent = Current;
		Callable _proceedCallback = Callable.From((string _) =>
		{
			try
			{
				Current.IsOpen = false;
				Current.Close?.Invoke(_newMenu);
				if (_newMenu == null || _newMenu.IsASubMenu)
					Available.Clear();
			}
			catch (Exception _error)
			{
				Logger.Print(Logger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error);
			}
			_isCallbackDone = true;
		}), _connect = Callable.From(() =>
		{
			Current.MenuPlayer.Connect(AnimationMixer.SignalName.AnimationFinished, _proceedCallback);
			Current.MenuPlayer.Play(StringNames.CloseAnim);
		}),
		 _disconnect = Callable.From(() =>
			_copyCurrent.MenuPlayer.Disconnect(AnimationMixer.SignalName.AnimationFinished, _proceedCallback));

		_connect.CallDeferred();
		while (!_isCallbackDone)
			await Task.Delay(1);
		_disconnect.CallDeferred();

		if (!alreadyActive) // make sure to not set it back to false if another function is managing it
			Processing = false;
		return true;
	}
/// <summary>
/// Switch to the given menu and updates the state, a value of 'null' just closes the menu without complications
/// </summary>
/// <param name="_menu">Next menu to display</param>
protected async Task<bool> SwitchTo(MenuInstance _newMenu)
{
	Processing = true;
	if (!await CloseCurrent(Pending))
		return false;

	MenuInstance _lastMenu = Current;

	// Open new menu
	if (Pending != null)
	{
		bool _isCallbackDone = false;
		Callable _callback = Callable.From(() =>
		{
			_newMenu.Open?.Invoke(_lastMenu);
			_newMenu.MenuPlayer?.Play(StringNames.OpenAnim);
			_isCallbackDone = true;
		});
		_callback.CallDeferred();
		while (!_isCallbackDone)
			await Task.Delay(1);
		Pending.IsOpen = true;
		Available.Add(Pending);
	}

	// Set menu state
	Current = Pending;
	IsActive = Available.Count > 0;

	try
	{
		for (int i = 0; i < OnSwitch.Count; i++)
		{
			int _index = i;
			Callable _actionCall = Callable.From(() =>
				OnSwitch[_index].Invoke(_lastMenu, _newMenu));
			_actionCall.CallDeferred();
		}
	}
	catch (Exception _error)
	{
		Logger.Print(Logger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error);
	}

	Pending = null;
	Processing = false;
	return true;
}
public async Task GoBack()
{
	if (Available.Count == 0)
		return;

	if (Available.Count > 1)
		Pending = Available[^2];
	else
		Pending = null;

	if (await SwitchTo(Pending))
	{
		if (Available.Count - 1 > 0)
			Available.RemoveAt(Available.Count - 1);
		IsActive = Available.Count > 0;
	}
}

public void Clear()
{
	Available.Clear();
	OnSwitch.Clear();
	Processing = IsActive = false;
	Current = Pending = null;
}
}
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
	/// <para>Param: Current menu that will be replaced for this one.</para>
	/// </summary>
	public Action<MenuInstance> Open;
	/// <summary>
	/// Function that fires before the closing menu animation plays.
	/// <para>Param: Current menu that will be replaced for this one.</para>
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

	public bool Equals(MenuInstance x, MenuInstance y) =>
		x.Name.Equals(y?.Name);
	public int GetHashCode(MenuInstance obj) =>
		obj.Name.GetHashCode();
}
