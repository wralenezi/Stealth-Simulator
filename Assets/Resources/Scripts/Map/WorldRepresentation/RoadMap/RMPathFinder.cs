using System.Collections.Generic;
using UnityEngine;

public class RMPathFinder 
{
    // path finding on the road map
    private List<Vector2> _tempPath;
    List<RoadMapNode> openListRoadMap;
    List<RoadMapNode> closedListRoadMap;

    public RMPathFinder()
    {
        _tempPath = new List<Vector2>();
        openListRoadMap = new List<RoadMapNode>();
        closedListRoadMap = new List<RoadMapNode>();
    }

    public float GetPathDistance(RoadMap roadMap, RoadMapNode startWp, RoadMapNode goalWp)
    {
        GetClosestPointToGoal(roadMap, startWp, goalWp);

        float distance = 0f;
        for (int i = 0; i < _tempPath.Count - 1; i++)
            distance += Vector2.Distance(_tempPath[i], _tempPath[i + 1]);

        return distance;
    }


    // Get the path to the goal, if it is not reachable then return the closest reachable node
    public void GetClosestPointToGoal(RoadMap roadMap, RoadMapNode startWp, RoadMapNode goalWp)
    {
        openListRoadMap.Clear();
        closedListRoadMap.Clear();

        if (Equals(startWp, null) || Equals(goalWp, null)) return;

        foreach (RoadMapNode p in roadMap.GetNode(true))
        {
            p.gDistance = Mathf.Infinity;
            p.hDistance = Mathf.Infinity;
            p.parent = null;
        }

        // Set Cost of starting node
        startWp.gDistance = 0f;
        startWp.hDistance = GetHeuristicValue(startWp, goalWp);

        openListRoadMap.Add(startWp);

        while (openListRoadMap.Count > 0)
        {
            RoadMapNode current = openListRoadMap[0];
            openListRoadMap.RemoveAt(0);

            foreach (RoadMapNode p in current.GetConnections(true))
            {
                if (closedListRoadMap.Contains(p) || Equals(p.type, NodeType.Corner)) continue;

                float gDistance = GetCostValue(current, p);
                float hDistance = GetHeuristicValue(current, goalWp);

                if (p.gDistance + p.hDistance > gDistance + hDistance)
                {
                    p.hDistance = hDistance;
                    p.gDistance = gDistance;

                    p.parent = current;
                }

                openListRoadMap.InsertIntoSortedList(p,
                    (x, y) => x.GetFvalue().CompareTo(y.GetFvalue()), Order.Asc);
            }

            closedListRoadMap.Add(current);

            // Stop the search if we reached the destination way point
            if (current.Equals(goalWp)) break;
        }

        // Get the path from the goal way point to the start way point.
        _tempPath.Clear();
        _tempPath.Add(startWp.GetPosition());

        RoadMapNode currentWayPoint = goalWp;
        while (currentWayPoint.parent != null)
        {
            _tempPath.Add(currentWayPoint.GetPosition());

            if (currentWayPoint.parent == null) break;

            currentWayPoint = currentWayPoint.parent;
        }
    }
    
    

    // Get heuristic value for way points road map
    static float GetHeuristicValue(RoadMapNode currentWayPoint, RoadMapNode goal)
    {
        Vector2 offset = currentWayPoint.GetPosition() - goal.GetPosition();
        float sqrMag = offset.sqrMagnitude;

        return sqrMag;
    }


    // Get the Cost Value (G) for the Waypoints roadmap
    static float GetCostValue(RoadMapNode previousWayPoint, RoadMapNode currentWayPoint)
    {
        float costValue = previousWayPoint.gDistance;

        // Euclidean Distance
        float distance = Vector2.Distance(previousWayPoint.GetPosition(), currentWayPoint.GetPosition());

        costValue += distance;

        return costValue;
    }
}