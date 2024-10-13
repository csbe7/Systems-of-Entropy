using Godot;
using System;

[GlobalClass]
public partial class AttackInfo : Resource
{
    public CharacterSheet attacker;
    
    [Export] public float damage;
    [Export] public float knockback;
    public Vector3 knockbackDir;
    [Export] public float hitstunDuration;
    
    public enum AttackType{
        meele,
        ranged,
    }
    [Export] AttackType attackType;

    public enum DamageType{
        physical,
        magical,
    }
    [Export] public DamageType damageType;

    

    public AttackInfo(float dmg, float kb, Vector3 kbDir, float htsDur)
    {
        damage = dmg;
        knockback = kb;
        knockbackDir = kbDir;
        hitstunDuration = htsDur;
    }

    public AttackInfo()
    {
        damage = 0f;
        knockback = 0f;
        knockbackDir = Vector3.Zero;
        hitstunDuration = 0f;
    }
}
