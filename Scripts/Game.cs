using Godot;
using System;


[GlobalClass]
public partial class Game : Node
{
    public static uint[] world_layers = {1, 2, 5};
    public static uint[] inanimate_layers = {1, 5};
    public static uint[] entity_layers = {2};

    public enum GameState
    {
        menu,
        gameplay,
    }
    public GameState state;

    public float Timescale = 1;
    private float lastTimescale = 1;
    [Signal] public delegate void TimescaleChangedEventHandler(float oldTS, float newTS);
    public void SetState(GameState s)
    {
        switch(s)
        {
            case GameState.menu:
            SetTimescale(0);
            break;

            case GameState.gameplay:
            SetTimescale(lastTimescale);
            break;
        }
    }
    
    public void SetTimescale(float newTS)
    {
        lastTimescale = Timescale;
        Timescale = newTS;

        AudioServer.PlaybackSpeedScale = (float)Mathf.Clamp(Timescale, 0.0001, 100);
        
        EmitSignal(SignalName.TimescaleChanged, lastTimescale, Timescale);
    }

    [ExportCategory("Gameplay Settings")]
    public bool aimAssist = true;
    public float aimAssistRadius = 1f;

    

    //functions
    public static uint GetBitMask(uint[] layers)
    {
        uint bitMask = 0;
        foreach (uint layer in layers)
        {
            bitMask += (uint)Mathf.Pow(2, layer-1);
        }
        return bitMask;
    }

    
    public static void NodeRaycast(RayCast3D raycast, Vector3 from, Vector3 to, uint mask , Godot.Collections.Array<Rid> Exclude = null)
    {
        raycast.GlobalPosition = from;
        raycast.TargetPosition = (to - from);
        raycast.CollisionMask = mask;
        raycast.ForceUpdateTransform();
        if (Exclude != null)
        {
            foreach(Rid exception in Exclude)
            {
               raycast.AddExceptionRid(exception);
            }
        }

        raycast.ForceRaycastUpdate();
    }

    public static Godot.Collections.Dictionary Raycast(Node3D worldNode, Vector3 startPos, Vector3 endPos, uint mask, Godot.Collections.Array<Rid> Exclude = null)
    {
        var spaceState = worldNode.GetWorld3D().DirectSpaceState;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(startPos, endPos, mask);
        if (Exclude != null) query.Exclude = Exclude;
        Godot.Collections.Dictionary result = spaceState.IntersectRay(query);
        return result;

    }
    
    public static Godot.Collections.Array<Godot.Collections.Dictionary> Shapecast(Node3D worldNode, Shape3D shape, Vector3 position, uint mask, Godot.Collections.Array<Rid> Exclude = null)
    {
        var spaceState = worldNode.GetWorld3D().DirectSpaceState;
        PhysicsShapeQueryParameters3D query = new PhysicsShapeQueryParameters3D();
        query.Shape = shape;
        Transform3D queryTransform = query.Transform;
        queryTransform.Origin = position;
        query.Transform = queryTransform;
        query.CollisionMask = mask;
        query.Exclude = Exclude;

        Godot.Collections.Array<Godot.Collections.Dictionary> results = spaceState.IntersectShape(query);
        return results;
    }

    public static float GetPercentage(float origin, float percentage)
    {
        float r;
        r = (origin/100) * percentage;
        return r;
    }

    public static float MapValue(float val, float minVal, float maxVal, float mapRangeMin = 0, float mapRangeMax = 1, bool clamp = false)
    {
        float map = mapRangeMin + (((val-minVal)/(maxVal-minVal)) * (mapRangeMax - mapRangeMin));
        if (clamp) map = Mathf.Clamp(map, mapRangeMin, mapRangeMax);
        return map;
    }

    public static Vector3 flattenVector(Vector3 v)
    {
        return new Vector3(v.X, 0, v.Z);
    }


    public static int SolveQuadraticEquation(double a, double b, double c, out double root1, out double root2)
    {
        double discriminant = (b*b) -(4*a*c);
        if (discriminant < 0) 
        {
            root1 = 0;
            root2 = 0;
            return 0;
        }

        root1 = (-b - Mathf.Sqrt(discriminant))/(2*a);
        root2 = (-b + Mathf.Sqrt(discriminant))/(2*a);

        return discriminant > 0 ? 2 : 1;
    }

    public static int Intercept(Vector3 a, Vector3 b, Vector3 vA, double sB, out Vector3 result, out Vector3 pos)
    {
        result = Vector3.Zero;
        pos = Vector3.Zero;

        Vector3 aToB = b - a;
        double dC = aToB.Length();

        double angle1 = aToB.AngleTo(vA);

        double sA = vA.Length();
        double r = sA/sB;

        if (SolveQuadraticEquation(1 - r * r, 2 * r * dC * Mathf.Cos(angle1), -(dC * dC), out double root1, out double root2) == 0) return 0;

        double dA;
        
        if (root1 < 0 && root2 < 0) return 0;

        if (root1 < 0) dA = root2;
        else if (root2 < 0) dA = root1;
        else
        {
            dA = Mathf.Min(root1, root2);
        }
        
        double t = dA / sB;
        Vector3 c = a + vA * (float)t;
        
        pos = c;
        result = (c - b).Normalized();

        return 1; 
    }

    public static void DebugSphere(Vector3 pos, Node3D world_node)
    {
        MeshInstance3D debugMesh = new MeshInstance3D();
        var sphere = new SphereMesh();
        sphere.Radius = 0.125f;
        sphere.Height = 0.25f;
        debugMesh.Mesh = sphere; 
        world_node.GetTree().Root.GetChild<Node3D>(world_node.GetTree().Root.GetChildCount()-1).AddChild(debugMesh);
        debugMesh.GlobalPosition = pos;
    }
    
}
