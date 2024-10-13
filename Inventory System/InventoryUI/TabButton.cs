using Godot;
using System;

public partial class TabButton : Button
{
    [Export] public Item.Type itemType;

    [Signal] public delegate void TabSelectedEventHandler(TabButton tab);

    public override void _Ready()
    {
        ButtonDown += OnButtonDown;
    }

    public void OnButtonDown()
    {
        EmitSignal(SignalName.TabSelected, this);
    }
}
