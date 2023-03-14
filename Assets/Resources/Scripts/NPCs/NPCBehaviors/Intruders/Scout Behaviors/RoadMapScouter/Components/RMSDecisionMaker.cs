using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RMSDecisionMaker
{
    private RoadMapScouterParams _params;

    public void Initiate(RoadMapScouterParams _params)
    {
        this._params = _params;
    }

    public HidingSpot GetBestSpot(List<HidingSpot> spots, float currentRisk, float maxSafeRisk)
    {
        bool isSafe = currentRisk <= maxSafeRisk;

        if (isSafe)
            switch (_params.goalPriority)
            {
                case GoalPriority.Safety:
                    SortGreedyGoalSpot(ref spots);
                    break;
                
                case GoalPriority.Weighted:
                    SortByWeight(ref spots, _params.safeWeights);
                    break;

            }
        else
            switch (_params.safetyPriority)
            {
                case SafetyPriority.Weighted:
                    SortByWeight(ref spots, _params.unsafeWeights);
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
                SortByWeight(ref spots, _params.unsafeWeights);
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
    private void SortByWeight(ref List<HidingSpot> spots, RoadMapScouterWeights weights)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.WeightedFitness(weights).CompareTo(x.WeightedFitness(weights));
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
    Safety,
    Weighted
}


public enum SafetyPriority
{
    Occlusion,
    GuardProximity,
    Weighted,
    ClosestWeightedSpot,
    Goal,
    Random
}