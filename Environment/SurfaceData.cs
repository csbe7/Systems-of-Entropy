using Godot;
using System;

[GlobalClass]
public partial class SurfaceData : Resource
{
    [Export] public Sound stepSound;
    [Export] public float pitchScale = 1;
    [Export] public float volumeMultipler = 1;
    [Export] public float soundDampening = 2;
    
}
