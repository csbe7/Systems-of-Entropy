using Godot;
using System;

[Tool]
[GlobalClass]
public partial class Faction : Resource
{
    [Export] public string name;

    [Export] public float baseRelationship = 0;


    [Export] public Godot.Collections.Dictionary<string, float> relationships = new Godot.Collections.Dictionary<string, float>();
    
    

    public float GetRelationship(Faction otherFaction)
    {
        if (otherFaction.name == name) return 100;

        if (relationships.ContainsKey(otherFaction.name)) return relationships[otherFaction.name];
        else 
        {
            return baseRelationship; 
        }
    }
}
