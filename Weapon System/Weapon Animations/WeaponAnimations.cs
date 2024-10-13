using Godot;
using System;

[GlobalClass]
public partial class WeaponAnimations : Resource
{
    
    [Export] public string lib;
    
    [Export] public string sprint;
    [Export] public string draw;
    [Export] public Godot.Collections.Array<string> attacks = new Godot.Collections.Array<string>();
    [Export] public Godot.Collections.Array<float> attackSpeeds = new Godot.Collections.Array<float>();
    //STANDING
    [ExportCategory("Standing")]
    [Export] public string standingIdle;
    [Export] public string walkForward;
    [Export] public string walkBackward;
    [Export] public string walkRight;
    [Export] public string walkLeft;
    [Export] public string walkForwardRight;
    [Export] public string walkForwardLeft;
    [Export] public string walkBackwardRight;
    [Export] public string walkBackwardLeft;
    
    //CROUCHING
    [ExportCategory("Crouching")]
    [Export] public string crouchingIdle;
    [Export] public string crouchForward;
    [Export] public string crouchBackward;
    [Export] public string crouchRight;
    [Export] public string crouchLeft;
    [Export] public string crouchForwardRight;
    [Export] public string crouchForwardLeft;
    [Export] public string crouchBackwardRight;
    [Export] public string crouchBackwardLeft;

}
