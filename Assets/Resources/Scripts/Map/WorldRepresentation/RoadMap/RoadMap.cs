using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The RoadMap the guards uses to for the search task
public class RoadMap
{
    // Way Points of the road map (including way points introduced by dividing the edges)
    private List<RoadMapNode> _wayPoints;

    // Line Segments of the road map ( the divided segments)
    private List<RoadMapLine> _lines;

    // Way points that are dead ends
    private List<RoadMapNode> _endPoints;

    // Actual nodes in the road map (since m_Waypoints contain all waypoints of the divided segments)
    private List<RoadMapNode> _wpsActual;

    // The original lines of the road map before dividing the edges.
    private List<RoadMapLine> _linesActual;

    // Temporary nodes on the road map, contains those nodes added when some segments of the road map is removed.
    private List<RoadMapNode> _tempWpsActual;

    private SAT _sat;

    private List<Vector2> _intersectionsWithRoadmap;

    // List of points to be propagated on the roadmap.
    private Queue<PointToProp> _points;

    // AdHoc way point placed on the map when the intruder is seen in a place far from the road map.
    private RoadMapNode _adHocWp;
    private RoadMapLine _adHocRmLine;

    private MapRenderer _mapRenderer;

    private RMProperties _properties;

    public RoadMap(SAT sat, MapRenderer mapRenderer)
    {
        _sat = sat;
        _wayPoints = _sat.GetDividedRoadMap();
        _wpsActual = _sat.GetOriginalRoadMap();
        _tempWpsActual = new List<RoadMapNode>();
        _mapRenderer = mapRenderer;

        _intersectionsWithRoadmap = new List<Vector2>();

        _points = new Queue<PointToProp>();

        PopulateEndNodes();
        PopulateLines();

        // _properties = new RMProperties(this);
        // Debug.Log("Map Area: " + MapManager.Instance.mapDecomposer.GetNavMeshArea());
    }


    // Populate the end nodes on the road map
    private void PopulateEndNodes()
    {
        _endPoints = new List<RoadMapNode>();

        foreach (var wp in _wayPoints.Where(wp => wp.GetConnections(true).Count == 1))
            _endPoints.Add(wp);
    }

    // Populate the unique line segments of the map
    private void PopulateLines()
    {
        _lines = new List<RoadMapLine>();
        _linesActual = new List<RoadMapLine>();

        // Add the original line segments 
        foreach (var wp in _wpsActual)
        {
            foreach (var con in wp.GetConnections(true))
            {
                // Check if the connection already exist
                bool isFound = _linesActual.Any(line => line.IsPointPartOfLine(wp) && line.IsPointPartOfLine(con));

                if (!isFound) _linesActual.Add(new RoadMapLine(con, wp));
            }
        }

        // Add the line connected to the way point
        foreach (var wp in _wpsActual)
            wp.AddLines(_linesActual, true);


        // Add the divided lines segments 
        foreach (var wp in _wayPoints)
        foreach (var con in wp.GetConnections(false))
        {
            // // Check if the connection already exist
            bool isFound = _lines.Any(line => line.IsPointPartOfLine(wp) && line.IsPointPartOfLine(con));
            if (!isFound) _lines.Add(new RoadMapLine(con, wp));
        }

        // Add the line connected to the way point
        foreach (var wp in _wayPoints)
            wp.AddLines(_lines, false);
    }

    public RoadMapNode GetNodeById(int id)
    {
        for (int i = 0; i < _wpsActual.Count; i++)
        {
            if (Equals(_wpsActual[i].Id, id))
                return _wpsActual[i];
        }

        return null;
    }


    public RoadMapNode GetClosestNodes(Vector2 point, bool isOriginal, NodeType type, float radius)
    {
        List<RoadMapNode> nodes = isOriginal ? _wpsActual : _wayPoints;

        float minMagDistance = Mathf.Infinity;
        RoadMapNode closestWp = null;

        foreach (var node in nodes)
        {
            if (!Equals(node.type, type)) continue;
            if (node.GetProbability() >= 0.99f) continue;

            if (!GeometryHelper.IsCirclesVisible(point, node.GetPosition(), radius, "Wall")) continue;

            Vector2 offset = point - node.GetPosition();

            float distance = offset.sqrMagnitude;

            if (distance < minMagDistance)
            {
                minMagDistance = distance;
                closestWp = node;
            }
        }

        return closestWp;
    }

    public void AddLine(RoadMapLine line, bool isOriginal)
    {
        List<RoadMapLine> lines = isOriginal ? _linesActual : _lines;

        bool isAlreadyInsert = false;

        foreach (var l in lines)
        {
            if (l.IsPointPartOfLine(line.wp1) && l.IsPointPartOfLine(line.wp2))
            {
                isAlreadyInsert = true;
                break;
            }
        }

        if (!isAlreadyInsert)
            lines.Add(line);
    }

    public void RemoveLine(RoadMapNode wp1, RoadMapNode wp2, bool isOriginal)
    {
        List<RoadMapLine> lines = isOriginal ? _linesActual : _lines;

        for (int i = 0; i < lines.Count; i++)
        {
            RoadMapLine line = lines[i];

            if (line.IsPointPartOfLine(wp1) && line.IsPointPartOfLine(wp2))
            {
                lines.Remove(line);
            }
        }
    }

    public void RemoveLine(RoadMapLine line, bool isOriginal)
    {
        List<RoadMapLine> lines = isOriginal ? _linesActual : _lines;

        lines.Remove(line);
    }

    public List<RoadMapNode> GetNode(bool isOriginal)
    {
        return isOriginal ? _wpsActual : _wayPoints;
    }

    public List<RoadMapNode> GetTempNodes()
    {
        return _tempWpsActual;
    }

    // Get the reference of actual way points
    private void PopulateWayPoints()
    {
        _wpsActual = new List<RoadMapNode>();

        foreach (var wp in _wayPoints.Where(wp => wp.Id != 0))
            _wpsActual.Add(wp);
    }


    public List<RoadMapLine> GetLines(bool isOriginal)
    {
        List<RoadMapLine> lines = isOriginal ? _linesActual : _lines;
        return lines;
    }


    private List<float> distances = new List<float>();
    private List<float> dotProducts = new List<float>();

    // Get the closest projection point to a position@param on the road map.
    // The closest two points will be considered, and the tie breaker will be the how the road map line is aligned with dir@param 
    public RoadMapNode GetClosestWp(Vector2 position, Vector2 dir)
    {
        // List of distances and direction differences of the parameters.
        distances.Clear();
        dotProducts.Clear();

        // Loop through the way points
        foreach (var wp in _wayPoints)
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
            if (!_mapRenderer.VisibilityCheck(position, _wayPoints[i].GetPosition())) continue;

            if (minDistance > distances[i])
            {
                closestWpIndex = i;
                minDistance = distances[i];
            }

            // If not in front skip
            if (dotProducts[i] < 0.4f) continue;

            if (minFrontalDistance > distances[i])
            {
                closestFrontalWpIndex = i;
                minFrontalDistance = distances[i];
            }
        }

        if (closestWpIndex == -1) return null;

        // If nothing is in the front then just get a visible closest node
        if (closestFrontalWpIndex == -1) return _wayPoints[closestWpIndex];

        try
        {
            return _wayPoints[closestFrontalWpIndex];
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
        RoadMapNode closestWp = GetClosestWp(position, dir);
        
        
        foreach (var line in closestWp.GetLines(false))
            line.PropagateToSegment(line.wp1.GetPosition(),line.wp2.GetPosition(), 1f, StealthArea.GetElapsedTimeInSeconds());
            // line.PropagateToSegment(closestWp.GetPosition(),closestWp.GetPosition(), 1f, StealthArea.GetElapsedTimeInSeconds());
    }

    // Create a search segment that doesn't belong to the road map. The line starts from position@param and connects to the closest roadMap node
    // in the direction of dir@param. 
    public void CreateArbitraryRoadMapLine(Vector2 position, Vector2 dir)
    {
        // Remove the old arbitrary road map line.
        RemoveRoadLineMap();

        // List of distances and direction differences of the parameters.
        List<float> distances = new List<float>();
        List<float> angleDiffs = new List<float>();

        // Loop through the way points
        foreach (var wp in _wayPoints)
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
            if (!_mapRenderer.VisibilityCheck(position, _wayPoints[i].GetPosition()) || angleDiffs[i] < 0.6f)
                continue;

            if (minDistance > distances[i])
            {
                closestWpIndex = i;
                minDistance = distances[i];
            }
        }

        RoadMapNode wp1 = _wayPoints[closestWpIndex];
        foreach (var line in wp1.GetLines(false))
        {
            line.SetSearchSegment(wp1.GetPosition(), wp1.GetPosition(), 1f, StealthArea.GetElapsedTimeInSeconds());
        }
    }

    /// <summary>
    /// Get the closest road map line to a point 
    /// </summary>
    /// <param name="position"> The position to find the closest road map from. </param>
    /// <param name="direction"> The direction of the npc, if it is not important then it is set as null</param>
    /// <param name="isOriginal"> To consider only the original road map</param>
    /// <param name="closestLine"> The output line. </param>
    /// <returns></returns>
    public Vector2? GetLineToPoint(Vector2 position, Vector2? direction, bool isOriginal, out RoadMapLine closestLine)
    {
        closestLine = null;
        float closetDistance = Mathf.Infinity;
        float angleCosineMax = Mathf.NegativeInfinity;
        Vector2? closestPoint = null;

        foreach (var line in GetLines(isOriginal))
        {
            Vector2 projectionPoint =
                GeometryHelper.ClosestProjectionOnSegment(line.wp1.GetPosition(), line.wp2.GetPosition(), position);

            float distance = Vector2.Distance(position, projectionPoint);

            if (distance <= closetDistance && _mapRenderer.VisibilityCheck(position, projectionPoint))
            {
                if (Equals(direction, null))
                {
                    Vector2 lineDirection = (line.wp1.GetPosition() - line.wp2.GetPosition()).normalized;
                    float angleCosine = Mathf.Abs(Vector2.Dot(direction.Value, lineDirection));

                    if (angleCosine > angleCosineMax)
                        angleCosineMax = angleCosine;
                    else
                        continue;
                }

                closetDistance = distance;
                closestPoint = projectionPoint;
                closestLine = line;
            }
        }

        return closestPoint;
    }


    public Vector2? GetClosetWpPairToPoint(Vector2 position, Vector2? direction, bool isOriginal, out RoadMapNode wp1,
        out RoadMapNode wp2)
    {
        // Output way points
        wp1 = null;
        wp2 = null;

        float closetDistance = Mathf.Infinity;
        float angleCosineMax = Mathf.NegativeInfinity;
        Vector2? closestPoint = null;

        foreach (var wp in GetNode(isOriginal))
        foreach (var con in wp.GetConnections(true))
        {
            Vector2 projectionPoint =
                GeometryHelper.ClosestProjectionOnSegment(wp.GetPosition(), con.GetPosition(), position);

            float distance = Vector2.Distance(position, projectionPoint);

            if (distance <= closetDistance && _mapRenderer.VisibilityCheck(position, projectionPoint))
            {
                if (Equals(direction, null))
                {
                    Vector2 lineDirection = (wp.GetPosition() - con.GetPosition()).normalized;
                    float angleCosine = Mathf.Abs(Vector2.Dot(direction.Value, lineDirection));

                    if (angleCosine > angleCosineMax)
                        angleCosineMax = angleCosine;
                    else
                        continue;
                }

                closetDistance = distance;
                closestPoint = projectionPoint;
                wp1 = wp;
                wp2 = con;
            }
        }

        return closestPoint;
    }


    // Get the possible possible positions when expanded in a direction for a certain distance
    public void ProjectPositionsInDirection(ref List<PossiblePosition> positions, Vector2 pointOnRoadMap,
        RoadMapLine line, int pointCount, float totalDistance, NPC npc)
    {
        _points.Clear();

        // Get the next Way point 
        RoadMapNode nextWayPoint = GetWayPointInDirection(pointOnRoadMap, npc.GetDirection(), line);
        Vector2 source = pointOnRoadMap;

        float maxStep = totalDistance / pointCount;

        float nextStep = Mathf.Min(maxStep, totalDistance);
        _points.Enqueue(new PointToProp(source, nextWayPoint, line, nextStep, totalDistance, 0f, npc));


        // Loop to insert the possible positions
        while (_points.Count > 0)
        {
            PointToProp pt = _points.Dequeue();

            float distance = Vector2.Distance(pt.source, pt.targetWp.GetPosition());
            pt.nextStep = pt.nextStep <= 0f ? maxStep : pt.nextStep;
            pt.nextStep = Mathf.Min(pt.nextStep, pt.remainingDist);

            if (pt.nextStep <= distance)
            {
                Vector2 displacement = (pt.targetWp.GetPosition() - pt.source).normalized * pt.nextStep;
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
                if (pt.remainingDist > 0f) _points.Enqueue(pt);
            }
            else
            {
                // Subtract the distance
                pt.remainingDist -= distance;
                pt.distance += distance;
                pt.nextStep -= distance;

                // If it is a dead end then place a point at the end
                if (pt.targetWp.GetLines(true).Count == 1)
                {
                    PossiblePosition possiblePosition =
                        new PossiblePosition(pt.targetWp.GetPosition(), npc, pt.distance);
                    positions.Add(possiblePosition);
                    continue;
                }

                // Loop through the connections of next Way point to add the points to propagate.
                foreach (var newConn in pt.targetWp.GetLines(true))
                {
                    // Skip if the line is the same
                    if (Equals(newConn, pt.line)) continue;

                    // set the new target
                    RoadMapNode nextWp = Equals(newConn.wp1, pt.targetWp) ? newConn.wp2 : newConn.wp1;

                    // Add the point to the list
                    _points.Enqueue(new PointToProp(pt.targetWp.GetPosition(), nextWp, newConn, pt.nextStep,
                        pt.remainingDist, pt.distance, npc));
                }
            }
        }
    }

    // Remove the added waypoints, each added waypoint should be connected to two points at max
    public void ClearTempWayPoints()
    {
        try
        {
            _tempWpsActual.Clear();
            foreach (var wp in _wpsActual)
            {
                wp.SetProbability(null, 0f);
                wp.LoadCons();
            }
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    // insert a new waypoint between two that currently connected 
    public void InsertWpInLine(RoadMapNode firstWp, RoadMapNode secWp, RoadMapNode newWp)
    {
        // Disconnect the connected waypoints
        firstWp.RemoveConnection(secWp, true);

        // Connect the new waypoint
        newWp.Connect(firstWp, true, true);
        newWp.Connect(secWp, true, true);
    }

    public RoadMapNode GetGuardRoadMapNode(Vector2 position, NPC passingGuard, float probability)
    {
        RoadMapNode newWp = new RoadMapNode(position);

        newWp.SetProbability(passingGuard, probability);

        newWp.Id = _wpsActual.Count + _tempWpsActual.Count;

        return newWp;
    }

    // Get the probability to give for the intermediate points
    private static float GetProbabilityValue(NPC guard, Vector2 position, PointToProp pt, float distance, float fov,
        float maxDistance)
    {
        // Spot is seen
        if (guard.GetFovPolygon().IsPointInPolygon(position, true)) return 1f;

        if (Equals(pt, null) || Equals(pt.type, PointType.Stright))
        {
            float value = Mathf.Max(distance - fov, 0f);

            if (fov >= maxDistance) return 0.9f;

            return Mathf.Round(Mathf.Max(0.9f - value / (maxDistance - fov), 0f) * 10f) * 0.1f;
        }

        if (Equals(pt.type, PointType.Corner)) return pt.fixedRiskValue.Value;

        return 0f;
    }

    public List<RoadMapNode> GetPossibleGuardPositions()
    {
        return _tempWpsActual;
    }

    public void ProjectPositionsInDirection(ref List<PossibleTrajectory> trajectory, Vector2 pointOnRoadMap,
        RoadMapNode wp1, RoadMapNode wp2, float stepSize, float totalDistance, NPC npc)
    {
        _points.Clear();

        // Get the next Way point 
        RoadMapNode nextWayPoint = GetWayPointInDirection(pointOnRoadMap, npc.GetDirection(), wp1, wp2);
        Vector2 source = pointOnRoadMap;

        // Add a temporary way point to mark the guard's position on the road map
        float fov = Properties.GetFovRadius(NpcType.Guard);

        RoadMapNode sourceWp =
            GetGuardRoadMapNode(source, npc,
                GetProbabilityValue(npc, source, null, 0, fov, totalDistance));
        
        InsertWpInLine(wp1, wp2, sourceWp);
        
        sourceWp.distanceFromGuard = 0f;
        _tempWpsActual.Add(sourceWp);

        PointToProp ptp = new PointToProp(source, sourceWp, nextWayPoint, stepSize, stepSize, totalDistance, 0f, npc);
        ptp.GetTrajectory().AddPoint(source);

        _points.Enqueue(ptp);

        int limit = 100;
        int counter = 0;

        // Loop to insert the possible positions
        while (_points.Count > 0 && counter < limit)
        {
            PointToProp pt = _points.Dequeue();
            counter++;

            float distance = Vector2.Distance(pt.source, pt.targetWp.GetPosition());
            pt.nextStep = pt.nextStep <= 0f ? pt.stepSize : pt.nextStep;
            pt.nextStep = Mathf.Min(pt.nextStep, pt.remainingDist);

            // add a new point since it will not reach the next way point
            if (pt.nextStep <= distance)
            {
                Vector2 displacement = (pt.targetWp.GetPosition() - pt.source).normalized * pt.nextStep;
                Vector2 newPosition = pt.source + displacement;
                pt.distance += pt.nextStep;

                // Add the possible position
                pt.GetTrajectory().AddPoint(newPosition);

                // Update the point's data
                pt.remainingDist -= pt.nextStep;
                pt.nextStep = 0f;
                pt.source = newPosition;

                // Add a temporary way point to mark the guard possibly passing to
                RoadMapNode newWp = GetGuardRoadMapNode(newPosition, npc,
                    GetProbabilityValue(npc, pt.source, pt, pt.distance, fov, totalDistance));
                InsertWpInLine(pt.sourceWp, pt.targetWp, newWp);
                pt.sourceWp = newWp;
                newWp.distanceFromGuard = pt.distance;
                _tempWpsActual.Add(newWp);

                // If there are distance remaining then enqueue the point
                if (pt.remainingDist > 0f)
                    _points.Enqueue(pt);
                else
                    trajectory.Add(pt.GetTrajectory());
            }
            else
            {
                // Subtract the distance
                pt.remainingDist -= distance;
                pt.distance += distance;
                pt.nextStep -= distance;

                // Set the probability to non zero since a guard might pass through here
                pt.targetWp.SetProbability(npc,
                    GetProbabilityValue(npc, pt.targetWp.GetPosition(), pt, pt.distance, fov,
                        totalDistance));

                // If it is a dead end then place a point at the end
                if (pt.targetWp.GetConnections(true).Count == 1)
                {
                    pt.GetTrajectory().AddPoint(pt.targetWp.GetPosition());
                    trajectory.Add(pt.GetTrajectory());

                    // Add a temporary way point to mark the guard possibly passing to
                    RoadMapNode newWp =
                        GetGuardRoadMapNode(pt.targetWp.GetPosition(), npc,
                            GetProbabilityValue(npc, pt.targetWp.GetPosition(), pt, pt.distance, fov,
                                totalDistance));
                    InsertWpInLine(pt.sourceWp, pt.targetWp, newWp);
                    pt.sourceWp = newWp;
                    newWp.distanceFromGuard = pt.distance;
                    _tempWpsActual.Add(newWp);

                    continue;
                }

                // Loop through the connections of next Way point to add the points to propagate.
                foreach (var con in pt.targetWp.GetConnections(true))
                {
                    // Skip if the connection is the same
                    if (Equals(con, pt.sourceWp)) continue;

                    pt.GetTrajectory().AddPoint(pt.targetWp.GetPosition());

                    // Add the point to the list
                    PointToProp newPt = new PointToProp(pt.targetWp.GetPosition(), pt.targetWp, con, pt.nextStep,
                        pt.stepSize,
                        pt.remainingDist, pt.distance, npc);
                    newPt.GetTrajectory().CopyTrajectory(pt.GetTrajectory());
                    _points.Enqueue(newPt);
                }
            }
        }
    }


    // Probogate trajectory based on the angle of the corners
    public void ProjectPositionsByAngle(ref List<PossibleTrajectory> trajectory, Vector2 pointOnRoadMap,
        RoadMapNode wp1, RoadMapNode wp2, float stepSize, float totalDistance, NPC npc)
    {
        RoadMapNode firstIntersection = null;

        foreach (RoadMapNode n in _wpsActual)
            n.isChecked = false;

        foreach (RoadMapNode n in _tempWpsActual)
            n.isChecked = false;

        _points.Clear();

        // Get the next Way point 
        RoadMapNode nextWayPoint = GetWayPointInDirection(pointOnRoadMap, npc.GetDirection(), wp1, wp2);
        Vector2 source = pointOnRoadMap;

        // Add a temporary way point to mark the guard's position on the road map
        float fov = Properties.GetFovRadius(NpcType.Guard);

        RoadMapNode sourceWp =
            GetGuardRoadMapNode(source, npc,
                GetProbabilityValue(npc, source, null, 0, fov, totalDistance));
        InsertWpInLine(wp1, wp2, sourceWp);
        sourceWp.distanceFromGuard = 0f;
        _tempWpsActual.Add(sourceWp);

        PointToProp ptp = new PointToProp(source, sourceWp, nextWayPoint, stepSize, stepSize, totalDistance, 0f, npc);
        ptp.GetTrajectory().AddPoint(source);
        _points.Enqueue(ptp);

        int limit = 10000;
        int counter = 0;

        // Loop to insert the possible positions
        while (_points.Count > 0 && counter < limit)
        {
            PointToProp pt = _points.Dequeue();
            counter++;

            float distance = Vector2.Distance(pt.source, pt.targetWp.GetPosition());
            pt.nextStep = pt.nextStep <= 0f ? pt.stepSize : pt.nextStep;
            pt.nextStep = Mathf.Min(pt.nextStep, pt.remainingDist);

            // add a new point since it will not reach the next way point
            if (pt.nextStep <= distance)
            {
                Vector2 displacement = (pt.targetWp.GetPosition() - pt.source).normalized * pt.nextStep;
                Vector2 newPosition = pt.source + displacement;
                pt.distance += pt.nextStep;

                // Add the possible position
                pt.GetTrajectory().AddPoint(newPosition);

                // Update the point's data
                pt.remainingDist -= pt.nextStep;
                pt.nextStep = 0f;
                pt.source = newPosition;

                // Add a temporary way point to mark the guard possibly passing to
                RoadMapNode newWp = GetGuardRoadMapNode(newPosition, npc,
                    GetProbabilityValue(npc, pt.source, pt, pt.distance, fov, totalDistance));
                InsertWpInLine(pt.sourceWp, pt.targetWp, newWp);
                pt.sourceWp = newWp;
                newWp.distanceFromGuard = pt.distance;
                _tempWpsActual.Add(newWp);

                // If there are distance remaining then enqueue the point
                if (pt.remainingDist > 0f)
                    _points.Enqueue(pt);
                else
                    trajectory.Add(pt.GetTrajectory());
            }
            else
            {
                // Subtract the distance
                pt.remainingDist -= distance;
                pt.distance += distance;
                pt.nextStep -= distance;

                // Set the probability to non zero since a guard might pass through here
                pt.targetWp.SetProbability(npc,
                    GetProbabilityValue(npc, pt.targetWp.GetPosition(), pt, pt.distance, fov,
                        totalDistance));

                // If it is a dead end then place a point at the end
                if (pt.targetWp.GetConnections(true).Count == 1)
                {
                    pt.GetTrajectory().AddPoint(pt.targetWp.GetPosition());
                    trajectory.Add(pt.GetTrajectory());

                    // Add a temporary way point to mark the guard possibly passing to
                    RoadMapNode newWp =
                        GetGuardRoadMapNode(pt.targetWp.GetPosition(), npc,
                            GetProbabilityValue(npc, pt.targetWp.GetPosition(), pt, pt.distance, fov,
                                totalDistance));
                    InsertWpInLine(pt.sourceWp, pt.targetWp, newWp);
                    pt.sourceWp = newWp;
                    newWp.distanceFromGuard = pt.distance;
                    _tempWpsActual.Add(newWp);

                    continue;
                }

                // Set the first node on a conjunction
                if (Equals(firstIntersection, null))
                {
                    if (pt.targetWp.GetConnections(true).Count > 2) firstIntersection = pt.targetWp;
                }
                else
                {
                    bool isVisible = GeometryHelper.IsCirclesVisible(pt.targetWp.GetPosition(),
                        firstIntersection.GetPosition(), Properties.NpcRadius, "Wall");


                    if (!isVisible && pt.targetWp.GetConnections(true).Count > 2) continue;
                }

                pt.targetWp.isChecked = true;


                // Loop through the connections of next Way point to add the points to propagate.
                foreach (var con in pt.targetWp.GetConnections(true))
                {
                    // Skip if the connection is the same
                    if (Equals(con, pt.sourceWp)) continue;

                    pt.GetTrajectory().AddPoint(pt.targetWp.GetPosition());

                    // Add the point to the list
                    PointToProp newPt = new PointToProp(pt.targetWp.GetPosition(), pt.targetWp, con, pt.nextStep,
                        pt.stepSize,
                        pt.remainingDist, pt.distance, npc);

                    newPt.GetTrajectory().CopyTrajectory(pt.GetTrajectory());

                    _points.Enqueue(newPt);
                    con.isChecked = true;
                }
            }
        }
    }


    private bool isOnStraightLine(PointToProp pt, RoadMapNode con)
    {
        float minDotProductAsStraightLine = 0.9f;

        Vector2 originalLine = pt.targetWp.GetPosition() - pt.sourceWp.GetPosition();
        Vector2 nextLine = con.GetPosition() - pt.targetWp.GetPosition();

        float dot = Vector2.Dot(originalLine.normalized, nextLine.normalized);

        return dot >= minDotProductAsStraightLine;
    }

    private void SetupNewPoint(NPC guard, PointToProp pt, RoadMapNode con, ref PointToProp newPt)
    {
        if (Equals(pt.type, PointType.Corner)) return;

        int connectionsCount = pt.targetWp.GetConnections(true).Count - 1;

        if (!isOnStraightLine(pt, con) && connectionsCount > 1 && Equals(newPt.fixedRiskValue, null))
        {
            newPt.remainingDist = guard.GetFovRadius();
            newPt.type = PointType.Corner;
        }

        newPt.fixedRiskValue = pt.targetWp.GetProbability() / connectionsCount;
    }


    private RoadMapNode GetWayPointInDirection(Vector2 source, Vector2 dir, RoadMapLine line)
    {
        Vector2 directionToEndLine = (line.wp1.GetPosition() - source).normalized;
        float dirSimilarityWp1 = Vector2.Dot(directionToEndLine, dir);

        // If the dot product is positive then it is on the same direction.
        return dirSimilarityWp1 > 0f ? line.wp1 : line.wp2;
    }

    private RoadMapNode GetWayPointInDirection(Vector2 source, Vector2 dir, RoadMapNode wp1, RoadMapNode wp2)
    {
        Vector2 directionToEndLine = (wp1.GetPosition() - source).normalized;
        float dirSimilarityWp1 = Vector2.Dot(directionToEndLine, dir);

        // If the dot product is positive then it is on the same direction.
        return dirSimilarityWp1 > 0f ? wp1 : wp2;
    }

    private Vector2? GetClosestIntersectionWithRoadmap(Vector2 start, Vector2 end)
    {
        _intersectionsWithRoadmap.Clear();

        foreach (var line in _lines)
        {
            Vector2 intersection = GeometryHelper.GetIntersectionPointCoordinates(start, end, line.wp1.GetPosition(),
                line.wp2.GetPosition(),
                true, out bool isFound);

            if (isFound)
                _intersectionsWithRoadmap.Add(intersection);
        }

        if (_intersectionsWithRoadmap.Count == 0)
            return null;

        Vector2 closestIntersection = _intersectionsWithRoadmap[0];
        float closestDistance = Vector2.Distance(start, closestIntersection);

        for (int i = 1; i < _intersectionsWithRoadmap.Count; i++)
        {
            float distance = Vector2.Distance(start, _intersectionsWithRoadmap[i]);

            if (distance < closestDistance)
            {
                closestIntersection = _intersectionsWithRoadmap[i];
                closestDistance = distance;
            }
        }

        return closestIntersection;
    }

    // Remove the ad hoc way point and its line
    private void RemoveRoadLineMap()
    {
        if (_adHocWp == null || _adHocRmLine == null)
            return;

        _lines.Remove(_adHocRmLine);

        _adHocWp.RemoveLine(_adHocRmLine);
        _adHocWp.GetConnections(false)[0].RemoveLine(_adHocRmLine);


        _adHocWp.GetConnections(false)[0].RemoveEdge(_adHocWp, false);
        _adHocWp.RemoveEdge(_adHocWp.GetConnections(false)[0], false);

        _adHocRmLine = null;
        _adHocWp = null;
    }

    public void ClearSearchSegments()
    {
        foreach (var line in _lines)
            line.ClearSearchSegs();
    }


    public void DrawWayPoints()
    {
        foreach (var wp in _wayPoints)
        {
            // Handles.Label(wp.GetPosition(), wp.Id.ToString());
        }
    }

    // Render the lines of road map
    public void DrawDividedRoadMap()
    {
        foreach (var line in _lines)
        {
            line.DrawLine();
        }
    }


    public void DrawWalkableRoadmap(bool noCorners)
    {
        foreach (var t in _wpsActual)
        {
            if (noCorners && Equals(t.type, NodeType.Corner)) continue;

            if (t.GetProbability() >= 0.5f) continue;

            Gizmos.DrawSphere(t.GetPosition(), 0.025f);
            // float prob = Mathf.Round(t.GetProbability() * 100f) * 0.01f;

#if UNITY_EDITOR
            // Handles.Label(t.GetPosition(), prob.ToString());
#endif
            

            foreach (var wp in t.GetConnections(true))
            {
                if (noCorners && Equals(wp.type, NodeType.Corner)) continue;

                if (t.GetProbability() != 0f && wp.GetProbability() != 0f)
                    Gizmos.color = Color.red;
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(t.GetPosition(), wp.GetPosition());
                }
            }
        }

        return;


        Gizmos.color = Color.red;
        foreach (var t in _tempWpsActual)
        {
            if (noCorners && Equals(t.type, NodeType.Corner)) continue;
            Gizmos.DrawSphere(t.GetPosition(), 0.025f);
            float prob = Mathf.Round(t.GetProbability() * 100f) * 0.01f;
            
#if UNITY_EDITOR
            Handles.Label(t.GetPosition(), prob.ToString());
#endif

            foreach (var wp in t.GetConnections(true))
            {
                if (noCorners && Equals(wp.type, NodeType.Corner)) continue;
                if (_tempWpsActual.Contains(wp))
                    Gizmos.DrawLine(t.GetPosition(), wp.GetPosition());
            }
        }
    }


    public void DrawRoadMap()
    {
        List<RoadMapNode> nodes = _wpsActual;

        // Go through each edge
        for (int i = 0; i < nodes.Count; i++)
        {
            // Ignore the added corner nodes
            if (Equals(nodes[i].type, NodeType.Corner)) continue;

#if UNITY_EDITOR
            Handles.Label(nodes[i].GetPosition(), nodes[i].Id.ToString());
#endif

            float maxDistance = Mathf.NegativeInfinity;
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (Equals(nodes[j].type, NodeType.Corner)) continue;

                List<RoadMapNode> firstConnections = nodes[i].GetConnections(true);
                bool isSecNodeConnected = firstConnections.Contains(nodes[j]);

                List<RoadMapNode> secondConnections = nodes[j].GetConnections(true);
                bool isfirstNodeConnected = secondConnections.Contains(nodes[i]);

                if (!isfirstNodeConnected || !isSecNodeConnected) continue;

                Gizmos.DrawLine(nodes[i].GetPosition(), nodes[j].GetPosition());
            }
        }
    }
}

// Point to be propagated on the road map
public class PointToProp
{
    // source position of the point
    public Vector2 source;

    public RoadMapNode sourceWp;

    // target way point of the propagation
    public RoadMapNode targetWp;

    // Road Map Edge the propagation is happening on
    public RoadMapLine line;

    public PointType type;

    public float? fixedRiskValue;

    public float stepSize;

    // The length of the next step
    public float nextStep;

    // The remaining distance of the propagation
    public float remainingDist;

    // Accumulated distance from the beginning to this point
    public float distance;

    private PossibleTrajectory _trajectory;

    public PointToProp(Vector2 _source, RoadMapNode _sourceWp, RoadMapNode _targetWp, float _nextStep, float _stepSize,
        float _remainingDist,
        float _distance, NPC npc)
    {
        source = _source;
        sourceWp = _sourceWp;
        targetWp = _targetWp;
        remainingDist = _remainingDist;
        stepSize = _stepSize;
        nextStep = _nextStep;
        distance = _distance;
        _trajectory = new PossibleTrajectory(npc);
        fixedRiskValue = null;
    }

    public PointToProp(Vector2 _source, RoadMapNode _targetWp, RoadMapLine _line, float _nextStep, float _remainingDist,
        float _distance, NPC npc)
    {
        source = _source;
        line = _line;
        targetWp = _targetWp;
        remainingDist = _remainingDist;
        nextStep = _nextStep;
        distance = _distance;
        _trajectory = new PossibleTrajectory(npc);
        fixedRiskValue = null;
    }

    public List<Vector2> GetTrajectoriesCount()
    {
        return _trajectory.GetPath();
    }

    public PossibleTrajectory GetTrajectory()
    {
        return _trajectory;
    }
}

public enum PointType
{
    Stright,

    Corner
}