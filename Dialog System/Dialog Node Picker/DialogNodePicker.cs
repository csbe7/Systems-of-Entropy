using Godot;
using System;

public partial class DialogNodePicker : Control
{
    [Export] Godot.Collections.Array<PackedScene> dialogNodes = new Godot.Collections.Array<PackedScene>();
    [Export] PackedScene PickerButton;

    public override void _Ready()
    {
        Control grid = GetNode<Control>("%GridContainer");
        foreach(PackedScene node in dialogNodes)
        {
            var dialogNode = node.Instantiate<GraphNode>();
            
            var button = PickerButton.Instantiate<DialogNodePickerButton>();
            button.DialogNode = node;
            var label = button.GetNode<Label>("Label");
            label.Text = "Add " + dialogNode.Title + " node";
            button.NodePicked += OnNodePicked;
            grid.AddChild(button);
            dialogNode.QueueFree();
        }
    }

    public void OnNodePicked(PackedScene node)
    {
        var dialogNode = node.Instantiate<GraphNode>();
        var editor = GetParent<DialogEditor>();
        GetParent().AddChild(dialogNode);
        dialogNode.PositionOffset = (GlobalPosition - new Vector2(578, 324)) - editor.ScrollOffset;
        //dialogNode.SetEditableInstance(dialogNode, true);
        //GD.Print(dialogNode.GlobalPosition);
        QueueFree();
    }
    
    public void OnClose()
    {
        QueueFree();
    }


}
