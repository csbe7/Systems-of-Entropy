using Godot;
using System;

public partial class InventoryUI : Control
{ 
    public WeaponManager wm;

    public bool external;
    public Inventory inventory;
    [Export] PackedScene slotUI;
    
    TabManager tabManager;
    TabButton currTab;

    [Signal] public delegate void SlotHoveredEventHandler(SlotButton slot);
    [Signal] public delegate void SlotClickedEventHandler(SlotButton slot);

    public override void _Ready()
    {
        Hide();
        tabManager = GetNode<TabManager>("%Tab Manager");
        currTab = tabManager.tabs[0];
        tabManager.TabChanged += OnTabChanged;
        UpdateInventory();
    }



    public void UpdateInventory()
    {
        Control grid = GetNode<Control>("%GridContainer");
        foreach (Node child in grid.GetChildren())
        {
            child.GetNode<SlotButton>("%SlotButton").SlotHovered -= OnSlotHovered;
            child.QueueFree();
        }

        
        foreach (SlotData slot in inventory.inv.items)
        {
            if (currTab.itemType != Item.Type.none && slot.item.type != currTab.itemType) continue;

            Control slotBox = slotUI.Instantiate<Control>();
            grid.AddChild(slotBox);
            if (IsInstanceValid(wm) && wm.isEquipped(slot)) slotBox.GetNode<Label>("%item_name").Text = "*" + slot.item.name;
            else slotBox.GetNode<Label>("%item_name").Text = slot.item.name;

            if (slot.amount > 1) slotBox.GetNode<Label>("%item_amount").Text = ("x" + slot.amount.ToString());
            else slotBox.GetNode<Label>("%item_amount").Text = "";
            
            SlotButton button = slotBox.GetNode<SlotButton>("%SlotButton");
            button.external = external;
            button.SlotHovered += OnSlotHovered;
            button.SlotClicked += OnSlotClicked;
            button.slotData = slot;
        }
    }

    public void OnSlotHovered(SlotButton slot)
    {
        EmitSignal(SignalName.SlotHovered, slot);
    }

    public void OnSlotClicked(SlotButton slot)
    {
        EmitSignal(SignalName.SlotClicked, slot);
    }

    public void OnTabChanged(TabButton newTab)
    {
        if (currTab != newTab)
        {
            currTab = newTab;
            UpdateInventory();
        }
    }
}
