using Godot;
using System;

[GlobalClass]
public partial class DialogTransitionData : Resource
{
    public string fromNode;
    public int fromPort; 
    public string toNode; 
    public int toPort;
}
