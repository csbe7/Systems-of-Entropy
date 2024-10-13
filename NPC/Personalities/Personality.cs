using Godot;
using System;

[GlobalClass]
public partial class Personality : Resource
{
    //[Export] public float healthDangerMultiplier;
    [ExportCategory("Values")]
    [Export] public float healthDangerMax = 40;
    //[Export] public float staminaDangerMultiplier;
    [Export] public float staminaDangerMax = 20;
    //[Export] public float ammoDangerMultiplier;
    [Export] public float ammoDangerMax = 10;
    [Export] public float exposureDanger = 5;
    [Export] public float exposureDangerMax = 20;
    
    [ExportCategory("Behavior")]
    [Export] public float hideTreshold;
    [Export] public float reloadTreshold;
}
