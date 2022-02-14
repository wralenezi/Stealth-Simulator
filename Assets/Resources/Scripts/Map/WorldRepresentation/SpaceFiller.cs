using System;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using UnityEngine;

public class SpaceFiller : MonoBehaviour
{
    private int m_StartingPolygonVertices = 50;
    private float m_MinimumThreshold = 2f;
    private float m_MaxThreshold = 2f;


    private MapDecomposer m_MapDecomposer;
    private MapRenderer m_MapRenderer;
    private List<Polygon> m_SearchRegions;
    private List<CyclicalList<InterceptPoint>> m_InterceptionPoints;

    // Obstacle Layer
    private LayerMask m_ObstacleMask;

    public void Initiate()
    {
        m_MapDecomposer = GetComponent<MapDecomposer>();
        m_MapRenderer = GetComponent<MapRenderer>();
        m_SearchRegions = new List<Polygon>();
        m_InterceptionPoints = new List<CyclicalList<InterceptPoint>>();

        // Set the layer to intersect vision
        m_ObstacleMask = LayerMask.GetMask("Wall");
    }

    // Initiate the expanding circle
    public void CreateExpandingCircle(Vector2 origin, List<Guard> guards)
    {
        m_SearchRegions.Clear();
        m_InterceptionPoints.Clear();


        Polygon expanding = new Polygon();

        float angleStep = 360f / m_StartingPolygonVertices;

        for (float i = 0; i < 360; i += angleStep)
        {
            Vector2 direction = new Vector2(Mathf.Sin(i * Mathf.Deg2Rad), Mathf.Cos(i * Mathf.Deg2Rad));
            Vector2 vertex = origin + direction * 3f;
            expanding.AddPoint(vertex);
        }

        expanding.EnsureWindingOrder(Properties.outerPolygonWinding);

        m_SearchRegions.Add(expanding);

        Expand();
        Restrict(guards);

        for (int i = 0; i < m_SearchRegions.Count; i++)
        {
            if (!m_SearchRegions[i].IsPointInPolygon(origin, true))
            {
                m_SearchRegions.RemoveAt(i);
                i--;
            }
        }

        CreateTargetPoints();
    }

    // Create the target points
    public void CreateTargetPoints()
    {
        CyclicalList<InterceptPoint> points = new CyclicalList<InterceptPoint>();

        foreach (var polygon in m_SearchRegions)
        {
            // Find the ears
            for (int i = 0; i < polygon.GetVerticesCount(); i++)
            {
                EarClipDecomp.UpdateVertex(i, polygon);


                if (!polygon.GetVertex(i).isEar)
                {
                    Vector2 dir = GeometryHelper.GetNormal(polygon.GetPoint(i - 1), polygon.GetPoint(i),
                        polygon.GetPoint(i + 1));

                    InterceptPoint point = new InterceptPoint(polygon.GetPoint(i) + dir * -0.2f);

                    points.Add(point);
                }
            }
        }

        ReduceTargets(points);

        RemoveNearbyPoints(points);

        m_InterceptionPoints.Add(points);
    }

    // To remove the next step point if they are too close to the previous points
    public void RemoveNearbyPoints(CyclicalList<InterceptPoint> points)
    {
        if (m_InterceptionPoints.Count > 1)
        {
            foreach (var point in m_InterceptionPoints[m_InterceptionPoints.Count - 1])
            {
                for (int i = 0; i < points.Count; i++)
                {
                    float distance = Vector2.Distance(points[i].Position, point.Position);

                    if (distance < m_MinimumThreshold)
                    {
                        points.RemoveAt(i);
                        i--;
                        break;
                    }

                    RaycastHit2D hit = Physics2D.Linecast(points[i].Position, point.Position, m_ObstacleMask);

                    // if (distance < m_MinimumThreshold + m_MaxThreshold)
                    {
                        points[i].Parent = point;
                    }
                }
            }
        }
    }

    // Reduce the number of close targets to meaningful segments 
    void ReduceTargets(CyclicalList<InterceptPoint> points)
    {
        int i = 0;
        while (i < points.Count)
        {
            if (points.Count == 1)
                break;

            float distanceToNext = Vector2.Distance(points[i].Position, points[i + 1].Position);

            RaycastHit2D ray = Physics2D.Linecast(points[i].Position, points[i + 1].Position, m_ObstacleMask);

            if (distanceToNext < 3f && !ray)
            {
                Vector2 midPoint = new Vector2((points[i].Position.x + points[i + 1].Position.x) * 0.5f,
                    (points[i].Position.y + points[i + 1].Position.y) * 0.5f);

                points[i].Position = midPoint;
                points.RemoveAt(i + 1);
                continue;
            }

            i++;
        }
    }

    public void RemoveFromCircle(List<Polygon> fov)
    {
        if (m_SearchRegions.Count > 0)
            m_SearchRegions = PolygonHelper.MergePolygons(fov, m_SearchRegions, ClipType.ctDifference);
    }


    public void Expand()
    {
        if (m_SearchRegions.Count > 0)
        {
            foreach (var polygon in m_SearchRegions)
                polygon.Enlarge(-0.03f);
        }
    }


    public void Restrict(List<Guard> guards)
    {
        // foreach (var guard in guards)
        // {
        //     guard.RestrictSearchArea();
        // }

        m_SearchRegions =
            PolygonHelper.MergePolygons(m_SearchRegions, m_MapRenderer.GetInteriorWalls(),
                ClipType.ctIntersection);
    }


    public void RemoveSpottedInterceptionPoints(List<Polygon> fov)
    {
        for (int i = 0; i < m_InterceptionPoints.Count; i++)
        {
            for (int j = 0; j < m_InterceptionPoints[i].Count; j++)
            {
                if (fov[0].IsPointInPolygon(m_InterceptionPoints[i][j].Position, false))
                {
                    m_InterceptionPoints[i].RemoveAt(j);
                    j--;
                }
            }
        }
    }


    // If the search is active
    public bool IsSearchActive()
    {
        return m_SearchRegions.Count > 0;
    }


    public List<CyclicalList<InterceptPoint>> GetInterceptionPoints()
    {
        return m_InterceptionPoints;
    }


    public Vector2? GetGoal(Vector2 guardPosition)
    {
        if (m_InterceptionPoints.Count > 0)
            return m_InterceptionPoints[0][0].Position;


        return null;
    }


    public void Clear()
    {
        m_SearchRegions.Clear();
        m_InterceptionPoints.Clear();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (m_SearchRegions != null)
            foreach (var polygon in m_SearchRegions)
            {
                polygon.Draw("");
            }


        if(m_InterceptionPoints != null)
            foreach (var layer in m_InterceptionPoints)
            foreach (var point in layer)
            {
                point.Draw();
            }
    }
}