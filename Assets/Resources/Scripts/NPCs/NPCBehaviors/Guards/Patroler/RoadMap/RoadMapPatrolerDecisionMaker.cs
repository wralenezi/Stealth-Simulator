using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadMapPatrolerDecisionMaker
{
    private Dictionary<string, Vector2> _guardGoals;

    // Variables for path finding
    private List<RoadMapLine> open;
    private List<RoadMapLine> closed;

    public void Initiate()
    {
        _guardGoals = new Dictionary<string, Vector2>();
        
        open = new List<RoadMapLine>();
        closed = new List<RoadMapLine>();
    }

    private bool IsGoalTaken(Guard guard, Vector2 goal)
    {
        float minSqrMagThreshold = 1f;

        foreach (var guardGoal in _guardGoals)
        {
            if (Equals(guardGoal.Key, guard.name)) continue;

            float sqrMag = (guardGoal.Value - goal).sqrMagnitude;

            if (minSqrMagThreshold >= sqrMag) return true;
        }

        return false;
    }

    public void SetTarget(Guard guard, List<Guard> guards, RoadMapPatrolerParams patrolerParams, RoadMap roadMap)
    {
        if (_guardGoals.ContainsKey(guard.name)) _guardGoals.Remove(guard.name);

        switch (patrolerParams.DecisionType)
        {
            case RMDecision.DijkstraPath:
                SetDijkstraPath(guard, guards, patrolerParams, roadMap);
                break;
            
            case RMDecision.EndPoint:
                break;
        }


        if (Equals(guard.GetGoal(), null)) return;

        _guardGoals[guard.name] = guard.GetGoal().Value;
    }
    
    
    // Get a complete path of no more than param@length that a guard needs to traverse to search for an intruder.
    private void SetDijkstraPath(Guard guard, List<Guard> guards, RoadMapPatrolerParams _params, RoadMap roadMap)
    {
        open.Clear();
        closed.Clear();

        // Get the closest Way point
        RoadMapNode closestWp = roadMap.GetClosestWp(guard.GetTransform().position, guard.GetDirection());

        if (Equals(closestWp, null)) return;

        RoadMapLine startLine = null;
        float maxUtility = Mathf.NegativeInfinity;

        // Get the start line from the way point
        foreach (var line in closestWp.GetLines(false))
        {
            float utility = GetUtility(guards, _params, line);
            if (maxUtility < utility)
            {
                startLine = line;
                maxUtility = utility;
            }
        }

        // Clear the variables
        foreach (var line in roadMap.GetLines(false))
        {
            line.pathUtility = Mathf.NegativeInfinity;
            line.distance = Mathf.Infinity;
            line.pathParent = null;
        }

        startLine.pathUtility = GetUtility(guards, _params, startLine);
        startLine.distance = 0f;
        startLine.pathParent = null;

        open.Add(startLine);

        RoadMapLine bestLine = null;

        // Dijkstra
        while (open.Count > 0)
        {
            RoadMapLine currentLine = open[0];
            open.RemoveAt(0);

            // Skip if the search reach its limit
            if(currentLine.distance / PathFinding.Instance.longestShortestPath >= _params.MaxNormalizedPathLength) continue;
            
            foreach (var neighbor in currentLine.GetWp1Connections())
            {
                if (!closed.Contains(neighbor) && !open.Contains(neighbor) && neighbor != currentLine)
                {
                    // Update the distance
                    neighbor.distance = currentLine.distance + neighbor.GetLength();

                    float utilityTotal = currentLine.pathUtility + GetUtility(guards, _params, neighbor);

                    if (neighbor.pathUtility < utilityTotal)
                    {
                        neighbor.pathUtility = utilityTotal;
                        neighbor.pathParent = currentLine;
                    }

                    
                    open.InsertIntoSortedList(neighbor,
                        delegate(RoadMapLine x, RoadMapLine y) { return x.pathUtility.CompareTo(y.pathUtility); },
                        Order.Dsc);
                }
            }

            foreach (var neighbor in currentLine.GetWp2Connections())
            {
                if (!closed.Contains(neighbor) && !open.Contains(neighbor) && neighbor != currentLine)
                {
                    // Update the distance
                    neighbor.distance = currentLine.distance + neighbor.GetLength();

                    float utilityTotal = currentLine.pathUtility + GetUtility(guards, _params, neighbor);

                    if (neighbor.pathUtility < utilityTotal)
                    {
                        neighbor.pathUtility = utilityTotal;
                        neighbor.pathParent = currentLine;
                    }

                    open.InsertIntoSortedList(neighbor,
                        delegate(RoadMapLine x, RoadMapLine y) { return x.pathUtility.CompareTo(y.pathUtility); },
                        Order.Dsc);
                }
            }

            if (bestLine != null)
            {
                if (bestLine.pathUtility < currentLine.pathUtility)
                    bestLine = currentLine;
            }
            else
                bestLine = currentLine;


            closed.Add(currentLine);
        }

        guard.ClearLines();

        if(Equals(bestLine, null)) return;
        
        // Get the member of the sequence of lines the guard will be visiting
        List<RoadMapLine> linesToVisit = guard.GetLinesToPass();

        // fill the path
        while (bestLine.pathParent != null)
        {
            // Mark that a guard will be passing through here
            bestLine.AddPassingGuard(guard);
            linesToVisit.Add(bestLine);

            if (bestLine.pathParent == null)
                break;

            bestLine = bestLine.pathParent;
        }

        // Reverse the path to start from the beginning.
        linesToVisit.Reverse();

        // Get the path member variable to load it to the guard
        List<Vector2> path = guard.GetPath();

        path.Add(guard.GetTransform().position);

        // Add the necessary intermediate nodes only.
        for (int i = 0; i < linesToVisit.Count; i++)
        {
            RoadMapLine line = linesToVisit[i];

            Vector2 lastPoint = path[path.Count - 1];

            if ((line.wp1.Id != 0 && line.wp2.Id != 0) || i == linesToVisit.Count - 1)
            {
                float wp1Distance = Vector2.Distance(lastPoint, line.wp1.GetPosition());
                float wp2Distance = Vector2.Distance(lastPoint, line.wp2.GetPosition());

                if (wp1Distance < wp2Distance)
                {
                    path.Add(line.wp1.GetPosition());
                    path.Add(line.wp2.GetPosition());
                }
                else
                {
                    path.Add(line.wp2.GetPosition());
                    path.Add(line.wp1.GetPosition());
                }
            }
            else if (line.wp1.Id != 0)
                path.Add(line.wp1.GetPosition());
            else if (line.wp2.Id != 0)
                path.Add(line.wp2.GetPosition());
        }

        // Remove the start node since it is not needed
        path.RemoveAt(0);

        // SimplifyPath(ref path);
    }

    private void SetGoal(Guard guard, List<Guard> guards, RoadMapPatrolerParams _params, RoadMap roadMap)
    {
        List<RoadMapLine> lines = roadMap.GetLines(false);

        RoadMapLine bestLine = null;
        float highestFitness = Mathf.NegativeInfinity;
        
        foreach (var line in lines)
        {
            if (IsGoalTaken(guard, line.GetMid())) continue;

            float score = 0f;

            score += line.GetProbability() * _params.StalenessWeight;

        //     score += GetAreaPortion(visPoly) * patrolerParams.AreaWeight;
        //
        //     // Subtracted by 1 to reverse the relation ( higher value is closer, thus more desirable)
        //     score += (1f - GetNormalizedDistance(guard, visPoly)) * patrolerParams.DistanceWeight;
        //
        //     score += GetClosestGuardDistance(guard, guards, visPoly) * patrolerParams.SeparationWeight;
        //
        //     if (highestScore < score)
        //     {
        //         highestScore = score;
        //         bestTarget = visPoly;
        //     }
        //     
        }

    }


    private float GetUtility(List<Guard> guards, RoadMapPatrolerParams patrolerParams, RoadMapLine rmLine)
    {
        float utility = 0f;

        utility += rmLine.GetProbability() * patrolerParams.StalenessWeight;

        int passingGuards = rmLine.GetPassingGuardsCount();
        switch (patrolerParams.PGSen)
        {
            case RMPassingGuardsSenstivity.Max:
                passingGuards = passingGuards > 0 ? guards.Count : 0;
                break;

            case RMPassingGuardsSenstivity.Actual:
                break;
        }
        utility += (1f - (float) passingGuards / guards.Count) * patrolerParams.PassingGuardsWeight;

        // Minus two because too ignore the edge connection itself
        int connectionsCount = rmLine.GetWp1Connections().Count + rmLine.GetWp2Connections().Count - 2;
        utility += (connectionsCount / 10f) * patrolerParams.ConnectivityWeight;

        return utility;
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

public enum RMDecision
{
    // Find a complete path
    DijkstraPath,
    // Simply find a target and take the shortest path towards it.
    EndPoint
}

public enum RMPassingGuardsSenstivity
{
    Max,
    Actual
}