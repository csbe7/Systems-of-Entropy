using Godot;
using System;

[GlobalClass]
public partial class InventoryData : Resource
{
    [Export] public Godot.Collections.Array<SlotData> items = new Godot.Collections.Array<SlotData>();
}
