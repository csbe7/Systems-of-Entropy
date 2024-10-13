using Godot;
using System;

[GlobalClass]
public partial class NPCMoveState : Resource
{
    [Export] public float followWeight = 1;
    [Export] public float strafeWeight = 1;
    [Export] public float obstacleAvoidanceDistance = 3;
    [Export] public float neighbourAvoidanceWeight = 1;
    [Export] public float fleeWeight = 0;
    [Export] public float lineOfSightDistance = 1;
    [Export] public float lineOfSightWeight = 0;

}
