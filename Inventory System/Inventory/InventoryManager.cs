using Godot;
using System;

public partial class InventoryManager : Node
{
    Game game;
    camera cam;
    CollisionObject3D body;
    WeaponManager wm;
    [Export] Inventory inventory;
    
    Node Canvas;
    
    InventoryUI inventoryUI;
    InventoryUI externalInventoryUI;
    [Export] PackedScene inventoryScene;
    [Export] PackedScene quantitySelectionUI;
    QuantitySelector selector;

    [Export] PackedScene itemDrop;

    [Export] public float maxDistanceFromContainer;

    bool open;

    public SlotButton selectedSlot;

    public override void _Ready()
    {
        game = GetTree().Root.GetChild<Game>(0);
        cam = GetParent().GetParent().GetNode<camera>("%CameraPivot");
        body = GetParent<CollisionObject3D>();
        wm = GetNode<WeaponManager>("%WeaponManager");

        Canvas = GetParent().GetParent().GetNode("%CanvasLayer");
        inventoryUI = Canvas.GetNode<InventoryUI>("InventoryUI");
   
        inventoryUI.inventory = inventory;
        inventoryUI.wm = wm;

        inventoryUI.SlotHovered += OnSlotHovered;
        
        //inventoryUI.UpdateInventory();
        inventoryUI.Hide();
    }


    public override void _Process(double delta)
    {
        bool selectingQuantity = IsInstanceValid(selector);
        bool externalInventoryOpen = IsInstanceValid(externalInventoryUI);
        if (externalInventoryOpen && !IsInstanceValid(externalInventoryUI.inventory)) externalInventoryUI.QueueFree();
        
        if (open && inventory.hasBeenModified)
        {
            inventoryUI.UpdateInventory();
            inventory.hasBeenModified = false;
        } 
        //TOGGLE INVENTORY
        if (Input.IsActionJustPressed("ToggleInventory") && !selectingQuantity)
        {
            if (open)
            {
                game.SetState(Game.GameState.gameplay);
                inventoryUI.Hide();
                if (externalInventoryOpen) externalInventoryUI.QueueFree();
                open = false;
            }
            else{
                game.SetState(Game.GameState.menu);
                if (inventory.hasBeenModified)
                {
                    inventoryUI.UpdateInventory();
                    inventory.hasBeenModified = false;
                } 
                inventoryUI.Show();
                open = true;
            }
        }
        
        //DROP ITEM
        if (open && Input.IsActionJustPressed("DropItem") && !selectingQuantity && IsInstanceValid(selectedSlot) && selectedSlot.slotData.item.droppable && !externalInventoryOpen)
        {
            if (selectedSlot.slotData.amount == 1)
            {
                Drop(1);
                return;
            }
            if (Input.IsActionPressed("ModifierKey"))
            {
                Drop(selectedSlot.slotData.amount);
                return;
            }
            selector = quantitySelectionUI.Instantiate<QuantitySelector>();
            selector.min = 1;
            selector.max = selectedSlot.slotData.amount;
            selector.QuantityConfirmed += Drop;
            inventoryUI.AddChild(selector);
        }
        
        //EQUIP ITEM
        if (open && Input.IsActionJustPressed("EquipItem") && !selectingQuantity && IsInstanceValid(selectedSlot) && !externalInventoryOpen)
        {
            if (selectedSlot.slotData.item is Weapon)
            {
               if (wm.reloading) return;
               if (wm.currWeapon != null && wm.currWeaponSlotData != selectedSlot.slotData) EquippedToggle(FindSlotButton(wm.currWeaponSlotData));
               EquippedToggle(selectedSlot);
            }
        }

        //TRANSFER ITEM
        if (open && Input.IsActionJustPressed("MoveItem") && !selectingQuantity && IsInstanceValid(selectedSlot) && externalInventoryOpen)
        {
            if (selectedSlot.slotData.amount == 1)
            {
                Transfer(1);
                return;
            }
            if (Input.IsActionPressed("ModifierKey"))
            {
                Transfer(selectedSlot.slotData.amount);
                return;
            }
            selector = quantitySelectionUI.Instantiate<QuantitySelector>();
            selector.min = 1;
            selector.max = selectedSlot.slotData.amount;
            selector.QuantityConfirmed += Transfer;
            inventoryUI.AddChild(selector);
        }
        
        //OPEN CONTAINER
        if (!open && Input.IsActionJustPressed("OpenContainer"))
        {
            Godot.Collections.Dictionary result = cam.ShootRayToMouse((uint)Mathf.Pow(2, 31));
            if (result == null) return;
            foreach(var node in result)
            {
                if ((((Node3D)result["collider"]).GlobalPosition - body.GlobalPosition).Length() > maxDistanceFromContainer) continue;

                Inventory foundInventory = ((Node)result["collider"]).GetNodeOrNull<Inventory>("%Inventory");
                if (IsInstanceValid(foundInventory))
                {
                    Godot.Collections.Array<Rid> exclude = new Godot.Collections.Array<Rid>();
                    exclude.Add(body.GetRid());
                    exclude.Add(((CollisionObject3D)result["collider"]).GetRid());
                    if (Game.Raycast(body, body.GlobalPosition, ((Node3D)result["collider"]).GlobalPosition, 1,exclude).Count > 0) break;
                    
                    CreateExternalInventory(foundInventory);
                    game.SetState(Game.GameState.menu);
                    inventoryUI.Show();
                    open = true;
                    break;
                } 
            }
        }


    }
    

    void CreateExternalInventory(Inventory externalInventory)
    {
        if (IsInstanceValid(externalInventoryUI)) externalInventoryUI.QueueFree();
        

        externalInventoryUI = inventoryScene.Instantiate<InventoryUI>();
        externalInventoryUI.external = true;
        externalInventoryUI.inventory = externalInventory;
        
        externalInventoryUI.SlotHovered += OnSlotHovered;

        Canvas.AddChild(externalInventoryUI);
        
        externalInventoryUI.GlobalPosition = new Vector2(inventoryUI.GlobalPosition.X + 500, inventoryUI.GlobalPosition.Y);
        
        externalInventoryUI.UpdateInventory();
        externalInventoryUI.Show();
    }
    

    public void Drop(int amount)
    {
        if (IsInstanceValid(selectedSlot))
        { 
            if (wm.isEquipped(selectedSlot.slotData))
            {
                GD.Print(selectedSlot.slotData.item.name);
                EquippedToggle(selectedSlot);
            } 
            var drop = itemDrop.Instantiate<ItemDrop>();
            drop.inventory.AddItem(selectedSlot.slotData, Mathf.Min(amount, selectedSlot.slotData.amount));
            inventory.RemoveItem(selectedSlot.slotData, amount);

            GetTree().Root.AddChild(drop);
            drop.GlobalPosition = GetParent<Node3D>().GlobalPosition;
            drop.Pool();
                
            inventoryUI.UpdateInventory();
        }
    }

    public void Transfer(int amount)
    {
        if (!IsInstanceValid(selectedSlot) || !IsInstanceValid(externalInventoryUI)) return;
        
        if (wm.isEquipped(selectedSlot.slotData)) EquippedToggle(selectedSlot);

        Inventory source, destination;

        if (selectedSlot.external)
        {
            source = externalInventoryUI.inventory;
            destination = inventoryUI.inventory;
        }
        else
        {
            source = inventoryUI.inventory;
            destination = externalInventoryUI.inventory;
        }

        destination.AddItem(selectedSlot.slotData, amount);
        source.RemoveItem(selectedSlot.slotData, amount);
        
        externalInventoryUI.UpdateInventory();
        inventoryUI.UpdateInventory();
    }
    

    public void OnSlotHovered(SlotButton slot)
    {
        if (IsInstanceValid(selector)) return;

        Color newColor;
        
        if (IsInstanceValid(selectedSlot))
        {
            newColor = selectedSlot.bg.Color;
            newColor.A -= 0.2f;
            selectedSlot.bg.Color = newColor;
        }
        
        selectedSlot = slot;
        newColor = selectedSlot.bg.Color;
        newColor.A += 0.2f;
        selectedSlot.bg.Color = newColor;
    }
   
   
    public SlotButton FindSlotButton(SlotData toFind)
    {
        var grid =inventoryUI.GetNode<GridContainer>("%GridContainer");
        foreach (Node child in grid.GetChildren())
        {
            SlotButton slotB = child.GetNode<SlotButton>("%SlotButton");
            if (slotB.slotData == toFind)
            {
               return slotB;
            }
        }
        return null;
    }

    public void EquippedToggle(SlotButton slot)
    {
        if (slot == null) return;

        if (wm.isEquipped(slot.slotData))
        {
            GD.Print("Weapon equipped");
            slot.GetNode<Label>("%item_name").Text = slot.slotData.item.name;
            wm.UnequipWeapon();
        }
        else
        {
            slot.GetNode<Label>("%item_name").Text = "*" + slot.slotData.item.name;
            wm.EquipWeapon(slot.slotData);
       }
    }

}
