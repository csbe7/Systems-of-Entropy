using Godot;
using System;

[GlobalClass]
public partial class SlotData : Resource
{
    [Export] public Item item;
    [Export] public int amount = 1;
    
    [ExportCategory("Gun Data")]
    [Export] public int charge;
    
}
