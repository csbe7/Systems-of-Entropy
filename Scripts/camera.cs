using Godot;
using System;

public partial class camera : Node3D
{
    Camera3D cam;
	[Export] Node3D center;

    public override void _Ready()
    {
        cam = GetNode<Camera3D>("Camera3D");

    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition = center.GlobalPosition;
    }

    public Godot.Collections.Dictionary ShootRayToMouse(uint collisionMask)
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
        var rayLen = 1000f;

		var from = cam.ProjectRayOrigin(mousePos);
		var to = from + (cam.ProjectRayNormal(mousePos) * rayLen);

		var space = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(from, to, collisionMask);
		var result = space.IntersectRay(query);

		if (result.Count == 0) return null;
		return result;
	}
}
