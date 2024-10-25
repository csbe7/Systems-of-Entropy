using Godot;
using System;
using System.IO;


public partial class NPC_pathfinding : NavigationAgent3D
{
    [Export] CharacterController cc;
    
    [Export] public float baseTargetDistance;
    [Export] public float followDistance;
    [Export] public float followWaitDistance;
    public Node3D targetNode;
    public Vector3 targetPos;
    Vector3 baseDir;
    
    public uint[] entity_layers = {2};
    public uint[] static_layers= {1, 5};
    public uint[] world_layers = {1, 2, 5};


    [Signal] public delegate void TargetReachedEventHandler();

    public enum MoveState{
        still,
        wander,
        travel,
        follow,
    }
    [Export] MoveState defaultState;
    MoveState state = MoveState.still;
     
    [ExportCategory("Steering")]
    [Export] public int maxRays;
    [Export] Vector3 rayOffset;
    [Export] float rayLenght = 3;

    public RayCast3D raycastNode;
    public ShapeCast3D shapecastNode;

    [Export] int directionResolution = 16;
    public Vector3[] directions;
    float[] interest;
    public Godot.Collections.Array<Node3D> neighbours = new Godot.Collections.Array<Node3D>();
    public Godot.Collections.Dictionary<Node3D, float> toFlee = new Godot.Collections.Dictionary<Node3D, float>();
    public NPCMoveState steerType;


    public override async void _Ready()
    {
        SetPhysicsProcess(false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SetPhysicsProcess(true);
        
        raycastNode = GetNode<RayCast3D>("%RayCast3D");
        raycastNode.CollisionMask = Game.GetBitMask(world_layers);

        shapecastNode = GetNode<ShapeCast3D>("%ShapeCast3D");
        shapecastNode.CollisionMask = Game.GetBitMask(world_layers);
        
        directions = new Vector3[directionResolution];
        interest = new float[directionResolution];
        
        for(int i = 0; i < directionResolution; i++)
        {
           Vector3 vec = Vector3.Right.Rotated(Vector3.Up, (2*Mathf.Pi*i)/(directionResolution));
           //GD.Print(vec);
           directions[i] = vec;
           interest[i] = 0;
        } 
        
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (state)
        {
            case MoveState.still:
            Still();
            break;

            case MoveState.travel:
            Travel();
            break;

            case MoveState.follow:
            Follow();
            break;
        }
    }
    
    public void SetStateStill()
    {
        state = MoveState.still;
    }

    void Still()
    {
        cc.moveDir = Vector3.Zero;
        cc.sheet.Velocity = Vector3.Zero;
    }


    public void SetStateTravel(Vector3 target, float desiredDistance = 0f)
    {
        if (desiredDistance == 0)
        {
            TargetDesiredDistance = baseTargetDistance;
        }
        else TargetDesiredDistance = baseTargetDistance;
        TargetPosition = target;
        state = MoveState.travel;
    }

    void Travel()
    {
        if (cc.sheet.GlobalPosition.DistanceTo(TargetPosition) > TargetDesiredDistance)
        {
            Vector3 point = NavigationServer3D.MapGetClosestPoint(GetNavigationMap(), cc.sheet.GlobalPosition);
            if (!Mathf.IsZeroApprox(point.DistanceTo(cc.sheet.GlobalPosition)))
            {
                Vector3 direction = Game.flattenVector(point - cc.sheet.GlobalPosition).Normalized();
                Move(direction);
            }
            Vector3 nextPos = GetNextPathPosition();
            Vector3 dir = Game.flattenVector(nextPos - cc.sheet.GlobalPosition).Normalized();
            Move(dir);
        }
        else
        {
            state = defaultState;
            EmitSignal(SignalName.TargetReached);
        }
    }


    public void SetStateFollow(Node3D target)
    {
        targetNode = target;
        state = MoveState.follow;
    }

    void Follow()
    {
        /*TargetPosition = targetNode.GlobalPosition;
        float distance = cc.body.GlobalPosition.DistanceTo(TargetPosition);
        if (distance <= followDistance + followWaitDistance && distance > followDistance && HasLineOfSight(targetNode)) return;
        if (distance <= followDistance) 
        {
            cc.moveDir = Vector3.Zero;
            return;
        }
        
        Vector3 nextPos = GetNextPathPosition();


        Vector3 moveDir = (nextPos - cc.body.GlobalPosition).Normalized();
        Velocity = moveDir * cc.sheet.GetStatValue("Speed", true);
        moveDir = Velocity;
        
        moveDir = Game.flattenVector(moveDir).Normalized();
        moveDir = Steer(moveDir);
        cc.moveDir = moveDir;*/
    }

     


    void Move(Vector3 dir)
    {
        ClearWeights();
        CalculateCollisionAvoidance(steerType.obstacleAvoidanceDistance);
        CalculateFollow(dir, steerType.followWeight);
        CalculateNeighbourAvoidance(neighbours, 3, steerType.neighbourAvoidanceWeight);
        CalculateFlee(steerType.fleeWeight);
        CalculateStrafe(dir, steerType.strafeWeight);
        //CalculateLineOfSight(steerType.lineOfSightDistance, targetNode.GlobalPosition, steerType.lineOfSightWeight);

        if (cc.sheet.Velocity != Vector3.Zero) CalculateFollow(cc.sheet.Velocity.Normalized(), 0.1f);
        
        cc.moveDir = cc.moveDir.Lerp(CalculateBestDir(), 0.5f);
    }


    void ClearWeights()
    {
        for(int i = 0; i < directionResolution; i++)
        {
            interest[i] = 0;
        }
    }
    

    void CalculateFollow(Vector3 targetDir, float weight = 1)
    {
        if (weight <= 0) return;

        for (int i = 0; i < directionResolution; i++)
        {
            if (interest[i] == -Mathf.Inf) continue;
            
            targetDir = Game.flattenVector(targetDir).Normalized();
            float dot = targetDir.Dot(directions[i]);
            dot = (dot + 1)/2;
            interest[i] += dot * weight;
        }
    }


    void CalculateFlee(float weight = 1)
    {
        if (weight == 0 || toFlee.Count == 0) return;
        Vector3 fleeDir = Vector3.Zero;
        foreach(var danger in toFlee.Keys)
        {
            Vector3 displacement = danger.GlobalPosition - cc.sheet.GlobalPosition;
            float distance = displacement.Length();
            
            fleeDir -= displacement.Normalized() * (1/distance) * toFlee[danger];
        }
        
        CalculateFollow(fleeDir.Normalized(), weight);
    }


    void CalculateCollisionAvoidance(float distance)
    {
        for (int i = 0; i < directionResolution; i++)
        {
            if (cc.sheet.TestMove(cc.sheet.Transform, directions[i] * distance))
            {
                interest[i] = -Mathf.Inf;
            }
        }
    }


    void CalculateNeighbourAvoidance(Godot.Collections.Array<Node3D> neighbours, float avoidRadius, float weight = 1)
    {
        if (neighbours.Count == 0 || weight <= 0)  return;
        
        Vector3 avoidance = Vector3.Zero;

        foreach (var neighbour in neighbours)
        {
            Vector3 displacement = neighbour.GlobalPosition - cc.sheet.GlobalPosition;
            float distance = displacement.Length();

            if (distance <= avoidRadius)
            {
                if (neighbour == cc.sheet) continue;
                if (Mathf.IsZeroApprox(distance))
                {
                    cc.sheet.randomizer.Randomize();
                    float angle = cc.sheet.randomizer.RandfRange(-Mathf.Pi, Mathf.Pi);
                    avoidance += Vector3.Right.Rotated(Vector3.Up, angle);
                }
                else
                {
                    float map = 1 - (distance/avoidRadius);
                    avoidance = Game.flattenVector(displacement).Normalized() * map;
                }
            }
        }

        if (avoidance != Vector3.Zero)
        {
            CalculateFollow(avoidance, weight);
        }
    }


    void CalculateStrafe(Vector3 targetDir, float weight = 1)
    {
        if (weight <= 0) return;

        for (int i = 0; i < directionResolution; i++)
        {
            float dot = targetDir.Dot(directions[i]);
            float mod = 1 - (float)Mathf.Pow(Mathf.Abs(dot + 0.25), 2);

            dot = (dot + 1)/2;
            interest[i] += dot * mod * weight;
        }
    }
    
    
    /*void CalculateLineOfSight(float distance, Vector3 target, float weight = 1)
    {
        if (weight <= 0 || distance <= 0) return;

        for (int i = 0; i < directionResolution; i++)
        {
            if (interest[i] == -Mathf.Inf) continue;

            Vector3 pos = cc.sheet.GetCenterPosition(cc.wm);
            pos += directions[i] * distance;
            if (!HasLineOfSight(pos, target, true, true))
            {
               interest[i] += weight;
            }
        }
    }*/


    Vector3 CalculateBestDir()
    {
        int highestIndex = 0;
        for (int i = 1; i < directionResolution; i++)
        {
            if (interest[i] > interest[highestIndex]) highestIndex = i;
        }
        return directions[highestIndex];
    }
    
    
   
    int GetIndex(int index)
    {
        if (index >= directionResolution) index = 0;
        else if (index < 0) index = directionResolution + index;

        return index;
    }


    public bool HasLineOfSight(Vector3 from, Node3D to, bool hitBackFaces = false, bool hitFromInside = false)
    {
        //GD.Print(raycastNode);
        raycastNode.GlobalPosition = from;
        raycastNode.TargetPosition = (to.GlobalPosition - from);
        bool prev = raycastNode.HitBackFaces;
        bool prev1 = raycastNode.HitFromInside;
        raycastNode.HitBackFaces = hitBackFaces;
        raycastNode.HitFromInside = hitFromInside;
        raycastNode.ForceRaycastUpdate();
        raycastNode.HitBackFaces = prev;
        raycastNode.HitFromInside = prev1;

        if (!raycastNode.IsColliding() || raycastNode.GetCollider() == to) return true;

        return false;
    }

}
