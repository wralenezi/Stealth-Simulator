using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadMapPatroler : Patroler
{
    // Road map of the level
    private RoadMap m_RoadMap;

    private RoadMapPatrolerDecisionMaker _decisionMaker;
    // // Variables for path finding
    // private List<RoadMapLine> open;
    // private List<RoadMapLine> closed;

    private RoadMapPatrolerParams _params;

    public override void Initiate(MapManager mapManager)
    {
        m_RoadMap = mapManager.GetRoadMap();
        _decisionMaker = new RoadMapPatrolerDecisionMaker();
        _decisionMaker.Initiate();
        // open = new List<RoadMapLine>();
        // closed = new List<RoadMapLine>();
    }

    public override void Start()
    {
        FillSegments();
    }

    public override void UpdatePatroler(List<Guard> guards, float speed, float timeDelta)
    {
        float maxProbability = 0f;

        // Spread the probability similarly to Third eye crime
        foreach (var line in m_RoadMap.GetLines(false))
        {
            line.PropagateProb();
            line.IncreaseProbability(speed, timeDelta);
            line.ExpandSs(speed, timeDelta);

            // Get the max probability
            float prob = line.GetSearchSegment().GetProbability();
            if (maxProbability < prob)
                maxProbability = prob;
        }

        foreach (var line in m_RoadMap.GetLines(false))
        {
            CheckSeenSs(guards, line);

            SearchSegment sS = line.GetSearchSegment();
            if (Math.Abs(maxProbability) > 0.0001f)
                sS.SetProb(sS.GetProbability() / maxProbability);
            else
                sS.SetProb(sS.GetProbability());
        }
    }

    public override void Patrol(List<Guard> guards)
    {
        AssignGoals(guards);
    }

    private void AssignGoals(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (!guard.IsBusy())
                _decisionMaker.SetTarget(guard, guards, _params, m_RoadMap);
        }
    }

    // Check for the seen search segments
    private void CheckSeenSs(List<Guard> guards, RoadMapLine line)
    {
        foreach (var guard in guards)
        {
            // Trim the parts seen by the guards and reset the section if it is all seen 
            line.CheckSeenSegment(guard);
        }
    }

    // Set the road map segments to 1. To mark the beginning of the patrol shift
    public void FillSegments()
    {
        foreach (var line in m_RoadMap.GetLines(false))
        {
            line.PropagateToSegment(line.GetMid(), 1f, StealthArea.GetElapsedTimeInSeconds());
        }
    }

    //     // // Get a complete path of no more than param@length that a guard needs to traverse to search for an intruder.
    // public void GetPath(Guard guard)
    // {
    //     open.Clear();
    //     closed.Clear();
    //
    //     // Get the closest Way point
    //     RoadMapNode closestWp = m_RoadMap.GetClosestWp(guard.GetTransform().position, guard.GetDirection());
    //
    //     if (Equals(closestWp, null)) return;
    //
    //     RoadMapLine startLine = null;
    //     float maxProb = Mathf.NegativeInfinity;
    //
    //     // Get the start line from the way point
    //     foreach (var line in closestWp.GetLines(false))
    //     {
    //         if (maxProb < line.GetSearchSegment().GetProbability())
    //         {
    //             startLine = line;
    //             maxProb = line.GetSearchSegment().GetProbability();
    //         }
    //     }
    //
    //     // Clear the variables
    //     float minUtility = Mathf.Infinity;
    //     foreach (var line in m_RoadMap.GetLines(false))
    //     {
    //         line.pathUtility = Mathf.NegativeInfinity;
    //         line.distance = Mathf.Infinity;
    //         line.pathParent = null;
    //
    //         if (minUtility > line.GetUtility())
    //         {
    //             minUtility = line.GetUtility();
    //         }
    //     }
    //
    //     // if the min utility is negative, inverse it's sign to modify all utilities to be zero or more
    //     minUtility = minUtility < 0f ? -minUtility : 0f;
    //     // minUtility = 5f;
    //
    //
    //     startLine.pathUtility = startLine.GetUtility() + minUtility;
    //     startLine.distance = 0f;
    //     startLine.pathParent = null;
    //
    //     open.Add(startLine);
    //
    //     RoadMapLine bestLine = null;
    //
    //     // Dijkstra
    //     while (open.Count > 0)
    //     {
    //         RoadMapLine currentLine = open[0];
    //         open.RemoveAt(0);
    //
    //         foreach (var neighbor in currentLine.GetWp1Connections())
    //         {
    //             if (!closed.Contains(neighbor) && !open.Contains(neighbor) && neighbor != currentLine)
    //             {
    //                 // Update the distance
    //                 neighbor.distance = currentLine.distance + neighbor.GetLength();
    //
    //                 float utilityTotal = currentLine.pathUtility + neighbor.GetUtility() + minUtility;
    //
    //                 if (neighbor.pathUtility < utilityTotal)
    //                 {
    //                     neighbor.pathUtility = utilityTotal;
    //                     neighbor.pathParent = currentLine;
    //                 }
    //
    //                 open.InsertIntoSortedList(neighbor,
    //                     delegate(RoadMapLine x, RoadMapLine y) { return x.pathUtility.CompareTo(y.pathUtility); },
    //                     Order.Dsc);
    //             }
    //         }
    //
    //         foreach (var neighbor in currentLine.GetWp2Connections())
    //         {
    //             if (!closed.Contains(neighbor) && !open.Contains(neighbor) && neighbor != currentLine)
    //             {
    //                 // Update the distance
    //                 neighbor.distance = currentLine.distance + neighbor.GetLength();
    //
    //                 float utilityTotal = currentLine.pathUtility + neighbor.GetUtility() + minUtility;
    //
    //                 if (neighbor.pathUtility < utilityTotal)
    //                 {
    //                     neighbor.pathUtility = utilityTotal;
    //                     neighbor.pathParent = currentLine;
    //                 }
    //
    //                 open.InsertIntoSortedList(neighbor,
    //                     delegate(RoadMapLine x, RoadMapLine y) { return x.pathUtility.CompareTo(y.pathUtility); },
    //                     Order.Dsc);
    //             }
    //         }
    //
    //         if (bestLine != null)
    //         {
    //             if (bestLine.pathUtility < currentLine.pathUtility)
    //                 bestLine = currentLine;
    //         }
    //         else
    //             bestLine = currentLine;
    //
    //
    //         closed.Add(currentLine);
    //     }
    //
    //     guard.ClearLines();
    //
    //     // Get the member of the sequence of lines the guard will be visiting
    //     List<RoadMapLine> linesToVisit = guard.GetLinesToPass();
    //
    //     // fill the path
    //     while (bestLine.pathParent != null)
    //     {
    //         // Mark that a guard will be passing through here
    //         bestLine.AddPassingGuard(guard);
    //         linesToVisit.Add(bestLine);
    //
    //         if (bestLine.pathParent == null)
    //             break;
    //
    //         bestLine = bestLine.pathParent;
    //     }
    //
    //     // Reverse the path to start from the beginning.
    //     linesToVisit.Reverse();
    //
    //     // Get the path member variable to load it to the guard
    //     List<Vector2> path = guard.GetPath();
    //
    //     path.Add(guard.GetTransform().position);
    //
    //     // Add the necessary intermediate nodes only.
    //     for (int i = 0; i < linesToVisit.Count; i++)
    //     {
    //         RoadMapLine line = linesToVisit[i];
    //
    //         Vector2 lastPoint = path[path.Count - 1];
    //
    //         if ((line.wp1.Id != 0 && line.wp2.Id != 0) || i == linesToVisit.Count - 1)
    //         {
    //             float wp1Distance = Vector2.Distance(lastPoint, line.wp1.GetPosition());
    //             float wp2Distance = Vector2.Distance(lastPoint, line.wp2.GetPosition());
    //
    //             if (wp1Distance < wp2Distance)
    //             {
    //                 path.Add(line.wp1.GetPosition());
    //                 path.Add(line.wp2.GetPosition());
    //             }
    //             else
    //             {
    //                 path.Add(line.wp2.GetPosition());
    //                 path.Add(line.wp1.GetPosition());
    //             }
    //         }
    //         else if (line.wp1.Id != 0)
    //             path.Add(line.wp1.GetPosition());
    //         else if (line.wp2.Id != 0)
    //             path.Add(line.wp2.GetPosition());
    //     }
    //
    //     // Remove the start node since it is not needed
    //     path.RemoveAt(0);
    //
    //
    //     SimplifyPath(ref path);
    // }


    // private void SimplifyPath(ref List<Vector2> path)
    // {
    //     for (int i = 0; i < path.Count - 2; i++)
    //     {
    //         Vector2 first = path[i];
    //         Vector2 second = path[i + 2];
    //
    //         float distance = Vector2.Distance(first, second);
    //         bool isMutuallyVisible = GeometryHelper.IsCirclesVisible(first, second, Properties.NpcRadius, "Wall");
    //
    //         if (distance < 0.1f || isMutuallyVisible)
    //         {
    //             path.RemoveAt(i + 1);
    //             i--;
    //         }
    //     }
    // }
}

public class RoadMapPatrolerParams : PatrolerParams
{
    public readonly float MaxNormalizedPathLength;
    public readonly float StalenessWeight;
    public readonly float PassingGuardsWeight;
    public readonly float ConnectivityWeight;
    public readonly RMDecision DecisionType;
    public readonly RMPassingGuardsSenstivity PGSen;

    public RoadMapPatrolerParams(float _maxNormalizedPathLength, float _stalenessWeight, float _PassingGuardsWeight, float _connectivityWeight, RMDecision _decisionType, RMPassingGuardsSenstivity _pgSen)
    {
        MaxNormalizedPathLength = _maxNormalizedPathLength;
        StalenessWeight = _stalenessWeight;
        PassingGuardsWeight = _PassingGuardsWeight;
        ConnectivityWeight = _connectivityWeight;
        DecisionType = _decisionType;
        PGSen = _pgSen;
    }
}