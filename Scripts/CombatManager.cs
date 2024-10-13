using Godot;
using System;

public partial class CombatManager : Node
{
    public static string LoSRequest = "find line of sight";
    public static string CoverRequest = "find cover";

    public Godot.Collections.Array<CharacterSheet> combatants = new Godot.Collections.Array<CharacterSheet>(); 
    
    [Signal] public delegate void CombatantAddedEventHandler(CharacterSheet combatant);
    [Signal] public delegate void CombatantRemovedEventHandler(CharacterSheet combatant);

    Godot.Collections.Dictionary<NPC_AI, string> pathfindingRequests = new Godot.Collections.Dictionary<NPC_AI, string>();

    public bool AddCombatant(CharacterSheet combatant)
    {
        if (!combatants.Contains(combatant))
        {
            combatants.Add(combatant);
            EmitSignal(SignalName.CombatantAdded, combatant);
            return true;
        }
        return false;
    }

    public bool RemoveCombatant(CharacterSheet combatant)
    {
        if (combatants.Contains(combatant))
        {
            combatants.Remove(combatant);
            EmitSignal(SignalName.CombatantRemoved, combatant);
            return true;
        }
        return false;
    }
    

    public override async void _PhysicsProcess(double delta)
    {
        SynchronizeRequests();
    }
    
    public void RequestPathfinding(NPC_AI requester, string request)
    { 
        if (pathfindingRequests.ContainsKey(requester)) pathfindingRequests[requester] = request;
        else
        {
            pathfindingRequests.Add(requester, request);
        }
    }

    public async void SynchronizeRequests()
    {
        foreach(NPC_AI requester in pathfindingRequests.Keys)
        {
            if (!IsInstanceValid(requester) || !pathfindingRequests.ContainsKey(requester)) continue;

            if (pathfindingRequests[requester] == LoSRequest)
            {
                requester.targetPoint = await EnvironmentQuery.FindPointWithLineOfSight(requester.target.GlobalPosition, requester);
            }
            else if (pathfindingRequests[requester] == CoverRequest)
            {
                requester.targetPoint = await EnvironmentQuery.FindCover(requester.enemies, requester);
                //GD.Print(requester.targetPoint.point);
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        
        pathfindingRequests.Clear();
    }

 
}
