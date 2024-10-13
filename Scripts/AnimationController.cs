using Godot;
using System;
using System.Runtime.InteropServices;


public partial class AnimationController : Node
{
    CharacterController cc;
    Node3D meshPivot;
    public MeshSelection humanoidMesh;
    
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

        stateMachine = (AnimationNodeStateMachine)at.TreeRoot;
		stateMachinePlayback = (AnimationNodeStateMachinePlayback)at.Get("parameters/playback");
        
        skeleton = humanoidMesh.GetNode<Skeleton3D>("%GeneralSkeleton");

        rightArmIK = humanoidMesh.GetNode<SkeletonIK3D>("%right_arm_IK3D");
        leftArmIK = humanoidMesh.GetNode<SkeletonIK3D>("%left_arm_IK3D");
        headIK = humanoidMesh.GetNode<SkeletonIK3D>("%head_IK3D");

        cc.StateChanged += onChangeState;

        humanoidMesh.ApplyMeshes();
        humanoidMesh.WeaponPlaced += cc.wm.ToggleHolster;
    }
    

    public void SetLeftArmIK(bool on)
    {
        if (on) leftArmIK.Start();
        else leftArmIK.Stop();
    }

    public void onChangeState()
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
        
        float Delta = (float)delta * animationTimescale * cc.sheet.localTimescale * cc.game.Timescale; 

        if (IsInstanceValid(tween)) tween.CustomStep(Delta);
        at.Advance(Delta);
        
        //MOVE DIRECTION AND GUN TOGGLE
        /*Vector3 inputDir = new Vector3(c.inputDir.X, 0f, c.inputDir.Y);
        inputDir = inputDir.Rotated(Vector3.Up, (float)Math.PI/4);
        Vector3 moveDir3D = (inputDir.Z * meshPivot.GlobalBasis.Z) + (inputDir.X * meshPivot.GlobalBasis.X);
        Vector2 moveDir = new Vector2 (moveDir3D.X, moveDir3D.Z);*/
        
        Vector2 moveDir = new Vector2(cc.moveDir.Dot(cc.forwardDir.Rotated(Vector3.Up, -(float)Math.PI/2)), cc.moveDir.Dot(cc.forwardDir));

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
        Vector3 StartPos = boneTransformLocal.Origin;//skeleton.ToGlobal(boneTransformLocal.Origin);
        
        rightHandTarget = humanoidMesh.GetNode<Node3D>("%right_arm_target");
        //rightHandTarget.Position = StartPos;
        rightHandTarget.Basis = boneTransformLocal.Basis;
        
        //set target pos
        Vector3 newPos = StartPos;
        newPos.Z = newPos.Z - (cc.wm.currWeapon.kickback);
        rightHandTarget.Position = newPos;

        bone = skeleton.FindBone("Neck");
        boneTransformLocal = skeleton.GetBoneGlobalPose(bone);
        StartPos = boneTransformLocal.Origin;

        headTarget = humanoidMesh.GetNode<Node3D>("%head_target");
        //headTarget.Position = StartPos;
        headTarget.Basis = boneTransformLocal.Basis;

        newPos = StartPos; //- (headTarget.Basis.Z * c.wm.currWeapon.kickback);
        newPos.Z = newPos.Z -(cc.wm.currWeapon.kickback * 0.5f);
        headTarget.Position = newPos;

        cc.isAttacking = true;
        rightArmIK.Start();
        headIK.Start();
    }
    
    float recoilProgress = 0;
    float curveSample;
    public void Recoil(float delta)
    {
        curveSample = cc.wm.currWeapon.recoilCurve.Sample(recoilProgress);
        
        rightArmIK.Interpolation = curveSample;
        headIK.Interpolation = curveSample;
        /*Transform3D boneTransformLocal = skeleton.GetBoneGlobalPose(chest);
        boneTransformLocal.Origin = chestStartPos - (c.forwardDir.Normalized() * c.wm.currWeapon.kickback * curveSample * 0.05f);
        skeleton.SetBonePosePosition(chest, boneTransformLocal.Origin);
        skeleton.SetBonePoseRotation(chest, boneTransformLocal.Basis.GetRotationQuaternion());*/
        
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
    
    public void StartAttack()
    {
        at.Set("parameters/MoveState/attack/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
        
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
