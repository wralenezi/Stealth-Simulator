using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisMeshPatrolDecisionMaker
{
    public void SetTarget(Guard guard, List<VisibilityPolygon> unseenPolys)
    {
        VisibilityPolygon bestVisbilityPolygon = null;
        float highestScore = Mathf.NegativeInfinity;

        foreach (var visPoly in unseenPolys)
        {
            float score = visPoly.GetStaleness();

            if (highestScore < score)
            {
                highestScore = score;
                bestVisbilityPolygon = visPoly;
            }
        }

        if (Equals(bestVisbilityPolygon, null)) return;

        guard.SetDestination(bestVisbilityPolygon.GetCentroidPosition(), false, false);
    }

}
