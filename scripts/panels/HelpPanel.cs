using System;
using Godot;

public partial class HelpPanel : MenuControl
{
    public override string ID => "help";
    public override bool IsASubMenu => true;

	[Export] private PackedScene[] panels;
	[Export] private Control container_node;
	[Export] private Label count_label, title_label;
	[Export] private BaseButton left_button, right_button;

	private int current;
	private enum Direction { Left, Right }

	public override void _Ready()
	{
		SetProcessInput(false);
		left_button.Pressed += () => AdvancePage(Direction.Left);
		right_button.Pressed += () => AdvancePage(Direction.Right);
    }

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed(InputNames.Left))
			AdvancePage(Direction.Left);
		else if (@event.IsActionPressed(InputNames.Right))
			AdvancePage(Direction.Right);
	}

	private void AdvancePage(Direction _direction)
	{
		if (_direction == Direction.Left)
		{
			current--;
			if (current < 0)
				current = panels.Length - 1;
		}
		else if (_direction == Direction.Right)
		{
			current++;
			if (current == panels.Length)
				current = 0;
		}
		SetPage(current);
	}
	public void SetPage(int _index)
	{
		current = _index;
		container_node.GetChildOrNull<Control>(0)?.QueueFree();
		Control _node = panels[current].Instantiate<Control>();
		SetAllLabels(_node);
		container_node.AddChild(_node);

		count_label.Text = $"{current + 1}/{panels.Length}";
		SoundManager.CreateSound("ui/button_switch");
	}
	private void SetAllLabels(Control _page)
	{
		var _labels = _page.GetChildren();
		string _categoryName = _page.Name;
		title_label.Text = Tr($"help_{_categoryName}_title");

		for (int i = 0, d = 0; i < _labels.Count; i++)
		{
			if (_labels[i] is not RichTextLabel)
				continue;
			string _dialogue = Tr($"help_{_categoryName}_{d}");

			if (_labels[i].HasMeta("controls"))
			{
				string[] _args = Array.ConvertAll((string[])_labels[i].GetMeta("controls"), ControlsManager.GetLocalizedActionName);
				for (int s = 0; s < _args.Length; s++)
					_args[s] = $"[color=gold]{_args[s]}[/color]";
				_dialogue = string.Format(_dialogue, _args);
			}

			(_labels[i] as RichTextLabel).Text = _dialogue;
			d++;
		}
	}

    protected override void Open(MenuInstance _last)
    {
		SetProcessInput(true);
		current = 0;
		SetPage(0);
    }
    protected override void Close(MenuInstance _next)
    {
        SetProcessInput(false);
    }
}
