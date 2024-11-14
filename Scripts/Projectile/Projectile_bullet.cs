using Godot;
using System;

public partial class Projectile_bullet : Projectile
{
    [Export] Area3D area;
    
    Game game;

    public override void _Ready()
    {
        game = GetTree().Root.GetNode<Game>("Game");
        direction = new Vector3(direction.X, 0, direction.Z).Normalized();
        LookAt(direction + GlobalPosition);
        
        area.BodyEntered += OnBodyEntered;

        ScaledTimer t = new ScaledTimer();
        AddChild(t);
        t.Start(duration); 
        t.destroyOnTimeout = true;
        t.Timeout += QueueFree;        
    }

    public override void _Process(double delta)
    {
        float Delta = (float)delta * game.Timescale;
        GlobalPosition += direction * speed * Delta;
    }

    void OnBodyEntered(Node3D body)
    {
        if (body == shooter)
        {
            return;
        }
        
        QueueFree();

        if (body is CharacterSheet sheet)
        {
            AttackInfo attack = (AttackInfo)weapon.attackInfo.Duplicate();
            attack.knockbackDir = direction.Normalized();
            attack.attacker = shooter;
   
            sheet.TakeAttack(attack);
            //shooter.wm.OnWeaponHit(cs);
        }
    }

}
