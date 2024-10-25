using Godot;
using System;
using System.Threading.Tasks;


public partial class NPC_AI : Node
{   
    Game game;
    
    [Export] Personality personality;

    public enum State
    {
        idle,
        combat,
    }
    public State state = State.idle;

    [Signal] public delegate void StateChangedEventHandler();

    [Export] public CharacterSheet sheet;
    [Export] public CharacterController cc;
    [Export] public Inventory inventory;
    [Export] public WeaponManager wm;


    Area3D nearRange;
    //RayCast3D pf.raycastNode;

    [Export(PropertyHint.Range, "0,100,")] public float fight;
    
    [Export] public bool hostile; 
 
    [ExportCategory("Pathfinding")]
    [Export] public NPC_pathfinding pf;

    [Export] NPCMoveState chaseState;
    [Export] NPCMoveState strafeState;
    [Export] NPCMoveState fleeState;
    

    [ExportCategory("Combat")]
    CombatManager cm;
    [Export] public float attackCooldown = 1.5f;
    [Export] public int attackCombo = 1;
    [Export] StatModifier strafeSpeedMod;
    int attackCounter = 0;
    ScaledTimer attackCooldownTimer;


    public Godot.Collections.Array<CharacterSheet> allies = new Godot.Collections.Array<CharacterSheet>();
    public Godot.Collections.Array<CharacterSheet> enemies = new Godot.Collections.Array<CharacterSheet>();
    public Godot.Collections.Array<CharacterSheet> neutrals = new Godot.Collections.Array<CharacterSheet>();
    public Godot.Collections.Array<CharacterSheet> hidden = new Godot.Collections.Array<CharacterSheet>();

    public Godot.Collections.Array<AttackInfo> recentAttacks = new Godot.Collections.Array<AttackInfo>(); 

    public CharacterSheet target;
    public EnvironmentQuery.EnvironmentPoint targetPoint;    

    RandomNumberGenerator randomizer;
    
    [Export] float tickrate;
    ScaledTimer tick = new ScaledTimer(true);

 
    public enum CombatState{
        non_combat,
        chase,
        strafe,
        attack,
        hide,
        flee,
    }
    public CombatState combatState = CombatState.chase;
    [Signal] public delegate void CombatStateChangedEventHandler();

    public override async void _Ready()
    {
        cm = GetTree().Root.GetNode<CombatManager>("CombatManager");
        wm = GetNode<WeaponManager>("%WeaponManager");
        sheet = GetParent<CharacterSheet>();
        pf = GetNode<NPC_pathfinding>("%NavigationAgent3D");

        attackCooldownTimer = new ScaledTimer();
        AddChild(attackCooldownTimer);
        
        SetProcess(false);
        SetPhysicsProcess(false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SetProcess(true);
        SetPhysicsProcess(true);


        randomizer = new RandomNumberGenerator();

        nearRange = GetNode<Area3D>("%Near Range");
        nearRange.BodyEntered += OnEntityEntered;
        foreach(var body in nearRange.GetOverlappingBodies())
        {
            OnEntityEntered(body);
        }
        
        
        sheet.Attacked += OnAttacked;
        
        sheet.AddTimer(attackChargeTimer);
        attackChargeTimer.Timeout += StartAttack;

        tick.Timeout += OnTick;
        AddChild(tick);
        tick.Start(tickrate);
    }

    public override void _PhysicsProcess(double delta)
    {
        GetForwardDir((float)delta);
        HandleMoveState();

        switch(state)
        {
            case State.idle:
            break;

            case State.combat:
            Combat();
            break;
        }
    }


    public void OnTick()
    {
        //CHECK IF HIDDEN ARE VISIBLE
        foreach(CharacterSheet eSheet in hidden)
        {
            if (!CanSee(eSheet)) continue;

            float rel = sheet.faction.GetRelationship(eSheet.faction);
            if (sheet.faction == eSheet.faction) //ALLY
            {
                if (!allies.Contains(eSheet))
                {
                    allies.Add(eSheet);
                }
            }
            else if (rel < 0) //ENEMY
            {
                if (!enemies.Contains(eSheet))
                {
                    enemies.Add(eSheet);
                    pf.toFlee.Add(eSheet, 5);
                    if (hostile && state != State.combat)
                    {
                        target = eSheet;
                        StartCombat();
                    }
                }
            }
            else //NEUTRAL
            {
                if (!neutrals.Contains(eSheet))
                {
                    neutrals.Add(eSheet);
                }
            }
        }

    }
    
    float danger = 0;
    bool canSee;
    float distance = 0;
    bool canAttack;
    void StartCombat()
    {
        if (state == State.combat) return;

        state = State.combat;

        EquipWeapon();
        cc.StartHoldingWeapon();
        cc.EmitSignal(CharacterController.SignalName.StateChanged);
        
        randomizer.Randomize();
        attackCooldownTimer.Start(wm.currWeapon.attackCooldown + randomizer.RandfRange(-0.5f, 0.5f));
        
        tick.Timeout += CombatTick;

        attackChargeTimer.countdown = ATTACK_CHARGE_IDLE_VALUE;

        pf.steerType = chaseState;
        EmitSignal(SignalName.StateChanged);
        StateChanged += EndCombat;
    }
    void Combat()
    {
        cc.EmitSignal(CharacterController.SignalName.StateChanged);
        //DECIDE STATE
        
        canSee = CanSee(target);
        distance = sheet.GlobalPosition.DistanceTo(target.GlobalPosition);
        canAttack = cc.canAttack && canSee && (CanShoot() || wm.currWeapon.weaponType != Weapon.WeaponType.ranged) && (distance <= wm.currWeapon.range) && (!wm.currWeapon.useCharge || wm.currWeaponSlotData.charge >= wm.currWeapon.chargePerAttack && attackCooldownTimer.countdown <= 0);
        

        if (cc.isStunned) attackChargeTimer.countdown = -1;
        if (canAttack)
        {
            StartAttackCharge();
        }
        if (danger > personality.hideTreshold && hideCooldownTimer.countdown <= 0)
        {
            StartHide();
        }
        else if (!canSee || distance > wm.currWeapon.idealDistanceMAX)
        {
            StartChase();
        }
        else if (distance < wm.currWeapon.idealDistanceMIN)
        {
            StartFlee();
        }
        else if ((distance < wm.currWeapon.idealDistanceMAX && distance > wm.currWeapon.idealDistanceMIN))
        {
            StartStrafe();
        }

        if (wm.currWeaponSlotData.charge < wm.currWeapon.chargePerAttack && wm.currWeapon.useCharge && (danger < personality.reloadTreshold))
        {
            wm.StartReload();
        }

   
        //RESET ATTACK COUNTER
        if (attackCooldownTimer.countdown <= 0 && attackCounter >= attackCombo)
        {
            attackCounter = 0;
        }

        switch (combatState)
        {
            case CombatState.chase:
            Chase();
            break;

            case CombatState.flee:
            Flee();
            break;

            case CombatState.strafe:
            Strafe();
            break;

            case CombatState.attack:
            Attack();
            break;

            case CombatState.hide:
            Hide();
            break;
        }
    }
    void CombatTick()
    {
        danger = CalculateDanger();
        
        //choose target
        float maxPriority = 0;
        CharacterSheet newTarget = target;
        foreach(CharacterSheet enemy in enemies)
        {
            float priority = 100;
            
            if (!CanSee(enemy)) priority /= personality.sightWeight;
            if (enemy == target) priority *= personality.targetFocus;

            float d = sheet.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            float x = (wm.currWeapon.idealDistanceMIN + wm.currWeapon.idealDistanceMAX)/2;
            float e = Mathf.Abs(d - Mathf.Pow(x, 2)) * personality.distanceWeight;
            
            float h = (enemy.GetStatValue("CurrentHealth")/enemy.GetStatValue("Health")) * 100 * personality.healthWeight;
            

            priority = priority/(1 + e) + h;
            
            
            if (priority > maxPriority)
            {
                maxPriority = priority;
                newTarget = enemy;
            }
        }

        target = newTarget;
    }
    void EndCombat()
    {
        StateChanged -= EndCombat;
        tick.Timeout -= CombatTick;
        combatState = CombatState.non_combat;
        EmitSignal(SignalName.CombatStateChanged);
    }


    void StartChase()
    {
        if (combatState == CombatState.chase) return;

        combatState = CombatState.chase;
        pf.steerType = chaseState;

        tick.Timeout += ChaseTick;

        EmitSignal(SignalName.CombatStateChanged);
        CombatStateChanged += EndChase;
    }
    void Chase()
    {
        if (targetPoint.valid) 
        {
            float distance = targetPoint.point.DistanceTo(sheet.GlobalPosition);
            if (distance - wm.currWeapon.idealDistanceMAX > 1.5f) cc.StartSprint();
            else if (distance <= pf.TargetDesiredDistance) cc.EndSprint();

            pf.SetStateTravel(targetPoint.point);
            if (distance < pf.TargetDesiredDistance) NullTargetPoint();
        }
        else
        {
            float distance = target.GlobalPosition.DistanceTo(sheet.GlobalPosition);
            if (distance > wm.currWeapon.idealDistanceMAX * 1.5f) cc.StartSprint();
            else if (distance <= wm.currWeapon.idealDistanceMAX) cc.EndSprint();

            pf.SetStateTravel(target.GlobalPosition);
        } 
    }
    void ChaseTick()
    {
        if (!canSee) cm.RequestPathfinding(this, CombatManager.LoSRequest);
    }
    void EndChase()
    {
        CombatStateChanged -= EndChase;
        tick.Timeout -= ChaseTick;

        cc.EndSprint();
    }

    void StartStrafe()
    {
        if (combatState == CombatState.strafe) return;
        combatState = CombatState.strafe;
        pf.steerType = strafeState;
        sheet.AddStatModifier(strafeSpeedMod, "Speed");

        cc.EndSprint();

        EmitSignal(SignalName.CombatStateChanged);
        CombatStateChanged += EndStrafe;
    }
    void Strafe()
    {
        pf.SetStateTravel(target.GlobalPosition);
    }
    void EndStrafe()
    {
        CombatStateChanged -= EndStrafe;
        sheet.RemoveStatModifier(strafeSpeedMod, "Speed");
    }
    
    void StartFlee()
    {
        if (combatState == CombatState.flee) return;
        combatState = CombatState.flee;

        pf.steerType = fleeState;

        pf.toFlee[target] *= 2; //target could change

        EmitSignal(SignalName.CombatStateChanged);
        CombatStateChanged += EndFlee;
    }
    void Flee()
    {

    }
    void EndFlee()
    {
        pf.toFlee[target] *= 2;
        CombatStateChanged -= EndFlee;
    }

    public ScaledTimer attackChargeTimer = new ScaledTimer();
    public static float ATTACK_CHARGE_IDLE_VALUE = 100;
    void StartAttackCharge()
    {
        if (attackChargeTimer.countdown <= 0 || attackChargeTimer.countdown == ATTACK_CHARGE_IDLE_VALUE)
        {
            attackChargeTimer.Start(wm.currWeapon.delayUseTime);
        } 
    }
    void StartAttack()
    {
        if (combatState == CombatState.attack || !canAttack) return;
        combatState = CombatState.attack;
        EmitSignal(SignalName.CombatStateChanged);
        
        
        wm.UseWeapon();
        attackCounter++;
        if (attackCounter >= wm.currWeapon.attackCombo)
        {
            randomizer.Randomize();
            attackCooldownTimer.Start(wm.currWeapon.attackCooldown + randomizer.RandfRange(0, 1));
        }

        CombatStateChanged += EndAttack;
        EndAttack();
    }
    void Attack()
    {
        
    }
    void EndAttack()
    {
        CombatStateChanged -= EndAttack;
    }
    
    public ScaledTimer maxHideTimer = new ScaledTimer();
    public ScaledTimer hideCooldownTimer = new ScaledTimer();
    void StartHide()
    {
        if (combatState == CombatState.hide) return;
        combatState = CombatState.hide; 
        EmitSignal(SignalName.CombatStateChanged);

        pf.steerType = chaseState;
        cm.RequestPathfinding(this, CombatManager.CoverRequest);

        tick.Timeout += HideTick;
        
        maxHideTimer.Start(personality.maxHideTime);
        maxHideTimer.Timeout += EndHide;
        CombatStateChanged += EndHide;
    }
    void Hide()
    {
        if (targetPoint.valid && sheet.GlobalPosition.DistanceTo(targetPoint.point) > pf.TargetDesiredDistance)
        {
            pf.SetStateTravel(targetPoint.point);
            GD.Print(targetPoint.point);
        }
        else
        {
            NullTargetPoint();
            pf.SetStateStill();
        } 
    }
    void HideTick()
    {
        cm.RequestPathfinding(this, CombatManager.CoverRequest);
    }
    void EndHide()
    {
        hideCooldownTimer.Start(personality.hideCooldown);
        CombatStateChanged -= EndHide;
        tick.Timeout -= HideTick;
    }

    public void EquipWeapon()
    {
        
        float priority = 0;
        SlotData toEquip = null;
        foreach(SlotData slot in inventory.inv.items)
        {
            if (slot.item is Weapon w)
            {
                //Weapon w = (Weapon)slot.item;
                int scoreAmmo = slot.charge + inventory.FindItemByName(w.chargeItemName).amount;
                if (scoreAmmo <= 0) continue;
                scoreAmmo = scoreAmmo * (100/Mathf.Max(1, scoreAmmo));
                float scoreDamage = w.attackInfo.damage * (100/Mathf.Max(1, w.attackInfo.damage));
                float scoreRange;
                if (IsInstanceValid(target)) scoreRange = w.range;
                else scoreRange = 100/(Mathf.Abs(sheet.GlobalPosition.DistanceTo(target.GlobalPosition) - w.range) + 1);

                float score = (0.4f * scoreAmmo) + (0.4f * scoreRange) + (0.2f * scoreDamage);

                if (priority < score)
                {
                    priority = score;
                    toEquip = slot;
                } 
            }
        }

        if (IsInstanceValid(toEquip))
        {
            wm.EquipWeapon(toEquip);
            if (((Weapon)toEquip.item).weaponType == Weapon.WeaponType.ranged)
            {
                bulletShape = ((Weapon)toEquip.item).projectile.Instantiate<Projectile>().GetNodeOrNull<CollisionShape3D>("Area3D/CollisionShape3D").Shape;
            }
            //pf.followDistance = wm.currWeapon.idealDistanceMAX;
        } 
    }
    

    void OnAttacked(AttackInfo attack)
    {
        if (sheet.GetStatValue("CurrentHealth") <= 0) sheet.QueueFree();
        
        if (state != State.combat && attack.attacker.faction != sheet.faction)
        {
            target = attack.attacker;
            StartCombat();
        }
    }

    void OnEntityEntered(Node3D entity)
    {
        if (entity is not CharacterSheet eSheet || entity == sheet) return;

        if (!CanSee(entity))
        {
            hidden.Add(eSheet);
            return;
        }
        
        float rel = sheet.faction.GetRelationship(eSheet.faction);
        if (sheet.faction == eSheet.faction) //ALLY
        {
            if (!allies.Contains(eSheet))
            {
                allies.Add(eSheet);
            }
        }
        else if (rel < 0) //ENEMY
        {
            if (!enemies.Contains(eSheet))
            {
                enemies.Add(eSheet);
                pf.toFlee.Add(eSheet, 5);
                if (hostile && state != State.combat)
                {
                    target = eSheet;
                    StartCombat();
                }
            }
        }
        else //NEUTRAL
        {
            if (!neutrals.Contains(eSheet))
            {
                neutrals.Add(eSheet);
            }
        }
    }

    public bool CanSee(Node3D entity)
    {
        return pf.HasLineOfSight(sheet.GetCenterPosition(wm), entity, true, true);
    }
    
    Shape3D bulletShape;
    public bool CanShoot()
    {
        if (canSee && IsInstanceValid(bulletShape))
        {
            pf.shapecastNode.GlobalPosition = sheet.GetCenterPosition(wm);
            pf.shapecastNode.TargetPosition = (target.GetCenterPosition() - pf.shapecastNode.GlobalPosition);
            pf.shapecastNode.Shape = bulletShape;

            pf.shapecastNode.AddException(target);
            pf.shapecastNode.ForceShapecastUpdate();
            pf.shapecastNode.RemoveException(target);

            if (!pf.shapecastNode.IsColliding()) return true;
        }
        return false;
    }

    void SelectTarget()
    {
        Godot.Collections.Dictionary<CharacterSheet, float> attackers = new Godot.Collections.Dictionary<CharacterSheet, float>();
        CharacterSheet highestPriority = null;
        
        foreach(AttackInfo att in recentAttacks)
        {
            if (!attackers.ContainsKey(att.attacker))
            {
                attackers.Add(att.attacker, att.damage);
                continue;
            }

            attackers[att.attacker] += att.damage;
        }
        
        float maxPriority = 0;

        foreach(CharacterSheet enemy in enemies)
        {
            float priority = 10;
            if (attackers.ContainsKey(enemy)) priority = attackers[enemy];
            if (CanSee(enemy)) priority *= 2;
            priority /= sheet.GlobalPosition.DistanceTo(enemy.GlobalPosition);

            if (priority > maxPriority) 
            {
                maxPriority = priority;
                highestPriority = enemy;
            }
        }

        if (IsInstanceValid(highestPriority)) target = highestPriority;
    }
    
    float CalculateDanger()
    {
        float dang = 0;
        float exposure = 0;
        foreach (CharacterSheet enemy in enemies)
        {
            Godot.Collections.Array<Rid> exclude = new Godot.Collections.Array<Rid>();
            exclude.Add(sheet.GetRid());
            exclude.Add(enemy.GetRid());
            var result = Game.Raycast(sheet, enemy.GlobalPosition, sheet.GlobalPosition, Game.GetBitMask(pf.world_layers), exclude);
            if (result.Count == 0) exposure += personality.exposureDanger;
        }
        dang += exposure;


        if (wm.currWeapon.useCharge)
        {
            int ammoLeft = wm.currWeaponSlotData.charge;
            dang += Game.MapValue(wm.currWeapon.maxCharge - ammoLeft, 0, wm.currWeapon.maxCharge, 0, personality.ammoDangerMax);
        }

        dang += Game.MapValue(sheet.GetStatValue("Health") - sheet.GetStatValue("CurrentHealth"), 0, sheet.GetStatValue("Health"), 0, personality.healthDangerMax);
        dang += Game.MapValue(sheet.GetStatValue("Stamina") - sheet.GetStatValue("CurrentStamina"), 0, sheet.GetStatValue("Stamina"), 0, personality.healthDangerMax);


        return dang;
    }

    void HandleMoveState()
    {
        //cc.inputDir = new Vector2(cc.moveDir.X, cc.moveDir.Z);

        if (!cc.isStrafing && cc.holdingWeapon && !cc.isSprinting && cc.forwardDir.Dot(cc.moveDir) < 0.5f)
        {
            cc.StartStrafe();
        }
        else if ((cc.isStrafing && cc.forwardDir.Dot(cc.moveDir) >= 0.5f) || ((!cc.holdingWeapon || cc.isSprinting) && cc.isStrafing))
        {
            cc.EndStrafe();
        }
    }

    void GetForwardDir(float delta)
    {
        if (cc.isSprinting)
        {
            float angle;
            if (cc.moveDir != Vector3.Zero)
            {
                angle = cc.forwardDir.SignedAngleTo(cc.moveDir, Vector3.Up);
                cc.forwardDir = cc.forwardDir.Rotated(Vector3.Up, angle * cc.sprintRotationSpeed * delta);
            }
            return;
        }

        if (cc.holdingWeapon)
        {
            if (state == State.combat)
            {
                if (!IsInstanceValid(target) || wm.currWeapon == null)
                {
                    if (cc.moveDir != Vector3.Zero) cc.forwardDir = cc.moveDir.Normalized();
                    return;
                }

                if (wm.currWeapon.weaponType == Weapon.WeaponType.ranged)
                {
                    if (Game.Intercept(target.GlobalPosition, cc.sheet.GlobalPosition, target.Velocity, wm.currWeapon.projectileSpeed, out Vector3 dir, out Vector3 pos) == 1)
                    {
                        var ray = Game.Shapecast(sheet, bulletShape, sheet.GlobalPosition, Game.GetBitMask(pf.static_layers));
                        if (ray.Count != 0 /*&& (Node3D)ray[0]["collider"] != target && (Node3D)ray[0]["collider"] != sheet*/ || !dir.IsFinite()) //FIX THIS!!!!!
                        {
                            cc.forwardDir = Game.flattenVector((target.GlobalPosition - cc.sheet.GlobalPosition)).Normalized();
                            return;
                            //GD.Print("Intercept not viable");
                        } 
                        cc.forwardDir = Game.flattenVector(dir).Normalized();
                        return;
                    }
                    else cc.forwardDir = Game.flattenVector((target.GlobalPosition - cc.sheet.GlobalPosition)).Normalized();
                }
                cc.forwardDir = Game.flattenVector((target.GlobalPosition - cc.sheet.GlobalPosition)).Normalized();
            } 
            else if (cc.moveDir != Vector3.Zero) cc.forwardDir = cc.moveDir.Normalized();
        }
        else
        {
           if (cc.moveDir != Vector3.Zero) cc.forwardDir = cc.moveDir.Normalized();
        }
    }
    
    public void NullTargetPoint()
    {
        targetPoint = new EnvironmentQuery.EnvironmentPoint(Vector3.Zero);
        targetPoint.valid = false;
    }
}
