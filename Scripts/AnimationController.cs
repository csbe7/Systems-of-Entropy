using Godot;
using System;
using System.Runtime.InteropServices;


public partial class AnimationController : Node
{
    CharacterController cc;
    Node3D meshPivot;
    public MeshSelection humanoidMesh;
    
    public AnimationPlayer ap;
    public AnimationTree at;

    AnimationNodeStateMachine stateMachine;
	AnimationNodeStateMachinePlayback stateMachinePlayback;
    
    Tween tween;

    [Export]public float animationTimescale = 1;
    [Export]public float rotationSpeed = 10f;
    Vector2 dir = Vector2.Zero;
    [Export]float lerpSpeed = 7f;

    [ExportCategory("IK")]
    public Skeleton3D skeleton;

    public SkeletonIK3D leftArmIK;
    public SkeletonIK3D rightArmIK;
    public SkeletonIK3D headIK;

    [Signal] public delegate void AttackFinishedEventHandler();

    
    public override async void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SetProcess(true);
        SetPhysicsProcess(true);

        cc = GetNode<CharacterController>("%CharacterController");
        meshPivot = GetNode<Node3D>("%Mesh");
        
        humanoidMesh = meshPivot.GetNode<MeshSelection>("HumanoidMesh");

        at = humanoidMesh.GetNode<AnimationTree>("%AnimationTree");
        ap = humanoidMesh.GetNode<AnimationPlayer>("%AnimationPlayer");

        stateMachine = (AnimationNodeStateMachine)at.TreeRoot;
		stateMachinePlayback = (AnimationNodeStateMachinePlayback)at.Get("parameters/playback");
        
        skeleton = humanoidMesh.GetNode<Skeleton3D>("%GeneralSkeleton");

        rightArmIK = humanoidMesh.GetNode<SkeletonIK3D>("%right_arm_IK3D");
        leftArmIK = humanoidMesh.GetNode<SkeletonIK3D>("%left_arm_IK3D");
        headIK = humanoidMesh.GetNode<SkeletonIK3D>("%head_IK3D");

        cc.StateChanged += OnChangeState;

        humanoidMesh.ApplyMeshes();
        humanoidMesh.WeaponPlaced += cc.wm.ToggleHolster;
    }
    

    public void SetLeftArmIK(bool on)
    {
        if (on) leftArmIK.Start();
        else leftArmIK.Stop();
    }

    public void OnChangeState()
    {
        if (cc.isSprinting)
        {
            at.Set("parameters/MoveState/isSprintingWeapon/transition_request", "is_sprinting");
        }
        else
        {
            at.Set("parameters/MoveState/isSprintingWeapon/transition_request", "not_sprinting");
        }

        if (cc.holdingWeapon)
        {
            //if (cc.wm.currWeapon.useleftArmIK) leftArmIK.Start();
            
            at.Set("parameters/MoveState/hasWeapon/transition_request", "has_weapon");
            at.Set("parameters/MoveState/hand_blend/blend_amount", 1);

            if (cc.isCrouching)
            {
                at.Set("parameters/MoveState/isCrouchingGun/transition_request", "is_crouching");
            }
            else 
            {
                at.Set("parameters/MoveState/isCrouchingGun/transition_request", "not_crouching");
            }
            
        }
        else
        {
            leftArmIK.Stop();
            at.Set("parameters/MoveState/hasWeapon/transition_request", "no_weapon");
            at.Set("parameters/MoveState/hand_blend/blend_amount", 0);

            if (cc.isCrouching)
            {
                at.Set("parameters/MoveState/isCrouchingUnarmed/transition_request", "is_crouching");
            }
            else at.Set("parameters/MoveState/isCrouchingUnarmed/transition_request", "not_crouching");
        }
    }

    
    public override void _PhysicsProcess(double delta)
    {
        float timescale = animationTimescale * cc.sheet.localTimescale * cc.game.Timescale;

        at.Set("parameters/MoveState/animationTimescale/scale", timescale);

        if (!cc.isSprinting) at.Set("parameters/MoveState/movementTimescale/scale", Mathf.Clamp(cc.sheet.GetStatValue("Speed")/cc.sheet.speed, 0.5f, 1.5f));
        else at.Set("parameters/MoveState/movementTimescale/scale", Mathf.Clamp(cc.sheet.GetStatValue("Speed")/(cc.sheet.speed * (1 + cc.sheet.sprintSpeedModifier.value/100)), 0.5f, 1.5f));
        
        AnimationNodeBlendTree MoveState = (AnimationNodeBlendTree)(((AnimationNodeStateMachine)at.TreeRoot).GetNode("MoveState"));
        ((AnimationNodeTransition)MoveState.GetNode("hasWeapon")).XfadeTime = cc.wm.currWeapon.drawTime;
        
        var anim = (AnimationNodeAnimation)MoveState.GetNode("draw_anim");
        var speed = anim.TimelineLength / cc.wm.currWeapon.drawTime; 
        at.Set("parameters/MoveState/draw_speed/scale", speed);

        float Delta = (float)delta * animationTimescale * cc.sheet.localTimescale * cc.game.Timescale; 

        if (IsInstanceValid(tween)) tween.CustomStep(Delta);
        at.Advance(Delta);
        
        
        Vector2 moveDir = new Vector2(cc.moveDir.Dot(cc.forwardDir.Rotated(Vector3.Up, -(float)Math.PI/2)), cc.moveDir.Dot(cc.forwardDir));
        if (cc.isStunned) moveDir = Vector2.Zero;

        if (cc.sheet.GetStatValue("Speed", true) == 0) moveDir = Vector2.Zero;

        dir = dir.Lerp(moveDir, Delta * lerpSpeed);

        
        if (cc.holdingWeapon && !cc.isSprinting)
        {
            if (cc.isCrouching)
            {
                at.Set("parameters/MoveState/gunCrouchBlend/blend_position", dir);
            }
            else
            {
                at.Set("parameters/MoveState/gunIdleBlend/blend_position", dir);
            } 

            Rotate(cc.forwardDir, rotationSpeed, Delta);
        }
        else
        {
            if (cc.sheet.Velocity.Length() > 0.1f)
            {
                if (IsInstanceValid(tween)) tween.Kill();
        
                tween = CreateTween();
                float blendValue;
                if (cc.isSprinting) blendValue = 2;
                else 
                {
                    blendValue = (cc.sheet.Velocity.Length()/cc.sheet.GetStatValue("Speed", true));
                }
                tween.TweenProperty(at, "parameters/MoveState/movementBlend/blend_position", blendValue, 0.2);
                tween.Parallel().TweenProperty(at, "parameters/MoveState/movementBlendCrouch/blend_position", 1, 0.2);
                tween.Pause();
                //tween.Parallel().TweenProperty(at, "parameters/MoveState/animationTimescale/scale", ms.animationSpeed, 0.7);
            }
            else 
            {
                if (IsInstanceValid(tween)) tween.Kill();
        
                tween = CreateTween();
                tween.TweenProperty(at, "parameters/MoveState/movementBlend/blend_position", 0, 0.1);
                tween.Parallel().TweenProperty(at, "parameters/MoveState/movementBlendCrouch/blend_position", 0, 0.1);
                tween.Pause();
            }

            if (cc.sheet.Velocity.Length() > 0)
            {
                Rotate(cc.sheet.Velocity, rotationSpeed, Delta);
            }

        }
        
        //RECOIL
        if (cc.isAttacking && cc.wm.currWeapon.weaponType == Weapon.WeaponType.ranged) Recoil(Delta);

        //HIT REACTION
        if (cc.isStunned) HitReaction(Delta);

    }



    public void Rotate(Vector3 direction, float speed, float delta)
	{
        Vector3 scale = meshPivot.Scale;

		float angle = Mathf.LerpAngle(meshPivot.GlobalRotation.Y, Mathf.Atan2(direction.X, direction.Z), speed * delta); 
		Vector3 newRotation = new Vector3(meshPivot.GlobalRotation.X, angle, meshPivot.GlobalRotation.Z);
		meshPivot.GlobalRotation = newRotation;

		meshPivot.Scale = scale;
	}

    

    //RECOIL
    Node3D rightHandTarget; //set in ready
    Node3D headTarget;
    public void StartRecoil()
    {
        int bone = skeleton.FindBone(rightArmIK.TipBone);
        Transform3D boneTransformLocal = skeleton.GetBoneGlobalPose(bone);
        Vector3 StartPos = boneTransformLocal.Origin;
        
        rightHandTarget = humanoidMesh.GetNode<Node3D>("%right_arm_target");
        //rightHandTarget.Position = StartPos;
        rightHandTarget.Basis = boneTransformLocal.Basis;
        rightHandTarget.Transform = boneTransformLocal;
        
        //set target pos
        Vector3 newPos = StartPos;
        newPos.Z = newPos.Z - (cc.wm.currWeapon.kickback);
        rightHandTarget.Position = newPos;

        bone = skeleton.FindBone(headIK.TipBone);
        boneTransformLocal = skeleton.GetBoneGlobalPose(bone);
        StartPos = boneTransformLocal.Origin;

        headTarget = humanoidMesh.GetNode<Node3D>("%head_target");
        //headTarget.Position = StartPos;
        headTarget.Basis = boneTransformLocal.Basis;
        headTarget.Transform = boneTransformLocal;

        newPos = StartPos; //- (headTarget.Basis.Z * c.wm.currWeapon.kickback);
        newPos.Z = newPos.Z -(cc.wm.currWeapon.kickback * 0.5f);
        headTarget.Position = newPos;

        cc.isAttacking = true;
        rightArmIK.Start();
        headIK.Start();
    }
    
    float recoilProgress = 0;
    float recoilCurveSample;
    public void Recoil(float delta)
    {
        //TEST
        headIK.Stop();
        rightArmIK.Stop();
        int bone = skeleton.FindBone(rightArmIK.TipBone);
        Transform3D boneTransformLocal = skeleton.GetBoneGlobalPose(bone);
        Vector3 StartPos = boneTransformLocal.Origin;
        Vector3 newPos = StartPos;
        newPos.Z = newPos.Z - (cc.wm.currWeapon.kickback);
        rightHandTarget.Position = newPos;

        bone = skeleton.FindBone(headIK.TipBone);
        boneTransformLocal = skeleton.GetBoneGlobalPose(bone);
        StartPos = boneTransformLocal.Origin;

        headTarget = humanoidMesh.GetNode<Node3D>("%head_target");
        headTarget.Transform = boneTransformLocal;

        newPos = StartPos; 
        newPos.Z = newPos.Z -(cc.wm.currWeapon.kickback * 0.5f);
        headTarget.Position = newPos;
        
        headIK.Start();
        rightArmIK.Start();
        //END TEST
 
        recoilCurveSample = cc.wm.currWeapon.recoilCurve.Sample(recoilProgress);
        
        rightArmIK.Interpolation = recoilCurveSample;
        headIK.Interpolation = recoilCurveSample;

        recoilProgress += delta * cc.wm.currWeapon.recoilSpeed;

        if (recoilProgress > 1)
        {
            recoilProgress = 0;
            rightArmIK.Stop();
            headIK.Stop();
            cc.isAttacking = false;
            EmitSignal(SignalName.AttackFinished);
        }
    }
    

    public void StartHitReaction(AttackInfo att)
    {
        if (cc.isAttacking || cc.isRecovering)
        {
            AbortAttack(1);
        }

        //cc.isStunned = true;
        attack = att;
        headIK.Start();
    }
    
    AttackInfo attack;
    float reactionProgress = 0;
    float reactionCurveSample;
    public void HitReaction(float delta)
    {
        headIK.Stop();
        
        int bone = skeleton.FindBone(headIK.TipBone);
        Transform3D boneTransformLocal = skeleton.GetBoneGlobalPose(bone);
        Vector3 startPos = skeleton.ToGlobal(boneTransformLocal.Origin);
        Vector3 newPos = startPos;

        headTarget = humanoidMesh.GetNode<Node3D>("%head_target");
        headTarget.Transform = boneTransformLocal;

        newPos += attack.knockbackDir.Normalized() * attack.knockback * 0.1f;
        headTarget.GlobalPosition = newPos;

        headIK.Start();

        reactionCurveSample = attack.hitReactionCurve.Sample(reactionProgress);

        headIK.Interpolation = reactionCurveSample;

        reactionProgress += delta/attack.hitstunDuration;

        if (reactionProgress > 1)
        {
            reactionProgress = 0;
            headIK.Stop();
            cc.EndHitStun();
        }
    }


    public void StartAttack()
    {
        if (cc.isRecovering) AbortAttack();
        at.Set("parameters/MoveState/attack/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
    }

    public void AbortAttack(int mode = 0)
    {
        if (mode == 0) at.Set("parameters/MoveState/attack/request", (int)AnimationNodeOneShot.OneShotRequest.Abort);
        else at.Set("parameters/MoveState/attack/request", (int)AnimationNodeOneShot.OneShotRequest.FadeOut);
        cc.wm.OnAttackEnded();
    }


    public void DrawWeapon()
    {
        AnimationNodeBlendTree MoveState = (AnimationNodeBlendTree)(((AnimationNodeStateMachine)at.TreeRoot).GetNode("MoveState"));
        var anim = (AnimationNodeAnimation)MoveState.GetNode("draw_anim");
        
        if (anim.Animation == null)
        {
            cc.EndDrawingWeapon();
            return;
        }
        else
        {
            anim.PlayMode = AnimationNodeAnimation.PlayModeEnum.Forward;
            at.Set("parameters/MoveState/draw_weapon/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
            humanoidMesh.WeaponHeld += cc.EndDrawingWeapon;
        }

    }

    public void HolsterWeapon()
    {
        AnimationNodeBlendTree MoveState = (AnimationNodeBlendTree)(((AnimationNodeStateMachine)at.TreeRoot).GetNode("MoveState"));
        var anim = (AnimationNodeAnimation)MoveState.GetNode("draw_anim");
        
        if (anim.Animation == null)
        {
            cc.EndHolsteringWeapon();
            return;
        }
        else
        {
            anim.PlayMode = AnimationNodeAnimation.PlayModeEnum.Backward;
            at.Set("parameters/MoveState/draw_weapon/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
            humanoidMesh.WeaponHolstered += cc.EndHolsteringWeapon;
        }

    }

}
