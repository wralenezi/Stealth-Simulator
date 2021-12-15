using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

// The RoadMap the guards uses to for the search task
public class RoadMap
{
    // Way Points of the road map (including way points introduced by dividing the edges)
    private List<WayPoint> m_WayPoints;

    // Line Segments of the road map ( the divided segments)
    private List<RoadMapLine> m_Lines;

    // Way points that are dead ends
    private List<WayPoint> m_endPoints;

    // Actual nodes in the road map (since m_Waypoints contain all waypoints of the divided segments)
    private List<WayPoint> m_WpsActual;

    // The original lines of the road map before dividing the edges.
    private List<RoadMapLine> m_LinesActual;

    private SAT m_Sat;

    private List<Vector2> m_intersectionsWithRoadmap;


    // List of points to be propagated on the roadmap.
    private Queue<PointToProp> m_Points;

    // Variables for path finding
    private List<RoadMapLine> open;
    private List<RoadMapLine> closed;

    // AdHoc way point placed on the map when the intruder is seen in a place far from the road map.
    private WayPoint m_AdHocWp;
    private RoadMapLine m_AdHocRmLine;

    private MapRenderer m_mapRenderer;

    public RoadMap(SAT sat, MapRenderer _mapRenderer)
    {
        m_Sat = sat;
        m_WayPoints = m_Sat.GetDividedRoadMap();
        m_WpsActual = m_Sat.GetOriginalRoadMap();
        m_mapRenderer = _mapRenderer;

        m_intersectionsWithRoadmap = new List<Vector2>();

        m_Points = new Queue<PointToProp>();

        open = new List<RoadMapLine>();
        closed = new List<RoadMapLine>();

        PopulateEndNodes();
        PopulateLines();
        PopulateWayPoints();
    }


    // Populate the end nodes on the road map
    private void PopulateEndNodes()
    {
        m_endPoints = new List<WayPoint>();

        foreach (var wp in m_WayPoints.Where(wp => wp.GetConnections(true).Count == 1))
            m_endPoints.Add(wp);
    }

    // Populate the unique line segments of the map
    private void PopulateLines()
    {
        m_Lines = new List<RoadMapLine>();
        m_LinesActual = new List<RoadMapLine>();

        // Add the original line segments 
        foreach (var wp in m_WpsActual)
        foreach (var con in wp.GetConnections(true))
        {
            // Check if the connection already exist
            bool isFound = m_LinesActual.Any(line => line.IsPointPartOfLine(wp) && line.IsPointPartOfLine(con));

            if (!isFound) m_LinesActual.Add(new RoadMapLine(con, wp));
        }

        // Add the line connected to the way point
        foreach (var wp in m_WpsActual)
            wp.AddLines(m_LinesActual, true);


        // Add the divided lines segments 
        foreach (var wp in m_WayPoints)
        foreach (var con in wp.GetConnections(false))
        {
            // Check if the connection already exist
            bool isFound = m_Lines.Any(line => line.IsPointPartOfLine(wp) && line.IsPointPartOfLine(con));

            if (!isFound) m_Lines.Add(new RoadMapLine(con, wp));
        }

        // Add the line connected to the way point
        foreach (var wp in m_WayPoints)
            wp.AddLines(m_Lines, false);
    }


    // Get the reference of actual waypoints
    private void PopulateWayPoints()
    {
        m_WpsActual = new List<WayPoint>();

        foreach (var wp in m_WayPoints.Where(wp => wp.Id != 0))
            m_WpsActual.Add(wp);
    }


    public List<RoadMapLine> GetLines()
    {
        return m_Lines;
    }

    
    private List<float> distances = new List<float>();
    private List<float> dotProducts = new List<float>();
    
    // Get the closest projection point to a position@param on the road map.
    // The closest two points will be considered, and the tie breaker will be the how the road map line is aligned with dir@param 
    public WayPoint GetClosestWp(Vector2 position, Vector2 dir)
    {
        // List of distances and direction differences of the parameters.
        distances.Clear();
        dotProducts.Clear();

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
            float dotProduct = Vector2.Dot(toWayPointDir, velocityNorm);
            dotProducts.Add(dotProduct);
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
            if (dotProducts[i] < 0.6f) continue;

            if (minFrontalDistance > distances[i])
            {
                closestFrontalWpIndex = i;
                minFrontalDistance = distances[i];
            }
        }

        // If nothing is in the front then just get a visible closest node
        if (closestFrontalWpIndex == -1) return m_WayPoints[closestWpIndex];

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

        foreach (var line in closestWp.GetLines(false))
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
        foreach (var line in wp1.GetLines(false))
        {
            line.SetSearchSegment(wp1.GetPosition(), wp1.GetPosition(), 1f, StealthArea.GetElapsedTime());
        }
    }


    public Vector2? GetClosestProjection(Vector2 position, out RoadMapLine closestLine)
    {
        closestLine = null;
        float closetDistance = Mathf.Infinity;
        Vector2? closestPoint = null;

        foreach (var line in m_LinesActual)
        {
            Vector2 projectionPoint =
                GeometryHelper.ClosestProjectionOnSegment(line.wp1.GetPosition(), line.wp2.GetPosition(), position);

            float distance = Vector2.Distance(position, projectionPoint);

            if (distance < closetDistance && m_mapRenderer.VisibilityCheck(position, projectionPoint))
            {
                closetDistance = distance;
                closestPoint = projectionPoint;
                closestLine = line;
            }
        }

        return closestPoint;
    }


    // Get the possible possible positions when expanded in a direction for a certain distance
    public void ProjectPositionsInDirection(ref List<PossiblePosition> positions, Vector2 pointOnRoadMap,
        RoadMapLine line, int pointCount, float totalDistance, NPC npc)
    {
        m_Points.Clear();

        // Get the next Way point 
        WayPoint nextWayPoint = GetWayPointInDirection(pointOnRoadMap, npc.GetDirection(), line);
        Vector2 source = pointOnRoadMap;

        float maxStep = totalDistance / pointCount;

        float nextStep = Mathf.Min(maxStep, totalDistance);
        m_Points.Enqueue(new PointToProp(source, nextWayPoint, line,nextStep ,totalDistance, 0f));
        

        // Loop to insert the possible positions
        while (m_Points.Count > 0)
        {
            PointToProp pt = m_Points.Dequeue();

            float distance = Vector2.Distance(pt.source, pt.target.GetPosition());
            pt.nextStep = pt.nextStep <= 0f ? maxStep : pt.nextStep;
            pt.nextStep = Mathf.Min(pt.nextStep, pt.remainingDist);

            if (pt.nextStep <= distance)
            {
                Vector2 displacement = (pt.target.GetPosition() - pt.source).normalized * pt.nextStep;
                Vector2 newPosition = pt.source + displacement;
                pt.distance += pt.nextStep;

                // Add the possible position
                PossiblePosition possiblePosition = new PossiblePosition(newPosition, npc, pt.distance);
                positions.Add(possiblePosition);

                // Update the point's data
                pt.remainingDist -= pt.nextStep;
                pt.source = newPosition;
                pt.nextStep -= pt.nextStep;

                // If there are distance remaining then enqueue the point
                if (pt.remainingDist > 0f) m_Points.Enqueue(pt);
            }
            else
            {
                // Subtract the distance
                pt.remainingDist -= distance;
                pt.distance += distance;
                pt.nextStep -= distance;

                // If it is a dead end then place a point at the end
                if (pt.target.GetLines(true).Count == 1)
                {
                    PossiblePosition possiblePosition = new PossiblePosition(pt.target.GetPosition(), npc, pt.distance);
                    positions.Add(possiblePosition);
                    continue;
                }

                // Loop through the connections of next Way point to add the points to propagate.
                foreach (var newConn in pt.target.GetLines(true))
                {
                    // Skip if the line is the same
                    if (Equals(newConn, pt.line)) continue;

                    // set the new target
                    WayPoint nextWp = Equals(newConn.wp1, pt.target) ? newConn.wp2 : newConn.wp1;

                    // Add the point to the list
                    m_Points.Enqueue(new PointToProp(pt.target.GetPosition(), nextWp, newConn,pt.nextStep,
                        pt.remainingDist, pt.distance));
                }
            }
        }


        // // Get the distance from the interception point to the next node on the road map
        // float distanceToDestination = Vector2.Distance(phNode.position, phNode.destination.GetPosition());
        //
        // Vector2 direction = (phNode.destination.GetPosition() - phNode.source.GetPosition()).normalized;
        //
        // // Don't add anything if this is at the end line.
        // if (distanceToDestination == 0f) return;
        //
        // // If the distance to expand to the interception point is shorter then simply insert it. 
        // if (distanceToDestination >= distance)
        // {
        //     InterceptionPoint newPhNode = new InterceptionPoint(phNode.position + direction * distance,
        //         phNode.destination, phNode.source, generation);
        //
        //     // AddDistancesToEndNodes(newPhNode);
        //     m_interceptionPoints.Add(newPhNode);
        // }
        // // If not then propagate the nodes through the road map, then propagate to other nodes.
        // else
        // {
        //     float remainingDistance = distance - distanceToDestination;
        //
        //     if (phNode.destination.GetConnections(true).Count == 1)
        //     {
        //         InterceptionPoint newPhNode = new InterceptionPoint(phNode.destination.GetPosition(),
        //             phNode.destination, phNode.source, generation);
        //
        //         // Add metrics to the interception point
        //         // AddDistancesToEndNodes(newPhNode);
        //         m_interceptionPoints.Add(newPhNode);
        //         return;
        //     }
        //
        //     // for each connection recursively place the possible position
        //     foreach (var wayPoint in phNode.destination.GetConnections(true))
        //     {
        //         if (phNode.source == wayPoint)
        //             continue;
        //
        //         InterceptionPoint newPhNode = new InterceptionPoint(phNode.destination.GetPosition(),
        //             wayPoint, phNode.destination, generation);
        //
        //         PlacePossiblePositions(newPhNode, generation, remainingDistance);
        //     }
        // }
        //
        // // Remove interception points that are too close
        // AggregateInterceptionPoints();
    }

    private WayPoint GetWayPointInDirection(Vector2 source, Vector2 dir, RoadMapLine line)
    {
        Vector2 directionToEndLine = (line.wp1.GetPosition() - source).normalized;
        float dirSimilarityWp1 = Vector2.Dot(directionToEndLine, dir);

        // If the dot product is positive then it is on the same direction.
        return dirSimilarityWp1 > 0f ? line.wp1 : line.wp2;
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


    // // Add the adhoc roadMap line to the road map, and connect it to an existing way point.
    // private void AddRoadLineMap(Vector2 newWpPosition, WayPoint destWp)
    // {
    //     m_AdHocWp = new WayPoint(newWpPosition);
    //
    //     // Connect the way point to the road map.
    //     m_AdHocWp.Connect(destWp, false);
    //
    //     m_AdHocRmLine = new RoadMapLine(m_AdHocWp, destWp);
    //
    //     m_AdHocWp.AddLine(m_AdHocRmLine);
    //     destWp.AddLine(m_AdHocRmLine);
    //
    //     // Add the line to the list of lines of the road map.
    //     m_Lines.Add(m_AdHocRmLine);
    // }

    // Remove the ad hoc way point and its line
    private void RemoveRoadLineMap()
    {
        if (m_AdHocWp == null || m_AdHocRmLine == null)
            return;

        m_Lines.Remove(m_AdHocRmLine);

        m_AdHocWp.RemoveLine(m_AdHocRmLine);
        m_AdHocWp.GetConnections(false)[0].RemoveLine(m_AdHocRmLine);


        m_AdHocWp.GetConnections(false)[0].RemoveEdge(m_AdHocWp,false);
        m_AdHocWp.RemoveEdge(m_AdHocWp.GetConnections(false)[0],false);

        m_AdHocRmLine = null;
        m_AdHocWp = null;
    }

    // // Get a complete path of no more than param@length that a guard needs to traverse to search for an intruder.
    public void GetPath(Guard guard)
    {
        open.Clear();
        closed.Clear();

        // Get the closest Way point
        WayPoint closestWp = GetClosestWp(guard.GetTransform().position, guard.GetDirection());

        RoadMapLine startLine = null;
        float maxProb = Mathf.NegativeInfinity;

        // Get the start line from the way point
        foreach (var line in closestWp.GetLines(false))
        {
            if (maxProb < line.GetSearchSegment().GetProbability())
            {
                startLine = line;
                maxProb = line.GetSearchSegment().GetProbability();
            }
        }

        // Clear the variables
        float minUtility = Mathf.Infinity;
        foreach (var line in GetLines())
        {
            line.pathUtility = Mathf.NegativeInfinity;
            line.distance = Mathf.Infinity;
            line.pathParent = null;

            if (minUtility > line.GetUtility())
            {
                minUtility = line.GetUtility();
            }
        }

        // if the min utility is negative, inverse it's sign to modify all utilities to be zero or more
        minUtility = minUtility < 0f ? -minUtility : 0f;
        // minUtility = 5f;


        startLine.pathUtility = startLine.GetUtility() + minUtility;
        startLine.distance = 0f;
        startLine.pathParent = null;

        open.Add(startLine);

        RoadMapLine bestLine = null;

        // Dijkstra
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

                    float utilityTotal = currentLine.pathUtility + neighbor.GetUtility() + minUtility;

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

                    float utilityTotal = currentLine.pathUtility + neighbor.GetUtility() + minUtility;

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
    public void DrawDividedRoadMap()
    {
        foreach (var line in m_Lines)
        {
            line.DrawLine();
        }
    }

    public void DrawRoadMap()
    {
        foreach (var line in m_LinesActual)
        {
            line.DrawLine();
        }
    }
}

// Point to be propagated on the road map
class PointToProp
{
    // source position of the point
    public Vector2 source;

    // target way point of the propagation
    public WayPoint target;

    // Road Map Edge the propagation is happening on
    public RoadMapLine line;

    // The length of the next step
    public float nextStep;

    // The remaining distance of the propagation
    public float remainingDist;

    // Accumulated distance from the beginning to this point
    public float distance;

    public PointToProp(Vector2 _source, WayPoint _target, RoadMapLine _line, float _nextStep,float _remainingDist,
        float _distance)
    {
        source = _source;
        target = _target;
        line = _line;
        remainingDist = _remainingDist;
        nextStep = _nextStep;
        distance = _distance;
    }
}