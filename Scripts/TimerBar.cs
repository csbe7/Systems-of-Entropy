using Godot;
using System;

public partial class TimerBar : Control
{
    public ScaledTimer timer;
    [Export] ProgressBar pb;
    
    [Export] bool HideOnReady = true;
    [Export] bool inverse;

   

    public override void _Ready()
    {
        pb = GetNode<ProgressBar>("ProgressBar");
        if (HideOnReady) HideAll();
    }


    public override void _Process(double delta)
    {
        if (IsInstanceValid(timer))
        {
            if (inverse) pb.Value = pb.MaxValue - (timer.countdown/timer.time) * pb.MaxValue;
            else pb.Value = (timer.countdown/timer.time) * pb.MaxValue;
        }
    }

    public void HideAll()
    {
        Hide();
        if (IsInstanceValid(pb)) pb.Hide();
    }

    public void ShowAll()
    {
        Show();
        if (IsInstanceValid(pb)) pb.Show();
    }
}
