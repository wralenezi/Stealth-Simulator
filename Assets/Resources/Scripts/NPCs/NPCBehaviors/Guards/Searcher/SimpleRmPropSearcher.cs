using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRmPropSearcher : RoadMapSearcher
{
    // Properties of the simple propagation method.
    private float m_expansionMultiplier = 1f;

    public override void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
        if (isStillCheating) return;
        float timeBefore = Time.realtimeSinceStartup;
        UpdateSearch(speed, guards, timeDelta);
        Updated = (Time.realtimeSinceStartup - timeBefore);
    }

    // The probability is propagated with a factor.
    private void UpdateSearch(float speed, List<Guard> guards, float timeDelta)
    {
        float maxProbability = Mathf.NegativeInfinity;
        // Spread the probability similarly to Third eye crime
        foreach (var line in m_RoadMap.GetLines(false))
        {
            // Debug.Log("searching");

            line.PropagateProb();
            line.IncreaseProbability(speed, timeDelta);
            line.ExpandSs(speed * m_expansionMultiplier, timeDelta);

            CheckSeenSs(guards, line);

            float prob = line.GetSearchSegment().GetProbability();
            if (maxProbability < prob) maxProbability = prob;

            if (float.IsNaN(prob))
            {
                CommenceSearch(m_Intruder);
                break;
            }
        }

        if (maxProbability < m_minSegThreshold) NormalizeSegments(maxProbability);
    }
}