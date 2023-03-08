using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RMSDecisionMaker
{
    private GoalPriority _goalPriority;
    private SafetyPriority _safetyPriority;

    public void Initiate(GoalPriority goalPriority, SafetyPriority safetyPriority)
    {
        _goalPriority = goalPriority;
        _safetyPriority = safetyPriority;
    }

    public HidingSpot GetBestSpot(List<HidingSpot> spots, float currentRisk, float maxSafeRisk)
    {
        bool isSafe = currentRisk <= maxSafeRisk;

        if (isSafe)
            switch (_goalPriority)
            {
                case GoalPriority.Safety:
                    SortGreedyGoalSpot(ref spots);
                    break;
            }
        else
            switch (_safetyPriority)
            {
                case SafetyPriority.WeightedSpot:
                    GreedySafeSpot(ref spots);
                    break;

                case SafetyPriority.ClosestWeightedSpot:
                    GreedyClosestSafeSpot(ref spots);
                    break;

                case SafetyPriority.Occlusion:
                    GreedySafeOccludedSpot(ref spots);
                    break;

                case SafetyPriority.GuardProximity:
                    GreedySafeDistantSpot(ref spots);
                    break;

                case SafetyPriority.Goal:
                    SortGreedyGoalSpot(ref spots);
                    break;

                case SafetyPriority.Random:
                    GetRandomSafetyMethod(ref spots);
                    break;
            }

        foreach (var spot in spots)
        {
            if (spot.IsAlreadyChecked()) continue;

            return spot;
        }

        return null;
    }


    private void GetRandomSafetyMethod(ref List<HidingSpot> spots)
    {
        int totalMethods = 4;
        float randomValue = Random.Range(0, totalMethods);

        switch (randomValue)
        {
            case 0:
                GreedySafeSpot(ref spots);
                break;

            case 1:
                GreedySafeOccludedSpot(ref spots);
                break;

            case 2:
                GreedySafeDistantSpot(ref spots);
                break;

            case 3:
                SortGreedyGoalSpot(ref spots);
                break;
        }
    }


    // Goal oriented methods
    private void SortGreedyGoalSpot(ref List<HidingSpot> spots)
    {
        // Sort by closeness to 
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.GoalUtility.CompareTo(x.GoalUtility);
            if (ret != 0) return ret;
            return y.CoverUtility.CompareTo(x.CoverUtility);
        });
    }


    // Safety oriented methods
    private void GreedySafeSpot(ref List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.WeightedFitness().CompareTo(x.WeightedFitness());
            return ret;
        });
    }

    private void GreedyClosestSafeSpot(ref List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.CostUtility.CompareTo(x.CostUtility);
            if (ret != 0) return ret;
            ret = y.WeightedFitness().CompareTo(x.WeightedFitness());
            return ret;
        });
    }


    private void GreedySafeOccludedSpot(ref List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.WeightedOcclusion().CompareTo(x.WeightedOcclusion());
            return ret;
        });
    }

    private void GreedySafeDistantSpot(ref List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.GuardProximityUtility.CompareTo(x.GuardProximityUtility);
            return ret;
        });
    }
}

public enum GoalPriority
{
    Safety
}


public enum SafetyPriority
{
    Occlusion,
    GuardProximity,
    WeightedSpot,
    ClosestWeightedSpot,
    Goal,
    Random
}