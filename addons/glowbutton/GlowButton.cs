using Godot;

public partial class GlowButton : TextureButton
{
	/// <summary>
	/// Uses the color set by SelfModulate.
	/// </summary>
	public Color DefaultColor = new("white");
	[Export] public Color PressedColor = new("white");
	[Export] public Color FocusColor = new("white");
	[Export] public Color HoverColor = new("white");
	[Export] public bool ModulateChildren = true;

	public override void _EnterTree()
	{
		DefaultColor = ModulateChildren ? Modulate : SelfModulate;
		ButtonDown += SetPressedColor;
		ButtonUp += SetPressedColor;
		FocusEntered += SetFocusColor;
		FocusExited += SetFocusColor;
		MouseEntered += SetHoverColor;
		MouseExited += SetHoverColor;
	}
	public override void _ExitTree()
	{
		ButtonDown -= SetPressedColor;
		ButtonUp -= SetPressedColor;
		FocusEntered -= SetFocusColor;
		FocusExited -= SetFocusColor;
		MouseEntered -= SetHoverColor;
		MouseExited -= SetHoverColor;
	}

	public void SetHoverColor()
	{
		if (Disabled)
			return;

		if (!ButtonPressed && !HasFocus())
		{
			if (ModulateChildren)
			{
				if (!IsHovered())
					Modulate = HoverColor;
				else
					Modulate = DefaultColor;
			}
			else
			{
				if (!IsHovered())
					SelfModulate = HoverColor;
				else
					SelfModulate = DefaultColor;
			}
		}
	}
	public void SetFocusColor()
	{
		if (Disabled)
			return;

		if (!ButtonPressed)
		{
			if (ModulateChildren)
			{
				if (HasFocus())
					Modulate = FocusColor;
				else
					Modulate = DefaultColor;
			}
			else
			{
				if (HasFocus())
					SelfModulate = FocusColor;
				else
					SelfModulate = DefaultColor;
			}
		}
	}
	public void SetPressedColor()
	{
		if (Disabled)
			return;

		if (ButtonPressed)
			Modulate = PressedColor;
		else
		{
			if (ModulateChildren)
			{
				if (HasFocus())
					Modulate = FocusColor;
				else if (IsHovered())
					Modulate = HoverColor;
				else
					Modulate = DefaultColor;
			}
			else
			{
				if (HasFocus())
					SelfModulate = FocusColor;
				else if (IsHovered())
					SelfModulate = HoverColor;
				else
					SelfModulate = DefaultColor;
			}
		}
	}
}
