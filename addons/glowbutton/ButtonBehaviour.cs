using Godot;

public partial class ButtonBehaviour : TextureButton
{
	/// <summary>
	/// Uses the color set by SelfModulate.
	/// </summary>
	public Color DefaultColor = new("white");
	[Export] public Color PressedColor = new("white");
	[Export] public Color FocusColor = new("white");
	[Export] public Color HoverColor = new("white");

	public override void _EnterTree()
	{
		DefaultColor = Modulate;
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
		if (!ButtonPressed && !HasFocus())
		{
			if (!IsHovered())
				Modulate = HoverColor;
			else
				Modulate = DefaultColor;
		}
	}
	public void SetFocusColor()
	{
		if (!ButtonPressed)
		{
			if (HasFocus())
				Modulate = FocusColor;
			else
				Modulate = DefaultColor;
		}
	}
	public void SetPressedColor()
	{
		if (ButtonPressed)
			Modulate = PressedColor;
		else
		{
			if (HasFocus())
				Modulate = FocusColor;
			else if (IsHovered())
				Modulate = HoverColor;
			else
				Modulate = DefaultColor;
		}
	}
}
