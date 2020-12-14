using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GridGuard : Guard
{
    // grid info to know how far is the distance between two nodes
    protected GridWorld m_grid;

    private int m_seenNodes;

    public override void Initialize()
    {
        base.Initialize();
        m_grid = transform.parent.parent.Find("Map").GetComponent<GridWorld>();
    }

    public override Vector2? GetPatrolGoal()
    {
        Vector2? goal = null;

        switch (Data.guardPlanner.Value.patrol)
        {
            case GuardPatrolPlanner.Stalest:
                goal = GetStalestNodePosition();
                break;

            case GuardPatrolPlanner.UserInput:
                goal = null;
                break;
        }

        return goal;
    }



    Vector2? GetStalestNodePosition()
    {
        float maxStaleness = Mathf.NegativeInfinity;
        Vector2? position = null;

        var position1 = transform.position;
        foreach (Node node in m_grid.NodeList)
        {
            if (node.GetStaleness() >= maxStaleness)
            {
                if (maxStaleness == node.GetStaleness() && position != null)
                {
                    float distanceToMax = Vector2.Distance(position1, position.Value);
                    float distanceToNewMax = Vector2.Distance(position1, node.worldPosition);

                    // Igonre this node if it is further away
                    if (distanceToMax < distanceToNewMax)
                        continue;
                }

                maxStaleness = node.GetStaleness();
                position = node.worldPosition;
            }
        }

        return position;
    }

    public override LogSnapshot LogNpcProgress()
    {
        return new LogSnapshot(GetTravelledDistance(), StealthArea.episodeTime, Data,"",0f,0f, m_foundHidingSpots, m_grid.GetAverageStaleness());
    }

    public void IncrementSeenNodes()
    {
        m_seenNodes++;
    }

    public void ResetSeenNodesCount()
    {
        m_seenNodes = 0;
    }

    public override void SetSeenPortion()
    {
        m_guardSeenAreaPercentage = Mathf.RoundToInt(Mathf.Min(m_seenNodes / m_grid.GetTotalArea(), 1) * 100f);
    }

    public bool IsNodeInSeenRegion(Vector2 point)
    {
        return PolygonHelper.IsPointInPolygons(SeenArea, point);
    }
}