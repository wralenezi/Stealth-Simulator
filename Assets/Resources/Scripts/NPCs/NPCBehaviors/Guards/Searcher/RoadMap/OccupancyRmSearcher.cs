using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OccupancyRmSearcher : RoadMapSearcher
{
    public override void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
        if (isStillCheating) return;
        float timeBefore = Time.realtimeSinceStartup;
        UpdateSearch(speed, guards, timeDelta);
        Updated = (Time.realtimeSinceStartup - timeBefore);
    }

    // The probability is diffused, similar to Third eye crime
    private void UpdateSearch(float speed, List<Guard> guards, float timeDelta)
    {
        float maxProbability = Mathf.NegativeInfinity;

        foreach (var line in _RoadMap.GetLines(false))
        {
            DiffuseProb(line);
            line.ExpandSs(speed, timeDelta);

            CheckSeenSs(guards, line);

            float prob = line.GetSearchSegment().GetProbability();

            if (maxProbability < prob) maxProbability = prob;
        }

        // Cheat if they lost track by spreading the location
        if (float.IsNegativeInfinity(maxProbability)) CommenceSearch(m_Intruder);

        NormalizeSegments(maxProbability);
    }

    // Diffusing the probability among neighboring segments 
    // Source: EXPLORATION AND COMBAT IN NETHACK - Johnathan Campbell - Chapter 2.2.1
    public void DiffuseProb(RoadMapLine line)
    {
        SearchSegment sS = line.GetSearchSegment();
        float probabilitySum = 0f;
        int neighborsCount = 0;

        foreach (var con in line.GetWp1Connections())
            if (line != con)
            {
                float normalizedAge = con.GetSearchSegment().GetAge() / 10f;
                normalizedAge = normalizedAge > 1f ? 1f : normalizedAge;
                probabilitySum += con.GetSearchSegment().GetProbability() * Properties.ProbDiffFac;
                neighborsCount++;
            }

        foreach (var con in line.GetWp2Connections())
            if (line != con)
            {
                float normalizedAge = con.GetSearchSegment().GetAge() / 10f;
                normalizedAge = normalizedAge > 1f ? 1f : normalizedAge;
                probabilitySum += con.GetSearchSegment().GetProbability() * Properties.ProbDiffFac;
                neighborsCount++;
            }


        float newProbability = (1f - Properties.ProbDiffFac) * sS.GetProbability() +
                               probabilitySum / neighborsCount;


        sS.SetProb(newProbability);
    }
}