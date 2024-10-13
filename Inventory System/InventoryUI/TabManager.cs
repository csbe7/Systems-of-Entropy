using Godot;
using System;

public partial class TabManager : Control
{
    [Export] public Godot.Collections.Array<TabButton> tabs = new Godot.Collections.Array<TabButton>();
    int currTab;

    [Export] Color selectedColor;
    [Export] Color unselectedColor;

    [Signal] public delegate void TabChangedEventHandler(TabButton newTab);

    public override void _Ready()
    {
        foreach(TabButton tab in tabs)
        {
            tab.TabSelected += OnTabSelected;
        }
    }

    public void OnTabSelected(TabButton newTab)
    {
        tabs[currTab].GetNode<ColorRect>("Tab").Color = unselectedColor;

        int i = 0;
        foreach(TabButton tab in tabs)
        {
            if (tabs[i] == newTab) currTab = i;
            i++;
        }

        tabs[currTab].GetNode<ColorRect>("Tab").Color = selectedColor;

        EmitSignal(SignalName.TabChanged, newTab);
    }


}
