using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;



public partial class EnvironmentQuery : Node
{
    public struct EnvironmentPoint
    {
        public Vector3 point;
        public bool valid;// = true;

        public EnvironmentPoint(Vector3 p)
        {
            point = p;
            valid = true;
        }
    }

    static float gridLenght = 100;
    static float gridWidth = 100;
    static float gridHeight = 10;
    static float gridStep = 2f; 

    static uint[] layers = {1};
    static uint[] obstacle_layers = {5};

    struct Grid
    {
        public List<EnvironmentPoint> points = new();
        public Vector3 center;

        public Grid()
        {
            points = new();
            center = Vector3.Zero;
        }
    }

    static Grid grid = new();
    
    
    static void ChainRaycast(RayCast3D raycast, ShapeCast3D shapecast, Vector3 from, Vector3 to)
    {
        raycast.GlobalPosition = from;
        raycast.TargetPosition = Vector3.Down * gridHeight;
        raycast.ForceRaycastUpdate();
        
        while(raycast.IsColliding())
        {
            Vector3 cPoint = raycast.GetCollisionPoint();
            if (raycast.GetCollisionNormal().Dot(Vector3.Up) >= 0.45f)
            {
                if (from.DistanceSquaredTo(cPoint) > 1)
                {
                    shapecast.GlobalPosition = cPoint;
                    shapecast.ForceShapecastUpdate();
                    
                    if (!shapecast.IsColliding())
                    {
                        EnvironmentPoint point = new EnvironmentPoint(raycast.GetCollisionPoint());
                        grid.points.Add(point);
                    }
                }
            }
            float d = cPoint.DistanceTo(to);
            if (d > 1)
            {
                raycast.GlobalPosition = new Vector3(cPoint.X, cPoint.Y - 0.01f, cPoint.Z);
                raycast.TargetPosition = Vector3.Down * (d);
                raycast.ForceRaycastUpdate();
            }
        }

        
    }

    public static void MakeGrid(Vector3 center, Node3D worldNode)
    {
        grid.points.Clear();

        grid.center = center;
        
        ShapeCast3D shapecast = new ShapeCast3D();
        RayCast3D raycast = new RayCast3D();

        BoxShape3D shape = new();
        shape.Size = new Vector3(1, 1, 1);

        uint layerMask = Game.GetBitMask(layers);

        shapecast.Shape = shape;
        shapecast.CollisionMask = Game.GetBitMask(obstacle_layers);
        worldNode.AddChild(shapecast);

        raycast.CollisionMask = layerMask;
        raycast.HitBackFaces = false;
        worldNode.AddChild(raycast);


        Vector3 startPos = center - new Vector3(gridLenght/2, 0, gridWidth/2);
        Vector3 finishPos = center + new Vector3(gridLenght/2, 0, gridWidth/2);
        Vector3 pointPos = startPos;

        var wait_signal = worldNode.ToSignal(worldNode.GetTree(), SceneTree.SignalName.ProcessFrame);


        for (; pointPos.Z <= finishPos.Z; pointPos.Z += gridStep)
        {
            for (; pointPos.X <= finishPos.X; pointPos.X += gridStep)
            {
                Vector3 rayStart = new Vector3(pointPos.X, center.Y + gridHeight/2, pointPos.Z);
                Vector3 rayEnd = new Vector3(pointPos.X, center.Y - gridHeight/2, pointPos.Z);
                ChainRaycast(raycast, shapecast, rayStart, rayEnd);
            }
            pointPos.X = startPos.X;
            //await wait_signal;
        }

        shapecast.QueueFree();
        raycast.QueueFree();
    

        /*int j = 1;
        foreach(var environmentPoint in grid.points)
        {
            
            MeshInstance3D debugMesh = new MeshInstance3D();
            var sphere = new SphereMesh();
            sphere.Radius = 0.125f;
            sphere.Height = 0.25f;
            debugMesh.Mesh = sphere;

            //GD.Print(environmentPoint.point);

            worldNode.GetParent().AddChild(debugMesh);
            
            debugMesh.GlobalPosition = environmentPoint.point;

            j++;
        
        }*/
        GD.Print(grid.points.Count);
    }

    
    public async static Task<EnvironmentPoint> FindPointWithLineOfSight(Vector3 pos, NPC_AI npc)
    {
        double maxScore = 0;
        EnvironmentPoint calcPoint = new EnvironmentPoint(Vector3.Zero);
        calcPoint.valid = false;
        uint layerMask = Game.GetBitMask(obstacle_layers);
        RayCast3D raycast = new RayCast3D();
        raycast.CollisionMask = layerMask;
        raycast.HitBackFaces = false;
        npc.sheet.AddChild(raycast);
        
        var wait_signal = npc.sheet.ToSignal(npc.sheet.GetTree(), SceneTree.SignalName.ProcessFrame);

        Vector3 oldPos = npc.pf.TargetPosition;
        int p = 0;
        int threshold = 10;

        foreach(EnvironmentPoint point in grid.points)
        {
            Vector3 startPos = new Vector3(point.point.X, point.point.Y + npc.wm.weaponTip.GlobalPosition.Y - npc.sheet.GlobalPosition.Y, point.point.Z);
            raycast.GlobalPosition = startPos;
            raycast.TargetPosition = (pos - startPos);
            raycast.ForceRaycastUpdate();
            if (!raycast.IsColliding())
            {
                double score;

                npc.pf.TargetPosition = point.point;
                double lenght = npc.pf.DistanceToTarget();
                
                /*float d = point.point.DistanceTo(pos);
                d = Mathf.Abs(d - desiredDistance);*/

                score = (1/(1+lenght)/* + (1/(1+d) * 2)*/);
                
                if (score > maxScore)
                {
                    maxScore = score;
                    calcPoint = point;
                }

            }
            /*p++;
            if (p > threshold)
            {
                threshold += 10;
                npc.pf.TargetPosition = oldPos;
                await wait_signal;
            }*/
            
        }
        npc.pf.TargetPosition = oldPos;

        /*MeshInstance3D debugMesh = new MeshInstance3D();
        var sphere = new SphereMesh();
        sphere.Radius = 0.125f;
        sphere.Height = 0.25f;
        debugMesh.Mesh = sphere;
        npc.sheet.GetParent().AddChild(debugMesh);
        debugMesh.GlobalPosition = calcPoint.point;
        
        GD.Print(maxScore, calcPoint.point);*/
        return calcPoint;

    }
    
    public async static Task<EnvironmentPoint> FindCover(Godot.Collections.Array<CharacterSheet> toHide, NPC_AI npc)
    {
        double maxScore = 0;
        EnvironmentPoint calcPoint = new EnvironmentPoint(Vector3.Zero);
        calcPoint.valid = false;
        uint layerMask = Game.GetBitMask(obstacle_layers);
        RayCast3D raycast = new RayCast3D();
        raycast.CollisionMask = layerMask;
        raycast.HitBackFaces = false;
        npc.sheet.AddChild(raycast);
        
        var wait_signal = npc.sheet.ToSignal(npc.sheet.GetTree(), SceneTree.SignalName.ProcessFrame);

        Vector3 oldPos = npc.pf.TargetPosition;

        foreach(EnvironmentPoint point in grid.points)
        {
            double score;

            int hiddenFrom = 0;
            foreach(CharacterSheet sheet in toHide)
            {
                raycast.GlobalPosition = sheet.GetCenterPosition();
                raycast.TargetPosition = (point.point - raycast.GlobalPosition);
                raycast.ForceRaycastUpdate();
                if (raycast.IsColliding()) hiddenFrom++;  
            }

            if (hiddenFrom == 0) continue;

            npc.pf.TargetPosition = point.point;
            double lenght = npc.pf.DistanceToTarget();

            score = (1/(1+lenght) + (hiddenFrom * 10));
                
            if (score > maxScore)
            {
                maxScore = score;
                calcPoint = point;
            }

        }

        /*MeshInstance3D debugMesh = new MeshInstance3D();
        var sphere = new SphereMesh();
        sphere.Radius = 0.125f;
        sphere.Height = 0.25f;
        debugMesh.Mesh = sphere;
        npc.sheet.GetParent().AddChild(debugMesh);
        debugMesh.GlobalPosition = calcPoint.point;*/
        
        //GD.Print(maxScore, calcPoint.point);
        return calcPoint;
    }

    

    /*public static EnviromentPoint GetClosestLoS(Vector3 target, Vector3 center)
    {
        Vector3 pointPos =  center - Ve
    }*/
}
