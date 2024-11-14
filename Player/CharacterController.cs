using Godot;
using System;
using System.Runtime.InteropServices;



public partial class CharacterController : Node
{
    public Game game;
    
    [Export] string debug;
    public CharacterSheet sheet;
    public WeaponManager wm;
    public AnimationController ac;
    public SoundEmitter se;

    public RandomNumberGenerator randomizer = new RandomNumberGenerator();

    [ExportCategory("Physics")]
    Vector3 lastVelocity;
    [Export] public float friction = 30;
    [Export] public float acceleration = 40;
    [Export] public float sprintRotationSpeed = 10;
    
    [Export] public float dashSpeed = 20;

    [Export] public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * 6;
    
    [ExportCategory("State variables")]
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
    public bool isRecovering;
    public bool isStunned;

    public bool canAttack;
    public bool canDrawWeapon;

    public int moveInputBlockers = 0;

    [ExportCategory("Stealth")]
    [Export] public float standingVisibilityModifier = 1;
    [Export] public float crouchingVisibilityModifier = 0.8f;
    [Export] public float stillVisibilityModifier = 1f;
    [Export] public float movingVisibilityModifier = 1f;

    [Export] public float crouchSoundReduction = 2;
    
    [ExportCategory("Sound")]
    [Export] Sound stepSound;
    [Export] public float baseStepTime = 0.1f;
    public float stepTime;
    ScaledTimer stepTimer = new ScaledTimer();

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
        se = GetNode<SoundEmitter>("%AudioStreamPlayer3D");

        game.TimescaleChanged += OnTimescaleChanged;

        sprintRecoveryTimer = new ScaledTimer();
        sheet.AddTimer(sprintRecoveryTimer);

        dashStamBlockTimer = new ScaledTimer();
        dashStamBlockTimer.Timeout += EndDashStaminaBlock;
        sheet.AddTimer(dashStamBlockTimer);

        sheet.AddTimer(stepTimer);
        stepTimer.countdown = -1;
        stepTime = baseStepTime;

        randomizer.Randomize();

        sheet.Attacked += StartHitStun;

        sheet.GetStat("Speed").ModifierAdded += OnSpeedModified;
        sheet.GetStat("Speed").ModifierRemoved += OnSpeedModified;
        
        forwardDir = sheet.GlobalBasis.Z;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("Debug"))
        {
            if  (sheet.name == "Player") EnvironmentQuery.MakeGrid(sheet.GlobalPosition, sheet);
        }
        
        sheet.visibility = 1;
        if (sheet.Velocity.Length() <= 0.1f) sheet.visibility *= stillVisibilityModifier;
        else sheet.visibility *= movingVisibilityModifier;

        if (isCrouching) sheet.visibility *= crouchingVisibilityModifier;
        else sheet.visibility *= standingVisibilityModifier;
        


        float ts = game.Timescale * sheet.localTimescale;
        float D = ts * (float)delta;

        canAttack = !wm.reloading && !isDashing && !isSprinting && !isAttacking && !isStunned;
        canDrawWeapon = !drawingWeapon && !holsteringWeapon && !isSprinting && !isStunned;
        
        
        
        if (sprintRecoveryTimer.countdown > 0) sprintRecoveryTimer.SetTimescale(ts);
    

        if (isStunned || isAttacking) moveDir = Vector3.Zero;
        //STEP SOUND
        var surface = Game.Raycast(sheet, sheet.GlobalPosition, new Vector3(sheet.GlobalPosition.X, sheet.GlobalPosition.Y-1, sheet.GlobalPosition.Z), Game.GetBitMask(Game.inanimate_layers));
        if (surface.Count > 0) onFloor = true;
        else onFloor = false;
        
        if (sheet.Velocity.Length() > 0.1f && moveDir.Length() > 0.1f && onFloor)
        {
            if (stepTimer.countdown <= 0)
            {
                SurfaceData sdata = SurfaceManager.GetSurfaceData((string)((Node3D)surface["collider"]).GetMeta("SurfaceName"));

                if (sdata != null)
                {
                    randomizer.Randomize();
                    Sound s = (Sound)sdata.stepSound.Duplicate();
                    s.volumeDb *= sdata.volumeMultipler; 
                    if (isCrouching)
                    {
                        s.maxHearingDistance /= crouchSoundReduction;
                        s.volumeDb -= 10;
                    } 
                    se.Play(s, randomizer.RandfRange(sdata.pitchScale - 0.2f, sdata.pitchScale + 0.2f), false);
                    stepTimer.Start(stepTime);
                }

            }
        }
        else
        {
            stepTimer.countdown = -1;
        }

        Move(ts, D);
        Friction(D);
        
        sheet.MoveAndSlide();
    }
    

    void Move(float timescale, float delta)
    {
        Vector3 velocity = sheet.Velocity;

        if (isRecovering && moveDir != Vector3.Zero) InterruptRecovery();

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
        //else if (isAttacking) return speed; 
        else return (speed.Normalized() * sheet.GetStatValue("Speed", true));
    }

    void Friction(float delta)
    {
        if (moveDir == Vector3.Zero) 
        {
            Vector3 velocity = sheet.Velocity;
            if (isAttacking) velocity -= velocity.Normalized() * Mathf.Min(velocity.Length(), (friction/5f) * delta); 
            else velocity -= velocity.Normalized() * Mathf.Min(velocity.Length(), friction * delta); 
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

        if (isRecovering) InterruptRecovery();
 
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
        if (isRecovering) InterruptRecovery();
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
        
        if (isRecovering) InterruptRecovery();

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
        EmitSignal(SignalName.StateChanged);
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
        EmitSignal(SignalName.StateChanged);
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
        isStunned = true;
        ac.StartHitReaction(attack);

        if (isSprinting) EndSprint();
        sheet.Velocity = Vector3.Zero;
        if (isRecovering) InterruptRecovery();
    }

    public void EndHitStun()
    {
        if (!isStunned) return;
        isStunned = false;
    }

    public void InterruptRecovery()
    {
        if (!isRecovering) return;
        isRecovering = false;
        ac.AbortAttack(1);
    }

    void OnSpeedModified(StatModifier mod)
    {
        if (mod.mode == StatModifier.Mode.PercentageFromBase)
        {
            if (sheet.GetStatValue("Speed") == 0)
            {
                stepTimer.count = false;
                return;
            }
            else stepTimer.count = true;

            //stepTimer.countdown -= (stepTimer.countdown/100f)*mod.value;
            float percent = sheet.GetStatValue("Speed", false)/sheet.GetStatValue("Speed", true);
            stepTime = baseStepTime * percent;
        }
    }
}
