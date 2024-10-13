using Godot;
using System;

[GlobalClass]
public partial class StatModifier : Resource
{
    public enum Mode
    {
       Flat,
       PercentageFromBase,
       Percentage,
    }

	[Export] public float value;
    [Export] public Mode mode; 
    [Export] public int order;


	/*public StatModifier(float v, Mode m, int o)
    {
        value = v;
        mode = m;
        order = o;
    }

    public StatModifier(float v, Mode m) : this(v, m, (int)m * 10) { }*/
}
