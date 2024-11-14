using Godot;
using System;

[GlobalClass]
public partial class SurfaceCollection : Resource
{
    [Export] public Godot.Collections.Dictionary<string, SurfaceData> dictionary = new Godot.Collections.Dictionary<string, SurfaceData>();
}
