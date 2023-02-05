using System.Collections.Generic;
using UnityEngine;

public class RMScoutPathFinder
{
    // path finding on the road map
    private List<Vector2> _tempPath;
    List<RoadMapNode> openListRoadMap;
    List<RoadMapNode> closedListRoadMap;

    public RMScoutPathFinder()
    {
        _tempPath = new List<Vector2>();
        openListRoadMap = new List<RoadMapNode>();
        closedListRoadMap = new List<RoadMapNode>();
    }

    // Get the path to the goal, if it is not reachable then return the closest reachable node
    public void GetClosestPointToGoal(RoadMap roadMap, Vector2 start, Vector2 goal, int numberOfWPs,
        ref List<RoadMapNode> closestWps, ref List<Vector2> path,
        bool isSafe)
    {
        RoadMapNode startWp = roadMap.GetClosestNodes(start, true, NodeType.RoadMap, Properties.NpcRadius);
        RoadMapNode goalWp = roadMap.GetClosestNodes(goal, true, NodeType.RoadMap, Properties.NpcRadius);

        closestWps.Clear();
        openListRoadMap.Clear();
        closedListRoadMap.Clear();

        if (Equals(startWp, null) || Equals(goalWp, null)) return;

        foreach (RoadMapNode p in roadMap.GetNode(true))
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
        startWp.hDistance = GetHeuristicValue(startWp, goalWp, isSafe);

        openListRoadMap.Add(startWp);

        while (openListRoadMap.Count > 0)
        {
            RoadMapNode current = openListRoadMap[0];
            openListRoadMap.RemoveAt(0);

            if (RMThresholds.GetMaxRisk() >= current.GetProbability())
                foreach (RoadMapNode p in current.GetConnections(true))
                {
                    if (closedListRoadMap.Contains(p) || Equals(p.type, NodeType.Corner)) continue;

                    float gDistance = GetCostValue(current, p);
                    float hDistance = GetHeuristicValue(current, goalWp, isSafe);

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

        // goal is not reachable, find the closest reachable node and return it.
        if (Equals(goalWp.parent, null))
        {
            closedListRoadMap.Sort((x, y) =>
                {
                    int ret = x.GetProbability().CompareTo(y.GetProbability());
                    if (ret != 0) return ret;
                    ret = x.hDistance.CompareTo(y.hDistance);
                    if (ret != 0) return ret;
                    ret = y.gDistance.CompareTo(x.gDistance);
                    return ret;
                }
            );


            int addedNodes = 0;
            int i = -1;
            while (addedNodes < numberOfWPs && i < closedListRoadMap.Count - 1)
            {
                i++;

                if (Equals(closedListRoadMap[i].hDistance, Mathf.Infinity)) continue;

                RoadMapNode node = closedListRoadMap[i];
                if (IsSpotTooCloseRestSpots(node, closestWps, 1f)) continue;

                closestWps.Add(node);
                addedNodes++;
            }

            return;
        }

        // Get the path from the goal way point to the start way point.
        _tempPath.Clear();
        _tempPath.Add(goal);
        RoadMapNode currentWayPoint = goalWp;
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

        // Option 1 -- More efficient path
        if (path.Count == 3)
        {
            path.RemoveAt(0);
            Vector2 tempEnd = path[1];
            PathFinding.Instance.GetShortestPath(start, tempEnd, ref path);
        }
        else
        {
            int pointer = 0;
            while (pointer < path.Count - 2)
            {
                path.RemoveAt(pointer + 1);
                PathFinding.Instance.GetShortestPath(path[pointer], path[pointer + 1], ref _tempPath);
                for (int i = 0; i < _tempPath.Count - 1; i++)
                {
                    path.Insert(++pointer, _tempPath[i]);
                }
            }

            path.RemoveAt(0);
        }

        // Option 2 -- Less efficient path
        // path.RemoveAt(0);
        //
        // if (path.Count == 2)
        // {
        //     Vector2 tempEnd = path[1];
        //     PathFinding.Instance.GetShortestPath(start, tempEnd, ref path);
        // }
    }

    private bool IsSpotTooCloseRestSpots(RoadMapNode newNode, List<RoadMapNode> existingNodes, float leastDistance)
    {
        foreach (var existingNode in existingNodes)
        {
            Vector2 offset = newNode.GetPosition() - existingNode.GetPosition();
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag < leastDistance * leastDistance) return true;

            bool isVisible = GeometryHelper.IsCirclesVisible(newNode.GetPosition(), existingNode.GetPosition(),
                Properties.NpcRadius, "Wall");
            if (isVisible) return true;
        }

        return false;
    }


    private RoadMapNode GetAncestor(RoadMapNode node, float rangeLimit)
    {
        RoadMapNode output = node;
        float currentDistanceFromStart = node.gDistance;

        while (currentDistanceFromStart >= rangeLimit && !Equals(output.parent, null))
        {
            output = output.parent;
            currentDistanceFromStart = output.gDistance;
        }


        return output;
    }

    // Get shortest path on the road map
    // The start node is a node on the road map and the goal is the position of the phantom 
    // for ease of implementation we start the search from the goal to the start node
    public void GetShortestPath(RoadMap roadMap, Vector2 start, HidingSpot goalSpot, ref List<Vector2> path,
        float highestRiskThreshold)
    {
        RoadMapNode startWp = roadMap.GetClosestNodes(start, true, NodeType.RoadMap, Properties.NpcRadius);
        RoadMapNode goalWp =
            roadMap.GetClosestNodes(goalSpot.Position, true, NodeType.RoadMap, Properties.NpcRadius);

        openListRoadMap.Clear();
        closedListRoadMap.Clear();

        if (Equals(startWp, null) || Equals(goalWp, null)) return;

        foreach (RoadMapNode p in roadMap.GetNode(true))
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
        startWp.hDistance = GetHeuristicValue(startWp, goalWp, true);

        openListRoadMap.Add(startWp);

        while (openListRoadMap.Count > 0)
        {
            RoadMapNode current = openListRoadMap[0];
            openListRoadMap.RemoveAt(0);

            if (highestRiskThreshold >= current.GetProbability())
            // if (RMThresholds.GetMaxRisk() >= current.GetProbability())
                foreach (RoadMapNode p in current.GetConnections(true))
                {
                    if (closedListRoadMap.Contains(p) || Equals(p.type, NodeType.Corner)) continue;

                    float gDistance = GetCostValue(current, p);
                    float hDistance = GetHeuristicValue(current, goalWp, true);

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

        // goal is not reachable
        if (Equals(goalWp.parent, null))
        {
            goalSpot.MarkAsChecked();
            return;
        }

        // Get the path from the goal way point to the start way point.
        _tempPath.Clear();
        _tempPath.Add(goalSpot.Position);
        RoadMapNode currentWayPoint = goalWp;
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

        // Option 1 -- More efficient path
        if (path.Count == 3)
        {
            path.RemoveAt(0);
            Vector2 tempEnd = path[1];
            PathFinding.Instance.GetShortestPath(start, tempEnd, ref path);
        }
        else
        {
            int pointer = 0;
            while (pointer < path.Count - 2)
            {
                path.RemoveAt(pointer + 1);
                PathFinding.Instance.GetShortestPath(path[pointer], path[pointer + 1], ref _tempPath);
                for (int i = 0; i < _tempPath.Count - 1; i++)
                {
                    path.Insert(++pointer, _tempPath[i]);
                }
            }

            path.RemoveAt(0);
        }
    }

    // Get heuristic value for way points road map
    static float GetHeuristicValue(RoadMapNode currentWayPoint, RoadMapNode goal, bool isSafe)
    {
        // float heuristicValue =
        //     Vector2.Distance(currentWayPoint.GetPosition(), goal.GetPosition());

        if (!isSafe) return 0f;

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