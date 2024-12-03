using Godot;
using System;

[GlobalClass]
public partial class Personality : Resource
{
    //[Export] public float healthDangerMultiplier;
    [ExportCategory("Danger Calculation")]
    [Export] public float healthDangerMax = 40;
    //[Export] public float staminaDangerMultiplier;
    [Export] public float staminaDangerMax = 20;
    //[Export] public float ammoDangerMultiplier;
    [Export] public float ammoDangerMax = 10;
    [Export] public float exposureDanger = 5;
    [Export] public float exposureDangerMax = 20;
    
    [ExportCategory("Behavior")]
    [Export] public float hideTreshold;
    [Export] public float maxHideTime = 60;
    [Export] public float hideCooldown;
    [Export] public float reloadTreshold;
    [Export] public float maxInvestigationTime = 5;

    [ExportCategory("Target Selection")]
    [Export] public float targetFocus = 2;
    [Export] public float distanceWeight = 1;
    [Export] public float healthWeight = 1;
    [Export] public float sightWeight = 1;
}
