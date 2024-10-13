using Godot;
using System;

public partial class SaveButton : Button
{
    [Export] PackedScene SaveUI;

    public void OnButtonDown()
    {
        var ui = SaveUI.Instantiate<SaveUi>();
        
        var editor = GetParent<DialogEditor>();

        ui.Save += editor.SaveTree;

        editor.AddChild(ui);

    }
}
