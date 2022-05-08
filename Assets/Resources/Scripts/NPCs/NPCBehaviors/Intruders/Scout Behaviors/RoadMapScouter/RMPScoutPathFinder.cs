using System.Collections.Generic;
using UnityEngine;

public class RMPScoutPathFinder
{
    // path finding on the road map
    private List<Vector2> _tempPath;
    List<WayPoint> openListRoadMap;
    List<WayPoint> closedListRoadMap;

    public RMPScoutPathFinder()
    {
        _tempPath = new List<Vector2>();
        openListRoadMap = new List<WayPoint>();
        closedListRoadMap = new List<WayPoint>();
    }


    public void SetPathOnRoadmap(RoadMap roadMap, Intruder intruder, HidingSpot goal, bool isForced,
        float highestRiskThreshold)
    {
        if (isForced || !intruder.IsBusy())
        {
            List<Vector2> pathToTake = intruder.GetPath();
            GetShortestPath(roadMap, intruder.GetTransform().position, goal, ref pathToTake, highestRiskThreshold);
        }
    }

    // Get shortest path on the road map
    // The start node is a node on the road map and the goal is the position of the phantom 
    // for ease of implementation we start the search from the goal to the start node
    private void GetShortestPath(RoadMap roadMap, Vector2 start, HidingSpot goalSpot, ref List<Vector2> path,
        float highestRiskThreshold)
    {
        bool isOriginal = true;
        WayPoint startWp = roadMap.GetClosestNodes(start, isOriginal, NodeType.RoadMap, Properties.NpcRadius);

        WayPoint goalWp =
            roadMap.GetClosestNodes(goalSpot.Position, isOriginal, NodeType.RoadMap, Properties.NpcRadius);

        openListRoadMap.Clear();
        closedListRoadMap.Clear();

        if (Equals(startWp, null) || Equals(goalWp, null)) return;

        foreach (WayPoint p in roadMap.GetNode(true))
        {
            p.gDistance = Mathf.Infinity;
            p.hDistance = Mathf.Infinity;
            p.parent = null;
        }

        foreach (var p in roadMap.GetTempNodes())
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
            WayPoint current = openListRoadMap[0];
            openListRoadMap.RemoveAt(0);

            if (highestRiskThreshold >= current.GetProbability())
                foreach (WayPoint p in current.GetConnections(isOriginal))
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

        if (Equals(goalWp.parent, null))
        {
            goalSpot.lastFailedTimeStamp = StealthArea.GetElapsedTime();
            return;
        }

        // Get the path from the goal way point to the start way point.
        _tempPath.Clear();
        _tempPath.Add(goalSpot.Position);
        WayPoint currentWayPoint = goalWp;
        while (currentWayPoint.parent != null)
        {
            _tempPath.Add(currentWayPoint.GetPosition());

            if (currentWayPoint.parent == null) break;

            currentWayPoint = currentWayPoint.parent;
        }

        _tempPath.Reverse();

        path.Clear();

        path.Add(start);
        path.Add(startWp.GetPosition());
        foreach (var node in _tempPath)
            path.Add(node);

        SimplifyPath(ref path);

        path.RemoveAt(0);

        if (path.Count == 2)
        {
            Vector2 tempEnd = path[1];
            PathFinding.Instance.GetShortestPath(start, tempEnd, ref path);
        }
    }

    // Get heuristic value for way points road map
    static float GetHeuristicValue(WayPoint currentWayPoint, WayPoint goal)
    {
        float heuristicValue =
            Vector2.Distance(currentWayPoint.GetPosition(), goal.GetPosition());

        return heuristicValue;
    }

    // Get the Cost Value (G) for the Waypoints roadmap
    static float GetCostValue(WayPoint previousWayPoint, WayPoint currentWayPoint)
    {
        float costValue = previousWayPoint.gDistance;

        // Euclidean Distance
        float distance = Vector2.Distance(previousWayPoint.GetPosition(), currentWayPoint.GetPosition());

        costValue += distance;

        return costValue;
    }

    private void SimplifyPath(ref List<Vector2> path)
    {
        for (int i = 0; i < path.Count - 2; i++)
        {
            Vector2 first = path[i];
            Vector2 second = path[i + 2];

            float distance = Vector2.Distance(first, second);
            bool isMutuallyVisible = GeometryHelper.IsCirclesVisible(first, second, Properties.NpcRadius, "Wall");

            if (distance < 0.1f || isMutuallyVisible)
            {
                path.RemoveAt(i + 1);
                i--;
            }
        }
    }
}