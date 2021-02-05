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

    // Line Segments of the road map
    private List<RoadMapLine> m_Lines;

    // AdHoc way point placed on the map when the intruder is seen in a place far from the road map.
    private WayPoint m_AdHocWp;
    private RoadMapLine m_AdHocRmLine;

    private MapRenderer m_mapRenderer;


    public RoadMap(List<WayPoint> wayPoints, MapRenderer _mapRenderer)
    {
        m_WayPoints = wayPoints;
        m_mapRenderer = _mapRenderer;

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


    public List<RoadMapLine> GetLines()
    {
        return m_Lines;
    }


    // Find the projection point on the road map
    // Find the closest projection point 
    public InterceptionPoint GetInterceptionPointOnRoadMap(Vector2 position, Vector2 dir)
    {
        float minDistance = Mathf.Infinity;
        // Position
        Vector2? projectionOnRoadMap = null;
        // Direction of the phantom node movement
        //Vector2? direction = null;
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

                destination = (cosineAngle > 0) ? line.wp2 : line.wp1;
                source = (cosineAngle > 0) ? line.wp1 : line.wp2;
            }
        }

        return new InterceptionPoint(projectionOnRoadMap.Value,
            source, destination, 0);
    }


    // Add the search line segment for searching for an intruder in the search phase
    // This is done by finding the closest point to the intruder on the road map
    public void InsertSearchLineSegment(Vector2 position, Vector2 dir)
    {
        float minDistance = Mathf.Infinity;
        // Positions
        List<Vector2> projections = new List<Vector2>();
        // Direction of the phantom node movement
        List<Vector2> directions = new List<Vector2>();

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
            projections.Add(pro);

            // The distance from the projection point to the intruder position
            float distance = Vector2.Distance(position, pro);
            distances.Add(distance);

            // Get the normalized direction of the road map edge
            Vector2 edgeDir = line.wp1.GetPosition() - line.wp2.GetPosition();
            edgeDir = edgeDir.normalized;
            directions.Add(edgeDir);

            // Get the normalized Velocity of the intruder
            Vector2 velocityNorm = dir.normalized;

            // Get the cosine of the smalled angel between the road map edge and velocity; to measure the alignment between the vectors
            // The closer to one the more aligned 
            float cosineAngle = Vector2.Dot(edgeDir, velocityNorm);
            angleDiffs.Add(Mathf.Abs(cosineAngle));
        }


        // Get the index of the closest line
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


        // check if the distance to both lines are similar then create two segments on both projections.
        // if (Mathf.Abs(distances[closestLineIndex] - distances[secondClosestLineIndex]) < 0.5f)
        // {
        //     m_Lines[closestLineIndex].AddSearchSegment();
        // }

        // 
        closestRoadMapLine.AddSearchSegment(projections[bestFit] + directions[bestFit],
            projections[bestFit] - directions[bestFit],
            directions[bestFit], 1f);

        // for (int i = 0; i < 5; i++)
        //     ExpandSearchSegments(0.5f);

        EditorApplication.isPaused = true;
    }

    // Create a search segment that doesn't belong to the road map.
    public void CreateArbitrarySearchSegment(Vector2 position, Vector2 dir)
    {
        RemoveRoadLineMap();

        float minDistance = Mathf.Infinity;
        // Direction of the phantom node movement
        List<Vector2> directions = new List<Vector2>();

        // Line the search segment lies on
        RoadMapLine closestRoadMapLine = null;

        // List of distance and direction difference of the lines and agent
        List<float> distances = new List<float>();
        List<float> angleDiffs = new List<float>();

        foreach (var wp in m_WayPoints)
        {
            // The distance from the way point to the intruder position
            float distance = Vector2.Distance(position, wp.GetPosition());
            distances.Add(distance);

            // Get the normalized direction of intruder's position to the waypoint
            Vector2 toWayPointDir = wp.GetPosition() - position;
            toWayPointDir = toWayPointDir.normalized;
            directions.Add(toWayPointDir);

            // Get the normalized Velocity of the intruder
            Vector2 velocityNorm = dir.normalized;

            // Get the cosine of the smalled angel between the road map edge and velocity; to measure the alignment between the vectors
            // The closer to one the more aligned 
            float cosineAngle = Vector2.Dot(toWayPointDir, velocityNorm);
            angleDiffs.Add(cosineAngle);
        }

        int closestWpIndex = 0;

        // Get the index of the closest way point that is in front of intruder 
        minDistance = Mathf.Infinity;
        for (int i = 0; i < distances.Count; i++)
        {
            if (!m_mapRenderer.VisibilityCheck(position, m_WayPoints[i].GetPosition()) || angleDiffs[i] < 0.7f)
                continue;

            if (minDistance > distances[i])
            {
                closestWpIndex = i;
                minDistance = distances[i];
            }
        }

        // Add a new line to the road map.
        AddRoadLineMap(position, m_WayPoints[closestWpIndex]);


        // 
        m_AdHocRmLine.AddSearchSegment(position,
            m_WayPoints[closestWpIndex].GetPosition(),
            directions[closestWpIndex], 1f);

        for (int i = 0; i < 2; i++)
            ExpandSearchSegments(0.5f);

        // EditorApplication.isPaused = true;
    }

    // Add the adhoc roadMap line to the road map, and connect it to an existing way point.
    private void AddRoadLineMap(Vector2 newWpPosition, WayPoint destWp)
    {
        m_AdHocWp = new WayPoint(newWpPosition);

        // Connect the way point to the road map.
        m_AdHocWp.AddEdge(destWp);
        destWp.AddEdge(m_AdHocWp);

        // Add the line to the list of lines of the road map.
        m_AdHocRmLine = new RoadMapLine(m_AdHocWp, destWp);
        m_Lines.Add(m_AdHocRmLine);
    }

    // Remove the ad hoc way point and its line
    private void RemoveRoadLineMap()
    {
        if (m_AdHocWp == null || m_AdHocRmLine == null)
            return;

        m_Lines.Remove(m_AdHocRmLine);

        m_AdHocWp.GetConnections()[0].RemoveEdge(m_AdHocWp);
        m_AdHocWp.RemoveEdge(m_AdHocWp.GetConnections()[0]);

        m_AdHocRmLine = null;
        m_AdHocWp = null;
    }


    // Trim and remove search segments seen by guards
    public void SeePossibleIntruderPaths(List<Guard> guards)
    {
        // Cut the search segments when seen or add new ones.
        foreach (var line in m_Lines)
            line.ModSearchSegments(guards);

        // Modify the seen road map nodes
        foreach (var wp in m_WayPoints)
            wp.Seen(guards);
    }

    // Get the mid point of the best search segment
    public Vector2 GetBestSearchSegment(Guard requestingGuard, List<Guard> guards, Intruder intruder,
        List<MeshPolygon> navMesh)
    {
        SearchSegment bestSs = null;
        float maxFitnessValue = Mathf.NegativeInfinity;

        // the weights for the properties of the search segment
        
        
        // Loop through the search segments in the lines
        foreach (var line in m_Lines)
        foreach (var sS in line.GetSearchSegments())
        {
            // Get the distance of the closest goal other guards are coming to visit
            float minGoalDistance = Mathf.Infinity;

            foreach (var guard in guards)
            {
                // Skip the guard without a goal
                if (guard.GetGoal() == null)
                    continue;

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
            float ssFitness = sS.GetFitness();

            // Reduce the fitness if there are other guards going there
            if (minGoalDistance != Mathf.Infinity)
                ssFitness = ssFitness + minGoalDistance * 0.01f;

            // ssFitness = ssFitness * (1 / distanceToGuard);

            if (maxFitnessValue < ssFitness)
            {
                maxFitnessValue = ssFitness;
                bestSs = sS;
            }
        }

        if (bestSs == null)
            return intruder.GetLastKnownLocation();

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

    public void DrawWayPoints()
    {
        foreach (var wp in m_WayPoints)
        {
            Handles.Label(wp.GetPosition(), wp.Id.ToString());
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