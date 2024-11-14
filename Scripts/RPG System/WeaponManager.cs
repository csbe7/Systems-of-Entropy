using Godot;
using System;
using System.Threading;


public partial class WeaponManager : Node
{ 
    AnimationTree at;

    CharacterController cc;
    CharacterSheet sheet;
    Inventory inventory;
    MeshSelection humanoidMesh;

    BoneAttachment3D handAttachment;
    BoneAttachment3D backAttachment;
    SkeletonIK3D leftArmIK;

    SoundEmitter weaponStreamPlayer;

    public Node3D weaponTip;

    AnimationNodeAnimation attackAnim;

    [Export] SlotData unarmed;

    public SlotData currWeaponSlotData;
    public Weapon currWeapon;
    
    [ExportCategory("Stat Modifiers")]
    [Export] public StatModifier reloadSpeedMod;

    [Signal] public delegate void WeaponEquippedEventHandler();
    [Signal] public delegate void WeaponUnequippedEventHandler();


    [Signal] public delegate void ReloadStartedEventHandler();
    [Signal] public delegate void ReloadFinishedEventHandler();
    [Signal] public delegate void AmmoChangedEventHandler();
    
    public ScaledTimer reloadTimer;
    public bool reloading;

    public bool weaponHolstered = false;

    int comboCounter = 0;

    public override async void _Ready()
    {
        humanoidMesh = GetNode<MeshSelection>("%Mesh/HumanoidMesh");

        at = humanoidMesh.GetNode<AnimationTree>("%AnimationTree");
        cc = GetNode<CharacterController>("%CharacterController");
        sheet = GetParent<CharacterSheet>();
        inventory = GetNode<Inventory>("%Inventory");

        SetProcess(false);
        SetPhysicsProcess(false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SetProcess(true);
        SetPhysicsProcess(true);
       
        AnimationNodeBlendTree MoveState = (AnimationNodeBlendTree)(((AnimationNodeStateMachine)at.TreeRoot).GetNode("MoveState"));
        attackAnim = (AnimationNodeAnimation)MoveState.GetNode("attack_anim");
        
        reloadTimer = new ScaledTimer();
        AddChild(reloadTimer);

        handAttachment = humanoidMesh.GetNode<BoneAttachment3D>("%weaponAttachment");
        backAttachment = humanoidMesh.GetNode<BoneAttachment3D>("%backAttachment");

        weaponStreamPlayer = handAttachment.GetNode<SoundEmitter>("%WeaponStreamPlayer");
        weaponStreamPlayer.Owner = sheet;

        humanoidMesh.AttackStarted += OnAttackStarted;
        humanoidMesh.AttackEnded += OnAttackEnded;
        humanoidMesh.HitStarted += OnHitStarted;
        humanoidMesh.HitEnded += OnHitEnded;
        humanoidMesh.RecoveryStarted += OnRecoveryStarted;

        leftArmIK = humanoidMesh.GetNode<SkeletonIK3D>("%left_arm_IK3D");
        if (!IsInstanceValid(currWeapon)) EquipWeapon(unarmed);
    }


    public bool isEquipped(SlotData slot)
    {
        return (currWeaponSlotData == slot);
    }
    

    public void EquipWeapon(SlotData newWeapon)
    {
        if (cc.isStrafing)
        {
            sheet.AddStatModifier(currWeapon.strafeSpeedMod, "Speed");
        }

        //if (IsInstanceValid(currWeapon)) UnequipWeapon();
        currWeaponSlotData = newWeapon;
        currWeapon = (Weapon)newWeapon.item;
        LoadWeapon(0);
        EmitSignal(SignalName.WeaponEquipped);
    }


    public void UnequipWeapon()
    {
        if (cc.isStrafing)
        {
            sheet.RemoveStatModifier(currWeapon.strafeSpeedMod, "Speed");
        }

        if (cc.isAttacking || cc.isRecovering) OnAttackEnded();


        currWeaponSlotData = null;
        currWeapon = null;
        

        Node3D weapon;
        if (weaponHolstered) weapon = backAttachment.GetNode<Node3D>("Weapon"); 
        else weapon = handAttachment.GetNode<Node3D>("Weapon"); 
        weapon.GetNode<MeshInstance3D>("MeshInstance3D").Mesh = null;
        //ac.SetLeftArmIK(false);

        progressCombo = false;
        comboCounter = 0;

        EmitSignal(SignalName.WeaponUnequipped);
        EquipWeapon(unarmed);
    }
    
  
    public void LoadWeapon(int mode) //0 = holster   1 = hand;
    {
        Node3D weapon = backAttachment.GetNode<Node3D>("Weapon");;
        switch (mode)
        {
            case 0:
            //weapon = backAttachment.GetNode<Node3D>("Weapon");
            weapon.Position = currWeapon.holsteredPosition;
            weapon.Rotation = currWeapon.holsteredRotation;
            weaponHolstered = true;
            break;

            case 1:
            weapon = handAttachment.GetNode<Node3D>("Weapon");
            weapon.Position = currWeapon.position;
            weapon.Rotation = currWeapon.rotation;
            weaponHolstered = false;
            break;
        }

        var weaponMesh = weapon.GetNode<MeshInstance3D>("MeshInstance3D");
        weaponTip = weapon.GetNode<Node3D>("Tip");
        weaponMesh.Mesh = currWeapon.mesh;
        weaponTip.Position = currWeapon.tipPosition;
        
        
        Node3D leftArmTarget = weapon.GetNode<Node3D>("leftHand_target");
        leftArmTarget.Position = currWeapon.leftArm_target_position;
        leftArmTarget.Rotation = currWeapon.leftArm_target_rotation;
        
        
        leftArmIK.TargetNode = leftArmTarget.GetPath(); 

        //LOAD ANIMATIONS
        WeaponAnimations wa = currWeapon.animations;
        AnimationNodeBlendTree MoveState = (AnimationNodeBlendTree)(((AnimationNodeStateMachine)at.TreeRoot).GetNode("MoveState"));
        AnimationNodeBlendSpace2D blendSpace =  (AnimationNodeBlendSpace2D)MoveState.GetNode("gunIdleBlend");
        

        for (int i = blendSpace.GetBlendPointCount() - 1; i >= 0; i--)
        {
            blendSpace.RemoveBlendPoint(i);
        } 
        
        AddAnimation(wa.lib + "/" + wa.standingIdle, Vector2.Zero, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkForward, Vector2.Down, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkBackward, Vector2.Up, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkLeft, Vector2.Left, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkRight, Vector2.Right, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkForwardLeft, (Vector2.Left + Vector2.Down).Normalized(), blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkForwardRight, (Vector2.Right + Vector2.Down).Normalized(), blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkBackwardLeft, (Vector2.Left + Vector2.Up).Normalized(), blendSpace);
        AddAnimation(wa.lib + "/" +  wa.walkBackwardRight, (Vector2.Right + Vector2.Up).Normalized(), blendSpace);
        

        blendSpace = (AnimationNodeBlendSpace2D)MoveState.GetNode("gunCrouchBlend");

        for (int i = blendSpace.GetBlendPointCount() - 1; i >= 0; i--)
        {
            blendSpace.RemoveBlendPoint(i);
        } 

        AddAnimation(wa.lib + "/" + wa.crouchingIdle, Vector2.Zero, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchForward, Vector2.Down, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchBackward, Vector2.Up, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchLeft, Vector2.Left, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchRight, Vector2.Right, blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchForwardLeft, (Vector2.Left + Vector2.Down).Normalized(), blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchForwardRight, (Vector2.Right + Vector2.Down).Normalized(), blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchBackwardLeft, (Vector2.Left + Vector2.Up).Normalized(), blendSpace);
        AddAnimation(wa.lib + "/" +  wa.crouchBackwardRight, (Vector2.Right + Vector2.Up).Normalized(), blendSpace);
        
        var anim = (AnimationNodeAnimation)MoveState.GetNode("weapon_sprint");
        if (wa.sprint != "" && wa.sprint != "base") anim.Animation = wa.lib + "/" + wa.sprint;
        else anim.Animation = "sprint";

        anim = (AnimationNodeAnimation)MoveState.GetNode("hand_pose");
        anim.Animation = wa.lib + "/" + wa.standingIdle;

        anim = (AnimationNodeAnimation)MoveState.GetNode("draw_anim");
        if (wa.draw != "" && wa.draw != "base") anim.Animation = wa.lib + "/" + wa.draw;
        else anim.Animation = null;

    }
    
    public void UnloadWeapon()
    {
        Node3D weapon;
        if (weaponHolstered) weapon = backAttachment.GetNode<Node3D>("Weapon"); 
        else weapon = handAttachment.GetNode<Node3D>("Weapon"); 
        weapon.GetNode<MeshInstance3D>("MeshInstance3D").Mesh = null;
    }


    public void ToggleHolster()
    {
        if (!IsInstanceValid(currWeapon)) return;
        
        UnloadWeapon();
        if (weaponHolstered) LoadWeapon(1);
        else LoadWeapon(0);
    }


    void AddAnimation(String animName, Vector2 pos, AnimationNodeBlendSpace2D blendSpace)
    {
        AnimationNodeAnimation animationNode = new AnimationNodeAnimation();
        animationNode.Animation = animName;
        blendSpace.AddBlendPoint(animationNode, pos);
    }

    bool progressCombo = false;
    public void UseWeapon(bool progress = false)
    {
        //CONSUME CHARGE
        if (currWeapon.useCharge && !cc.isAttacking)
        {
            if (currWeaponSlotData.charge < currWeapon.chargePerAttack) return;
            //ELSE
            currWeaponSlotData.charge -= currWeapon.chargePerAttack;
            EmitSignal(SignalName.AmmoChanged);
        }

        if (currWeapon.weaponType == Weapon.WeaponType.ranged)
        {
            if (cc.isAttacking) return;

            Projectile proj = currWeapon.projectile.Instantiate<Projectile>();
            proj.shooter = sheet;
            proj.direction = cc.forwardDir;
            proj.weapon = currWeapon;
            proj.distance = currWeapon.range;
            proj.speed = currWeapon.projectileSpeed;
            GetParent().GetParent().AddChild(proj);
            proj.GlobalPosition = weaponTip.GlobalPosition;

            cc.ac.AttackFinished += OnAttackFinished;

            sheet.AddStatModifier(currWeapon.attackSpeedMod, "Speed");

            cc.ac.StartRecoil();
        }
        else if (currWeapon.weaponType == Weapon.WeaponType.meele)
        {   
            if (progress); //CATCH 
            else if (cc.isAttacking && !progressCombo)
            {
                if (comboCounter >= currWeapon.animations.attacks.Count)
                {
                    return;
                } 
                comboCounter++;
                progressCombo = true;
                return;
            }
            else if (cc.isAttacking && progressCombo)
            {
                return;
            } 
            else if (cc.isRecovering)
            {
                /*if (comboCounter >= currWeapon.animations.attacks.Count) return;
                comboCounter++;
                cc.isRecovering = false;
                cc.isAttacking = true;*/
                return;
            }
          
            attackAnim.Animation = currWeapon.animations.lib + "/" + currWeapon.animations.attacks[comboCounter];
            cc.ac.at.Set("parameters/MoveState/attack_speed/scale", currWeapon.animations.attackSpeeds[comboCounter]);

            cc.ac.StartAttack();
        }
        
        Sound s = null;
        if (currWeapon.weaponSound.sound.Count >= comboCounter && currWeapon.weaponSound.sound.Count != 0)
        {
            s = (Sound)currWeapon.weaponSound.sound[0].Duplicate();
            s.emitter = sheet;
            weaponStreamPlayer.Stream = currWeapon.weaponSound.sound[0].stream;
            weaponStreamPlayer.VolumeDb = currWeapon.weaponSound.sound[0].volumeDb;
        }
        else if (currWeapon.weaponSound.sound.Count != 0)
        {
            s = (Sound)currWeapon.weaponSound.sound[comboCounter].Duplicate();
            s.emitter = sheet;
            weaponStreamPlayer.Stream = currWeapon.weaponSound.sound[comboCounter].stream;
            weaponStreamPlayer.VolumeDb = currWeapon.weaponSound.sound[comboCounter].volumeDb;
        }
        
        if (IsInstanceValid(s)) weaponStreamPlayer.Play(s, 1, false);
    }

    public void OnAttackStarted()
    {
        cc.isRecovering = false;
        cc.isAttacking = true;
        sheet.AddStatModifier(currWeapon.attackSpeedMod, "Speed");
        
    }

    public void OnAttackEnded()
    {
        sheet.RemoveStatModifier(currWeapon.attackSpeedMod, "Speed");
        cc.ac.Set("parameters/MoveState/attack_speed/scale", 1);
        cc.isAttacking = false;
        cc.isRecovering = false;
        progressCombo = false;
        comboCounter = 0;
        if (IsInstanceValid(hitbox)) hitbox.QueueFree();
    }
    
    Hitbox hitbox;
    public void OnHitStarted()
    {  
        hitbox = currWeapon.hitbox.Instantiate<Hitbox>();
        sheet.AddChild(hitbox);
        hitbox.GlobalPosition = sheet.GetCenterPosition() + cc.forwardDir;
        hitbox.attacker = sheet;
        hitbox.weapon = currWeapon;
        if (currWeapon.attackData.Count <= comboCounter) hitbox.hitInfo = (AttackInfo)currWeapon.attackData[0].Duplicate();
        else hitbox.hitInfo = (AttackInfo)currWeapon.attackData[comboCounter].Duplicate();
        hitbox.LookAt(hitbox.GlobalPosition + cc.forwardDir);
    }

    public void OnHitEnded()
    {
        if (IsInstanceValid(hitbox)) hitbox.QueueFree();
    }
    

    public void OnRecoveryStarted()
    {
        if (comboCounter >= currWeapon.animations.attacks.Count)
        {
            comboCounter = 0;
            progressCombo = false;
            cc.isAttacking = false;
            cc.isRecovering = true;
            return;
        }
        if (progressCombo)
        {
            UseWeapon(true);
            progressCombo = false;
        }
        else 
        {
            cc.isAttacking = false;
            cc.isRecovering = true;
        }
    }

    public void OnAttackFinished()
    {
        cc.ac.AttackFinished -= OnAttackFinished;
        sheet.RemoveStatModifier(currWeapon.attackSpeedMod, "Speed");
    }
    
    public void OnWeaponHit(CharacterStatus target)
    {
        
    }
    

    public bool StartReload()
    {
        if (reloading) return false;

        if (currWeapon.useCharge)
        {
            bool reloadCompleted = true;
            if (currWeaponSlotData.charge >= currWeapon.maxCharge) return true;

            SlotData ammoItem = inventory.FindItemByName(currWeapon.chargeItemName);
            if (ammoItem == null) return false;
            if (ammoItem.amount < currWeapon.chargePerAttack) reloadCompleted = false;
            
            reloadTimer.Timeout += Reload;
            reloadTimer.Start(currWeapon.reloadTime);
            reloading = true;
            sheet.AddStatModifier(currWeapon.reloadSpeedMod, "Speed");
            EmitSignal(SignalName.ReloadStarted);

            return reloadCompleted;
        }

        return true;
    }

    public void Reload()
    {
        reloadTimer.Timeout -= Reload;
        reloadTimer.Stop();
        reloading = false;
        sheet.RemoveStatModifier(currWeapon.reloadSpeedMod, "Speed");
        EmitSignal(SignalName.ReloadFinished);

        if (currWeapon.useCharge)
        {
            while (currWeaponSlotData.charge < currWeapon.maxCharge)
            {
                int requiredCharge = currWeapon.maxCharge - currWeaponSlotData.charge;
                SlotData chargeSlot = inventory.FindItemByName(currWeapon.chargeItemName);
                if (chargeSlot == null) break;
                currWeaponSlotData.charge += Mathf.Min(requiredCharge, chargeSlot.amount);
                inventory.RemoveItem(chargeSlot, requiredCharge);
            }
        }

        EmitSignal(SignalName.AmmoChanged);
    }
}
