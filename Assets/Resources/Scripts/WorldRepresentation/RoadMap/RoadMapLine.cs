using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadMapLine
{
    // First point
    public WayPoint wp1;

    // Connections of the first point
    public List<RoadMapLine> wp1Cons;

    // Second point
    public WayPoint wp2;

    // Connections of the second point
    public List<RoadMapLine> wp2Cons;

    // The segments that lies in this line
    private List<SearchSegment> m_SearchSegments;
    
    public RoadMapLine(WayPoint _wp1, WayPoint _wp2)
    {
        wp1 = _wp1;
        wp2 = _wp2;

        m_SearchSegments = new List<SearchSegment>();

        wp1Cons = new List<RoadMapLine>();
        wp2Cons = new List<RoadMapLine>();
    }

    public bool IsPointPartOfLine(WayPoint wp)
    {
        return (wp == wp1 || wp == wp2);
    }

    // Add the connected lines to this line
    public void AddLineConnections(List<RoadMapLine> roadMapLines)
    {
        foreach (var line in roadMapLines)
        {
            if (line.IsPointPartOfLine(wp1) && !line.IsPointPartOfLine(wp2))
                wp1Cons.Add(line);

            if (line.IsPointPartOfLine(wp2) && !line.IsPointPartOfLine(wp1))
                wp2Cons.Add(line);
        }
    }

    // Add a possible line where the intruder might be in
    public void AddSearchSegment(Vector2 startingPos1, Vector2 startingPos2, float prob,
        float timestamp = 0f)
    {
        SearchSegment searchSegment = new SearchSegment(wp1, startingPos1, wp2, startingPos2, prob,
            (wp1.GetPosition() + wp2.GetPosition()) / 2f);

        if (timestamp != 0f)
            searchSegment.SetTimestamp(timestamp);

        m_SearchSegments.Add(searchSegment);
    }


    // Compare two search segments if they have similar age.
    public bool IsSameAge(SearchSegment sS1, SearchSegment sS2)
    {
        return Mathf.Abs(sS1.GetAge() - sS2.GetAge()) < Properties.AgeThreshold;
    }

    // Expand the search segments
    public void ExpandSearch(float speed, float deltaTime)
    {
        // Expand the search segments
        foreach (var searchSegment in m_SearchSegments)
        {
            searchSegment.IncreaseProbability(deltaTime);
            searchSegment.Expand(speed, deltaTime);
        }

        // Resolve the collision between the search segments
        for (int i = 0; i < m_SearchSegments.Count; i++)
        for (int j = i + 1; j < m_SearchSegments.Count; j++)
            if (CheckForOverlapping(i, j))
            {
                i = 0;
                break;
            }
    }

    // Get search segments
    public List<SearchSegment> GetSearchSegments()
    {
        return m_SearchSegments;
    }

    // Check for the overlapping between two search segments
    // Will return true when a search segment has been remove for overlap resolution.
    private bool CheckForOverlapping(int first, int sec)
    {
        SearchSegment sS1 = m_SearchSegments[first];
        SearchSegment sS2 = m_SearchSegments[sec];


        // Determine the orientation of the search segments
        Vector2 sS1mid = (sS1.position1 + sS1.position2) / 2f;
        Vector2 sS2mid = (sS2.position1 + sS2.position2) / 2f;

        // Check the distance from one side
        float distanceSs1mid = Vector2.Distance(sS1.GetDestination1(), sS1mid);
        float distanceSs2mid = Vector2.Distance(sS1.GetDestination1(), sS2mid);

        // Check the orientation of the line segments to each other.
        bool sS1LeftSs2 = distanceSs1mid < distanceSs2mid;

        if (sS1LeftSs2)
        {
            // Check if the left side of the right segment intersect the left/ 
            bool isIntersect = GeometryHelper.IsPointOnLine(sS1.position1, sS1.position2,
                sS2.position1);

            // If no intersection then end
            if (!isIntersect)
                return false;

            return ResolveOverlapping(sS1, sS2);
        }
        else
        {
            bool isIntersect = GeometryHelper.IsPointOnLine(sS2.position1, sS2.position2,
                sS1.position1);

            // If no intersection then end
            if (!isIntersect)
                return false;

            return ResolveOverlapping(sS2, sS1);
        }
    }

    // Resolve the overlapping between two search segments; 
    private bool ResolveOverlapping(SearchSegment leftSegment, SearchSegment rightSegment)
    {
        // if both have the same prob then merge them
        if (IsSameAge(leftSegment, rightSegment))
        {
            leftSegment.position2 = rightSegment.position2;
            m_SearchSegments.Remove(rightSegment);
            return true;
        }

        // else keep them from overlapping
        Vector2 temp = leftSegment.position2;
        leftSegment.position2 = rightSegment.position1;
        rightSegment.position1 = temp + (temp - leftSegment.position2) * 0.1f;

        return false;
    }

    // Adjust the search segment if seen by guards
    public void ModSearchSegments(List<Guard> guards)
    {
        // Loop through the search segments
        for (int i = 0; i < m_SearchSegments.Count; i++)
        {
            SearchSegment curSeg = m_SearchSegments[i];

            foreach (var guard in guards)
            {
                Polygon foV = guard.GetFoV();

                // Trim the parts seen by the guards and reset the section if it is all seen 
                TrimSearchSegment(curSeg, foV);
            }
        }
    }

    // Remove part of the old search segment to add a new one
    private void TrimSearchSegment(SearchSegment curSeg, Polygon foV)
    {
        List<Vector2> intersections = foV.GetIntersectionWithLine(curSeg.position1, curSeg.position2,
            out var isIp1In, out var isIp2In);

        // isIp1In && isIp2In &&
        // Check if the segment is completely is in the field of vision
        if (isIp1In && isIp2In && foV.IsPointInPolygon(curSeg.GetMidPoint(), false))
            curSeg.Reset();

        // if there is an intersection
        if (intersections.Count > 0)
        {
            // if there is one point in and one out then shrink segment
            if (isIp1In && !isIp2In)
            {
                curSeg.position1 = intersections[0];
            }

            if (!isIp1In && isIp2In)
            {
                curSeg.position2 = intersections[0];
            }

            // // if there are many intersections and both points are out the 
            // if (!isIp1In && !isIp2In)
            // {
            //     // Get the first segment out
            //     Vector2 tempPosition2 = curSeg.position2;
            //     curSeg.position2 = GeometryHelper.GetClosestPointFromList(curSeg.position1, intersections);
            //
            //     Vector2 newPosition1 = GeometryHelper.GetClosestPointFromList(tempPosition2, intersections);
            //
            //     // Create a new search segment to be at the other side
            //     // AddSearchSegment(newPosition1, tempPosition2, curSeg.direction, curSeg.GetProbability(),
            //     //     curSeg.GetTimeStamp());
            // }
        }
    }

    // Expand the search segment; return true if the segment is not removed, else false 
    // private bool ExpandSearchSegment(SearchSegment curSeg, Polygon foV)
    // {
    //     List<Vector2> intersections = foV.GetIntersectionWithLine(curSeg.position1,
    //         curSeg.position2,
    //         out var isIp1In, out var isIp2In);
    //
    //     // if there is an intersection
    //     if (intersections.Count > 0)
    //     {
    //         // if there is one point in and one out then shrink segment
    //         if (isIp1In && !isIp2In)
    //         {
    //             curSeg.position1 = curSeg.destination1.GetPosition();
    //             curSeg.position2 = intersections[0];
    //         }
    //
    //         if (!isIp1In && isIp2In)
    //         {
    //             curSeg.position1 = intersections[0];
    //             curSeg.position2 = curSeg.destination2.GetPosition();
    //         }
    //
    //         // if there are many intersections and both points are out the 
    //         if (!isIp1In && !isIp2In)
    //         {
    //             // Get the first segment out
    //             curSeg.position1 =
    //                 GeometryHelper.GetClosestPointFromList(curSeg.destination1.GetPosition(), intersections);
    //             curSeg.position2 =
    //                 GeometryHelper.GetClosestPointFromList(curSeg.destination2.GetPosition(), intersections);
    //         }
    //
    //
    //         // Update the timestamp of the segment
    //         curSeg.SetTimestamp(StealthArea.episodeTime);
    //     }
    //
    //     return false;
    // }


    public void ClearSearchSegs()
    {
        m_SearchSegments.Clear();
    }


    public void DrawSearchSegments()
    {
        foreach (var sS in m_SearchSegments)
            sS.Draw();
    }

    public void DrawLine()
    {
        Gizmos.DrawLine(wp1.GetPosition(), wp2.GetPosition());
        // Handles.Label(wp1.GetPosition(), wp1.Id.ToString());
        // Handles.Label((wp1.GetPosition() + wp2.GetPosition()) / 2f,
        //     Vector2.Distance(wp1.GetPosition(), wp2.GetPosition()).ToString());
    }
}