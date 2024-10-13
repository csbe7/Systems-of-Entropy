using Godot;
using System;

public partial class DialogTextNode : DialogNode
{
    public override async void _Ready()
    {
        base._Ready();
        SetProcess(false);
        SetPhysicsProcess(false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SetProcess(true);
        SetPhysicsProcess(true);

        var textEdit = GetNodeOrNull<TextEdit>("TextEdit");
        var textEdit2 = GetNodeOrNull<TextEdit>("TextEdit2");

        if (IsInstanceValid(textEdit2))
        {
            textEdit.QueueFree();

            textEdit2.Name = "TextEdit";
            textEdit2.Position = textEdit.Position;
            Size = new Vector2(textEdit2.Size.X, textEdit2.Size.Y - 16);
        }
    }
}
