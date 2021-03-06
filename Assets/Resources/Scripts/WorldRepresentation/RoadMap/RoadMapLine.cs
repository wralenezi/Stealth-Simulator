﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadMapLine
{
    // First point
    public WayPoint wp1;

    // Second point
    public WayPoint wp2;

    // The segment that lies in this line
    private SearchSegment m_SearchSegment;

    public RoadMapLine(WayPoint _wp1, WayPoint _wp2)
    {
        wp1 = _wp1;
        wp2 = _wp2;

        m_SearchSegment = new SearchSegment(wp1, wp1.GetPosition(), wp2, wp2.GetPosition());
    }

    // Check if a way point is on this line
    public bool IsPointPartOfLine(WayPoint wp)
    {
        return (wp == wp1 || wp == wp2);
    }

    public List<RoadMapLine> GetWp1Connections()
    {
        return wp1.GetLines();
    }

    public List<RoadMapLine> GetWp2Connections()
    {
        return wp2.GetLines();
    }

    // Add a possible line where the intruder might be in
    public void SetSearchSegment(Vector2 startingPos1, Vector2 startingPos2, float prob,
        float timestamp)
    {
        m_SearchSegment.SetSsData(startingPos1, startingPos2, prob);
        m_SearchSegment.SetTimestamp(timestamp);
    }

    public void ProbabgateToSegment(Vector2 startPostion, float prob, float timeStamp)
    {
        GetSearchSegment().isPropagated = true;
        SetSearchSegment(startPostion, startPostion, prob, timeStamp);
    }

    // Expand the search segments
    public void ExpandSs(float speed, float deltaTime)
    {
        m_SearchSegment.Expand(speed, deltaTime);
    }

    // Increment the probability of a search segment if neighbors has non-zero probability. 
    public void IncreaseProbability(float deltaTime)
    {
        SearchSegment sS = GetSearchSegment();

        bool isValidToIncrement = false;

        if (GetSearchSegment().IsObserved)
            return;

        foreach (var wp1Line in GetWp1Connections())
        {
            SearchSegment wp1Ss = wp1Line.GetSearchSegment();

            if (wp1Ss.GetProbability() > SearchSegment.MinProbability && !GetSearchSegment().IsObserved)
            {
                isValidToIncrement = true;
                break;
            }
        }

        if (!isValidToIncrement)
            foreach (var wp2Line in GetWp2Connections())
            {
                SearchSegment wp1Ss = wp2Line.GetSearchSegment();

                if (wp1Ss.GetProbability() > SearchSegment.MinProbability && !GetSearchSegment().IsObserved)
                {
                    isValidToIncrement = true;
                    break;
                }
            }

        if (isValidToIncrement)
            sS.AddProbability(Properties.ProbabilityIncreaseRate * deltaTime);
    }

    // Propagate the search segment probability if needed.
    public void PropagateProb(float speed, float timeDelta)
    {
        SearchSegment sS = GetSearchSegment();

        float lineLength = Vector2.Distance(wp1.GetPosition(), wp2.GetPosition());
        // The age threshold of the search segment to propagate to other search segments
        // float ageThreshold = (speed * timeDelta) / (lineLength * Time.timeScale);// * Properties.GetMaxEdgeLength()) ;

        float ageThreshold = 0.025f * lineLength * speed; // * Properties.MaxPathDistance / Properties.PathDenom;

        if (GetSearchSegment().IsActive() && GetSearchSegment().GetAge() > ageThreshold)
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

        // Ignore segments that are too far away
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


    public void DrawSearchSegments()
    {
        if (m_SearchSegment != null)
            m_SearchSegment.Draw();
    }

    public void DrawLine()
    {
        var p1 = wp1.GetPosition();
        var p2 = wp2.GetPosition();
        var dir = (p1 - p2).normalized * 0.5f;
        var thickness = 3;
        Handles.DrawBezier(p1 - dir, p2 + dir, p1 + dir, p2 - dir, Color.red, null, thickness);

        // Gizmos.DrawLine(wp1.GetPosition(), wp2.GetPosition());
        // Handles.Label(wp1.GetPosition(), wp1.Id.ToString());
        // Handles.Label((wp1.GetPosition() + wp2.GetPosition()) / 2f,
        //     Vector2.Distance(wp1.GetPosition(), wp2.GetPosition()).ToString());
    }
}