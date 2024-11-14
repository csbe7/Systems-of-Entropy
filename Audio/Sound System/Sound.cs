using Godot;
using System;

[GlobalClass]
public partial class Sound : Resource
{
    [Export] public AudioStream stream;
    [Export] public float volumeDb;
    [Export] public float  maxHearingDistance;
    [Export] public int priority;
    public CharacterSheet emitter;
}
