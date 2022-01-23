using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WorldStateController
{
    public static void LostTrackOfIntruder(Intruder intruder)
    {
        // Record the last guard to see the intruder
        float maxTimeStamp = Mathf.NegativeInfinity;
        string lastGuard = WorldState.EMPTY_VALUE;

        foreach (var guard in NpcsManager.Instance.GetGuards())
        {
            string header = "last_time_" + guard.name + "_saw_" + intruder.name;

            string state = WorldState.Get(header);

            if (Equals(state, WorldState.EMPTY_VALUE)) continue;

            float timeStamp = float.Parse(state);

            if (timeStamp > maxTimeStamp)
            {
                maxTimeStamp = timeStamp;
                lastGuard = guard.name;
            }
        }

        WorldState.Set("last_guard_to_see_intruder", lastGuard);
    }

    public static void UpdateRegions(NPC npc)
    {
        List<Vector2> path = npc.GetPath();

        string startRegion = RegionLabelsManager.GetRegion(npc.GetTransform().position);

        string goalRegion = !Equals(npc.GetGoal(), null)
            ? RegionLabelsManager.GetRegion(npc.GetGoal().Value)
            : WorldState.EMPTY_VALUE;

        // Get in between regions
        string middleRegion = WorldState.EMPTY_VALUE;
        foreach (var point in path)
        {
            string region = RegionLabelsManager.GetRegion(point);
            if (!Equals(region, WorldState.EMPTY_VALUE) && !Equals(goalRegion, region) && !Equals(startRegion, region))
            {
                middleRegion = region;
                break;
            }
        }

        // Get the normalized remaining distance to the goal
        float normalizedDistance = npc.GetRemainingDistanceToGoal() / Properties.MaxPathDistance;
        WorldState.Set(npc.name + "_path_distance", normalizedDistance.ToString());
        // Mark the regions the npc will pass through
        WorldState.Set(npc.name + "_start_region", startRegion);
        WorldState.Set(npc.name + "_middle_region", middleRegion);
        WorldState.Set(npc.name + "_goal_region", goalRegion);
    }

    public static void UpdateWorldStateForDialog()
    {
        foreach (var guard in NpcsManager.Instance.GetGuards())
            UpdateRegions(guard);
        
        foreach (var intruder in NpcsManager.Instance.GetIntruders())
            UpdateRegions(intruder);
    }
}