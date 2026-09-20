using Godot;

public partial class ItemSlotTooltip : GlowButton
{
    public override GodotObject _MakeCustomTooltip(string forText)
    {
        if (string.IsNullOrWhiteSpace(forText))
            return null;
        RichTextLabel _tooltip = ResourceLoader.Load<PackedScene>("uid://c70h72bbst8n8").Instantiate() as RichTextLabel;
        string[] _text = forText.Split("@");

        _tooltip.GetChild<RichTextLabel>(0).Text = _text[0]; //title

        // tooltip content
        _tooltip.Text = "\n\n" + _text[1];
        return _tooltip;
    }
}
