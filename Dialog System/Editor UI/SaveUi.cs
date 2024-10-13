using Godot;
using System;

public partial class SaveUi : Control
{
    [Export] TextEdit textEdit;
    [Signal] public delegate void SaveEventHandler(string scene_name);

    public void OnSave()
    {
        if (textEdit.Text == "")
        {
            GD.PrintErr("Must name the dialogue tree");
            return;
        }
        else{
            SetOwner(null);
            EmitSignal(SignalName.Save, textEdit.Text);
            QueueFree();
        }
    }

    public void OnCancel()
    {
        QueueFree();
    }
}
