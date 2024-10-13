using Godot;
using System;

[GlobalClass]
public partial class Item : Resource
{
    public enum Type{
        misc,
        key,
        aid,
        apparel,
        none,
    }

    [ExportCategory("Info")]
    [Export] public string name = "";
    [Export] public string description = "";
    [Export] public Type type = Type.none;
    [Export] public int value = 0;
    
    [ExportCategory("Functions")]
    [Export] public bool droppable = true;
    [Export] public bool usable = false;
    [Export] public bool equippable = false;

    [ExportCategory("Inventory Info")]
    [Export] public int maxStack = 999;
    [Export] public float weight = 0;

}
