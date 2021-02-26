using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Polygon when it is a part of a mesh
public class MeshPolygon : Polygon
{
    // number of polygons visible from this polygon
    private int m_VisibilityCount;

    // Dictionary of neighboring polygons; the key is the index of the first vertex i of the diagonal i,i+1 with the other polygon
    private Dictionary<int, MeshPolygon> m_NeighborPolygons;

    // Used for PathFinding
    // The total path cost from source polygon
    protected float m_gDistance;
    protected float m_hDistance;
    protected MeshPolygon m_previousPolygon;
    protected Vector2? m_entryPoint;

    public MeshPolygon()
    {
        m_VisibilityCount = 1;
    }

    public MeshPolygon(Polygon p) : base(p)
    {
    }

    public void SetVisibilityCount(int count)
    {
        m_VisibilityCount = count;
    }


    public int GetVisibilityCount()
    {
        return m_VisibilityCount;
    }


    public void GetDiagonalOfNeighbor(MeshPolygon nearbyPolygon, out Vector2 left, out Vector2 right)
    {
        int key = m_NeighborPolygons.KeyByValue(nearbyPolygon);

        left = GetPoint(key);
        right = GetPoint(key + 1);
    }

    // Get the mid point of the diagonal between two polygons
    public Vector2 GetMidPointOfDiagonalNeighbor(MeshPolygon nearbyPolygon)
    {
        Vector2 left, right;

        GetDiagonalOfNeighbor(nearbyPolygon, out left, out right);

        return new Vector2((left.x + right.x) / 2f, (left.y + right.y) / 2f);
    }

    public void SetEntryPoint(Vector2 entryPoint)
    {
        m_entryPoint = entryPoint;
    }

    public Vector2? GetEntryPoint()
    {
        return m_entryPoint;
    }


    public Dictionary<int, MeshPolygon> GetAdjcentPolygons()
    {
        return m_NeighborPolygons;
    }


    // Pathfinding functions
    // Set the total distance  of the path from the source to here
    public void SetgDistance(float dist)
    {
        m_gDistance = dist;
    }

    public void SethDistance(float dist)
    {
        m_hDistance = dist;
    }

    public float GetgDistance()
    {
        return m_gDistance;
    }

    public float GethDistance()
    {
        return m_hDistance;
    }

    public float GetFvalue()
    {
        return m_gDistance + m_hDistance;
    }

    public void SetPreviousPolygon(MeshPolygon prev)
    {
        m_previousPolygon = prev;
    }

    public MeshPolygon GetPreviousPolygon()
    {
        return m_previousPolygon;
    }

    public override void Draw(string label)
    {
        base.Draw(label);

        Gizmos.color = Color.green;
    }


    // Add a neighbor polygon to this polygon on the NavMesh
    public void AddNeighborPolygon(MeshPolygon poly, int vertexIndex)
    {
        if (m_NeighborPolygons == null)
            m_NeighborPolygons = new Dictionary<int, MeshPolygon>();

        m_NeighborPolygons.Add(vertexIndex, poly);
    }
}