using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class RoadMapSearcher : Searcher
{
    public bool RenderSearchSegments;

    // Road map used for the search
    protected RoadMap m_RoadMap;

    // The minimum probability for a segment to be considered by the guard
    protected float m_minSegThreshold = 0.4f;

    // Variables for path finding
    private List<RoadMapLine> open;
    private List<RoadMapLine> closed;

    public bool RenderExpandedPoints;

    // List of projections points by expanding a projected point on the road map
    private List<PossiblePosition> m_ExpandedPoints;

    // The probability of each guard's goal at the time of assignment
    // The index in the array is the guard's ID number
    private float[] m_goalProb;

    // Probability of the npc saying a line
    float m_BarkProb = 0.75f;

    private float m_MaxLength;

    private SearchWeights m_searchWeights;

    public override void Initiate(Session session, MapManager mapManager)
    {
        base.Initiate(session, mapManager);

        m_RoadMap = mapManager.GetRoadMap();
        open = new List<RoadMapLine>();
        closed = new List<RoadMapLine>();

        m_ExpandedPoints = new List<PossiblePosition>();

        m_MaxLength = PathFinding.Instance.longestShortestPath;

        m_goalProb = new float[session.guardsCount];

        m_searchWeights = new SearchWeights(1f, 0f, 1f, -1f);

        // RenderSearchSegments = true;
    }

    public override void CommenceSearch(NPC target)
    {
        Clear();
        m_RoadMap.CommenceProbabilityFlow(target.GetTransform().position, target.GetDirection());
    }

    // Normalize the probabilities of the segments
    // if the max prob is zero, then find the max prob
    protected void NormalizeSegments(float maxProb)
    {
        foreach (var line in m_RoadMap.GetLines(false))
        {
            SearchSegment sS = line.GetSearchSegment();

            if (Math.Abs(maxProb) < 0f)
            {
                sS.SetProb(1f);
                continue;
            }

            sS.SetProb(sS.GetProbability() / maxProb);
        }
    }

    public override void Search(Guard guard) // , SpeechType speechType
    {
        if (isStillCheating)
        {
            guard.SetDestination(m_Intruder.GetTransform().position, true, true);
            return;
        }

        // Once the chaser is idle that means that the intruder is still not seen
        // Now Guards should start visiting the nodes with distance more than zero
        if (!guard.IsBusy())
        {
            // Get a new goal and swap it with the closest guard to that goal and take their goals instead.
            switch (guard.GetNpcData().behavior.searchFormat)
            {
                case PlanOutput.HillClimbPath:
                    GreedyPath(guard);
                    break;

                case PlanOutput.DijkstraPath:
                    BuildDijkstraPath(guard, false);
                    break;

                case PlanOutput.DijkstraPathMax:
                    BuildDijkstraPath(guard, true);
                    break;

                case PlanOutput.Point:
                    GetSearchGoal(guard, m_Intruder, NpcsManager.Instance.GetGuards());
                    break;
            }

            if (guard.GetLinesToPass().Count == 0) return;

            // m_goalProb[guard.GetNpcData().id] = guard.GetLinesToPass()[guard.GetLinesToPass().Count - 1]
            //     .GetSearchSegment().GetProbability();
        }
        else
        {
            // if (guard.GetLinesToPass().Count == 0) return;
            // SearchSegment sS = guard.GetLinesToPass()[guard.GetLinesToPass().Count - 1].GetSearchSegment();
            // float prob = sS.GetProbability();
            // if (prob < m_goalProb[guard.GetNpcData().id])
            //     guard.ClearGoal();
        }
    }

    private void GetSearchGoal(Guard guard, Intruder intruder, List<Guard> guards)
    {
        Vector2? newGoal = GetSearchSegment(guard, guards);

        if (!Equals(newGoal, null)) SwapGoal(guard, guards, newGoal.Value, false);
    }

    // Get the best Search segment the guard should visit.
    public Vector2? GetSearchSegment(Guard requestingGuard, List<Guard> guards)
    {
        SearchSegment bestSs = null;
        float maxFitnessValue = Mathf.NegativeInfinity;
        float maxProbability = Mathf.NegativeInfinity;

        // Loop through the search segments in the lines
        foreach (var line in m_RoadMap.GetLines(false))
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
            ssFitness = ssFitness * m_searchWeights.probWeight +
                        (sS.GetAge() / Properties.MaxAge) * m_searchWeights.ageWeight +
                        (minGoalDistance / PathFinding.Instance.longestShortestPath) * m_searchWeights.dstToGuardsWeight +
                        (distanceToGuard / PathFinding.Instance.longestShortestPath) * m_searchWeights.dstFromOwnWeight;


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

    // Check for the seen search segments
    protected void CheckSeenSs(List<Guard> guards, RoadMapLine line)
    {
        foreach (var guard in guards)
        {
            // Trim the parts seen by the guards and reset the section if it is all seen 
            line.CheckSeenSegment(guard);
        }
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


    private void GreedyPath(Guard guard)
    {
        m_ExpandedPoints.Clear();

        // Get the closest point on the road map to the guard
        Vector2? point = m_RoadMap.GetLineToPoint(guard.GetTransform().position, null, true, out RoadMapLine startLine);

        // if there is no intersection then abort
        if (!point.HasValue) return;

        // Place the possible positions a guard can occupy in the future.
        m_RoadMap.ProjectPositionsInDirection(ref m_ExpandedPoints, point.Value, startLine, 1,
            guard.GetFovRadius() * 1.2f, guard);

        // Find the possible starting line with the highest utility.
        float maxUtility = Mathf.NegativeInfinity;
        foreach (var expandedPoint in m_ExpandedPoints)
        {
            point = m_RoadMap.GetLineToPoint(expandedPoint.position, null, true, out RoadMapLine tempLine);

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


    // // Get a complete path of no more than param@length that a guard needs to traverse to search for an intruder.
    private void BuildDijkstraPath(Guard guard, bool highestUtilityMethod)
    {
        // How far is the path start spot from 
        float projectionDistance = guard.GetFovRadius() * 0.5f;

        // How long is the path
        float distanceLimit = m_MaxLength;

        // clear the lists
        open.Clear();
        closed.Clear();

        m_ExpandedPoints.Clear();

        // Get the closest point on the road map to the guard
        Vector2? point = m_RoadMap.GetLineToPoint(guard.GetTransform().position, null, true, out RoadMapLine startLine);

        // if there is no intersection then abort
        if (!point.HasValue) return;

        // Place the possible positions a guard can occupy in the future.
        m_RoadMap.ProjectPositionsInDirection(ref m_ExpandedPoints, point.Value, startLine, 1, projectionDistance,
            guard);

        // Find the possible starting line with the highest utility.
        float maxUtility = Mathf.NegativeInfinity;
        foreach (var expandedPoint in m_ExpandedPoints)
        {
            point = m_RoadMap.GetLineToPoint(expandedPoint.position, null, true, out RoadMapLine tempLine);

            if (!point.HasValue) continue;

            float tempUtility = GetLineUtility(tempLine);

            if (maxUtility < tempUtility)
            {
                maxUtility = tempUtility;
                startLine = tempLine;
            }
        }

        guard.ClearLines();

        // Clear the variables for the lines
        foreach (var line in m_RoadMap.GetLines(false))
        {
            line.pathUtility = Mathf.NegativeInfinity;
            line.distance = Mathf.Infinity;
            line.pathParent = null;
        }

        startLine.pathUtility = GetLineUtility(startLine);
        startLine.distance = 0f;
        startLine.pathParent = null;

        open.Add(startLine);

        RoadMapLine targetLine = null;

        // The roadMap line with the highest utility
        RoadMapLine maxUtilityLine = null;

        // Dijkstra
        while (open.Count > 0)
        {
            RoadMapLine currentLine = open[0];
            open.RemoveAt(0);

            int neighborsCount =
                Mathf.Min(currentLine.GetWp1Connections().Count, currentLine.GetWp2Connections().Count);

            // Check if it is a dead end that haven't been explored then limit the search for this range
            if (neighborsCount == 1 && currentLine.GetSearchSegment().GetProbability() > m_minSegThreshold)
                distanceLimit = currentLine.distance;

            if (Equals(maxUtilityLine, null) || GetLineUtility(maxUtilityLine) < GetLineUtility(currentLine))
                maxUtilityLine = currentLine;

            foreach (var neighbor in currentLine.GetWp1Connections())
            {
                if (closed.Contains(neighbor) || open.Contains(neighbor) || Equals(neighbor, currentLine)) continue;


                float utilityTotal = (currentLine.pathUtility + GetLineUtility(neighbor)) * 1f;

                if (neighbor.pathUtility <= utilityTotal)
                {
                    // Update the distance
                    neighbor.distance =
                        currentLine.distance + Vector2.Distance(currentLine.GetMid(), neighbor.GetMid());
                    neighbor.pathUtility = utilityTotal;
                    neighbor.pathParent = currentLine;
                }

                if (neighbor.distance < distanceLimit)
                {
                    if (highestUtilityMethod)
                        open.InsertIntoSortedList(neighbor,
                            delegate(RoadMapLine x, RoadMapLine y) { return x.pathUtility.CompareTo(y.pathUtility); },
                            Order.Dsc);
                    else
                        open.InsertIntoSortedList(neighbor,
                            delegate(RoadMapLine x, RoadMapLine y) { return x.distance.CompareTo(y.distance); },
                            Order.Asc);
                }
            }

            foreach (var neighbor in currentLine.GetWp2Connections())
            {
                if (closed.Contains(neighbor) || open.Contains(neighbor) || Equals(neighbor, currentLine)) continue;


                float utilityTotal = (currentLine.pathUtility + GetLineUtility(neighbor)) * 1f;

                if (neighbor.pathUtility <= utilityTotal)
                {
                    // Update the distance
                    neighbor.distance =
                        currentLine.distance + Vector2.Distance(currentLine.GetMid(), neighbor.GetMid());
                    neighbor.pathUtility = utilityTotal;
                    neighbor.pathParent = currentLine;
                }

                if (neighbor.distance < distanceLimit)
                {
                    if (highestUtilityMethod)
                        open.InsertIntoSortedList(neighbor,
                            delegate(RoadMapLine x, RoadMapLine y) { return x.pathUtility.CompareTo(y.pathUtility); },
                            Order.Dsc);
                    else
                        open.InsertIntoSortedList(neighbor,
                            delegate(RoadMapLine x, RoadMapLine y) { return x.distance.CompareTo(y.distance); },
                            Order.Asc);
                }
            }

            if (targetLine != null)
            {
                if (targetLine.pathUtility < currentLine.pathUtility) targetLine = currentLine;
            }
            else
                targetLine = currentLine;

            closed.Add(currentLine);
        }

        // This focuses on only reaching the highest road map line
        if (highestUtilityMethod)
            targetLine = maxUtilityLine;

        guard.ClearLines();

        // Get the member of the sequence of lines the guard will be visiting
        List<RoadMapLine> linesToVisit = guard.GetLinesToPass();

        // fill the path
        while (targetLine?.pathParent != null)
        {
            // Mark that a guard will be passing through here
            linesToVisit.Add(targetLine);

            if (targetLine.pathParent == null)
                break;

            targetLine = targetLine.pathParent;
        }

        // Reverse the path to start from the beginning.
        linesToVisit.Reverse();

        // Get the path member variable to load it to the guard
        List<Vector2> path = guard.GetPath();

        PathFinding.Instance.GetShortestPath(guard.GetTransform().position, startLine.GetMid(),
            ref path);

        path.Insert(0, guard.GetTransform().position);

        // Add the necessary intermediate nodes only.
        int i = 0;
        float totalDistance = 0f;
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

        // Simplify the path
        SimplifyPath(ref path);

        // Remove the start node since it is the guard's actual position.
        path.RemoveAt(0);
    }

    private void SimplifyPath(ref List<Vector2> path)
    {
        for (int i = 0; i < path.Count - 2; i++)
        {
            Vector2 first = path[i];
            Vector2 second = path[i + 2];
            float margine = 0.1f;
            float distance = Vector2.Distance(first, second);

            bool isMutuallyVisible = GeometryHelper.IsCirclesVisible(first, second, margine, "Wall");

            if (distance < 0.1f || isMutuallyVisible)
            {
                path.RemoveAt(i + 1);
                i--;
            }
        }
    }


    // The search is over so clear the variables
    public override void Clear()
    {
        base.Clear();
        m_RoadMap.ClearSearchSegments();

        int guardCount = GameManager.Instance.GetActiveArea().GetSessionInfo().guardsCount;
        for (int i = 0; i < guardCount; i++)
        {
            m_goalProb[i] = 0f;
        }
    }

    public void OnDrawGizmos()
    {
        
        if (RenderSearchSegments)
            if (m_RoadMap != null)
            {
                foreach (var line in m_RoadMap.GetLines(false))
                {
                    float label = Mathf.Round(GetLineUtility(line) * 100f) / 100f;
                    // float label = Mathf.Round(line.GetSearchSegment().GetProbability() * 100f) / 100f;
                    // bool label = line.GetSearchSegment().isPropagated;
                    // float label = line.GetSearchSegment().GetAge();
                    line.DrawSearchSegment(label.ToString());
                }
            }

        if (RenderExpandedPoints)
            if (m_ExpandedPoints != null)
            {
                foreach (var point in m_ExpandedPoints)
                {
                    Gizmos.DrawSphere(point.position, 0.5f);
                }
            }
    }
}