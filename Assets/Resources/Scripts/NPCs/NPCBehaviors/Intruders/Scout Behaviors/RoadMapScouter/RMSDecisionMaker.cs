using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RMSDecisionMaker //: MonoBehaviour
{
    public HidingSpot GetBestSpot(List<HidingSpot> spots, List<HidingSpot> allSpots, float currentRisk)
    {
        if (currentRisk < 0.5f)
            // return GetClosestToGoalSafeSpot(0.5f);
            return GetClosestToGoalSafeSpotNew(spots, 0.5f);
        // return GetClosestCheapestToGoalSafeSpot(0.5f);
        // return GetSafestToGoalSpot();
        // return GetBestSpot_Simple();

        // return GetSafestSpot();
        return GetSafeSpot(allSpots);
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
            if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 0.05f)
            {
                // Debug.Log("Not ready");
                continue;
            }

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

    private HidingSpot GetSafeSpot(List<HidingSpot> spot)
    {
        float minCost = Mathf.Infinity;
        HidingSpot bestSpot = null;

        foreach (var hs in spot)
        {
            if (hs.RiskLikelihood < 1f) continue;
            if (StealthArea.GetElapsedTime() - hs.lastFailedTimeStamp < 0.05f) continue;

            if (minCost > hs.CostUtility)
            {
                bestSpot = hs;
                minCost = hs.CostUtility;
            }
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
