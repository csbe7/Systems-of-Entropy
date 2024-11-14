using Godot;
using System;

[GlobalClass]
public partial class WeaponSound : Resource
{
    [Export] public Godot.Collections.Array<Sound> sound = new Godot.Collections.Array<Sound>();
}
