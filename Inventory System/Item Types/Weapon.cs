using Godot;
using System;

[GlobalClass]
public partial class Weapon : Item
{
    public enum WeaponType
    {
        meele,
        ranged,
    }

    [ExportCategory("Info")]
    [Export] public WeaponType weaponType;

    [ExportCategory("Gamepay Settings")]
    //[Export] public PackedScene projectile;
    [Export] public AttackInfo attackInfo;
    [Export] public Godot.Collections.Array<AttackInfo> attackData = new Godot.Collections.Array<AttackInfo>();

    [Export] public StatModifier attackSpeedMod;
    [Export] public StatModifier strafeSpeedMod;
    
    [Export] public bool hold;

    [Export] public bool useCharge = true;
    [Export] public string chargeItemName;
    [Export] public int maxCharge;
    [Export] public int chargePerAttack = 1;

    [Export] public float reloadTime;
    [Export] public StatModifier reloadSpeedMod;

    [Export] public float drawTime;

    [ExportCategory("Ranged settings")]
    [Export] public PackedScene projectile;
    [Export] public float range;
    [Export] public float projectileSpeed;

    [ExportCategory("Meele settings")]
    [Export] public PackedScene hitbox;
    [Export] public Shape3D hitboxShape;
    [Export] public Vector3 hitboxDisplacement;


    [ExportCategory("Visual Settings")]
    [Export] public Mesh mesh;
    [Export] public WeaponAnimations animations;
    [Export] public Vector3 rotation;
    [Export] public Vector3 position;
    [Export] public Vector3 tipPosition;

    [Export] public Vector3 holsteredRotation;
    [Export] public Vector3 holsteredPosition;

    [Export] public Vector3 triggerPosition;
    [Export] public Vector3 triggerRotation;
    [Export] public Vector3 scale;

    [ExportCategory("Audio Settings")]
    [Export] public WeaponSound weaponSound;

    [ExportCategory("IK")]
    [Export] public Vector3 leftArm_target_rotation;
    [Export] public Vector3 leftArm_target_position;
    [Export] public bool useLeftArmIK = true;
    
    [ExportCategory("Recoil")]
    [Export] public float kickback;
    [Export] public float kickup;
    [Export] public float recoilSpeed;
    [Export] public Curve recoilCurve;

    [ExportCategory("NPC variables")]
    [Export] public int attackCombo;
    [Export] public float attackCooldown;
    [Export] public float idealDistanceMAX;
    [Export] public float idealDistanceMIN;
    //[Export] public bool delayUse;
    [Export] public float delayUseTime;
}
