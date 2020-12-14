using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

// The RoadMap the guards uses to for the search task
public class RoadMap
{
    // Way Points of the road map
    private List<WayPoint> m_WayPoints;

    // Way points that are dead ends
    private List<WayPoint> m_endPoints;

    // Line Segments of the road map
    private List<RoadMapLine> m_Lines;


    public RoadMap(List<WayPoint> wayPoints)
    {
        m_WayPoints = wayPoints;
        PopulateEndNodes();
        PopulateLines();
    }


    // Populate the end nodes on the road map
    private void PopulateEndNodes()
    {
        m_endPoints = new List<WayPoint>();

        foreach (var wp in m_WayPoints)
            if (wp.GetConnections().Count == 1)
                m_endPoints.Add(wp);
    }

    // Populate the unique line segments of the map
    private void PopulateLines()
    {
        m_Lines = new List<RoadMapLine>();

        foreach (var wp in m_WayPoints)
        foreach (var con in wp.GetConnections())
        {
            // Check if the connection already exist
            bool isFound = false;

            foreach (var line in m_Lines)
                if (line.IsPointPartOfLine(wp) && line.IsPointPartOfLine(con))
                {
                    isFound = true;
                    break;
                }

            if (!isFound)
            {
                // Prioritize adding left to right, top to down
                if ((wp.GetPosition().x < con.GetPosition().x) ||
                    ((wp.GetPosition().x == con.GetPosition().x) && (wp.GetPosition().y > con.GetPosition().y)))
                    m_Lines.Add(new RoadMapLine(wp, con));
                else
                    m_Lines.Add(new RoadMapLine(con, wp));
            }
        }

        foreach (var line in m_Lines)
            line.AddLineConnections(m_Lines);


        foreach (var wp in m_WayPoints)
            wp.AddLines(m_Lines);
    }


    // Find the projection point on the road map
    // Find the closest projection point 
    public InterceptionPoint GetInterceptionPointOnRoadMap(Vector2 position, Vector2 dir)
    {
        float minDistance = Mathf.Infinity;
        // Position
        Vector2? projectionOnRoadMap = null;
        // Direction of the phantom node movement
        Vector2? direction = null;
        // Destination of the phantom node
        WayPoint destination = null;
        // Source of the phantom
        WayPoint source = null;


        foreach (var line in m_Lines)
        {
            // Get the point projection on the line
            Vector2 pro = GeometryHelper.ClosestProjectionOnSegment(line.wp1.GetPosition(), line.wp2.GetPosition(),
                position);

            // The distance from the projection point to the intruder position
            float distance = Vector2.Distance(position, pro);

            if (distance < minDistance)
            {
                minDistance = distance;
                projectionOnRoadMap = pro;

                // Get the normalized direction of the road map edge
                Vector2 edgeDir = line.wp1.GetPosition() - line.wp2.GetPosition();
                edgeDir = edgeDir.normalized;

                // Get the normalized Velocity of the intruder
                Vector2 velocityNorm = dir.normalized;

                // Get the cosine of the smalled angel between the road map edge and velocity; to measure the alignment between the vectors
                // The closer to one the more aligned 
                float cosineAngle = Vector2.Dot(edgeDir, velocityNorm);

                direction = edgeDir * cosineAngle;

                destination = (cosineAngle > 0) ? line.wp2 : line.wp1;
                source = (cosineAngle > 0) ? line.wp1 : line.wp2;
            }
        }

        return new InterceptionPoint(projectionOnRoadMap.Value,
            source, destination, 0);
    }


    // Get the search line segment for searching for an intruder in the search phase
    public void InsertSearchLineSegment(Vector2 position, Vector2 dir)
    {
        float minDistance = Mathf.Infinity;
        // Position
        Vector2? projectionOnRoadMap = null;
        // Direction of the phantom node movement
        Vector2? direction = null;

        // Line the search segment lies on
        RoadMapLine closestRoadMapLine = null;

        // List of distance and direction difference of the lines and agent
        List<float> distances = new List<float>();
        List<float> angleDiffs = new List<float>();

        int closestLineIndex = 0;
        int secondClosestLineIndex = 0;

        foreach (var line in m_Lines)
        {
            // Get the point projection on the line
            Vector2 pro = GeometryHelper.ClosestProjectionOnSegment(line.wp1.GetPosition(), line.wp2.GetPosition(),
                position);

            // The distance from the projection point to the intruder position
            float distance = Vector2.Distance(position, pro);
            distances.Add(distance);

            // Get the normalized direction of the road map edge
            Vector2 edgeDir = line.wp1.GetPosition() - line.wp2.GetPosition();
            edgeDir = edgeDir.normalized;

            // Get the normalized Velocity of the intruder
            Vector2 velocityNorm = dir.normalized;

            // Get the cosine of the smalled angel between the road map edge and velocity; to measure the alignment between the vectors
            // The closer to one the more aligned 
            float cosineAngle = Vector2.Dot(edgeDir, velocityNorm);
            angleDiffs.Add(Mathf.Abs(cosineAngle));


            if (distance < minDistance)
            {
                minDistance = distance;
                projectionOnRoadMap = pro;
                direction = edgeDir * cosineAngle;

                closestRoadMapLine = line;
            }
        }


        // Get the closest index line
        minDistance = Mathf.Infinity;
        for (int i = 0; i < distances.Count; i++)
        {
            if (minDistance > distances[i])
            {
                closestLineIndex = i;
                minDistance = distances[i];
            }
        }

        // Get the index of second closest line 
        minDistance = Mathf.Infinity;
        for (int i = 0; i < distances.Count; i++)
        {
            if (i == closestLineIndex)
                continue;

            if (minDistance > distances[i])
            {
                secondClosestLineIndex = i;
                minDistance = distances[i];
            }
        }

        int bestFit;
        // decide the best fit line between the first and sec based on the highest cosine.
        if (angleDiffs[closestLineIndex] >= angleDiffs[secondClosestLineIndex])
            bestFit = closestLineIndex;
        else
            bestFit = secondClosestLineIndex;

        closestRoadMapLine = m_Lines[bestFit];

        closestRoadMapLine.AddSearchSegment(projectionOnRoadMap.Value + direction.Value,
            projectionOnRoadMap.Value + direction.Value,
            direction.Value, 1f);
    }


    // Trim and remove search segments seen by guards
    public void SeePossibleIntruderPaths(List<Guard> guards)
    {
        // Modify the search segments
        foreach (var line in m_Lines)
            line.ModSearchSegments(guards);

        // Modify the seen road map nodes
        foreach (var wp in m_WayPoints)
            wp.Seen(guards);
    }

    // Get the best search segment
    public Vector2 GetBestSearchSegment(Guard requestingGuard, List<Guard> guards, List<MeshPolygon> navMesh)
    {
        SearchSegment bestSs = null;
        float maxFitnessValue = Mathf.NegativeInfinity;

        foreach (var line in m_Lines)
        foreach (var sS in line.GetSearchSegments())
        {
            // Get the distance of the closest goal other guards are coming to
            float minGoalDistance = Mathf.Infinity;

            foreach (var guard in guards)
            {
                // Skip the guard without a goal
                if (guard.GetGoal() == null)
                    continue;

                // float distanceToGuardGoal = Vector2.Distance((sS.position1 + sS.position2) / 2f, guard.GetGoal().Value);
                float distanceToGuardGoal = PathFinding.GetShortestPathDistance(navMesh,
                    (sS.position1 + sS.position2) / 2f, guard.GetGoal().Value);

                if (minGoalDistance > distanceToGuardGoal)
                {
                    minGoalDistance = distanceToGuardGoal;
                }
            }


            // Get the distance from the requesting guard
            float distanceToGuard = PathFinding.GetShortestPathDistance(navMesh, (sS.position1 + sS.position2) / 2f,
                requestingGuard.transform.position);

            // Calculate the fitness of the search segment
            // start with the probability
            float ssFitness = sS.GetProbability();

            // Reduce the fitness if there are other guards going there
            if (minGoalDistance != Mathf.Infinity)
                ssFitness = ssFitness + minGoalDistance * 0.01f;

            if (maxFitnessValue < ssFitness)
            {
                maxFitnessValue = ssFitness;
                bestSs = sS;
            }
        }

        return (bestSs.position1 + bestSs.position2) / 2f;
    }


    // Expand the search segments
    public void ExpandSearchSegments(float speed)
    {
        foreach (var line in m_Lines)
            line.ExpandSearch(speed);
    }

    public void ClearSearchSegments()
    {
        foreach (var line in m_Lines)
            line.ClearSearchSegs();
    }

    // Get a random road map node
    public Vector2 GetRandomRoadMapNode()
    {
        int randomIndex = Random.Range(0, m_WayPoints.Count);
        return m_WayPoints[randomIndex].GetPosition();
    }

    // Clear the probabilities in the road map nodes
    public void ClearProbabilities()
    {
        foreach (var wp in m_WayPoints)
            wp.SetProbability(0f);
    }

    // Render the draw interception points & search segments 
    public void DrawSearchSegments()
    {
        // Render search segments
        foreach (var line in m_Lines)
            line.DrawSearchSegments();
    }

    // Render the lines of road map
    public void DrawRoadMap()
    {
        foreach (var line in m_Lines)
        {
            line.DrawLine();
        }
    }
}


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
    public void AddSearchSegment(Vector2 startingPos1, Vector2 startingPos2, Vector2 dir, float prob,
        float timestamp = 0f)
    {
        SearchSegment searchSegment = new SearchSegment(wp1, startingPos1, wp2, startingPos2, dir, prob);

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
    public void ExpandSearch(float speed)
    {
        // Expand the search segments
        foreach (var searchSegment in m_SearchSegments)
        {
            searchSegment.IncreaseProbability();
            searchSegment.Expand(speed);
        }

        // Resolve the collision between the search segments
        for (int i = 0; i < m_SearchSegments.Count; i++)
        for (int j = i + 1; j < m_SearchSegments.Count; j++)
            if (CheckForOverlapping(i, j))
            {
                i--;
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
        float distanceSs1mid = Vector2.Distance(sS1.destination1.GetPosition(), sS1mid);
        float distanceSs2mid = Vector2.Distance(sS1.destination1.GetPosition(), sS2mid);
        bool sS1LeftSs2 = distanceSs1mid < distanceSs2mid;


        if (sS1LeftSs2)
        {
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
        rightSegment.position1 = temp;
        return false;
    }

    public void ModSearchSegments(List<Guard> guards)
    {
        for (int i = 0; i < m_SearchSegments.Count; i++)
        {
            SearchSegment curSeg = m_SearchSegments[i];

            foreach (var guard in guards)
            {
                Polygon foV = guard.GetFoV();

                // Trim the parts seen by the guards and remove the section if it is all seen 
                if (TrimSearchSegment(curSeg, foV))
                    i--;
            }
        }
    }

    // Remove part of the old search segment to add a new one
    private bool TrimSearchSegment(SearchSegment curSeg, Polygon foV)
    {
        List<Vector2> intersections = foV.GetIntersectionWithLine(curSeg.position1, curSeg.position2,
            out var isIp1In, out var isIp2In);

        if (isIp1In && isIp2In && foV.IsPointInPolygon((curSeg.position1 + curSeg.position2) / 2f, false))
        {
            m_SearchSegments.Remove(curSeg);
            return true;
        }

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

            // if there are many intersections and both points are out the 
            if (!isIp1In && !isIp2In)
            {
                // Get the first segment out
                Vector2 tempPosition2 = curSeg.position2;
                curSeg.position2 = GeometryHelper.GetClosestPointFromList(curSeg.position1, intersections);

                Vector2 newPosition1 = GeometryHelper.GetClosestPointFromList(tempPosition2, intersections);

                // Create a new search segment to be at the other side
                AddSearchSegment(newPosition1, tempPosition2, curSeg.direction, curSeg.GetProbability(),
                    curSeg.GetTimeStamp());
            }
        }

        return false;
    }

    // Expand the search segment; return true if the segment is not removed, else false 
    private bool ExpandSearchSegment(SearchSegment curSeg, Polygon foV)
    {
        List<Vector2> intersections = foV.GetIntersectionWithLine(curSeg.position1,
            curSeg.position2,
            out var isIp1In, out var isIp2In);

        // if there is an intersection
        if (intersections.Count > 0)
        {
            // if there is one point in and one out then shrink segment
            if (isIp1In && !isIp2In)
            {
                curSeg.position1 = curSeg.destination1.GetPosition();
                curSeg.position2 = intersections[0];
            }

            if (!isIp1In && isIp2In)
            {
                curSeg.position1 = intersections[0];
                curSeg.position2 = curSeg.destination2.GetPosition();
            }

            // if there are many intersections and both points are out the 
            if (!isIp1In && !isIp2In)
            {
                // Get the first segment out
                curSeg.position1 =
                    GeometryHelper.GetClosestPointFromList(curSeg.destination1.GetPosition(), intersections);
                curSeg.position2 =
                    GeometryHelper.GetClosestPointFromList(curSeg.destination2.GetPosition(), intersections);
            }


            // Update the timestamp of the segment
            curSeg.SetTimestamp(StealthArea.episodeTime);
        }

        return false;
    }


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
    }
}