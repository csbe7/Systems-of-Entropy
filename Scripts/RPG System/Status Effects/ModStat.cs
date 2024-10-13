using Godot;
using System;

public partial class ModStat : StatusEffect
{
    [Export] public string targetStat;
    [Export] StatModifier modifier;

    public override void _Ready()
    {
        base._Ready();
        target.AddStatModifier(modifier, targetStat);
    }

    public override void OnTimeout()
    {
        target.RemoveStatModifier(modifier, targetStat);
        base.OnTimeout();
    }
}
