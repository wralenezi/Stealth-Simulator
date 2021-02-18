using System;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using UnityEngine;

public class MapDecomposer : MonoBehaviour
{
    [Header("Debug")] [Tooltip("NavMesh")] public bool showNavMesh;
    [Tooltip("Seen Regions")] public bool showSeenRegions;
    [Tooltip("Unseen Regions")] public bool showUnseenRegions;

    // The main area
    private StealthArea m_StealthArea;

    // NavMesh 
    private List<MeshPolygon> m_NavMesh;

    // Decomposition Borders (Actual walls or interior walls)
    private List<Polygon> m_WallBorders;

    // Walkable area
    private float m_WalakbleArea;

    // Regions before decomposition
    private List<List<Polygon>> m_SeenRegions;
    private List<List<Polygon>> m_UnseenRegions;

    // Polygon Lists the will make the NavMesh
    private List<VisibilityPolygon> m_SeenPolygons;
    private List<VisibilityPolygon> m_UnseenPolygons;


    public void Initiate(StealthArea stealthArea)
    {
        m_StealthArea = stealthArea;

        m_NavMesh = new List<MeshPolygon>();
        m_WallBorders = m_StealthArea.mapRenderer.GetInteriorWalls();

        m_SeenRegions = new List<List<Polygon>>();
        m_UnseenRegions = new List<List<Polygon>>();

        m_SeenPolygons = new List<VisibilityPolygon>();
        m_UnseenPolygons = new List<VisibilityPolygon>();
    }

    // Create the NavMesh
    public void CreateNavMesh()
    {
        // Cut the holes in the map
        Polygon simplePolygon = PolygonHelper.CutHoles(m_WallBorders);

        // Decompose Space
        m_NavMesh = HertelMelDecomp.ConvexPartition(simplePolygon);

        // Associate Polygons with each other
        HertelMelDecomp.BuildNavMesh(m_NavMesh);

        // Calculate the area of the interior
        m_WalakbleArea = 0f;
        foreach (var p in GetNavMesh())
            m_WalakbleArea += p.GetArea();
    }

    // Create the VisMesh
    public void CreateVisMesh()
    {
        // Prepare the guards vision to be considered in the space decomposition
        ConsiderGuardVision();

        // Modify the regions to facilitate triangulation
        PrepareRegions();

        
        RegularizePolygons();

        // Decompose the unseen area
        DecomposeUnseenArea();

        // Decompose the seen area
        DecomposeSeenArea();
    }

    // Model and Aggregate the guards seen region
    void ConsiderGuardVision()
    {
        m_SeenRegions.Clear();

        // Go through the guards
        if (m_StealthArea.guardsManager != null)
            foreach (var guard in m_StealthArea.guardsManager.GetGuards())
            {
                if (guard.GetSeenArea() != null && guard.GetSeenArea().Count > 0 &&
                    guard.GetSeenArea()[0].GetVerticesCount() > 0)
                    m_SeenRegions.Add(guard.CopySeenArea());
            }

        // Merge the guards seen areas if they intersect
        MergeGuardSeenAreas();
    }

    // Merge guards seen area
    void MergeGuardSeenAreas()
    {
        // Assume there are intersection of seen areas between the guards
        bool isThereIntersectionHappened = true;

        while (isThereIntersectionHappened)
        {
            isThereIntersectionHappened = false;
            int firstPoly = -1;
            int secondPoly = -1;

            for (var i = 0; i < m_SeenRegions.Count; i++)
            {
                for (var j = i + 1; j < m_SeenRegions.Count; j++)
                {
                    List<Polygon> intersection = PolygonHelper.MergePolygons(m_SeenRegions[i], m_SeenRegions[j],
                        ClipType.ctIntersection);

                    if (intersection.Count > 0)
                    {
                        isThereIntersectionHappened = true;
                        firstPoly = i;
                        secondPoly = j;

                        break;
                    }
                }

                // if there is intersection, stop the loop
                if (firstPoly != -1)
                    break;
            }

            // Remove the two intersecting areas and replace them with their union
            if (firstPoly != -1)
            {
                // Union the two guards seen area
                var seenAreaUnion = PolygonHelper.MergePolygons(m_SeenRegions[firstPoly], m_SeenRegions[secondPoly],
                    ClipType.ctUnion);

                // intersect it with the walls to remove overlapping areas
                seenAreaUnion = PolygonHelper.MergePolygons(m_WallBorders, seenAreaUnion,
                    ClipType.ctIntersection);

                m_SeenRegions.Remove(m_SeenRegions[secondPoly]);
                m_SeenRegions.Remove(m_SeenRegions[firstPoly]);
                m_SeenRegions.Add(seenAreaUnion);
            }
        }
    }

    public Polygon GetRandomPolygon()
    {
        int randPoly = UnityEngine.Random.Range(0, GetNavMesh().Count);
        return GetNavMesh()[randPoly];
    }

    // Prepare the unseen polygons of the NavMesh ( important to be done before the seen area since it modifies the seen area)
    private void PrepareRegions()
    {
        m_UnseenRegions.Clear();
        // the difference between the walkable area and seen area is the unseen area
        List<Polygon> differenceResult = new List<Polygon>();

        differenceResult.AddRange(m_WallBorders);

        // Take the difference for each seen region; The polygons in the result are Outer polygons (they have CounterClockWise winding) and holes (opposite winding)
        foreach (List<Polygon> guardSeenArea in m_SeenRegions)
            differenceResult = PolygonHelper.MergePolygons(guardSeenArea, differenceResult,
                ClipType.ctDifference);

        // To triangulate the area without guards
        if (m_SeenRegions.Count == 0)
        {
            m_UnseenRegions.Add(m_WallBorders);
            return;
        }

        // The result of the difference can contain complex polygons which will ruin the triangulation. This will simplify them
        // PolygonHelper.SimplifyComplexPolygons(differenceResult);

        // Remove the tiny shards 
        // CleanPolygons(differenceResult);

        // Sort the outer walls and inner walls
        OrganizeUnseenPolygons(differenceResult);

        OrganizeSeenRegions();
    }


    // Sort the CounterClockWise polygons as Outer polygons and find the holes in them
    void OrganizeUnseenPolygons(List<Polygon> differenceResult)
    {
        List<Polygon> holePolys = new List<Polygon>();

        foreach (Polygon p in differenceResult)
        {
            if (p.DetermineWindingOrder() == Properties.outerPolygonWinding)
            {
                List<Polygon> wall = new List<Polygon>();
                wall.Add(p);
                m_UnseenRegions.Add(wall);
            }
            else
            {
                holePolys.Add(p);
            }
        }

        differenceResult.Clear();

        // Add the hole to the outer wall the contains it
        foreach (List<Polygon> wall in m_UnseenRegions)
        {
            foreach (Polygon hole in holePolys)
                if (wall[0].IsPolygonInside(hole, false))
                {
                    wall.Add(hole);
                }
        }
    }

    void OrganizeSeenRegions()
    {
        List<Polygon> holePolys = new List<Polygon>();
        List<List<Polygon>> seenRegions = new List<List<Polygon>>();

        foreach (var seenRegion in m_SeenRegions)
        foreach (Polygon p in seenRegion)
        {
            if (p.DetermineWindingOrder() == Properties.outerPolygonWinding)
            {
                List<Polygon> wall = new List<Polygon>();
                wall.Add(p);
                seenRegions.Add(wall);
            }
            else
            {
                holePolys.Add(p);
            }
        }


        // Add the hole to the outer wall the contains it
        foreach (List<Polygon> wall in seenRegions)
        {
            foreach (Polygon hole in holePolys)
                if (wall[0].IsPolygonInside(hole, false))
                {
                    wall.Add(hole);
                }
        }

        // Intersect with the borders so it will intersect it
        int i = 0;
        while (i < seenRegions.Count)
        {
            seenRegions[i] = PolygonHelper.MergePolygons(seenRegions[i], m_WallBorders, ClipType.ctIntersection);
            i++;
        }

        m_SeenRegions = seenRegions;
    }

    private void RegularizePolygons()
    {
        for (int i = 0; i < m_UnseenRegions.Count; i++)
        {
            if (m_UnseenRegions[i].Count == 0)
            {
                m_UnseenRegions.RemoveAt(i);
                i = 0;
                continue;
            }
            
            for (int j = 0; j < m_UnseenRegions[i].Count; j++)
                if (m_UnseenRegions[i][j].GetVerticesCount() < 3)
                {
                    m_UnseenRegions[i].RemoveAt(j);
                    j = 0;
                    i = 0;
                }
        }


        for (int i = 0; i < m_SeenRegions.Count; i++)
        {
            if (m_SeenRegions[i].Count == 0)
            {
                m_SeenRegions.RemoveAt(i);
                i = 0;
                continue;
            }

            for (int j = 0; j < m_SeenRegions[i].Count; j++)
                if (m_SeenRegions[i][j].GetVerticesCount() < 3)
                {
                    m_SeenRegions[i].RemoveAt(j);
                    j = 0;
                    i = 0;
                }
        }
    }

    // Triangulate the unseen areas
    void DecomposeUnseenArea()
    {
        m_UnseenPolygons.Clear();

        foreach (List<Polygon> pL in m_UnseenRegions)
        {
            Polygon polygon = PolygonHelper.CutHoles(pL);
            List<MeshPolygon> tempPolys = HertelMelDecomp.ConvexPartition(polygon);

            m_UnseenPolygons.AddRange(tempPolys.ConvertAll(x => new VisibilityPolygon(x)));
        }
    }

    // Triangulate the seen area
    void DecomposeSeenArea()
    {
        m_SeenPolygons.Clear();

        foreach (List<Polygon> guardSeenArea in m_SeenRegions)
        {
            Polygon polygon = PolygonHelper.CutHoles(guardSeenArea);

            if (polygon != null)
            {
                List<MeshPolygon> tempPolys = HertelMelDecomp.ConvexPartition(polygon);
                m_SeenPolygons.AddRange(tempPolys.ConvertAll(x => new VisibilityPolygon(x)));
            }
        }
    }

    public List<MeshPolygon> GetNavMesh()
    {
        return m_NavMesh;
    }

    // Get the walkable area 
    public float GetNavMeshArea()
    {
        return m_WalakbleArea;
    }

    public List<VisibilityPolygon> GetSeenPolygons()
    {
        return m_SeenPolygons;
    }

    public List<VisibilityPolygon> GetUnseenPolygons()
    {
        return m_UnseenPolygons;
    }
    
    private void OnDrawGizmos()
    {
        if (showNavMesh)
        {
            foreach (var poly in m_NavMesh)
                poly.Draw(poly.GetgDistance().ToString());
        }

        if (showSeenRegions)
        {
            foreach (var region in m_SeenRegions)
            foreach (var poly in region)
                poly.Draw(poly.DetermineWindingOrder().ToString());
        }

        if (showUnseenRegions)
        {
            foreach (var region in m_UnseenRegions)
            foreach (var poly in region)
                poly.Draw(poly.DetermineWindingOrder().ToString());
        }
    }
}