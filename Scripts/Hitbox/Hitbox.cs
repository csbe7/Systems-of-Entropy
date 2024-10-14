using Godot;
using System;

public partial class Hitbox : Area3D
{
    
    [Export] Shape3D shape;
    public CharacterSheet attacker;
    public Weapon weapon;
    public AttackInfo hitInfo;

    public override void _Ready()
    {
        GetNode<CollisionShape3D>("CollisionShape3D").Shape = shape;

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not CharacterSheet || body == attacker) return;
        
        CharacterSheet sheet = (CharacterSheet)body;

        Godot.Collections.Array<Rid> exclude = new Godot.Collections.Array<Rid>();
        exclude.Add(sheet.GetRid()); 
        exclude.Add(attacker.GetRid());
        //if (Game.Raycast(attacker, attacker.GetCenterPosition(), sheet.GetCenterPosition(), Game.GetBitMask(Game.world_layers)).Count > 0) return;
        AttackInfo attack = (AttackInfo)weapon.attackInfo.Duplicate();

        Vector3 dir = (sheet.GlobalPosition - attacker.GlobalPosition).Normalized();
        float angle = dir.AngleTo(Vector3.Forward);
        attack.knockbackDir = attack.knockbackDir.Rotated(Vector3.Up, angle);
        angle = dir.AngleTo(Vector3.Up);
        attack.knockbackDir = attack.knockbackDir.Rotated(Vector3.Right, angle);

        attack.attacker = attacker;

        sheet.TakeAttack(attack);

    }

}
