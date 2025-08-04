﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

public class PathFinding : MonoBehaviour
{
    [SerializeField] private float m_longestPathInMap;

    public float longestShortestPath => m_longestPathInMap;

    // Simple Funnel Offset
    private float offsetMultiplier = 0.25f;

    // Containers for the path finding
    private List<MeshPolygon> openListMesh;
    private List<MeshPolygon> closedListMesh;
    private List<MeshPolygon> pathMesh;

    // Temp path (used for just finding the shortest path distance
    private List<Vector2> shortestPath;

    // Simple stupid funnel variables
    private List<Vector2> leftVertices;
    private List<Vector2> rightVertices;

    // For road map path finding
    private List<RoadMapNode> openListRoadMap;
    private List<RoadMapNode> closedListRoadMap;
    private List<Vector2> pathRoadMap;

    public static PathFinding Instance;

    // Initiate containers; to improve garbage collection
    public void Initiate()
    {
        Instance = this;

        openListMesh = new List<MeshPolygon>();
        closedListMesh = new List<MeshPolygon>();
        pathMesh = new List<MeshPolygon>();

        shortestPath = new List<Vector2>();

        leftVertices = new List<Vector2>();
        rightVertices = new List<Vector2>();

        openListRoadMap = new List<RoadMapNode>();
        closedListRoadMap = new List<RoadMapNode>();
        pathRoadMap = new List<Vector2>();

        CalculateLongestPathLength(MapManager.Instance.mapRenderer.GetInteriorWalls());
    }


    // Calculate the longest possible path in the map
    private void CalculateLongestPathLength(List<Polygon> walls)
    {
        float lengthFromPoint = 1f;
        float maxDistance = Mathf.NegativeInfinity;
        for (int j = 0; j < walls[0].GetVerticesCount(); j++)
        for (int k = j + 1; k < walls[0].GetVerticesCount(); k++)
        {
            // print(walls[0].GetPoint(j) - walls[0].GetAngelNormal(j) * lengthFromPoint + " - " +
            //       (walls[0].GetPoint(k) - walls[0].GetAngelNormal(k) * lengthFromPoint));
            //
            // float distance = GetShortestPathDistance(
            //     walls[0].GetPoint(j) - walls[0].GetAngelNormal(j) * lengthFromPoint,
            //     walls[0].GetPoint(k) - walls[0].GetAngelNormal(k) * lengthFromPoint);


            float distance = GetShortestPathDistance(
                walls[0].GetCorner(j),
                walls[0].GetCorner(k));


            if (maxDistance < distance)
                maxDistance = distance;
        }

        m_longestPathInMap = maxDistance;
    }


    public Vector2 GetPointFromCorner(float radius)
    {
        Polygon wall = MapManager.Instance.mapRenderer.GetInteriorWalls()[0];

        int index = Random.Range(0, wall.GetVerticesCount());

        return -wall.GetAngelNormal(index) * radius + wall.GetPoint(index);
    }


    public Vector2 GetCornerFurthestFromPoint(Vector2? point, float radius)
    {
        Polygon wall = MapManager.Instance.mapRenderer.GetInteriorWalls()[0];

        float minSafeDistanceFromGuard = 7f;

        float maxDistance = Mathf.NegativeInfinity;
        Vector2? corner = null;

        float maxSafeDistance = Mathf.NegativeInfinity;
        Vector2? safeCorner = null;

        for (int i = 0; i < wall.GetVerticesCount(); i++)
        {
            Vector2 cornerWall = -wall.GetAngelNormal(i) * radius + wall.GetPoint(i);
            
            
            float distance = Equals(point, null)?0f:GetShortestPathDistance(point.Value, cornerWall);

            bool isSafe = true;
            foreach (var guard in NpcsManager.Instance.GetGuards())
            {
                float distanceFromGuard =
                    GetShortestPathDistance(cornerWall, guard.GetTransform().position);

                if (distanceFromGuard < minSafeDistanceFromGuard)
                {
                    isSafe = false;
                    break;
                }
            }


            if (distance > maxDistance)
            {
                maxDistance = distance;
                corner = cornerWall;
            }

            if (distance > maxSafeDistance && isSafe)
            {
                maxSafeDistance = distance;
                safeCorner = cornerWall;
            }
        }

        return Equals(safeCorner, null) ? corner.Value : safeCorner.Value;
    }


    // Return the shortest path as a sequence of points
    public float GetShortestPath(Vector2 startPoint, Vector2 destinationPoint,
        ref List<Vector2> resultPath)
    {
        List<MeshPolygon> navMesh = MapManager.Instance.GetNavMesh();

        // Get shortest path in polygons
        SetShortestPathPolygons(navMesh, startPoint, destinationPoint);

        GetPathBySSFA(startPoint, destinationPoint, ref resultPath);

        // Insert the first point to the path
        resultPath.Insert(0, startPoint);

        float totalDistance = 0f;

        for (int i = 0; i < resultPath.Count - 1; i++)
            totalDistance += Vector2.Distance(resultPath[i], resultPath[i + 1]);

        return totalDistance;
    }

    public float GetShortestPathDistance(Vector2 startPoint, Vector2 destinationPoint)
    {
        GetShortestPath(startPoint, destinationPoint, ref shortestPath);

        // Insert the first point to the path
        shortestPath.Insert(0, startPoint);

        float totalDistance = 0f;

        for (int i = 0; i < shortestPath.Count - 1; i++)
            totalDistance += Vector2.Distance(shortestPath[i], shortestPath[i + 1]);

        return totalDistance;
    }


    // Receive Polygon start and end position and return Polygon based path
    private void SetShortestPathPolygons(List<MeshPolygon> navMesh, Vector2 start,
        Vector2 destination)
    {
        MeshPolygon startPolygon = GetCorrespondingPolygon(navMesh, start);
        MeshPolygon destinationPolygon = GetCorrespondingPolygon(navMesh, destination);

        if (Equals(startPolygon, null) || Equals(destinationPolygon, null))
        {
            return;
            throw new Exception();
        }

        openListMesh.Clear();
        closedListMesh.Clear();

        foreach (MeshPolygon p in navMesh)
        {
            p.SetgDistance(Mathf.Infinity);
            p.SethDistance(Mathf.Infinity);
            p.SetPreviousPolygon(null);
        }

        // Set Cost of starting Polygon
        startPolygon.SetgDistance(0f);
        startPolygon.SethDistance(Vector2.Distance(start, destination));
        startPolygon.SetEntryPoint(start);


        // Add the starting polygon to the open list
        openListMesh.Add(startPolygon);

        while (openListMesh.Count > 0)
        {
            MeshPolygon current = openListMesh[0];
            openListMesh.RemoveAt(0);

            foreach (KeyValuePair<int, MeshPolygon> pPair in current.GetAdjcentPolygons())
            {
                MeshPolygon p = pPair.Value;

                Vector2 possibleNextWaypoint = GetClosestPolygonVertex(current.GetEntryPoint().Value, p);

                if (!closedListMesh.Contains(p))
                {
                    float gDistance = GetCostValue(current, p, possibleNextWaypoint, destinationPolygon, destination);

                    float hDistance = Vector2.Distance(possibleNextWaypoint, destination);

                    if (p.GetgDistance() + p.GethDistance() > gDistance + hDistance)
                    {
                        p.SethDistance(hDistance);
                        p.SetgDistance(gDistance);

                        p.SetPreviousPolygon(current);
                        p.SetEntryPoint(possibleNextWaypoint);
                    }

                    openListMesh.InsertIntoSortedList(p,
                        delegate(MeshPolygon x, MeshPolygon y) { return x.GetFvalue().CompareTo(y.GetFvalue()); },
                        Order.Asc);
                }
            }

            closedListMesh.Add(current);

            // Stop the search if we reached the destination polygon
            if (current.Equals(destinationPolygon))
                break;
        }

        // Get the path starting from the goal node to the last polygon before the starting polygon
        pathMesh.Clear();

        MeshPolygon currentPolygon = destinationPolygon;
        while (currentPolygon.GetPreviousPolygon() != null)
        {
            pathMesh.Add(currentPolygon);

            if (currentPolygon.GetPreviousPolygon() == null)
                break;

            currentPolygon = currentPolygon.GetPreviousPolygon();
        }

        // Add the first polygon to the path
        pathMesh.Add(startPolygon);

        // reverse the path so it start from the start node
        pathMesh.Reverse();
    }

    /// <summary>
    /// Get the closest vertex of a polygon to a point
    /// </summary>
    /// <param name="point"></param>
    /// <param name="poly"></param>
    /// <returns></returns>
    static Vector2 GetClosestPolygonVertex(Vector2 point, Polygon poly)
    {
        float minDistance = Mathf.Infinity;
        Vector2? output = null;

        foreach (var v in poly.GetPoints())
        {
            float distance = Vector2.Distance(point, v.position);

            if (minDistance < distance) continue;

            minDistance = distance;
            output = v.position;
        }

        return output.Value;
    }


    // Get the Cost Value (G) for the Mesh polygons
    static float GetCostValue(MeshPolygon currentPolygon, MeshPolygon nextPolygon, Vector2 nextPoint,
        MeshPolygon destinationPolygon,
        Vector2 destination)
    {
        float costValue = currentPolygon.GetgDistance();

        Vector2? previousWaypoint = currentPolygon.GetEntryPoint();

        if (nextPolygon.Equals(destinationPolygon)) nextPoint = destination;

        // Euclidean Distance
        float distance = Vector2.Distance(previousWaypoint.Value, nextPoint);

        costValue += distance;

        return costValue;
    }


    // Get heuristic value for mesh polygons
    static float GetHeuristicValue(MeshPolygon currentPolygon, MeshPolygon goal)
    {
        float heuristicValue = Vector2.Distance(currentPolygon.GetCentroidPosition(), goal.GetCentroidPosition());

        return heuristicValue;
    }


    // Get the polygon that contains the specified point
    private static MeshPolygon GetCorrespondingPolygon(List<MeshPolygon> navMesh, Vector2 point)
    {
        // Check all polygons in the NavMesh if they contain the point 
        foreach (var p in navMesh)
            if (p.IsPointInPolygon(point, false))
                return p;

        // In case the point is out of all polygons 
        // Loop through the polygons and intersect them with the point to their centroids and return the closest intersection point

        float minDistance = Mathf.Infinity;
        MeshPolygon closestPolygon = null;

        foreach (var p in navMesh)
        {
            Vector2 intersection = p.GetClosestIntersectionPoint(point);
            float distance = Vector2.Distance(intersection, point);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestPolygon = p;
            }
        }


        return closestPolygon;
    }

    public static Polygon GetCorrespondingPolygon(List<VisibilityPolygon> navMesh, Vector2 point)
    {
        foreach (var p in navMesh)
            if (p.IsPointInPolygon(point, true))
                return p;

        return null;
    }


    // Get the path using the simple stupid funnel algorithm
    // http://ahamnett.blogspot.com/2012/10/funnel-algorithm.html
    private void GetPathBySSFA(Vector2 startPoint, Vector2 destinationPoint, ref List<Vector2> path)
    {
        path.Clear();

        leftVertices.Clear();
        rightVertices.Clear();

        // Initialise portal vertices first point.
        leftVertices.Add(startPoint);
        rightVertices.Add(startPoint);

        Vector2 leftV;
        Vector2 rightV;
        // Add the gates vertices
        for (int i = 0; i < pathMesh.Count - 1; i++)
        {
            pathMesh[i].GetDiagonalOfNeighbor(pathMesh[i + 1], out leftV, out rightV);
            leftVertices.Add(leftV);
            rightVertices.Add(rightV);
        }

        leftVertices.Add(destinationPoint);
        rightVertices.Add(destinationPoint);


        int left = 1;
        int right = 1;
        Vector2 apex = startPoint;
        // Step through channel.

        // Go through the polygons
        for (int i = 1; i <= pathMesh.Count; i++)
        {
            // if (path.Count > 60) break;

            // If new left vertex is different from the current, the check
            if (leftVertices[i] != leftVertices[left] && i > left)
            {
                // If new side does not widen funnel, update.
                if (GeometryHelper.SignedAngle(apex, leftVertices[left], leftVertices[i]) <= 0f)
                {
                    // If new side crosses other side, update apex.
                    if (GeometryHelper.SignedAngle(apex, rightVertices[right], leftVertices[i]) <= 0f)
                    {
                        int next = right;
                        int prev = right;

                        // Find next vertex.
                        for (int j = next; j <= pathMesh.Count; j++)
                        {
                            if (rightVertices[j] != rightVertices[next])
                            {
                                next = j;
                                break;
                            }
                        }

                        // Find previous vertex.
                        for (int j = prev; j >= 0; j--)
                        {
                            if (rightVertices[j] != rightVertices[prev])
                            {
                                prev = j;
                                break;
                            }
                        }

                        // Calculate line angles.
                        float nextAngle = Mathf.Atan2(rightVertices[next].y - rightVertices[right].y,
                            rightVertices[next].x - rightVertices[right].x);
                        float prevAngle = Mathf.Atan2(rightVertices[right].y - rightVertices[prev].y,
                            rightVertices[right].x - rightVertices[prev].x);

                        // Calculate minimum distance between line angles.
                        float distance = nextAngle - prevAngle;

                        if (Mathf.Abs(distance) > Mathf.PI)
                        {
                            distance -= 2f * (distance > 0 ? Mathf.PI : -Mathf.PI);
                        }

                        // Calculate left perpendicular to average angle.
                        float angle = prevAngle + (distance / 2) + Mathf.PI * 0.5f;

                        Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                        float offsetDistance = offsetMultiplier;

                        Vector2 newApex = rightVertices[right];

                        // Add new offset apex to list and update right side.
                        if (left != pathMesh.Count && right != pathMesh.Count)
                        {
                            path.Add(newApex + normal * offsetDistance);
                        }

                        // Reset the funnel 
                        apex = newApex;
                        right = next;
                        left = next;
                        i = next;
                    }
                    else
                    {
                        // Narrow the funnel from left
                        left = i;
                    }
                }
            }

            // If new right vertex is different, process.
            if (rightVertices[i] != rightVertices[right] && i > right)
            {
                // If new side does not widen funnel, update.
                if (GeometryHelper.SignedAngle(apex, rightVertices[right], rightVertices[i]) >= 0f)
                {
                    // If new side crosses other side, update apex.
                    if (GeometryHelper.SignedAngle(apex, leftVertices[left], rightVertices[i]) >= 0f)
                    {
                        int next = left;
                        int prev = left;

                        // Find next vertex.
                        for (int j = next; j <= pathMesh.Count; j++)
                        {
                            if (leftVertices[j] != leftVertices[next])
                            {
                                next = j;
                                break;
                            }
                        }

                        // Find previous vertex.
                        for (int j = prev; j >= 0; j--)
                        {
                            if (leftVertices[j] != leftVertices[prev])
                            {
                                prev = j;
                                break;
                            }
                        }

                        // Calculate line angles.
                        float nextAngle = Mathf.Atan2(leftVertices[next].y - leftVertices[left].y,
                            leftVertices[next].x - leftVertices[left].x);
                        float prevAngle = Mathf.Atan2(leftVertices[left].y - leftVertices[prev].y,
                            leftVertices[left].x - leftVertices[prev].x);

                        // Calculate minimum distance between line angles.
                        float distance = nextAngle - prevAngle;

                        if (Mathf.Abs(distance) > Mathf.PI)
                        {
                            distance -= 2f * (distance > 0 ? Mathf.PI : -Mathf.PI);
                        }

                        // Calculate left perpendicular to average angle.
                        float angle = prevAngle + (distance / 2) + Mathf.PI * 0.5f;

                        Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                        float offsetDistance = offsetMultiplier;

                        Vector2 newApex = leftVertices[left];

                        // Add new offset apex to list and update right side.
                        if (left != pathMesh.Count && right != pathMesh.Count)
                        {
                            path.Add(newApex - normal * offsetDistance);
                        }

                        // Reset the funnel
                        apex = newApex;
                        left = next;
                        right = next;
                        i = next;
                    }
                    else
                    {
                        // Narrow the funnel from right
                        right = i;
                    }
                }
            }
        }


        path.Add(destinationPoint);
    }


    public void OnDrawGizmos()
    {
        if (Equals(pathMesh, null))
            foreach (var p in pathMesh)
            {
                p.Draw("");
            }

        if (Equals(leftVertices, null))
            foreach (var v in leftVertices)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(v, 0.5f);
            }

        if (Equals(rightVertices, null))
            foreach (var v in rightVertices)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(v, 0.5f);
            }
    }
}