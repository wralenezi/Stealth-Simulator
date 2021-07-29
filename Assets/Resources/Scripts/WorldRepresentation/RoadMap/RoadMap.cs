using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

// The RoadMap the guards uses to for the search task
public class RoadMap
{
    // Way Points of the road map
    private List<WayPoint> m_WayPoints;

    // Way points that are dead ends
    private List<WayPoint> m_endPoints;

    // Actual nodes in the roadmap (since m_Waypoints contain all waypoints of the divided segments)
    private List<WayPoint> m_WpsActual;


    private SAT m_Sat;

    // Line Segments of the road map
    private List<RoadMapLine> m_Lines;

    private List<Vector2> m_intersectionsWithRoadmap;

    // AdHoc way point placed on the map when the intruder is seen in a place far from the road map.
    private WayPoint m_AdHocWp;
    private RoadMapLine m_AdHocRmLine;

    private MapRenderer m_mapRenderer;

    public RoadMap(SAT sat, MapRenderer _mapRenderer)
    {
        m_Sat = sat;
        m_WayPoints = m_Sat.GetRoadMap();
        m_mapRenderer = _mapRenderer;
        m_intersectionsWithRoadmap = new List<Vector2>();

        PopulateEndNodes();
        PopulateLines();
        PopulateWayPoints();
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
                m_Lines.Add(new RoadMapLine(con, wp));
        }

        // Add the line connected to the way point
        foreach (var wp in m_WayPoints)
            wp.AddLines(m_Lines);
    }


    // Get the reference of actual waypoints
    private void PopulateWayPoints()
    {
        m_WpsActual = new List<WayPoint>();

        foreach (var wp in m_WayPoints)
            if (wp.Id != 0)
                m_WpsActual.Add(wp);
    }


    public List<RoadMapLine> GetLines()
    {
        return m_Lines;
    }


    // Get the closest projection point to a position@param on the road map.
    // The closest two points will be considered, and the tie breaker will be the how the road map line is aligned with dir@param 
    public WayPoint GetClosestWp(Vector2 position, Vector2 dir)
    {
        // List of distances and direction differences of the parameters.
        List<float> distances = new List<float>();
        List<float> angleDiffs = new List<float>();

        // Loop through the way points
        foreach (var wp in m_WayPoints)
        {
            // The distance from the way point to the intruder position
            float distance = Vector2.Distance(position, wp.GetPosition());
            distances.Add(distance);

            // Get the normalized direction of intruder's position to the way point.
            Vector2 toWayPointDir = wp.GetPosition() - position;
            toWayPointDir = toWayPointDir.normalized;

            // Get the normalized Velocity of the intruder
            Vector2 velocityNorm = dir.normalized;

            // Get the cosine of the smalled angel between the road map edge and velocity; to measure the alignment between the vectors
            // The closer to one the more aligned 
            float cosineAngle = Vector2.Dot(toWayPointDir, velocityNorm);
            angleDiffs.Add(cosineAngle);
        }

        // Get the index of the closest way point that is in front of intruder
        int closestFrontalWpIndex = -1;
        float minFrontalDistance = Mathf.Infinity;

        // The closest point regardless of direction
        int closestWpIndex = -1;
        float minDistance = Mathf.Infinity;

        for (int i = 0; i < distances.Count; i++)
        {
            // if not visible then skip
            if (!m_mapRenderer.VisibilityCheck(position, m_WayPoints[i].GetPosition()))
                continue;

            if (minDistance > distances[i])
            {
                closestWpIndex = i;
                minDistance = distances[i];
            }


            // If not in front skip
            if (angleDiffs[i] < 0.6f)
                continue;

            if (minFrontalDistance > distances[i])
            {
                closestFrontalWpIndex = i;
                minFrontalDistance = distances[i];
            }
        }

        // If nothing is in the front then just get a visible closest node
        if (closestFrontalWpIndex == -1)
            return m_WayPoints[closestWpIndex];

        try
        {
            return m_WayPoints[closestFrontalWpIndex];
        }
        catch (Exception e)
        {
            Debug.Log(closestFrontalWpIndex);
            return null;
        }
    }


    // Start the flow of probability from the closest way point aligned with the direction. 
    public void CommenceProbabilityFlow(Vector2 position, Vector2 dir)
    {
        WayPoint closestWp = GetClosestWp(position, dir);

        foreach (var line in closestWp.GetLines())
        {
            line.SetSearchSegment(closestWp.GetPosition(), closestWp.GetPosition(), 1f, StealthArea.GetElapsedTime());
        }
    }

    // Create a search segment that doesn't belong to the road map. The line starts from position@param and connects to the closest roadMap node
    // in the direction of dir@param. 
    public void CreateArbitraryRoadMapLine(Vector2 position, Vector2 dir)
    {
        // Remove the old arbitrary road map line.
        RemoveRoadLineMap();

        // // Cast a ray from the last known position and direction, if it intersect with the road map then create a search segment there
        // Vector2 dest = position + dir;
        //
        // Vector2? intersection = GetClosestIntersectionWithRoadmap(position, dest);
        //
        // if (intersection != null)
        // {
        //     // Add a new line to the road map.
        //     AddRoadLineMap(position, intersection);
        //     
        //     m_AdHocRmLine.SetSearchSegment(position,
        //         position,
        //         1f,StealthArea.episodeTime);
        // }

        // List of distances and direction differences of the parameters.
        List<float> distances = new List<float>();
        List<float> angleDiffs = new List<float>();

        // Loop through the way points
        foreach (var wp in m_WayPoints)
        {
            // The distance from the way point to the intruder position
            float distance = Vector2.Distance(position, wp.GetPosition());
            distances.Add(distance);

            // Get the normalized direction of intruder's position to the way point.
            Vector2 toWayPointDir = wp.GetPosition() - position;
            toWayPointDir = toWayPointDir.normalized;

            // Get the normalized Velocity of the intruder
            Vector2 velocityNorm = dir.normalized;

            // Get the cosine of the smalled angel between the road map edge and velocity; to measure the alignment between the vectors
            // The closer to one the more aligned 
            float cosineAngle = Vector2.Dot(toWayPointDir, velocityNorm);
            angleDiffs.Add(cosineAngle);
        }

        int closestWpIndex = 0;

        // Get the index of the closest way point that is in front of intruder 
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < distances.Count; i++)
        {
            if (!m_mapRenderer.VisibilityCheck(position, m_WayPoints[i].GetPosition()) || angleDiffs[i] < 0.6f)
                continue;

            if (minDistance > distances[i])
            {
                closestWpIndex = i;
                minDistance = distances[i];
            }
        }

        // // Add a new line to the road map.
        // AddRoadLineMap(position, m_WayPoints[closestWpIndex]);
        //
        // m_AdHocRmLine.SetSearchSegment(position,
        //     position,
        //     1f,StealthArea.episodeTime);


        WayPoint wp1 = m_WayPoints[closestWpIndex];
        foreach (var line in wp1.GetLines())
        {
            line.SetSearchSegment(wp1.GetPosition(), wp1.GetPosition(), 1f, StealthArea.GetElapsedTime());
        }
    }


    public RoadMapLine GetClosestLine(Vector2 position)
    {
        RoadMapLine closetLine = null;
        float closetDistance = Mathf.Infinity;

        foreach (var line in m_Lines)
        {
            float distance = Vector2.Distance(position, line.wp1.GetPosition());

            if (distance < closetDistance && m_mapRenderer.VisibilityCheck(position, line.wp1.GetPosition()))
            {
                closetDistance = distance;
                closetLine = line;
            }

            distance = Vector2.Distance(position, line.wp2.GetPosition());

            if (distance < closetDistance && m_mapRenderer.VisibilityCheck(position, line.wp2.GetPosition()))
            {
                closetDistance = distance;
                closetLine = line;
            }
        }

        return closetLine;
    }


    private Vector2? GetClosestIntersectionWithRoadmap(Vector2 start, Vector2 end)
    {
        m_intersectionsWithRoadmap.Clear();

        foreach (var line in m_Lines)
        {
            Vector2 intersection = GeometryHelper.GetIntersectionPointCoordinates(start, end, line.wp1.GetPosition(),
                line.wp2.GetPosition(),
                true, out bool isFound);

            if (isFound)
                m_intersectionsWithRoadmap.Add(intersection);
        }

        if (m_intersectionsWithRoadmap.Count == 0)
            return null;

        Vector2 closestIntersection = m_intersectionsWithRoadmap[0];
        float closestDistance = Vector2.Distance(start, closestIntersection);

        for (int i = 1; i < m_intersectionsWithRoadmap.Count; i++)
        {
            float distance = Vector2.Distance(start, m_intersectionsWithRoadmap[i]);

            if (distance < closestDistance)
            {
                closestIntersection = m_intersectionsWithRoadmap[i];
                closestDistance = distance;
            }
        }

        return closestIntersection;
    }


    // Add the adhoc roadMap line to the road map, and connect it to an existing way point.
    private void AddRoadLineMap(Vector2 newWpPosition, WayPoint destWp)
    {
        m_AdHocWp = new WayPoint(newWpPosition);

        // Connect the way point to the road map.
        m_AdHocWp.Connect(destWp);
        // m_AdHocWp.AddEdge(destWp);
        // destWp.AddEdge(m_AdHocWp);

        m_AdHocRmLine = new RoadMapLine(m_AdHocWp, destWp);

        m_AdHocWp.AddLine(m_AdHocRmLine);
        destWp.AddLine(m_AdHocRmLine);

        // Add the line to the list of lines of the road map.
        m_Lines.Add(m_AdHocRmLine);
    }

    // Remove the ad hoc way point and its line
    private void RemoveRoadLineMap()
    {
        if (m_AdHocWp == null || m_AdHocRmLine == null)
            return;

        m_Lines.Remove(m_AdHocRmLine);

        m_AdHocWp.RemoveLine(m_AdHocRmLine);
        m_AdHocWp.GetConnections()[0].RemoveLine(m_AdHocRmLine);


        m_AdHocWp.GetConnections()[0].RemoveEdge(m_AdHocWp);
        m_AdHocWp.RemoveEdge(m_AdHocWp.GetConnections()[0]);

        m_AdHocRmLine = null;
        m_AdHocWp = null;
    }


    public void ClearSearchSegments()
    {
        foreach (var line in m_Lines)
            line.ClearSearchSegs();
    }

    // Get a random road map node
    public Vector2 GetRandomRoadMapNode()
    {
        int randomIndex = Random.Range(0, m_WpsActual.Count);
        return m_WpsActual[randomIndex].GetPosition();
    }
    
    
    
    

    // Render the draw interception points & search segments 
    public void DrawSearchSegments()
    {
        // Render search segments
        foreach (var line in m_Lines)
            line.DrawSearchSegments();
    }

    public void DrawWayPoints()
    {
        foreach (var wp in m_WayPoints)
        {
            // Handles.Label(wp.GetPosition(), wp.Id.ToString());
        }
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