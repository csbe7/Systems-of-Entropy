using Godot;
using System;

public partial class ScaledTimer : Node
{
    [Signal] public delegate void TimeoutEventHandler();

    Game game;
    public float localTimescale = 1;

    public bool loop = false;
    public bool count = false;
    public bool destroyOnTimeout = false;
    public float time = 0;
    public float countdown = 0;

    public ScaledTimer(bool Loop = false, bool Destroy = false)
    {
        loop = Loop;
        destroyOnTimeout = Destroy;
    }

    public override void _Ready()
    {
        game = GetTree().Root.GetNode<Game>("Game");
        //SetTimescale(game.Timescale);
    }

    public override void _Process(double delta)
    {
        if (!count) return;

        if (countdown > 0)
        {
            countdown -= (float)delta * game.Timescale * localTimescale;
        }

        if (countdown <= 0)
        {
            EmitSignal(SignalName.Timeout);

            if (loop) countdown = time;
            else if (destroyOnTimeout) QueueFree();
            else count = false;
        }
    }

    public void Start(float duration)
    {
        time = duration;
        countdown = duration;
        count = true;
    }

    public void Stop()
    {
        count = false;
    }

    public void SetTimescale(float ts)
    {
        localTimescale = ts;
    }
}
