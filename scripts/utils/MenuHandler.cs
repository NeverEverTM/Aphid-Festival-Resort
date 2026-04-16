using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MenuHandler
{
	public MenuInstance Current { get; protected set; }
	public MenuInstance Pending { get; protected set; }
	public readonly List<MenuInstance> Available = [];

	/// <summary>
	/// If is in the process of selecting a menu.
	/// </summary>
	public bool Processing { get; protected set; } = false;
	/// <summary>
	///  Wheter or not is displaying a menu.
	/// </summary>
	public bool IsActive { get; protected set; } = false;
	protected bool IsOnCallback { get; set; } = false;

	public class MenuArgs : EventArgs
	{
		/// <summary>
		/// The current menu loaded.
		/// </summary>
		public MenuInstance Current;
		/// <summary>
		/// The menu to be loaded in next.
		/// </summary>
		public MenuInstance Next;

		public bool IsActive, WasActive;
	}
	public enum MenuEvents
	{
		OnPreSwitch,
		OnPostSwitch
	}
	protected Dictionary<MenuEvents, List<Action<MenuArgs>>> Events = new()
	{
		{ MenuEvents.OnPreSwitch, new() },
		{ MenuEvents.OnPostSwitch, new() }
	};

	public void AddEventListener(Action<MenuArgs> _action, MenuEvents _event) =>
		Events[_event].Add(_action);
	public void RemoveEventListener(Action<MenuArgs> _action, MenuEvents _event) =>
		Events[_event].Remove(_action);

	protected void OnCloseConnect(StringName _)
	{
		try
		{ Current.Close?.Invoke(Pending); }
		catch (Exception _error)
		{ DebugLogger.Print(DebugLogger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error); }
		Current.MenuPlayer.Play(StringNames.CloseAnim);
		Current.MenuPlayer.AnimationFinished += OnCloseDisconnect;
	}
	protected void OnCloseDisconnect(StringName _)
	{
		Current.MenuPlayer.AnimationFinished -= OnCloseDisconnect;
		try
		{ Current.Dispose?.Invoke(); }
		catch (Exception _error)
		{ DebugLogger.Print(DebugLogger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error); }
		IsOnCallback = false;
	}
	protected void OnOpenConnect()
	{
		try
		{
			Pending.Open?.Invoke(Current);
		}
		catch (Exception _error)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, _error);
		}
		Pending.MenuPlayer.AnimationFinished += OnOpenDisconnect;
		Pending.MenuPlayer?.Play(StringNames.OpenAnim);
	}
	protected void OnOpenDisconnect(StringName _)
	{
		Pending.MenuPlayer.AnimationFinished -= OnOpenDisconnect;
		IsOnCallback = false;
	}
	protected async Task CallPreLoad()
	{
		IsOnCallback = true;
		Callable _actionCall = Callable.From(() =>
		{
			try
			{
				GlobalManager.Utils.InvokeEventListeners(Events[MenuEvents.OnPreSwitch], new()
				{
					Current = Current,
					Next = Pending,
					IsActive = Pending != null,
					WasActive = IsActive
				});
			}
			catch (Exception _error)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Error, "MenuHandler: Unable to invoke event.", _error);
			}

			IsOnCallback = false;
		});
		_actionCall.CallDeferred();
		while (IsOnCallback)
			await Task.Delay(1);
	}
	protected async Task CallPostLoad(MenuInstance _lastMenu)
	{
		IsOnCallback = true;
		Callable _actionCall = Callable.From(() =>
		{
			try
			{
				GlobalManager.Utils.InvokeEventListeners(Events[MenuEvents.OnPostSwitch], new()
				{
					Current = _lastMenu,
					Next = Current,
					IsActive = Available.Count > 0,
					WasActive = IsActive
				});
			}
			catch (Exception _error)
			{
				DebugLogger.Print(DebugLogger.LogPriority.Error, "MenuHandler: Unable to invoke event.", _error);
			}

			IsOnCallback = false;
		});
		_actionCall.CallDeferred();
		while (IsOnCallback)
			await Task.Delay(1);
	}

	/// <summary>
	/// Initializes the given menu into the menu stack. Closes the menu if set to null or gets handed the current root menu.
	/// </summary>
	/// <param name="_menu">The menu to intialize. Only one root menu can exist, from which submenus stack on top of.</param>
	/// <returns></returns>
	public async Task<bool> SetTo(MenuInstance _menu)
	{
		if (Processing)
			return false;
		Processing = true;
		try
		{
			if (_menu == null)
			{
				await GoBack(true);
				return true;
			}
			// if is the same menu we have, close it and go to previous one
			else if (_menu.Equals(Current))
			{
				await GoBack(true);
				return true;
			}
			// if menu isnt already in the list, open it up
			else if (!Available.Contains(_menu))
			{
				Pending = _menu;
				await SwitchToPending();
				Processing = false;
				return true;
			}
		}
		catch (Exception _error)
		{
			DebugLogger.Print(DebugLogger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error);
		}
		Processing = false;
		return false;
	}
	/// <summary>
	/// Closes the current menu. DOES not call events.
	/// </summary>
	/// <param name="_newMenu"></param>
	/// <returns></returns>
	protected async Task<bool> CloseCurrent()
	{
		// attempt to close this menu
		if (Current.TryClose != null)
		{
			bool _canProceed = true;
			Callable _closeCallback = Callable.From(() =>
			{
				try
				{ _canProceed = Current.TryClose.Invoke(Pending); }
				catch (Exception _error)
				{ DebugLogger.Print(DebugLogger.LogPriority.Error, "MenuHandler: Unable to process menu request.", _error); }
				IsOnCallback = false;
			});

			IsOnCallback = true;
			_closeCallback.CallDeferred();
			while (IsOnCallback)
				await Task.Delay(1);

			if (!_canProceed)
				return false;
		}

		// we wait for the all the callbacks to process
		IsOnCallback = true;
		Callable.From(() => OnCloseConnect(null)).CallDeferred();
		while (IsOnCallback)
			await Task.Delay(1);
		Current.IsOpen = false;

		return true;
	}

	/// <summary>
	/// Switch to the pending menu and updates the state, a value of 'null' closes and disposes of the whole menu list.
	/// </summary>
	/// <param name="_menu">Next menu to display</param>
	protected async Task<bool> SwitchToPending()
	{
		if (Current != null && !await CloseCurrent())
			return false;

		MenuInstance _lastMenu = Current;

		await CallPreLoad();

		// Open new menu
		if (Pending != null)
		{
			IsOnCallback = true;
			Callable.From(OnOpenConnect).CallDeferred();
			while (IsOnCallback)
				await Task.Delay(1);

			Pending.IsOpen = true;

			// if this is a root menu and it is not the current root, we clear the list and add this root
			if (!Pending.IsASubMenu)
			{
				if (!Available.Exists(m => m.Name.Equals(Pending.Name)))
				{
					Available.Clear();
					Available.Add(Pending);
				}
			}
			else
				Available.Add(Pending);
		}
		else
			Available.Clear();

		// Set menu state
		if (Current != null)
			Current.IsOpen = false;
		Current = Pending;

		await CallPostLoad(_lastMenu);

		IsActive = Available.Count > 0;
		Pending = null;
		return true;
	}
	/// <summary>
	/// Goes back to the previous menu on the list and removes it. If no more menus are present then it clears the list.
	/// </summary>
	/// <param name="_ignoreProcessing">By default, this method will not execute if the handler is busy. This is set to true by the handler internally to fire the method during processing.</param>
	public async Task GoBack(bool _ignoreProcessing = false)
	{
		if (!_ignoreProcessing && Processing)
			return;

		if (Available.Count == 0)
		{
			Processing = false;
			return;
		}
		Processing = true;

		// return to previous one, or set to null when no more are available
		if (Available.Count > 1)
			Pending = Available[^2];
		else
			Pending = null;

		// if the menu was able to switch, remove it from the list
		if (await SwitchToPending())
		{
			if (Available.Count > 0)
				Available.RemoveAt(Available.Count - 1);
			IsActive = Available.Count > 0;
		}
		Processing = false;
	}

	public void Clear()
	{
		Available.Clear();
		foreach (var _pair in Events)
			_pair.Value.Clear();
		Processing = IsActive = false;
		Current = Pending = null;
	}
}

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
/// <summary>
/// A menu instance for a MenuHandler. Allows an easy translation from a node to a menu instance, while giving it further control.
/// </summary>
public interface IMenuInstance
{
	/// <summary>
	/// Creates the menu instance and passes it to the caller, make sure to cache the menu and pass a reference instead when its called again.
	/// </summary>
	/// <returns></returns>
	public MenuInstance Create();

	/// <summary>
	/// Function that fires before the opening menu animation plays.
	/// <para>Param: The last active menu.</para>
	/// </summary>
	public void Open(MenuInstance _last);

	/// <summary>
	/// Function that disposes of menu states after closing.
	/// <para>Param: New menu that will replace this one.</para>
	/// <para>returns: Wheter or not it could be closed successfully.</para>
	/// </summary>
	public bool TryClose(MenuInstance _next);

	/// <summary>
	/// Function that fires before the closing menu animation plays.
	/// <para>Param: The next menu in line.</para>
	/// </summary>
	public void Close(MenuInstance _next);

	/// <summary>
	/// Function that fires after the closing menu animation plays. Meant for getting rid of unneeded data.
	/// </summar>
	public void CloseDispose() { }
}