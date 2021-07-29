using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VisMeshGuard : Guard
{
    private VisMesh m_VisMesh;

    public override void Initiate(StealthArea area, NpcData data)
    {
        base.Initiate(area, data);
        m_VisMesh = transform.parent.parent.Find("Map").GetComponent<VisMesh>();
    }
    
    // Clear the lines the guard planned to go through
    public override void ClearLines()
    {
        while (LinesToPassThrough.Count > 0)
        {
            RoadMapLine current = LinesToPassThrough[0];
            current.RemovePassingGuard(this);
            LinesToPassThrough.RemoveAt(0);
        }
    }

    public override float GetPassingsAverage()
    {
        float sum = 0f;

        foreach (var line in LinesToPassThrough)
        {
            sum += line.GetPassingGuardsCount() - 1;
            
        }

        return sum / LinesToPassThrough.Count;
    }

    // Get the next target the guard should observe
    public override Vector2? GetPatrolGoal()
    {
        Vector2? goal = null;

        switch (Data.guardPlanner.Value.patrol)
        {
            case GuardPatrolPlanner.Stalest:
                goal = GetStalestPolygon().GetCentroidPosition();
                break;

            case GuardPatrolPlanner.UserInput:
                goal = GetInputPoint();
                break;
        }

        return goal;
    }

    // Get the stalest Polygon in the whole map
    Polygon GetStalestPolygon()
    {
        var position = transform.position;

        Polygon currentPolygon = PathFinding.GetCorrespondingPolygon(m_VisMesh.GetVisMesh(), position);

        float maxStaleness = Mathf.NegativeInfinity;
        Polygon stalestPolygon = currentPolygon;

        foreach (VisibilityPolygon vp in m_VisMesh.GetVisMesh())
        {
            if (maxStaleness <= vp.GetStaleness())
            {
                if (Math.Abs(maxStaleness - vp.GetStaleness()) < 1f)
                {
                    float distanceToMax = Vector2.Distance(position, stalestPolygon.GetCentroidPosition());
                    float distanceToNewMax = Vector2.Distance(position, vp.GetCentroidPosition());

                    // Ignore this node if it is further away
                    if (distanceToMax < distanceToNewMax)
                        continue;
                }

                maxStaleness = vp.GetStaleness();
                stalestPolygon = vp;
            }
        }

        return stalestPolygon;
    }

    // Go see where the mouse pointed
    Vector2? GetInputPoint()
    {
        if (Input.GetMouseButtonDown(0))
            return Camera.main.ScreenToWorldPoint(Input.mousePosition);

        return null;
    }

    public override LogSnapshot LogNpcProgress()
    {
        return new LogSnapshot(GetTravelledDistance(), StealthArea.GetElapsedTime(), Data,"",0,0f,0f,0f, m_FoundHidingSpots, m_VisMesh.GetAverageStaleness());
    }

    public override void SetSeenPortion()
    {
        m_GuardSeenAreaPercentage =
            Mathf.RoundToInt(PolygonHelper.GetPolygonArea(SeenArea) * 100f / World.GetTotalArea());
    }
}