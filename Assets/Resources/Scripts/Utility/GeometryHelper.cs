﻿using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public static class GeometryHelper
{
    // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
    // To find orientation of ordered triplet (p, q, r). 
    // The function returns following values 
    // 0 --> p, q and r are collinear 
    // 1 --> Clockwise 
    // 2 --> Counterclockwise 
    static int GetPointsOrientation(Vector2 p, Vector2 q, Vector2 r)
    {
        // for details of below formula. 
        float val = (q.y - p.y) * (r.x - q.x) -
                    (q.x - p.x) * (r.y - q.y);

        if (val == 0) return 0; // collinear 

        return (val > 0) ? 1 : 2; // clock or counterclockwise 
    }

    // Given three collinear points p, q, r, the function checks if 
    // point q lies on line segment 'pr' 
    public static bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
            return true;

        return false;
    }


    // The main function that returns true if line segment 'p1q1' and 'p2q2' intersect. 
    public static bool DoLinesIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2, bool includeEndPoints)
    {
        if (!includeEndPoints)
            if (p1.Equals(p2) || p1.Equals(q2) || q1.Equals(p2) || q1.Equals(q2))
                return false;

        // Find the four orientations needed for general and 
        // special cases 
        int o1 = GetPointsOrientation(p1, q1, p2);
        int o2 = GetPointsOrientation(p1, q1, q2);
        int o3 = GetPointsOrientation(p2, q2, p1);
        int o4 = GetPointsOrientation(p2, q2, q1);

        // General case 
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases 
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1 
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are collinear and q2 lies on segment p1q1 
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are collinear and p1 lies on segment p2q2 
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are collinear and q1 lies on segment p2q2 
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases 
    }

    // Check if an angle made up of three points p1,p2,p3 is reflex
    public static bool IsReflex(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        // Get the direction from this points to the next, and the direction of the previous points to it
        Vector2 d1 = (p2 - p1).normalized;
        Vector2 d2 = (p3 - p2).normalized;

        // Flip the direction vertically 
        Vector2 n2 = new Vector2(-d2.y, d2.x);

        // Get the dot product and if it is negative or zero then reflex else it is convex
        return Vector2.Dot(d1, n2) < 0f;
    }

    // Check the signed angle between ac and ab; if negative then c is one the right of ab
    public static float SignedAngle(Vector2 a, Vector2 b, Vector2 c)
    {
        return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
    }

    // Get the unsigned angle between three points
    public static float GetAngle(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        return Vector2.Angle(v1 - v2, v3 - v2);
    }

    /// <summary>
    /// Get the normal for the corner v2 
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <returns></returns>
    public static Vector2 GetNormal(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        Vector2 dir1 = v2 - v1;
        Vector2 dir2 = v2 - v3;
        return (dir1.normalized + dir2.normalized).normalized;
    }

    // Check if point pt is in the triangle v1,v2 and v3.
    public static bool PointInTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 pt)
    {
        float d1 = SignedAngle(pt, v1, v2);
        float d2 = SignedAngle(pt, v2, v3);
        float d3 = SignedAngle(pt, v3, v1);

        var hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        var hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    /// <summary>
    /// Gets the coordinates of the intersection point of two lines.
    /// source : https://blog.dakwamine.fr/?p=1943
    /// </summary>
    /// <param name="A1">A point on the first line.</param>
    /// <param name="A2">Another point on the first line.</param>
    /// <param name="B1">A point on the second line.</param>
    /// <param name="B2">Another point on the second line.</param>
    /// <param name="found">Is set to false of there are no solution. true otherwise.</param>
    /// <returns>The intersection point coordinates. Returns Vector2.zero if there is no solution.</returns>
    public static Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2,
        bool includeVertices, out bool found)
    {
        float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

        // Check if they are parallel
        if (Math.Abs(tmp) < 0.0001f)
        {
            // No solution!
            found = false;
            return Vector2.zero;
        }

        float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

        Vector2 point = new Vector2(B1.x + (B2.x - B1.x) * mu, B1.y + (B2.y - B1.y) * mu);

        // Check if that point is between the points B1, B2
        found = DoLinesIntersect(A1, A2, B1, B2, includeVertices);

        return point;
    }

    // Check if point c lies on the lines a,b
    public static bool IsPointOnLine(Vector2 a, Vector2 b, Vector2 c)
    {
        float ab = Vector2.Distance(a, b);

        float ac = Vector2.Distance(a, c);

        float bc = Vector2.Distance(b, c);

        return Math.Abs(ab - (ac + bc)) < 0.01f;
    }

    // Get the closest point to a point from a list
    public static Vector2 GetClosestPointFromList(Vector2 origin, List<Vector2> points)
    {
        Vector2 closestIntersection = points[0];
        float minDist = Mathf.Infinity;

        foreach (var inter in points)
        {
            float dist = Vector2.Distance(origin, inter);

            if (dist < minDist)
            {
                closestIntersection = inter;
                minDist = dist;
            }
        }

        return closestIntersection;
    }

    // Return closest point to point p on line segment qr.
    public static Vector2 ClosestProjectionOnSegment(Vector2 segP1, Vector2 segP2, Vector2 p)
    {
        float length2 = Mathf.Pow(Vector2.Distance(segP1, segP2), 2);

        if (Math.Abs(length2) < 0.00001f) return segP1;

        // Consider the line extending the segment, parameterized as r + t (q - r).
        // We find projection of point p onto the line. 
        // It falls where t = [(p-r) . (q-r)] / |q-r|^2
        // We clamp t from [0,1] to handle points outside the segment qr.
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - segP2, segP1 - segP2) / length2));
        Vector2 projection = segP2 + t * (segP1 - segP2); // Projection falls on the segment

        return projection;
    }

    /// <summary>
    /// Check if two circles of the same size are mutually visible against colliders of a specific layer
    /// </summary>
    /// <param name="first">center of first circle</param>
    /// <param name="second">center of the second circle</param>
    /// <param name="radius">Radius of both circles</param>
    /// <param name="layer">Layer of the collider</param>
    /// <returns></returns>
    public static bool IsCirclesVisible(Vector2 first, Vector2 second, float radius, string layer)
    {
        // Vector2 dir = (second - first).normalized;
        // float distance = Vector2.Distance(first, second);
        // Vector2 left = Vector2.Perpendicular(dir);
        // RaycastHit2D hitLeft = Physics2D.Raycast(first + left * radius, dir, distance, LayerMask.GetMask(layer));
        // RaycastHit2D hitRight = Physics2D.Raycast(first - left * radius, dir, distance, LayerMask.GetMask(layer));
        
        // // Avoiding using the distance to spare some CPU
        Vector2 offset = (second - first);
        Vector2 left = Vector2.Perpendicular(offset.normalized);
        Vector2 leftStart = first + left * radius;
        RaycastHit2D hitLeft = Physics2D.Linecast(leftStart, offset + leftStart, LayerMask.GetMask(layer));
        Vector2 rightStart = first - left * radius;
        RaycastHit2D hitRight = Physics2D.Linecast(rightStart, offset + rightStart, LayerMask.GetMask(layer));
        

        return Equals(hitLeft.collider, null) && Equals(hitRight.collider, null);
    }

    public static Vector2? GetClosetPointOnPath(List<Vector2> path, Vector2 point, float radius)
    {
        float minSqrMag = Mathf.Infinity;
        Vector2? projection = null;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 pOnSegment = ClosestProjectionOnSegment(path[i], path[i + 1], point);

            bool isVisible = IsCirclesVisible(pOnSegment, point, radius, "Wall");

            Vector2 diff = point - pOnSegment;
            float sqrMag = diff.sqrMagnitude;

            if (minSqrMag > sqrMag && isVisible)
            {
                minSqrMag = sqrMag;
                projection = pOnSegment;
            }
        }

        return projection;
    }


    public static void SimplifiyLine(List<Vector2> line, float epsilon)
    {
        float minAngle = 10f;
        float maxAngle = 160f;

        for (int index = 1; index < line.Count - 1; index++)
        {
            // Remove the vertices if its angle is below the min threshold or more than the max threshold
            if (GetAngle(line[index - 1], line[index], line[index + 1]) >= maxAngle)
            {
                line.RemoveAt(index);
                index = 1;
                continue;
            }

            if (GetAngle(line[index - 1], line[index], line[index + 1]) <= minAngle)
            {
                line.RemoveAt(index);
                index = 1;
            }
            else if (Vector2.Distance(line[index - 1], line[index + 1]) <= epsilon)
            {
                line.RemoveAt(index);
                index = 1;
            }
        }
    }
}