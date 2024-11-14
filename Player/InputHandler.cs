using Godot;
using System;

public partial class InputHandler : Node
{
    CharacterController cc;
    camera camPivot;
    Camera3D cam;

    public override void _Ready()
    {
        cc = GetNode<CharacterController>("%CharacterController");
        camPivot =  GetTree().Root.GetChild<Node3D>(GetTree().Root.GetChildCount()-1).GetNode<camera>("%CameraPivot");
        cam = camPivot.GetNode<Camera3D>("Camera3D");
        //cc = GetNode<CharacterController>("%CharacterBody");
    }

    public override void _Process(double delta)
    {
        float ts = cc.game.Timescale * cc.sheet.localTimescale;
        float D = ts * (float)delta;

        GetForwardDir(D);
        GetInput(ts);
    }

    void GetInput(float timescale)
    {
        if (timescale == 0) return;
        

        bool updateState = false;

        if (Input.IsActionPressed("Sprint") && !cc.drawingWeapon && !cc.holsteringWeapon)
        {
            if (!cc.isSprinting)
            {
                updateState = true;
                cc.StartSprint();
            } 
        }
        else 
        {
            if (cc.isSprinting)
            {
                updateState = true;
                cc.EndSprint();
            } 
        }

        if (Input.IsActionJustPressed("Crouch") && !cc.isSprinting)
        {
            if (!cc.isCrouching) cc.StartSneak();
            else cc.EndSneak();
            updateState = true;
        }
        

        if (cc.moveInputBlockers == 0 && Input.IsActionPressed("Move"))
        {
            //Vector3 temp = cc.moveDir;
            cc.inputDir = new Vector2(Input.GetActionStrength("Right") - Input.GetActionStrength("Left"), Input.GetActionStrength("Up") - Input.GetActionStrength("Down"));
            Vector2 inputNorm = cc.inputDir.Normalized();
            cc.moveDir = (Game.flattenVector(cam.GlobalBasis.X) * inputNorm.X) + (Game.flattenVector(-cam.GlobalBasis.Z) * inputNorm.Y).Normalized();

            if (!cc.isStrafing && cc.holdingWeapon && !cc.isSprinting && cc.forwardDir.Dot(cc.moveDir) < 0.5f)
            {
                cc.StartStrafe();
            }
            else if ((cc.isStrafing && cc.forwardDir.Dot(cc.moveDir) >= 0.5f) || ((!cc.holdingWeapon || cc.isSprinting) && cc.isStrafing))
            {
                cc.EndStrafe();
            }
        }
        else
        {
            cc.inputDir = Vector2.Zero;
            cc.moveDir = Vector3.Zero;
        }
        
        if (Input.IsActionJustPressed("Dash") && !cc.isAttacking)
        {
            if (cc.moveDir != Vector3.Zero) cc.StartDash(cc.moveDir);
            else cc.StartDash(cc.forwardDir);
            updateState = true;
        }
        
        if (cc.wm.currWeapon != null)
        {
            if (Input.IsActionJustPressed("Weapon Switch") && cc.canDrawWeapon)
            {
                if (!cc.holdingWeapon) cc.StartHoldingWeapon();
                else cc.EndHoldingWeapon(); 
                updateState = true;
            }
        }
        else
        {
            if (cc.holdingWeapon)
            {
                cc.holdingWeapon = false;
                updateState = true;
            }
        }

        if (cc.holdingWeapon && Input.IsActionJustPressed("Reload") && !cc.wm.reloading && !cc.isAttacking && !cc.isSprinting && !cc.isDashing)
        {
            cc.wm.StartReload();
        }


        if ((Input.IsActionJustPressed("Attack") || (Input.IsActionPressed("Attack") && cc.wm.currWeapon.hold)) && cc.holdingWeapon && !cc.wm.reloading && !cc.isSprinting && !cc.isDashing)
        {
            cc.wm.UseWeapon();
        }

        if (updateState) cc.EmitSignal(CharacterController.SignalName.StateChanged);
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
            Godot.Collections.Dictionary r = camPivot.ShootRayToMouse(1);
            
            if (cc.game.aimAssist && r != null)
            {
                Vector3 targetPos = aimAssist((Vector3)r["position"], out bool valid);
                if (valid) cc.forwardDir = targetPos - cc.sheet.GlobalPosition;
                else cc.forwardDir = (Vector3)r["position"] - cc.sheet.GlobalPosition;
            }
            else if (r != null)cc.forwardDir = (Vector3)r["position"] - cc.sheet.GlobalPosition;
            cc.forwardDir = Game.flattenVector(cc.forwardDir).Normalized();
        }
        else
        {
           if (cc.moveDir != Vector3.Zero) cc.forwardDir = cc.moveDir.Normalized();
        }
    }

    Vector3 aimAssist(Vector3 position, out bool valid)
    {
        valid = false;
        PhysicsShapeQueryParameters3D query = new PhysicsShapeQueryParameters3D();
        Transform3D queryTransform = query.Transform;
        queryTransform.Origin = position;
        query.Transform = queryTransform;
        SphereShape3D sphere = new SphereShape3D();
        sphere.Radius = cc.game.aimAssistRadius;
        query.Shape = sphere;

        var space = cc.sheet.GetWorld3D().DirectSpaceState;

        var result = space.IntersectShape(query);
        
        CharacterSheet target = null;
        foreach(Godot.Collections.Dictionary r in result)
        {
            if ((Node)r["collider"] is not CharacterSheet s || s == cc.sheet)
            {
                continue;
            }
             
            target = s;
            valid = true;
        }

        if (!valid) return Vector3.Zero;
        
        //INTERCEPT
        
        if (cc.wm.currWeapon.weaponType == Weapon.WeaponType.ranged && Game.Intercept(target.GlobalPosition, cc.sheet.GlobalPosition, target.Velocity, cc.wm.currWeapon.projectileSpeed, out Vector3 dir, out Vector3 pos) == 1)
        {
            var ray = Game.Raycast(cc.sheet, cc.sheet.GlobalPosition, pos, Game.GetBitMask(Game.world_layers));
            if (ray.Count != 0 && (Node3D)ray["collider"] != target)
            {
                return target.GlobalPosition;
            } 
            return pos;
        }
        else return target.GlobalPosition;
    }
}
