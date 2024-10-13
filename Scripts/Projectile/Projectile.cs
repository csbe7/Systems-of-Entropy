using Godot;
using System;

public partial class Projectile : Node3D
{
    //[Signal] public delegate void HitEventHandler(Godot.Collections.Dictionary hitInfo);
    public Weapon weapon; 
    public CharacterSheet shooter;
    public Vector3 direction;

    [Export] public float distance;
    [Export] public float duration;
    [Export] public float speed;
    [Export] public Vector3 size;
}
