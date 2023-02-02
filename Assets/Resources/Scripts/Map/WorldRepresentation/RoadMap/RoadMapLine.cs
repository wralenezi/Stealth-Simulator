using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RoadMapLine
{
    // First point
    public RoadMapNode wp1;

    // Second point
    public RoadMapNode wp2;

    // The segment that lies in this line
    private SearchSegment m_SearchSegment;

    private readonly float m_length;

    // Variables for pathfinding using RoadMap in search
    // The guards planning to pass through this roadmap line.
    private List<Guard> m_passingGuardS;

    public float pathUtility;
    public float distance;
    public RoadMapLine pathParent;

    public RoadMapLine(RoadMapNode _wp1, RoadMapNode _wp2)
    {
        wp1 = _wp1;
        wp2 = _wp2;

        m_length = Vector2.Distance(wp1.GetPosition(), wp2.GetPosition());

        m_passingGuardS = new List<Guard>();

        m_SearchSegment = new SearchSegment(wp1, wp1.GetPosition(), wp2, wp2.GetPosition());
    }

    // Check if a way point is on this line
    public bool IsPointPartOfLine(RoadMapNode wp)
    {
        return (wp == wp1 || wp == wp2);
    }

    public List<RoadMapLine> GetWp1Connections()
    {
        return wp1.GetLines(false);
    }

    public List<RoadMapLine> GetWp2Connections()
    {
        return wp2.GetLines(false);
    }

    public float GetLength()
    {
        return m_length;
    }

    public void AddPassingGuard(Guard guard)
    {
        if (!m_passingGuardS.Contains(guard))
            m_passingGuardS.Add(guard);
    }

    public void RemovePassingGuard(Guard guard)
    {
        if (m_passingGuardS.Contains(guard))
            m_passingGuardS.Remove(guard);
    }

    public int GetPassingGuardsCount()
    {
        return m_passingGuardS.Count;
    }

    public float GetProbability()
    {
        return GetSearchSegment().GetProbability();
    }

    // Get the utility value of a guard passing through this line
    public float GetUtility()
    {
        float prob = GetSearchSegment().GetProbability();

        // float guardsPassingUtility = GetPassingGuardsCount() / (StealthArea.SessionInfo.guardsCount);
        float guardsPassingUtility = GetPassingGuardsCount() > 0 ? 1f : 0f;

        float utility = Mathf.Clamp(prob - guardsPassingUtility, 0f, 1f);
        
        return utility;
    }

    // Add a possible line where the intruder might be in
    public void SetSearchSegment(Vector2 startingPos1, Vector2 startingPos2, float prob,
        float timestamp)
    {
        m_SearchSegment.SetSsData(startingPos1, startingPos2, prob);
        m_SearchSegment.SetTimestamp(timestamp);
    }

    public void PropagateToSegment(Vector2 startPosition1, Vector2 startPosition2, float prob, float timeStamp)
    {
        SetSearchSegment(startPosition1, startPosition2, prob, timeStamp);
        GetSearchSegment().isPropagated = true;
    }

    // Expand the search segments
    public void ExpandSs(float speed, float deltaTime)
    {
        m_SearchSegment.Expand(speed, deltaTime);
    }

    // Increment the probability of a search segment if neighbors has non-zero probability. 
    public void IncreaseProbability(float speed, float deltaTime)
    {
        SearchSegment sS = GetSearchSegment();

        float ageThreshold = 0f;

        // If the segment is just seen wait before incrementing its probability
        if (sS.GetAge() < ageThreshold || !GetSearchSegment().isPropagated) return;

        bool isValidToIncrement = false;

        float maxProb = Mathf.NegativeInfinity;

        foreach (var wp1Line in GetWp1Connections())
        {
            SearchSegment wp1Ss = wp1Line.GetSearchSegment();

            if (wp1Ss.GetProbability() > SearchSegment.MinProbability) // && !wp1Ss.IsObserved && wp1Ss.IsReached())
            {
                isValidToIncrement = true;
                break;
            }

            if (maxProb < wp1Ss.GetProbability()) maxProb = wp1Ss.GetProbability();
        }

        if (!isValidToIncrement)
            foreach (var wp2Line in GetWp2Connections())
            {
                SearchSegment wp1Ss = wp2Line.GetSearchSegment();

                if (wp1Ss.GetProbability() > SearchSegment.MinProbability) // && !wp1Ss.IsObserved && wp1Ss.IsReached())
                {
                    isValidToIncrement = true;
                    break;
                }

                if (maxProb < wp1Ss.GetProbability()) maxProb = wp1Ss.GetProbability();
            }


        if (isValidToIncrement) sS.AddProbability(Properties.ProbabilityIncreaseRate * deltaTime); // * maxProb);
    }

    // Propagate the search segment probability if needed.
    public void PropagateProb()
    {
        SearchSegment sS = GetSearchSegment();

        if (sS.IsReached() && sS.isPropagated)
        {
            sS.PropagateDestination(1);
            sS.PropagateDestination(2);
        }
    }

    // Get search segments
    public SearchSegment GetSearchSegment()
    {
        return m_SearchSegment;
    }

    // Remove part of the old search segment to add a new one
    // Returns true if it is seen
    public void CheckSeenSegment(Guard guard)
    {
        Polygon foV = guard.GetFovPolygon();

        // Get the distance over the segment from the guard
        float distanceFromGuard = Vector2.Distance(GetSearchSegment().GetMidPoint(), guard.GetTransform().position);

        // Ignore segments that are too far away; to save computation
        if (distanceFromGuard > guard.GetFovRadius() * 1.5f)
            return;

        // Check the intersections with the field of view and the segment
        List<Vector2> intersections = foV.GetIntersectionWithLine(GetSearchSegment().position1,
            GetSearchSegment().position2,
            out var isIp1In, out var isIp2In);

        // if there is an intersection
        if (intersections.Count > 0)
        {
            // if there is one point in and one out then shrink segment
            if (isIp1In && !isIp2In)
            {
                GetSearchSegment().position1 = intersections[0];
            }

            if (!isIp1In && isIp2In)
            {
                GetSearchSegment().position2 = intersections[0];
            }
        }

        // Check if the segment is completely is in the field of vision
        if (IsSegmentSeen(guard)) //isIp1In && isIp2In &&
            GetSearchSegment().Seen();
        else
            GetSearchSegment().notSeen();
    }

    bool IsSegmentSeen(Guard guard)
    {
        Polygon foV = guard.GetFovPolygon();

        // Check if the mid point is in the field of view
        bool isMidPointInFov = foV.IsPointInPolygon(GetSearchSegment().GetMidPoint(), false);

        // Get the distance over the segment from the guard
        float distanceFromGuard = Vector2.Distance(GetSearchSegment().GetMidPoint(), guard.GetTransform().position);

        return (isMidPointInFov) || distanceFromGuard <= 0.3f;
    }

    // Search segment is no longer need, so it is reset.
    public void ClearSearchSegs()
    {
        m_SearchSegment.Reset();
    }


    public Vector2 GetMid()
    {
        return (wp1.GetPosition() + wp2.GetPosition()) / 2f;
    }


    public void DrawSearchSegment(string label)
    {
        m_SearchSegment?.Draw(label);
    }

    public void DrawLine()
    {
        Gizmos.DrawLine(wp1.GetPosition(), wp2.GetPosition());
    }
}