using Godot;
using System;

public partial class DialogEditor : GraphEdit
{
    [Export] string TreeSavePath;
    [Export] PackedScene NodePicker;
    Control nodePicker;
    
    [Export] Godot.Collections.Array<DialogTransitionData> nodeTransitions = new Godot.Collections.Array<DialogTransitionData>();

    Godot.Collections.Array<DialogNode> selectedNodes = new Godot.Collections.Array<DialogNode>();

    public override void _Ready()
    {
        /*foreach(var transition in nodeTransitions)
        {
            GD.Print("connection");
            ConnectNode(transition.fromNode, transition.fromPort, transition.toNode, transition.toPort);
        }*/
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("Right Click"))
        {
            if (IsInstanceValid(nodePicker)) nodePicker.QueueFree();
            Vector2 mousePos = GetViewport().GetMousePosition();
            nodePicker = NodePicker.Instantiate<Control>();
            AddChild(nodePicker);
            nodePicker.GlobalPosition = mousePos;
        }
    }

    public void OnConnectionRequest(string fromNode, int fromPort, string toNode, int toPort)
    {
        var transition = new DialogTransitionData();
        transition.fromNode = fromNode;
        transition.fromPort = fromPort;
        transition.toNode = toNode;
        transition.toPort = toPort; 
        nodeTransitions.Add(transition);
        ConnectNode(fromNode, fromPort, toNode, toPort);
    }

    public void OnDisconnectionRequest(string fromNode, int fromPort, string toNode, int toPort)
    {
        DisconnectNode(fromNode, fromPort, toNode, toPort);
    }

    public void OnNodeSelected(Node node)
    {
        if (node is DialogNode dialogNode)
        {
            selectedNodes.Add(dialogNode);
        }
    }

    public void OnNodeDeselected(Node node)
    {
        if (node is DialogNode dialogNode)
        {
            selectedNodes.Remove(dialogNode);
        }
    }

    public void OnDeleteNode()
    {
        foreach(DialogNode node in selectedNodes)
        {
            if (IsInstanceValid(node)) node.QueueFree();
        }
        selectedNodes.Clear();
    }

    public void SaveTree(string tree_name)
    {
        GD.Print("Saving");
        SetOwnerRecursive(this, this);
        var scene = new PackedScene();
        scene.Pack(this);
        ResourceSaver.Save(scene, TreeSavePath + "/" + tree_name + ".tscn");
    }

    void SetOwnerRecursive(Node root, Node owner)
    {
        foreach(var child in root.GetChildren())
        {
            if (child.Name.ToString().Contains("_connection_layer") || child is SaveUi || child is DialogNodePicker)
            {
                child.SetOwner(null);
                continue;
            }

            child.SetOwner(owner);
            SetOwnerRecursive(child, owner);
        }
    }

}
