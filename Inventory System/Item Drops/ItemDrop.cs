using Godot;
using System;

public partial class ItemDrop : StaticBody3D
{
    [Export] public Inventory inventory;

    [Export] public bool pool;
    [Export] public float poolRadius;

    public override void _Ready()
    {
        inventory = GetNode<Inventory>("%Inventory");
        
        inventory.ItemRemoved += OnItemRemoved;

        InventoryData newInv = new InventoryData();
        if (IsInstanceValid(inventory.inv))
        {
            Godot.Collections.Array<SlotData> copy = new Godot.Collections.Array<SlotData>();
            foreach(SlotData slot in inventory.inv.items)
            {
                if (IsInstanceValid(slot)) copy.Add((SlotData)slot.Duplicate());
            }
            newInv.items = copy;

            for (int i = inventory.inv.items.Count -1; i >= 0; i--)
            {
                inventory.inv.items.RemoveAt(i);
            }
        } 
        inventory.inv = newInv;
        
    }

    //[Export] public Item testItem;
    

    public void Pool()
    {
        if (pool)
        {
            PhysicsShapeQueryParameters3D query = new PhysicsShapeQueryParameters3D();
            query.Shape = new SphereShape3D();
            ((SphereShape3D)query.Shape).Radius = poolRadius;
            query.Transform = this.Transform;
            query.CollisionMask = (uint)Mathf.Pow(2, 31);

            var space = GetWorld3D().DirectSpaceState;
            var result = space.IntersectShape(query);
            foreach (var body in result)
            {
                Node bodyNode = (Node)body["collider"];
                
                if (bodyNode.IsInGroup("Item Drop") && (bodyNode != this))
                {
                    GD.Print("DROP FOUND");
                    ItemDrop drop = (ItemDrop)bodyNode;
                    if (!drop.pool) return;
                    foreach (SlotData slot in inventory.inv.items)
                    {
                        drop.inventory.AddItem(slot, slot.amount);
                    }
                    QueueFree();
                    break;
                }
            }
        }
    }
    

    public void OnItemRemoved(SlotData slot, int amount)
    {
        if (inventory.isEmpty()) QueueFree();
    }


}
