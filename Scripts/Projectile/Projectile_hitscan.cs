using Godot;
using System;
//using System.Numerics;

public partial class Projectile_hitscan : Projectile
{
    public override void _Ready()
    {
        CollisionObject3D shooterBody = shooter.GetParent<CollisionObject3D>();
        direction = new Vector3(direction.X, 0, direction.Z);
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(shooterBody.GlobalPosition + (Vector3.Up * 1.36f), shooterBody.GlobalPosition + (direction.Normalized() * distance), 1);
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
       
        if (result.Count > 0)
        {
            
            if (((Node)result["collider"]) == shooter)
            {
                GD.Print("HIT SELF");
                QueueFree();
                return;
            }
            
            if ((Node3D)result["collider"] is CharacterSheet sheet)
            {
                AttackInfo attack = (AttackInfo)weapon.attackInfo.Duplicate();
                attack.knockbackDir = direction.Normalized();

                sheet.TakeAttack(attack);
                //shooter.wm.OnWeaponHit(cs);
            }
        }
        QueueFree();
    }
}
