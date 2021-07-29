using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Used for the search representation the game 
public class Searcher : MonoBehaviour
{
    // Variables for path finding
    private List<RoadMapLine> open;
    private List<RoadMapLine> closed;

    private StealthArea m_StealthArea;
    private RoadMap m_roadMap;

    private GuardSearchPlanner m_searchType;

    public bool RenderSearchSegments;

    public void Initiate(StealthArea stealthArea)
    {
        open = new List<RoadMapLine>();
        closed = new List<RoadMapLine>();

        m_StealthArea = stealthArea;
        m_roadMap = stealthArea.roadMap;

        foreach (var npcData in m_StealthArea.GetSessionInfo().GetGuardsData())
        {
            m_searchType = npcData.guardPlanner.Value.search;
            break;
        }
    }

    // Move the interception point for the search phase
    public void PlaceSsForSearch(Vector2 position, Vector2 dir)
    {
        // Insert the search segment 
        m_roadMap.CommenceProbabilityFlow(position, dir);
    }


    public void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
        if (m_searchType == GuardSearchPlanner.RmPropSimple)
            UpdateSimpleSearch(speed, guards, timeDelta);
        else if (m_searchType == GuardSearchPlanner.RmPropOccupancyDiffusal)
            UpdateOccupancySearch(speed, guards, timeDelta);
    }


    // Start moving the phantoms across the road map and trim them if seen by guards
    // The probability is propagated with a factor.
    private void UpdateSimpleSearch(float speed, List<Guard> guards, float timeDelta)
    {
        float maxProbability = 0f;


        // Spread the probability similarly to Third eye crime
        foreach (var line in m_roadMap.GetLines())
        {
            SimplePropagation(speed, timeDelta, line);
            line.ExpandSs(speed, timeDelta);

            float prob = line.GetSearchSegment().GetProbability();

            if (maxProbability < prob)
                maxProbability = prob;
        }

        foreach (var line in m_roadMap.GetLines())
        {
            CheckSeenSs(guards, line);

            SearchSegment sS = line.GetSearchSegment();
            if (maxProbability != 0f)
                sS.SetProb(sS.GetProbability() / maxProbability);
            else
                sS.SetProb(sS.GetProbability());
        }
    }

    // The probability is diffused, similar to Third eye crime
    private void UpdateOccupancySearch(float speed, List<Guard> guards, float timeDelta)
    {
        foreach (var line in m_roadMap.GetLines())
        {
            DiffuseProb(line);
            line.ExpandSs(speed, timeDelta);
        }

        foreach (var line in m_roadMap.GetLines())
        {
            CheckSeenSs(guards, line);
        }

        NormalizeProbs();
    }


    // Propagate and increase the probability of the search segments
    public void SimplePropagation(float speed, float timeDelta, RoadMapLine line)
    {
        line.PropagateProb(speed, timeDelta);
        line.IncreaseProbability(speed, timeDelta);
    }


    // Diffusing the probability among neighboring segments 
    // Source: EXPLORATION AND COMBAT IN NETHACK - Johnathan Campbell - Chapter 2.2.1
    public void DiffuseProb(RoadMapLine line)
    {
        SearchSegment sS = line.GetSearchSegment();
        float probabilitySum = 0f;
        int neighborsCount = 0;

        foreach (var con in line.GetWp1Connections())
            if (line != con)
            {
                float normalizedAge = con.GetSearchSegment().GetAge() / 10f;
                normalizedAge = normalizedAge > 1f ? 1f : normalizedAge;
                // float diffuseFactor = Mathf.Lerp(0f, Properties.ProbDiffFac, normalizedAge);
                // probabilitySum += con.GetSearchSegment().GetProbability() * diffuseFactor;
                probabilitySum += con.GetSearchSegment().GetProbability() * Properties.ProbDiffFac;
                neighborsCount++;
            }

        foreach (var con in line.GetWp2Connections())
            if (line != con)
            {
                float normalizedAge = con.GetSearchSegment().GetAge() / 10f;
                normalizedAge = normalizedAge > 1f ? 1f : normalizedAge;
                // float diffuseFactor = Mathf.Lerp(0f, Properties.ProbDiffFac, normalizedAge);
                // probabilitySum += con.GetSearchSegment().GetProbability() * diffuseFactor;
                probabilitySum += con.GetSearchSegment().GetProbability() * Properties.ProbDiffFac;
                neighborsCount++;
            }


        // float newProbability = (1f - Properties.ProbDiffFac) * sS.GetProbability() +
        //                        (Properties.ProbDiffFac / neighborsCount) * probabilitySum;

        float newProbability = (1f - Properties.ProbDiffFac) * sS.GetProbability() +
                               probabilitySum / neighborsCount;


        sS.SetProb(newProbability);
    }

    // Check for the seen search segments
    public void CheckSeenSs(List<Guard> guards, RoadMapLine line)
    {
        foreach (var guard in guards)
        {
            // Trim the parts seen by the guards and reset the section if it is all seen 
            line.CheckSeenSegment(guard);
        }
    }


    // Normalize the probabilities of the segments
    public void NormalizeProbs()
    {
        float maxProbability = 0f;
        foreach (var line in m_roadMap.GetLines())
        {
            float prob = line.GetSearchSegment().GetProbability();

            if (maxProbability < prob)
                maxProbability = prob;
        }

        foreach (var line in m_roadMap.GetLines())
        {
            SearchSegment sS = line.GetSearchSegment();
            if (maxProbability != 0f)
                sS.SetProb(sS.GetProbability() / maxProbability);
            else
                sS.SetProb(1f);
        }
    }


    // Get the best Search segment the guard should visit.
    public Vector2 GetSearchSegment(Guard requestingGuard, List<Guard> guards, Intruder intruder,
        List<MeshPolygon> navMesh, SearchWeights searchWeights)
    {
        SearchSegment bestSs = null;
        float maxFitnessValue = Mathf.NegativeInfinity;

        float maxProbability = Mathf.NegativeInfinity;

        // Loop through the search segments in the lines
        foreach (var line in m_roadMap.GetLines())
        {
            SearchSegment sS = line.GetSearchSegment();

            if (maxProbability < sS.GetProbability())
            {
                maxProbability = sS.GetProbability();
            }

            // Skip the segment if it has a probability of zero or less
            if (sS.GetProbability() <= 0.1f)
                continue;


            // Get the distance of the closest goal other guards are coming to visit
            float minGoalDistance = Mathf.Infinity;

            foreach (var guard in guards)
            {
                // Skip the busy guards
                if (!guard.IsBusy())
                    continue;

                float distanceToGuardGoal =
                    PathFinding.GetShortestPathDistance(navMesh, sS.GetMidPoint(), guard.GetGoal().Value);

                if (minGoalDistance > distanceToGuardGoal)
                {
                    minGoalDistance = distanceToGuardGoal;
                }
            }

            minGoalDistance = minGoalDistance == Mathf.Infinity ? 0f : minGoalDistance;

            // Get the distance from the requesting guard
            float distanceToGuard = PathFinding.GetShortestPathDistance(navMesh, (sS.position1 + sS.position2) / 2f,
                requestingGuard.transform.position);

            // Calculate the fitness of the search segment
            // start with the probability
            float ssFitness = sS.GetFitness();

            // Calculate the overall heuristic of this search segment
            ssFitness = ssFitness * searchWeights.probWeight +
                        (sS.GetAge() / Properties.MaxAge) * searchWeights.ageWeight +
                        (minGoalDistance / Properties.MaxPathDistance) * searchWeights.dstToGuardsWeight +
                        (distanceToGuard / Properties.MaxPathDistance) * searchWeights.dstFromOwnWeight;


            if (maxFitnessValue < ssFitness)
            {
                maxFitnessValue = ssFitness;
                bestSs = sS;
            }
        }

        if (maxProbability < 0.2f)
            NormalizeProbs();


        if (bestSs == null)
            return intruder.GetLastKnownLocation();

        return (bestSs.position1 + bestSs.position2) / 2f;
    }


    // // Get a complete path of no more than param@length that a guard needs to traverse to search for an intruder.
    public void GetPath(Guard guard, float length)
    {
        open.Clear();
        closed.Clear();

        WayPoint closestWp = m_roadMap.GetClosestWp(guard.GetTransform().position, guard.GetDirection());

        RoadMapLine startLine = null;
        float maxProb = Mathf.NegativeInfinity;

        // Get the start line from the way point
        foreach (var line in closestWp.GetLines())
        {
            if (maxProb < line.GetSearchSegment().GetProbability())
            {
                startLine = line;
                maxProb = line.GetSearchSegment().GetProbability();
            }
        }

        // Clear the variables
        float minUtility = Mathf.Infinity;
        foreach (var line in m_roadMap.GetLines())
        {
            line.pathUtility = Mathf.NegativeInfinity;
            line.distance = Mathf.NegativeInfinity;
            line.pathParent = null;

            if (minUtility > line.GetUtility())
            {
                minUtility = line.GetUtility();
            }
        }

        // if the min utility is negative, inverse it's sign to modify all utilities to be zero or more
        // minUtility = minUtility < 0f ? -minUtility : 0f;
        minUtility = 5f;

        startLine.pathUtility = startLine.GetUtility() + minUtility;
        startLine.distance = 0f;
        startLine.pathParent = null;

        open.Add(startLine);

        RoadMapLine bestLine = null;

        while (open.Count > 0)
        {
            RoadMapLine currentLine = open[0];
            open.RemoveAt(0);


            foreach (var neighbor in currentLine.GetWp1Connections())
            {
                if (!closed.Contains(neighbor) && !open.Contains(neighbor) && neighbor != currentLine)
                {
                    // Update the distance
                    neighbor.distance = currentLine.distance + neighbor.GetLength();

                    // Reached max length of path
                    if (neighbor.distance >= length)
                        continue;

                    float utilityTotal = currentLine.pathUtility + neighbor.GetUtility();

                    utilityTotal -= neighbor.GetLength() / Properties.MaxPathDistance;

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

                    // Reached max length of path
                    if (neighbor.distance >= length)
                        continue;

                    float utilityTotal = currentLine.pathUtility + neighbor.GetUtility();

                    utilityTotal -= neighbor.GetLength() / Properties.MaxPathDistance;


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


        SimplifyPath(path);
    }


    public void FindLineAndPath(Guard guard)
    {
        open.Clear();
        closed.Clear();

        WayPoint closestWp = m_roadMap.GetClosestWp(guard.GetTransform().position, guard.GetDirection());

        RoadMapLine startLine = null;
        float maxProb = Mathf.NegativeInfinity;

        // Get the start line from the way point
        foreach (var line in closestWp.GetLines())
        {
            if (maxProb < line.GetSearchSegment().GetProbability())
            {
                startLine = line;
                maxProb = line.GetSearchSegment().GetProbability();
            }
        }

        RoadMapLine goalLine = null;
        // Clear the variables
        float maxUtility = Mathf.NegativeInfinity;
        foreach (var line in m_roadMap.GetLines())
        {
            line.pathUtility = Mathf.NegativeInfinity;
            line.distance = Mathf.NegativeInfinity;
            line.pathParent = null;

            if (maxUtility < line.GetUtility())
            {
                maxUtility = line.GetUtility();
                goalLine = line;
            }
        }


        float minUtility = 5f;

        startLine.pathUtility = startLine.GetUtility() + minUtility;
        startLine.distance = 0f;
        startLine.pathParent = null;


        RoadMapLine currentLine = startLine;
        open.Add(startLine);

        while (open.Count > 0)
        {
            currentLine = open[0];
            open.RemoveAt(0);


            foreach (var neighbor in currentLine.GetWp1Connections())
            {
                if (!closed.Contains(neighbor) && !open.Contains(neighbor) && neighbor != currentLine)
                {
                    // Update the distance
                    neighbor.distance = currentLine.distance + neighbor.GetLength();

                    float utilityTotal = -neighbor.distance /
                                         Properties.MaxPathDistance; //currentLine.pathUtility + neighbor.GetUtility();

                    // utilityTotal -= neighbor.GetLength() / Properties.MaxPathDistance;

                    utilityTotal -=
                        Vector2.Distance(neighbor.GetSearchSegment().GetMidPoint(),
                            goalLine.GetSearchSegment().GetMidPoint()) / Properties.MaxPathDistance;

                    utilityTotal += neighbor.GetUtility() + currentLine.pathUtility;

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

                    float utilityTotal = -neighbor.distance /
                                         Properties.MaxPathDistance; //currentLine.pathUtility + neighbor.GetUtility();

                    // utilityTotal -= neighbor.GetLength() / Properties.MaxPathDistance;

                    utilityTotal -=
                        Vector2.Distance(neighbor.GetSearchSegment().GetMidPoint(),
                            goalLine.GetSearchSegment().GetMidPoint()) / Properties.MaxPathDistance;

                    utilityTotal += neighbor.GetUtility() + currentLine.pathUtility;


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


            closed.Add(currentLine);

            if (currentLine.Equals(goalLine))
                break;
        }


        guard.ClearLines();

        // Get the member of the sequence of lines the guard will be visiting
        List<RoadMapLine> linesToVisit = guard.GetLinesToPass();

        // fill the path
        while (currentLine.pathParent != null)
        {
            // Mark that a guard will be passing through here
            currentLine.AddPassingGuard(guard);
            linesToVisit.Add(currentLine);

            if (currentLine.pathParent == null)
                break;

            currentLine = currentLine.pathParent;
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


        // GeometryHelper.SimplifiyLine(path, 1f);

        SimplifyPath(path);
    }


    private void SimplifyPath(List<Vector2> path)
    {
        for (int i = 0; i < path.Count - 2; i++)
        {
            Vector2 first = path[i];
            Vector2 second = path[i + 2];

            Vector2 dir = (second - first).normalized;
            float distance = Vector2.Distance(first, second);

            Vector2 left = Vector2.Perpendicular(dir);

            float margine = 0.1f;
            RaycastHit2D hitLeft = Physics2D.Raycast(first + left * margine, dir, distance, LayerMask.GetMask("Wall"));
            RaycastHit2D hitRight = Physics2D.Raycast(first - left * margine, dir, distance, LayerMask.GetMask("Wall"));


            if (hitLeft.collider == null && hitRight.collider == null)
            {
                path.RemoveAt(i + 1);
                i--;
            }
        }
    }


    // The search is over so clear the variables
    public void Clear()
    {
        m_roadMap.ClearSearchSegments();
    }

    public void OnDrawGizmos()
    {
        if (RenderSearchSegments)
            if (m_roadMap != null)
            {
                m_roadMap.DrawSearchSegments();
            }
    }
}