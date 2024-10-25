using Godot;
using System;
using System.IO;

public partial class FactionManager : Node
{
    public Godot.Collections.Array<Faction> Factions = new Godot.Collections.Array<Faction>();

    public override void _Ready()
    {
        var directory = DirAccess.Open("res://Scripts/RPG System/Faction System/Factions");

        string[] files = directory.GetFiles();

        foreach(string file in files)
        {
            Factions.Add((Faction)ResourceLoader.Load("res://Scripts/RPG System/Faction System/Factions/"+file));
        }

        foreach(Faction faction in Factions)
        {
            //GD.Print(faction.name);
        }
    }

    public Faction GetFaction(string name)
    {
        foreach(Faction faction in Factions)
        {
            if (faction.name == name) return faction;
        }

        return null;
    }

}
