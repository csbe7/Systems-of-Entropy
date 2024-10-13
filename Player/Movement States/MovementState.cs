using Godot;
using System;

public partial class MovementState : Resource
{
    [Export] public int id;

    [Export] public float acceleration;
    [Export] public float speed;

    [Export] public float animationSpeed = 1;
}
