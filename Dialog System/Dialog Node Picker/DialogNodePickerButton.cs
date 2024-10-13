using Godot;
using System;

public partial class DialogNodePickerButton : Button
{
    public PackedScene DialogNode;

    [Signal] public delegate void NodePickedEventHandler(PackedScene node);

    public override void _Ready()
    {
        ButtonDown += OnButtonDown;
    }

    private void OnButtonDown()
    {
        EmitSignal(SignalName.NodePicked, DialogNode);
    }

}
