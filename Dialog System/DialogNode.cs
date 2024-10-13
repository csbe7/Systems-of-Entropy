using Godot;
using System;


public partial class DialogNode : GraphNode
{
    [Export] int slotNumberRight;
    [Export] int slotNumberLeft;

    public override void _Ready()
    {
        int j = 0;
        if (slotNumberRight > slotNumberLeft)
        {
            for (int i = 0; i < slotNumberRight; i++)
            {
                Control slot = new Control();
                AddChild(slot);
                if (j < slotNumberLeft)
                {
                  SetSlot(i, true, 0, new Color(1,1,1,1), true, 0, new Color(1,1,1,1));
                  j++;
                }
                else SetSlot(i, false, 0, new Color(1,1,1,1), true, 0, new Color(1,1,1,1));
           }
        }
        else
        {
            for (int i = 0; i < slotNumberLeft; i++)
            {
                Control slot = new Control();
                AddChild(slot);
                if (j < slotNumberRight)
                {
                   SetSlot(i, true, 0, new Color(1,1,1,1), true, 0, new Color(1,1,1,1));
                   j++;
                }
                else SetSlot(i, true, 0, new Color(1,1,1,1), false, 0, new Color(1,1,1,1));
           }
        }
    }
}
