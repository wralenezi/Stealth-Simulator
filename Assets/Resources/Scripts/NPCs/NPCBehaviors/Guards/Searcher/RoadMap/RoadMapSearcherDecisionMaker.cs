using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadMapSearcherDecisionMaker 
{
    private Dictionary<string, Vector2> _guardGoals;

    // Variables for path finding
    private List<RoadMapLine> open;
    private List<RoadMapLine> closed;

    // The minimum probability for a segment to be considered by the guard
    protected float m_minSegThreshold = 0.4f;
    
    // List of projections points by expanding a projected point on the road map
    private List<PossiblePosition> m_ExpandedPoints;
    
    public bool RenderExpandedPoints;
    
    public bool RenderSearchSegments;


    private float m_MaxLength;

    
    public void Initiate()
    {
        _guardGoals = new Dictionary<string, Vector2>();
        
        open = new List<RoadMapLine>();
        closed = new List<RoadMapLine>();
        
        m_ExpandedPoints = new List<PossiblePosition>();

        m_MaxLength = PathFinding.Instance.longestShortestPath;

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

    public void SetTarget(Guard guard, List<Guard> guards, RoadMapSearcherParams searcherParams, RoadMap roadMap)
    {

        if (_guardGoals.ContainsKey(guard.name)) _guardGoals.Remove(guard.name);

        switch (searcherParams.DecisionType)
        {
            case RMDecision.HillClimbPath:
            GreedyPath(guard, roadMap);
            break;
            
            case RMDecision.DijkstraPath:
                SetDijkstraPath(guard, guards, searcherParams, roadMap);
                break;
            
            case RMDecision.EndPoint:
                GetSearchGoal(guard, NpcsManager.Instance.GetIntruders()[0], NpcsManager.Instance.GetGuards(), searcherParams, roadMap);
                break;
        }


        if (Equals(guard.GetGoal(), null)) return;

        _guardGoals[guard.name] = guard.GetGoal().Value;
        
        
        // // Once the chaser is idle that means that the intruder is still not seen
        // // Now Guards should start visiting the nodes with distance more than zero
        // if (!guard.IsBusy())
        // {
        //     // Get a new goal and swap it with the closest guard to that goal and take their goals instead.
        //     switch (guard.GetNpcData().behavior.searchFormat)
        //     {
        //         case PlanOutput.HillClimbPath:
        //             GreedyPath(guard);
        //             break;
        //
        //         case PlanOutput.DijkstraPath:
        //             BuildDijkstraPath(guard, false);
        //             break;
        //
        //         case PlanOutput.DijkstraPathMax:
        //             BuildDijkstraPath(guard, true);
        //             break;
        //
        //         case PlanOutput.Point:
        //             GetSearchGoal(guard, m_Intruder, NpcsManager.Instance.GetGuards());
        //             break;
        //     }
        //
        //     if (guard.GetLinesToPass().Count == 0) return;
        //
        //     // m_goalProb[guard.GetNpcData().id] = guard.GetLinesToPass()[guard.GetLinesToPass().Count - 1]
        //     //     .GetSearchSegment().GetProbability();
        // }
        // else
        // {
        //     // if (guard.GetLinesToPass().Count == 0) return;
        //     // SearchSegment sS = guard.GetLinesToPass()[guard.GetLinesToPass().Count - 1].GetSearchSegment();
        //     // float prob = sS.GetProbability();
        //     // if (prob < m_goalProb[guard.GetNpcData().id])
        //     //     guard.ClearGoal();
        // }
    }
    
    
    // Get a complete path of no more than param@length that a guard needs to traverse to search for an intruder.
    private void SetDijkstraPath(Guard guard, List<Guard> guards, RoadMapSearcherParams _params, RoadMap roadMap)
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

    
    private void GetSearchGoal(Guard guard, Intruder intruder, List<Guard> guards, RoadMapSearcherParams _params, RoadMap roadMap)
    {
        Vector2? newGoal = GetSearchSegment(guard, guards, _params, roadMap);

        if (!Equals(newGoal, null)) SwapGoal(guard, guards, newGoal.Value, false);
    }

        // Get the best Search segment the guard should visit.
    private Vector2? GetSearchSegment(Guard requestingGuard, List<Guard> guards, RoadMapSearcherParams _params, RoadMap roadMap)
    {
        SearchSegment bestSs = null;
        float maxFitnessValue = Mathf.NegativeInfinity;
        float maxProbability = Mathf.NegativeInfinity;

        // Loop through the search segments in the lines
        foreach (var line in roadMap.GetLines(false))
        {
            SearchSegment sS = line.GetSearchSegment();

            if (maxProbability < sS.GetProbability())
                maxProbability = sS.GetProbability();


            // Skip the segment if it has a probability of zero or less
            if (sS.GetProbability() <= m_minSegThreshold) continue;


            // Get the distance of the closest goal other guards are coming to visit
            float minGoalDistance = Mathf.Infinity;

            foreach (var guard in guards)
            {
                // Skip the busy guards
                if (!guard.IsBusy())
                    continue;

                float distanceToGuardGoal =
                    PathFinding.Instance.GetShortestPathDistance(sS.GetMidPoint(), guard.GetGoal().Value);

                if (minGoalDistance > distanceToGuardGoal)
                    minGoalDistance = distanceToGuardGoal;
            }

            minGoalDistance = float.IsPositiveInfinity(minGoalDistance) ? 0f : minGoalDistance;

            // Get the distance from the requesting guard
            float distanceToGuard = PathFinding.Instance.GetShortestPathDistance((sS.position1 + sS.position2) / 2f,
                requestingGuard.transform.position);

            // Calculate the fitness of the search segment
            // start with the probability
            float ssFitness = sS.GetFitness();

            // Calculate the overall heuristic of this search segment
            ssFitness = ssFitness * _params.probWeight;
            ssFitness += (sS.GetAge() / Properties.MaxAge) * _params.ageWeight;
            ssFitness += (minGoalDistance / PathFinding.Instance.longestShortestPath) * _params.dstToGuardsWeight;
            ssFitness += (distanceToGuard / PathFinding.Instance.longestShortestPath) * _params.dstFromOwnWeight;

            if (maxFitnessValue < ssFitness)
            {
                maxFitnessValue = ssFitness;
                bestSs = sS;
            }
        }

        if (bestSs == null)
            return null;

        return (bestSs.position1 + bestSs.position2) / 2f;
    }
    
        // Assign goal to closest guard and swap goals if needed if the guard was busy.
    public void SwapGoal(Guard assignedGuard, List<Guard> guards, Vector2 newGoal, bool isEnabled)
    {
        // Find the closest guard to the new goal
        float minDistance = Vector2.Distance(assignedGuard.transform.position, newGoal);
        Guard closestGuard = null;
        foreach (var curGuard in guards)
        {
            // float dstToOldGuard = Vector2.Distance(curGuard.transform.position, newGoal);
            float dstToOldGuard = PathFinding.Instance.GetShortestPathDistance(curGuard.transform.position, newGoal);

            // Check if the other guard is closer
            if (minDistance > dstToOldGuard)
            {
                minDistance = dstToOldGuard;
                closestGuard = curGuard;
            }
        }

        string heading = "";

        // Sort out the guard assignment
        if (isEnabled && !Equals(closestGuard, assignedGuard) && !Equals(closestGuard, null))
        {
            // Swap the goals between the closer guard
            if (closestGuard.IsBusy())
            {
                Vector2 tempGoal = closestGuard.GetGoal().Value;
                assignedGuard.SetDestination(tempGoal, false, true);

                // // Update the guards heading
                // heading = WorldState.GetHeading(assignedGuard.GetTransform().position, tempGoal);
                // WorldState.Set(assignedGuard.name + "_goal", heading);

                // m_SA.guardsManager.UpdateWldStNpcs();

                // guard announce to go instead 
                // scriptor.ChooseDialog(assignedGuard, closestGuard, "Plan", m_SA.GetSessionInfo().speechType,
                //     m_BarkProb);
            }

            // Assign the new goal to the other idle guard
            closestGuard.SetDestination(newGoal, false, true);

            // // Update the guards heading
            // heading = WorldState.GetHeading(closestGuard.GetTransform().position, newGoal);
            // WorldState.Set(assignedGuard.name + "_goal", heading);

            // m_SA.guardsManager.UpdateWldStNpcs();

            // scriptor.ChooseDialog(closestGuard, null, "Plan", m_SA.GetSessionInfo().speechType, m_BarkProb);
        }
        else // since no guards are closer then simply assign it to the one who chose it
        {
            assignedGuard.SetDestination(newGoal, false, false);

            // // Update the guards heading
            // heading = WorldState.GetHeading(assignedGuard.GetTransform().position, newGoal);
            // WorldState.Set(assignedGuard.name + "_goal", heading);

            // m_SA.guardsManager.UpdateWldStNpcs();

            // scriptor.ChooseDialog(assignedGuard, null, "Plan", m_SA.GetSessionInfo().speechType, m_BarkProb);
        }
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


    private float GetUtility(List<Guard> guards, RoadMapSearcherParams patrolerParams, RoadMapLine rmLine)
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
    
    private void GreedyPath(Guard guard, RoadMap _roadMap)
    {
        m_ExpandedPoints.Clear();
    
        // Get the closest point on the road map to the guard
        Vector2? point = _roadMap.GetLineToPoint(guard.GetTransform().position, null, true, out RoadMapLine startLine);
    
        // if there is no intersection then abort
        if (!point.HasValue) return;
    
        // Place the possible positions a guard can occupy in the future.
        _roadMap.ProjectPositionsInDirection(ref m_ExpandedPoints, point.Value, startLine, 1,
            guard.GetFovRadius() * 1.2f, guard);
    
        // Find the possible starting line with the highest utility.
        float maxUtility = Mathf.NegativeInfinity;
        foreach (var expandedPoint in m_ExpandedPoints)
        {
            point = _roadMap.GetLineToPoint(expandedPoint.GetPosition().Value, null, true, out RoadMapLine tempLine);
    
            if (!point.HasValue) continue;
    
            float tempUtility = GetLineUtility(tempLine);
            if (maxUtility < tempUtility)
            {
                maxUtility = tempUtility;
                startLine = tempLine;
            }
        }
    
        guard.ClearLines();
    
        if (Equals(startLine, null)) return;
    
        // Get the member of the sequence of lines the guard will be visiting
        List<RoadMapLine> linesToVisit = guard.GetLinesToPass();
    
        linesToVisit.Add(startLine);
    
        startLine.AddPassingGuard(guard);
        float totalDistance = startLine.GetLength();
    
        RoadMapLine currentLine;
        RoadMapLine maxLineUtility;
        while (totalDistance < m_MaxLength)
        {
            maxUtility = Mathf.NegativeInfinity;
            maxLineUtility = null;
    
            currentLine = linesToVisit[linesToVisit.Count - 1];
    
            foreach (var neighbor in currentLine.GetWp1Connections())
            {
                if (!linesToVisit.Contains(neighbor) && GetLineUtility(neighbor) > maxUtility)
                {
                    maxUtility = GetLineUtility(neighbor);
                    maxLineUtility = neighbor;
                }
            }
    
            foreach (var neighbor in currentLine.GetWp2Connections())
            {
                if (!linesToVisit.Contains(neighbor) && GetLineUtility(neighbor) > maxUtility)
                {
                    maxUtility = GetLineUtility(neighbor);
                    maxLineUtility = neighbor;
                }
            }
    
            if (Equals(maxLineUtility, null)) break;
    
            linesToVisit.Add(maxLineUtility);
            totalDistance += maxLineUtility.GetLength();
        }
    
        // Get the path member variable to load it to the guard
        List<Vector2> path = guard.GetPath();
    
        PathFinding.Instance.GetShortestPath(guard.GetTransform().position, startLine.GetMid(),
            ref path);
    
        // path.Add(guard.GetTransform().position);
    
        // Add the necessary intermediate nodes only.
        int i = 0;
        totalDistance = 0f;
        while (i < linesToVisit.Count)
        {
            RoadMapLine line = linesToVisit[i];
    
            Vector2 lastPoint = path[path.Count - 1];
    
            if ((line.wp1.Id != 0 && line.wp2.Id != 0) || i == linesToVisit.Count - 1)
            {
                float wp1Distance = Vector2.Distance(lastPoint, line.wp1.GetPosition());
                float wp2Distance = Vector2.Distance(lastPoint, line.wp2.GetPosition());
    
                totalDistance += Mathf.Min(wp1Distance, wp2Distance);
                totalDistance += Vector2.Distance(line.wp1.GetPosition(), line.wp2.GetPosition());
    
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
            {
                path.Add(line.wp1.GetPosition());
                totalDistance += Vector2.Distance(lastPoint, line.wp1.GetPosition());
            }
            else if (line.wp2.Id != 0)
            {
                path.Add(line.wp2.GetPosition());
                totalDistance += Vector2.Distance(lastPoint, line.wp2.GetPosition());
            }
    
            if (totalDistance >= m_MaxLength)
                break;
    
            line.AddPassingGuard(guard);
            i++;
        }
    
        // Increment the index
        i++;
    
        while (i < linesToVisit.Count)
            linesToVisit.RemoveAt(i);
    
    
        // Remove the start node since it is not needed
        // path.RemoveAt(0);
    
        SimplifyPath(ref path);
    
        guard.ForceToReachGoal(false);
    }
    
    private float GetLineUtility(RoadMapLine line)
    {
        float utility = 0f;

        // Get the 0 to 1 probability value.
        float fitness = line.GetSearchSegment().GetProbability();
        float fitnessWeight = 1f;
        utility += Mathf.Clamp(fitness * fitnessWeight, 0f, fitnessWeight);

        // Normalized value of when the segment was last seen.
        // float lastSeenPortion = line.GetSearchSegment().GetAge() / GetSearchTime();
        // float lastSeenWeight = 0.2f;
        // utility += Mathf.Clamp(lastSeenPortion * lastSeenWeight, 0f, lastSeenWeight);

        // Portions of guards planning to pass through this line. The value will be 0 to 1.
        float guardsPassingPortions = line.GetPassingGuardsCount() / StealthArea.SessionInfo.guardsCount;

        if (line.GetPassingGuardsCount() > 0) utility = guardsPassingPortions * utility;

        return Mathf.Clamp(utility, 0f, 1f);
    }


    public void DrawPoints(RoadMap _roadMap)
    {
        if (RenderExpandedPoints)
            if (m_ExpandedPoints != null)
            {
                foreach (var point in m_ExpandedPoints)
                {
                    Gizmos.DrawSphere(point.GetPosition().Value, 0.5f);
                }
            }
        
        if (RenderSearchSegments)
            if (_roadMap != null)
            {
                foreach (var line in _roadMap.GetLines(false))
                {
                    float label = Mathf.Round(GetLineUtility(line) * 100f) / 100f;
                    // float label = Mathf.Round(line.GetSearchSegment().GetProbability() * 100f) / 100f;
                    // bool label = line.GetSearchSegment().isPropagated;
                    // float label = line.GetSearchSegment().GetAge();
                    line.DrawSearchSegment(label.ToString());
                }
            }
    }

}
