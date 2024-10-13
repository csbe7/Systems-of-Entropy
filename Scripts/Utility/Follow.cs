using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Follow : Node3D
{
    Node3D target;

    public override async void _Ready()
    {
        target = GetParent<Node3D>();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Node breaker = GetNode<Node>("../Break Transform Inheritance");
        Reparent(breaker);
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition = target.GlobalPosition;
    }
}
