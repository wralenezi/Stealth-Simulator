using System;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using UnityEngine;
using UnityEngine.Serialization;

public class MapDecomposer : MonoBehaviour
{
    [Header("Debug")] [Tooltip("NavMesh")] public bool showNavMesh;

    private List<MeshPolygon> _navMesh;

    // Decomposition Borders (Actual walls or interior walls)
    private List<Polygon> _wallBorders;

    // Walkable area
    [SerializeField] private float _walkableArea;


    public void Initiate(List<Polygon> walls)
    {
        _navMesh = new List<MeshPolygon>();
        _wallBorders = walls;

        CreateNavMesh();
    }

    // Create the NavMesh
    private void CreateNavMesh()
    {
        // Cut the holes in the map
        Polygon simplePolygon = PolygonHelper.CutHoles(_wallBorders);

        // Decompose Space
        _navMesh = HertelMelDecomp.ConvexPartition(simplePolygon);

        // Associate Polygons with each other
        HertelMelDecomp.BuildNavMesh(_navMesh);

        // Calculate the area of the interior
        _walkableArea = 0f;
        foreach (var p in GetNavMesh())
            _walkableArea += p.GetArea();
    }

    // Get a random polygon from the NavMesh
    public Polygon GetRandomPolygonInNavMesh()
    {
        int randPoly = UnityEngine.Random.Range(0, GetNavMesh().Count);
        return GetNavMesh()[randPoly];
    }
    
    public List<MeshPolygon> GetNavMesh()
    {
        return _navMesh;
    }

    // Get the walkable area 
    public float GetNavMeshArea()
    {
        return _walkableArea;
    }

    private void OnDrawGizmos()
    {
        if (showNavMesh)
        {
            foreach (var poly in _navMesh)
            {
                poly.Draw(poly.GetgDistance().ToString());
            }
        }
    }
}