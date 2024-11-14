using Godot;
using System;

public partial class AI : Node
{
    [Export] public CharacterSheet sheet;
    [Signal] public delegate void StateChangedEventHandler();

    [Signal] public delegate void SoundHeardEventHandler(Sound sound, Vector3 source);
    [Signal] public delegate void EnemyDetectedEventHandler();
    [Signal] public delegate void AlertedEventHandler();
}
