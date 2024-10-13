using Godot;
using System;

public partial class BulletManager : Node
{
    static int starting_bullets = 20;
    static int add_amount = 5;
    static Godot.Collections.Array<Projectile_bullet> bulletPool = new Godot.Collections.Array<Projectile_bullet>();

    public override void _Ready()
    {
        AddBullets(starting_bullets);
    }


    static Projectile_bullet RetrieveBullet()
    {
        var bullet = bulletPool[0];
        bulletPool.RemoveAt(0);
        return bullet;
    }

    static void InsertBullet(Projectile_bullet bullet)
    {
        
    }

    void AddBullets(int amount)
    {
        for(int i = 0; i < amount; i++)
        {
            Projectile_bullet bullet = new Projectile_bullet();
            bulletPool.Add(bullet);
        }
    }
}
