using System.Collections.Generic;
using UnityEngine;

public class RMSDecisionMaker //: MonoBehaviour
{
    public HidingSpot GetBestSpot(List<HidingSpot> spots, float currentRisk)
    {
        // if (currentRisk < 0.5f)
        // return GetClosestToGoalSafeSpot(0.5f);
        // return GetClosestToGoalSafeSpotNew(spots, 0.5f);

        // if (currentRisk < 0.5f)
        //     // return GetClosestToGoalSafeSpotNew(spots, 0.5f);
        //     return GreedyGoalSpot(spots);

        return GreedySafeSpot(spots);
        // return GetClosestCheapestToGoalSafeSpot(0.5f);
        // return GetSafestToGoalSpot();
        // return GetBestSpot_Simple();

        // return GetSafestSpot();
    }


    private HidingSpot GetBestSpot_Simple(List<HidingSpot> spots)
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in spots)
        {
            hs.Fitness = hs.RiskLikelihood;

            hs.Fitness = Mathf.Round(hs.Fitness * 10000f) * 0.0001f;

            if (!(maxFitness < hs.Fitness)) continue;

            bestHs = hs;
            maxFitness = hs.Fitness;
        }

        return bestHs;
    }


    private HidingSpot GreedyGoalSpot(List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = y.GoalUtility.CompareTo(x.GoalUtility);
            return ret != 0 ? ret : x.RiskLikelihood.CompareTo(y.RiskLikelihood);
        });

        foreach (var hs in spots)
        {
            // if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 1f) continue;
            // if (hs.RiskLikelihood > ScoutRiskEvaluator.Instance.GetRisk()) continue;

            return hs;
        }

        return null;
    }


    private HidingSpot GreedySafeSpot(List<HidingSpot> spots)
    {
        spots.Sort((x, y) =>
        {
            int ret = x.RiskLikelihood.CompareTo(y.RiskLikelihood);
            return ret != 0 ? ret : x.CostUtility.CompareTo(y.CostUtility);
        });

        foreach (var hs in spots)
        {
            // if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 1f) continue;
            // if (hs.RiskLikelihood > ScoutRiskEvaluator.Instance.GetRisk()) continue;

            return hs;
        }

        return null;
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
            if (hs.RiskLikelihood >= maxAcceptedRisk) continue;

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
            if (hs.RiskLikelihood >= maxAcceptedRisk) continue;
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
            if (hs.RiskLikelihood >= maxAcceptedRisk) continue;

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

        spots.Sort((x, y) => x.RiskLikelihood.CompareTo(y.RiskLikelihood));

        HidingSpot bestSpot = null;

        foreach (var hs in spots)
        {
            if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 1f) continue;

            if (hs.RiskLikelihood > ScoutRiskEvaluator.Instance.GetRisk()) continue;

            float distance =
                PathFinding.Instance.GetShortestPathDistance(intruder.GetTransform().position, hs.Position);

            if (distance > MaxDistance) continue;

            bestSpot = hs;
        }

        return bestSpot;
    }


    private HidingSpot GetSafestSpot(List<HidingSpot> spots)
    {
        // Sorted in Asc order
        spots.Sort((x, y) => x.RiskLikelihood.CompareTo(y.RiskLikelihood));

        float minCost = Mathf.Infinity;
        HidingSpot bestSpot = null;

        foreach (var hs in spots)
        {
            if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 0.05f) continue;

            if (minCost > hs.CostUtility)
            {
                bestSpot = hs;
                minCost = hs.CostUtility;
            }
        }

        return bestSpot;
    }

    private HidingSpot GetSafestToGoalSpot(List<HidingSpot> spots)
    {
        // Sorted in Asc order
        spots.Sort((x, y) => x.RiskLikelihood.CompareTo(y.RiskLikelihood));

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
}