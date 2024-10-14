using Godot;
using System;



public partial class CharacterController : Node
{
    public Game game;
    
    
    public CharacterSheet sheet;
    public WeaponManager wm;
    public AnimationController ac;

    [ExportCategory("Physics")]
    Vector3 lastVelocity;
    [Export] public float friction = 30;
    [Export] public float acceleration = 40;
    [Export] public float sprintRotationSpeed = 10;
    
    [Export] public float dashSpeed = 20;

    [Export] public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * 6;
    
    [ExportCategory("State variables")]
    
    public HumanoidState stateVars = new HumanoidState();

    public Vector3 forwardDir;
    
    public bool onFloor;
    public bool holdingWeapon;
    public bool drawingWeapon;
    public bool holsteringWeapon;
    public bool isCrouching;
    public bool isSprinting;
    public bool isDashing;
    public bool isStrafing;
    public bool isAttacking;
    public bool isStunned;

    public bool canAttack;
    public bool canDrawWeapon;

    public int moveInputBlockers = 0;

    ScaledTimer sprintRecoveryTimer;
    ScaledTimer dashRecoveryTimer;
    ScaledTimer dashStamBlockTimer;

    
    public Vector2 inputDir;
    public Vector3 moveDir;
    

    //[Signal] public delegate void MovementStateChangedEventHandler(MovementState newState);
    [Signal] public delegate void StateChangedEventHandler();
    

    public override async void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SetProcess(true);
        SetPhysicsProcess(true);

        game = GetTree().Root.GetNode<Game>("Game");

        sheet = GetParent<CharacterSheet>();
        ac = GetNode<AnimationController>("%AnimationController");
        wm = GetNode<WeaponManager>("%WeaponManager");

        game.TimescaleChanged += OnTimescaleChanged;

        sprintRecoveryTimer = new ScaledTimer();
        sheet.AddTimer(sprintRecoveryTimer);

        dashStamBlockTimer = new ScaledTimer();
        dashStamBlockTimer.Timeout += EndDashStaminaBlock;
        sheet.AddTimer(dashStamBlockTimer);

        sheet.Attacked += StartHitStun;
        
        forwardDir = sheet.GlobalBasis.Z;
    }

    public override void _PhysicsProcess(double delta)
    {
        
        if (Input.IsActionJustPressed("Debug"))
        {
            if  (sheet.name == "Player") EnvironmentQuery.MakeGrid(sheet.GlobalPosition, sheet);
        }

        float ts = game.Timescale * sheet.localTimescale;
        float D = ts * (float)delta;

        canAttack = !wm.reloading && !isDashing && !isSprinting && !isAttacking;
        canDrawWeapon = !drawingWeapon && !holsteringWeapon && !isSprinting;
        
        if (sprintRecoveryTimer.countdown > 0) sprintRecoveryTimer.SetTimescale(ts);
        Move(ts, D);
        Friction(D);
        
        sheet.MoveAndSlide();
    }
    

    void Move(float timescale, float delta)
    {
        Vector3 velocity = sheet.Velocity;
        
        if (isDashing)
        {
            velocity = sheet.GetStatValue("Dash Speed", true) * dashDir * timescale;
            sheet.Velocity = velocity;
            return;
        }
        
        if (isSprinting)
        {
            if (sheet.GetStatValue("CurrentStamina", false) <= 0)
            {
                EndSprint();
                sprintRecoveryTimer.Start(sheet.sprintRecoveryTime);
            } 
            sheet.ChangeStamina(-sheet.sprintStamPerSec * delta);
            velocity += forwardDir * acceleration * delta;
            sheet.Velocity = CapSpeed(velocity);
            return;
        }
        
        velocity += moveDir * acceleration * delta;
        sheet.Velocity = CapSpeed(velocity);
    }
    
    Vector3 CapSpeed(Vector3 speed)
    {
        if (speed.Length() <= sheet.GetStatValue("Speed", true)) return speed;
        else return speed.Normalized() * sheet.GetStatValue("Speed", true);
    }

    void Friction(float delta)
    {
        if (moveDir == Vector3.Zero) 
        {
            Vector3 velocity = sheet.Velocity;
            velocity -= velocity.Normalized() * Mathf.Min(velocity.Length(), friction * delta); 
            sheet.Velocity = velocity;
        }
    }
    
    public void OnTimescaleChanged(float oldTS, float newTS)
    {
        if (oldTS == 0) 
        {
            sheet.Velocity = lastVelocity * newTS;
            return;
        }
        Vector3 velocity = sheet.Velocity;
        if (newTS == 0) lastVelocity = velocity;
        velocity = (velocity/oldTS) * newTS;
        sheet.Velocity = velocity;
        
    }
    //CHANGE STATE
    public void StartSprint()
    {
        if (isSprinting || wm.reloading) return;
        if (sprintRecoveryTimer.countdown > 0) return;
        if (sheet.GetStatValue("CurrentStamina", false) <= 0) return;
 
        sheet.AddStatModifier(sheet.sprintSpeedModifier, "Speed");
        sheet.staminaRegenBlockers += 1;

        if (isCrouching) EndSneak();
        isSprinting = true;
    }
    public void EndSprint()
    {
        if (!isSprinting) return;
        sheet.RemoveStatModifier(sheet.sprintSpeedModifier, "Speed");
        sheet.staminaRegenBlockers -= 1;
        isSprinting = false;
    }

    public void StartSneak()
    {
        if (isCrouching) return;
        sheet.AddStatModifier(sheet.sneakSpeedModifier, "Speed");
        isCrouching = true;
        ((CapsuleShape3D)GetNode<CollisionShape3D>("%CollisionShape3D").Shape).Height /= 2;
    }
    public void EndSneak()
    {
        if (!isCrouching) return;
        sheet.RemoveStatModifier(sheet.sneakSpeedModifier, "Speed");
        isCrouching = false;
        ((CapsuleShape3D)GetNode<CollisionShape3D>("%CollisionShape3D").Shape).Height *= 2;
    }
    
    Vector3 dashDir;
    public void StartDash(Vector3 dir)
    {
        if (isDashing || wm.reloading) return;
        if (IsInstanceValid(dashRecoveryTimer)) return;
        if (sheet.GetStatValue("CurrentStamina", false) < sheet.dashCost) return;

        sheet.ChangeStamina(-sheet.dashCost);
        
        sheet.AddInvincibility(sheet.dashTime, 0);

        EndSprint();
        dashDir = dir;
        sheet.staminaRegenBlockers += 1;
        isDashing = true;

        ScaledTimer dashTimer = new ScaledTimer();
        dashTimer.Timeout += EndDash;
        dashTimer.destroyOnTimeout = true;
        sheet.AddTimer(dashTimer);
        dashTimer.Start(sheet.dashTime);

        dashRecoveryTimer = new ScaledTimer();
        dashRecoveryTimer.Start(sheet.GetStatValue("Dash Recovery", true));
        dashRecoveryTimer.destroyOnTimeout = true;
        sheet.AddTimer(dashRecoveryTimer);


        moveInputBlockers++;
    }
    public void EndDash()
    {
        if (!isDashing) return;
        isDashing = false;
        moveInputBlockers--;
        if (dashStamBlockTimer.countdown > 0) sheet.staminaRegenBlockers--;
        dashStamBlockTimer.Start(sheet.dashStaminaRecoveryTime);
    }
    public void EndDashStaminaBlock()
    {
        sheet.staminaRegenBlockers--;
    }

    public void StartStrafe()
    {
        if (isStrafing) return;
        sheet.AddStatModifier(wm.currWeapon.strafeSpeedMod, "Speed");
        isStrafing = true;
    }
    public void EndStrafe()
    {
        if (!isStrafing) return;
        sheet.RemoveStatModifier(wm.currWeapon.strafeSpeedMod, "Speed");
        isStrafing = false;
    }
    
    public void StartDrawingWeapon()
    {
        if (holdingWeapon || !canDrawWeapon) return;
        
        moveDir = Vector3.Zero;
        drawingWeapon = true;
        holdingWeapon = true;
        ac.DrawWeapon();
    }
    public void EndDrawingWeapon()
    {
        if (!drawingWeapon) return;
        ac.humanoidMesh.WeaponHeld -= EndDrawingWeapon;
       
        drawingWeapon = false;

      
        if (wm.currWeapon.useLeftArmIK) ac.SetLeftArmIK(true);
        else ac.SetLeftArmIK(false);
    
        
    }

    public void StartHolsteringWeapon()
    {
        if (!holdingWeapon || !canDrawWeapon) return;
        moveDir = Vector3.Zero;
        holsteringWeapon = true;
        holdingWeapon = false;
        ac.HolsterWeapon();
    }
    public void EndHolsteringWeapon()
    {
        if (!holsteringWeapon) return;
        
        ac.humanoidMesh.WeaponHolstered -= EndHolsteringWeapon;
        holsteringWeapon = false;
        
        ac.humanoidMesh.WeaponHolstered -= EndHolsteringWeapon;
        ac.SetLeftArmIK(false);
    }

    public void StartHoldingWeapon()
    {
        if (holdingWeapon || !canDrawWeapon || isSprinting) return;
        
        StartDrawingWeapon();
    }
    public void EndHoldingWeapon()
    {
        if (!holdingWeapon || !canDrawWeapon || isSprinting) return;
        StartHolsteringWeapon();
    }
    
    public void StartHitStun(AttackInfo attack)
    {
        moveInputBlockers++;
        ac.StartHitReaction(attack);
    }
}
