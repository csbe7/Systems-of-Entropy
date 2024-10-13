using Godot;
using System;

[Tool]
public partial class MeshSelection : Node3D
{
    
    bool apply;
    [Export] bool Apply{
        get{
            return apply;
        }
        set{
           ApplyMeshes();
           apply = false;
        }
    }

    [Export] public PackedScene Joints;
    [Export] public PackedScene Surface;
    

    PhysicalBoneSimulator3D ragdollSimulator;
    public override void _Ready()
    {
        if (!Engine.IsEditorHint())
        {
            ragdollSimulator = GetNode<PhysicalBoneSimulator3D>("%PhysicalBoneSimulator3D");
            ragdollSimulator.PhysicalBonesStartSimulation();
        }
        
        ApplyMeshes();
    }

    public void ApplyMeshes()
    {
        if (!IsInstanceValid(Joints) || !IsInstanceValid(Surface))
        {
            return;
        }

        Skeleton3D GeneralSkeleton = GetNode<Skeleton3D>("%GeneralSkeleton");
        foreach (Node child in GeneralSkeleton.GetChildren())
        {
            if (child is MeshInstance3D)
            {
                child.QueueFree();
            }
        }
        
        var joints = Joints.Instantiate<MeshInstance3D>();
        var surface = Surface.Instantiate<MeshInstance3D>();

        GeneralSkeleton.AddChild(joints);
        GeneralSkeleton.AddChild(surface);

        GeneralSkeleton.MoveChild(joints, 0);
        GeneralSkeleton.MoveChild(surface, 1);
    }

    [Signal] public delegate void WeaponPlacedEventHandler();
    [Signal] public delegate void WeaponHeldEventHandler();
    [Signal] public delegate void WeaponHolsteredEventHandler();

    [Signal] public delegate void AttackStartedEventHandler();
    [Signal] public delegate void AttackEndedEventHandler();

    [Signal] public delegate void HitStartedEventHandler();
    [Signal] public delegate void HitEndedEventHandler();

    [Signal] public delegate void RecoveryStartedEventHandler();
    //[Signal] public
    public void EmitWeaponPlaced()
    {
        EmitSignal(SignalName.WeaponPlaced);
    }
    public void EmitWeaponHeld()
    {
        EmitSignal(SignalName.WeaponHeld);
    }
    public void EmitWeaponHolstered()
    {
        EmitSignal(SignalName.WeaponHolstered);
    }

    public void EmitAttackStarted()
    {
        EmitSignal(SignalName.AttackStarted);
    }

    public void EmitAttackEnded()
    {
        EmitSignal(SignalName.AttackEnded);
    }
    
    public void EmitHitStarted()
    {
        EmitSignal(SignalName.HitStarted);
    }

    public void EmitHitEnded()
    {
        EmitSignal(SignalName.HitEnded);
    }

    public void EmitRecoveryStarted()
    {
        EmitSignal(SignalName.RecoveryStarted);
    }
}
