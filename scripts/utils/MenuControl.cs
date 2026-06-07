using Godot;

/// <summary>
/// A menu instance for a MenuHandler. Allows an easy translation from a node to a menu instance, while giving it further control.
/// </summary>
public abstract partial class MenuControl : Control
{
	public abstract string ID { get; }
	public virtual bool IsASubMenu { get; } = false;

	[Export] private AnimationPlayer animPlayer;
	private MenuInstance cached_menu;

	/// <summary>
	/// Creates the menu instance and passes it to the caller, make sure to cache the menu and pass a reference instead when its called again.
	/// </summary>
	/// <returns></returns>
	internal virtual MenuInstance GetMenuInstance()
	{
		cached_menu ??= new(ID, animPlayer, Open, TryClose, Close, CloseDispose, IsASubMenu);
        return cached_menu;
	}

	/// <summary>
	/// Function that fires before the opening menu animation plays.
	/// <para>Param: The last active menu.</para>
	/// </summary>
	protected abstract void Open(MenuInstance _last);

	/// <summary>
	/// Function that disposes of menu states after closing.
	/// <para>Param: New menu that will replace this one.</para>
	/// <para>returns: Wheter or not it could be closed successfully.</para>
	/// </summary>
	protected virtual bool TryClose(MenuInstance _next)
	{
		return true;
	}

	/// <summary>
	/// Function that fires before the closing menu animation plays.
	/// <para>Param: The next menu in line.</para>
	/// </summary>
	protected virtual void Close(MenuInstance _next)
	{
		return;
	}

	/// <summary>
	/// Function that fires after the closing menu animation plays. Meant for getting rid of unneeded data.
	/// </summary>
	protected virtual void CloseDispose() { }
}