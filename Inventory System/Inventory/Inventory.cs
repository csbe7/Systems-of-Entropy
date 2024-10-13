using Godot;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class Inventory : Node
{
    [Export] public InventoryData inv;

    public bool hasBeenModified;
    
    [Signal] public delegate void ItemAddedEventHandler(SlotData slot, int amount);
    [Signal] public delegate void ItemRemovedEventHandler(SlotData slot, int amount);
    

    public void AddItem(SlotData passedSlot, int amount)
    {
        if (passedSlot.item.maxStack == 1)
        {
            for (int i = 0; i < amount; i++)
            {
                inv.items.Add((SlotData)passedSlot.Duplicate());
            }
            
            EmitSignal(SignalName.ItemAdded, passedSlot, amount);
            hasBeenModified = true;
            return;
        }

        int originalAmount = amount;
        Item item = passedSlot.item;
        foreach (SlotData slot in inv.items)
        {
           if (slot.item == item && slot.amount < item.maxStack)
           {
               int spaceLeft = item.maxStack - slot.amount;
               int toAdd = Mathf.Min(amount, spaceLeft);
               slot.amount += toAdd;
               amount -= toAdd;
            
               if (amount <= 0)
               {
                   EmitSignal(SignalName.ItemAdded, slot, originalAmount);
                   return;
               } 
           }
        }
 

        while(amount > 0)
        {
            SlotData newSlot = new SlotData();
            int toAdd = Mathf.Min(amount, item.maxStack);
            newSlot.item = item;
            newSlot.amount = toAdd;
            amount -= toAdd;

            inv.items.Add(newSlot);
            EmitSignal(SignalName.ItemAdded, newSlot, originalAmount);
        }
    }


    public int RemoveItem(SlotData toRemove, int amount)
    {
        foreach(SlotData slot in inv.items)
        {
            if (slot == toRemove)
            {
               slot.amount -= amount;
               if (slot.amount <= 0)
               {
                   inv.items.Remove(slot);
               }
               EmitSignal(SignalName.ItemRemoved, slot, amount);
               hasBeenModified = true;
               return 1;
            }
        }

        return 0;
    }


    public int RemoveItemPos(int index, int amount)
    {
        if (index > inv.items.Count-1) return 0;
        SlotData slot = inv.items[index];
        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            inv.items.RemoveAt(index);
        }
        EmitSignal(SignalName.ItemRemoved, slot, amount);
        hasBeenModified = true;
        return 1;

    }
    
    public SlotData FindItemByName(string name)
    {
        foreach(SlotData slot in inv.items)
        {
            if (slot.item.name == name) return slot;
        }
        return null;
    }

    public bool isEmpty()
    {
        return (inv.items.Count == 0);
    }
}
