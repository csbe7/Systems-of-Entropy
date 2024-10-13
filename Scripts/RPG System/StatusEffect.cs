using Godot;
using System;

public partial class StatusEffect : Node
{
    public CharacterSheet giver;
    public CharacterSheet target;
    [Export] public int priority = 0;

    [Export] public float power = 1; 
    [Export] public float duration; //0 = infinite
    [Export] public bool inheritTargetTimescale = true;
    [Export] public bool inheritGiverTimescale = false;

    public ScaledTimer Timer;

    public override void _Ready()
    {
        if (duration == 0) return;
        Timer = new ScaledTimer();
        Timer.destroyOnTimeout = true;

        if (inheritTargetTimescale) Timer.SetTimescale(Timer.localTimescale * target.localTimescale);
        if (inheritGiverTimescale && giver != target) Timer.SetTimescale(Timer.localTimescale * giver.localTimescale);

        Timer.Start(duration);

        Timer.Timeout += OnTimeout;
    }

    public virtual void OnTimeout()
    {
        QueueFree();
    }

}
