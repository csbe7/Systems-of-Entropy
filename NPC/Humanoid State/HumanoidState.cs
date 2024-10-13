using Godot;
using System;

public partial class HumanoidState : Resource
{
   public Vector3 forwardDir;

    public bool onFloor;
    public bool holdingWeapon;
    public bool drawingWeapon;
    public bool isCrouching;
    public bool isSprinting;
    public bool isDashing;
    public bool isStrafing;
    public bool isAttacking;

    public bool canAttack;

    public int moveInputBlockers = 0;

    ScaledTimer sprintRecoveryTimer;
    ScaledTimer dashRecoveryTimer;
    ScaledTimer dashStamBlockTimer;
}
