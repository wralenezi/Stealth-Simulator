using System.Collections.Generic;
using UnityEngine;

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
        if (currentRisk <= maxSafeRisk)
            switch (_goalPriority)
            {
                case GoalPriority.Safety:
                    SortGreedyGoalSpot(ref spots);
                    break;
            }
        else
            switch (_safetyPriority)
            {
                case SafetyPriority.GetWeightedCostVsGuardDistance:
                    GreedySafeSpot(ref spots);
                    break;
                

                case SafetyPriority.Occlusion:
                    GreedySafeOccludedSpot(ref spots);
                    break;

                case SafetyPriority.GuardProximity:
                    GreedySafeDistantSpot(ref spots);
                    break;

                case SafetyPriority.Random:
                    GetRandomSafetyMethod(ref spots);
                    break;
            }

        foreach (var spot in spots)
        {
            if(spot.IsAlreadyChecked()) continue;

            return spot;
        }
        
        // if(spots.Count > 0) Debug.Log("No new goals");

        return null;
    }


    private void GetRandomSafetyMethod(ref List<HidingSpot> spots)
    {
        int totalMethods = 3;
        float randomValue = Random.Range(1, totalMethods);

        switch (randomValue)
        {
            case 1:
                GreedySafeSpot(ref spots);
                break;

            case 2:
                GreedySafeOccludedSpot(ref spots);
                break;

            case 3:
                GreedySafeDistantSpot(ref spots);
                break;
        }
    }


    // Goal oriented methods

    private void SortGreedyGoalSpot(ref List<HidingSpot> spots)
    {
        // Sort by closeness to 
        spots.Sort((x, y) =>
        {
            int ret = y.GoalUtility.CompareTo(x.GoalUtility);
            return ret != 0 ? ret : x.Risk.CompareTo(y.Risk);
        });

        // return spots.Count > 0 ? spots[0] : null;
    }


    // Safety oriented methods

    private void GreedySafeSpot(ref List<HidingSpot> spots)
    {
        float costWeight = 0.6f;
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.DeadEndProximity.CompareTo(x.DeadEndProximity);
            if (ret != 0) return ret;
            // ret = x.CostUtility.CompareTo(y.CostUtility);
            // if (ret != 0) return ret;
            ret = y.GetWeightedCostVsGuardDistance(costWeight).CompareTo(x.GetWeightedCostVsGuardDistance(costWeight));
            return ret;
        });

        // return spots.Count > 0 ? spots[0] : null;
    }


    private void GreedySafeOccludedSpot(ref List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.DeadEndProximity.CompareTo(x.DeadEndProximity);
            if (ret != 0) return ret;
            ret = y.OcclusionUtility.CompareTo(x.OcclusionUtility);
            return ret;
        });

        // return spots.Count > 0 ? spots[0] : null;
    }

    private void GreedySafeDistantSpot(ref List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.Risk.CompareTo(y.Risk);
            if (ret != 0) return ret;
            ret = y.DeadEndProximity.CompareTo(x.DeadEndProximity);
            if (ret != 0) return ret;
            ret = y.GuardProximityUtility.CompareTo(x.GuardProximityUtility);
            return ret;
        });

        // return spots.Count > 0 ? spots[0] : null;
    }


    private HidingSpot GetSafestSpot(List<HidingSpot> spots)
    {
        // Sorted in Asc order
        spots.Sort((x, y) => x.Risk.CompareTo(y.Risk));

        float minCost = Mathf.Infinity;
        HidingSpot bestSpot = null;

        foreach (var hs in spots)
        {
            if (minCost > hs.CostUtility)
            {
                bestSpot = hs;
                minCost = hs.CostUtility;
            }
        }

        return bestSpot;
    }


    private HidingSpot GetBestSpot_Simple(List<HidingSpot> spots)
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in spots)
        {
            hs.Fitness = hs.Risk;

            hs.Fitness = Mathf.Round(hs.Fitness * 10f) * 0.01f;

            if (!(maxFitness < hs.Fitness)) continue;

            bestHs = hs;
            maxFitness = hs.Fitness;
        }

        return bestHs;
    }


    /// <summary>
    /// Get the best hiding spots by filtering them through each utility sequentially
    /// </summary>
    /// <returns>The best hiding spot</returns>
    private HidingSpot GetClosestToGoalSafeSpot(List<HidingSpot> spots, float maxAcceptedRisk)
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;

        foreach (var hs in spots)
        {
            if (hs.Risk >= maxAcceptedRisk) continue;

            if (maxFitness >= hs.GoalUtility) continue;

            bestHs = hs;
            maxFitness = hs.GoalUtility;
        }

        return bestHs;
    }


    private HidingSpot GetClosestToGoalSafeSpotNew(List<HidingSpot> spots, float maxAcceptedRisk)
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;

        foreach (var hs in spots)
        {
            if (hs.Risk >= maxAcceptedRisk) continue;
            if (maxFitness > hs.GoalUtility) continue;
            // // if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 1f)
            // {
            //     // Debug.Log("Not ready");
            //     continue;
            // }

            bestHs = hs;
            maxFitness = hs.GoalUtility;
        }

        return bestHs;
    }


    private HidingSpot GetClosestCheapestToGoalSafeSpot(List<HidingSpot> spots, float maxAcceptedRisk)
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;

        spots.Sort((x, y) => y.CoverUtility.CompareTo(x.CoverUtility));

        foreach (var hs in spots)
        {
            if (hs.Risk >= maxAcceptedRisk) continue;

            float fitness = hs.GoalUtility;

            if (maxFitness >= fitness) continue;

            bestHs = hs;
            maxFitness = fitness;
        }

        return bestHs;
    }

    private HidingSpot GetSafeSpot(List<HidingSpot> spots)
    {
        Intruder intruder = NpcsManager.Instance.GetIntruders()[0];
        float MaxDistance = PathFinding.Instance.longestShortestPath * 0.4f;

        spots.Sort((x, y) => x.Risk.CompareTo(y.Risk));

        HidingSpot bestSpot = null;

        foreach (var hs in spots)
        {
            // if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 1f) continue;

            if (hs.Risk > RMRiskEvaluator.Instance.GetRisk()) continue;

            float distance =
                PathFinding.Instance.GetShortestPathDistance(intruder.GetTransform().position, hs.Position);

            if (distance > MaxDistance) continue;

            bestSpot = hs;
        }

        return bestSpot;
    }


    private HidingSpot GetSafestToGoalSpot(List<HidingSpot> spots)
    {
        // Sorted in Asc order
        spots.Sort((x, y) => x.Risk.CompareTo(y.Risk));

        int firstQuarter = Mathf.FloorToInt(spots.Count * 0.5f);

        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;
        for (int i = 0; i <= firstQuarter; i++)
        {
            HidingSpot hs = spots[i];

            float fitness = hs.GoalUtility;

            if (maxFitness >= fitness) continue;

            bestHs = hs;
            maxFitness = fitness;
        }

        return bestHs;
    }

    // // List of curves to determine how utilities are mapped.
    // [SerializeField] private AnimationCurve _SafetyCurve;

    // private void SetCurves()
    // {
    //     SetSafetyCurve();
    // }
    //
    // private void SetSafetyCurve()
    // {
    //     _SafetyCurve = new AnimationCurve();
    //
    //     for (float i = 0; i < 1; i += 0.1f)
    //     {
    //         float y = (i <= 0.5) ? i * 0.1f : i;
    //         float x = i;
    //         Keyframe keyframe = new Keyframe(x, y);
    //         _SafetyCurve.AddKey(keyframe);
    //     }
    // }
}

public enum GoalPriority
{
    Safety,
    None
}


public enum SafetyPriority
{
    Occlusion,
    GuardProximity,
    GetWeightedCostVsGuardDistance,
    Random,
    None
}