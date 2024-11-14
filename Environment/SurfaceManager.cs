using Godot;
using System;

public partial class SurfaceManager : Node
{
    public static SurfaceCollection collection;

    public override void _Ready()
    {
        collection = (SurfaceCollection)ResourceLoader.Load("res://Environment/SurfaceDictionary.tres");
    }

    public static SurfaceData GetSurfaceData(string surfaceName)
    {
        return collection.dictionary[surfaceName];
    }
}
