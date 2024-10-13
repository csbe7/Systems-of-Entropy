using Godot;
using System;

public partial class SlotButton : Button
{
    public bool external;
    public SlotData slotData;
    public ColorRect bg;


    [Signal] public delegate void SlotClickedEventHandler(SlotButton slot);
    [Signal] public delegate void SlotHoveredEventHandler(SlotButton slot);
    
    public override void _Ready()
    {   
        bg = GetNode<ColorRect>("%Background");
 
        ButtonDown += OnButtonDown;
        MouseEntered += OnMouseEntered;

        MouseExited += OnMouseExited;
    }

    public void OnButtonDown()
    {
        EmitSignal(SignalName.SlotClicked, this);
    }

    public void OnMouseEntered()
    {
        EmitSignal(SignalName.SlotHovered, this);
    }

    public void OnMouseExited()
    {
        
    }

}
