using System.Collections.Generic;
using ClipperLib;
using UnityEngine;

public class VisMesh : MonoBehaviour
{
    private float _maxSeenRegionAreaPerGuard;

    // Guards seen regions
    private Dictionary<string, List<Polygon>> _guardsSeenRegions;

    // Regions before decomposition
    public bool showSeenRegions;
    public bool showUnseenRegions;
    private List<Polygon> _newSeenRegion;
    private List<Polygon> _newUnseenRegion;
    
    private List<Polygon> _SeenRegion;
    private List<Polygon> _UnseenRegion;

    // Previous Polygons
    private List<VisibilityPolygon> _preSeenPolygons;
    private List<VisibilityPolygon> _preUnseenPolygons;

    // Current Polygons 
    public bool showSeenPolygons;
    public bool showUnseenPolygons;
    private List<VisibilityPolygon> _curSeenPolygons;
    private List<VisibilityPolygon> _curUnseenPolygons;

    // Temp containers
    List<VisibilityPolygon> overallMesh;

    public static float OldestTimestamp;

    // Visibility mesh polygons
    private List<VisibilityPolygon> _visMeshPolygons;

    // The last timestamp recorded
    private float _lastTimestamp;

    public void Initiate(float maxSeenRegionAreaPerGuard)
    {
        _maxSeenRegionAreaPerGuard = maxSeenRegionAreaPerGuard;

        _guardsSeenRegions = new Dictionary<string, List<Polygon>>();

        // Current Polygons 
        _curSeenPolygons = new List<VisibilityPolygon>();
        _curUnseenPolygons = new List<VisibilityPolygon>();

        // Previous Polygons
        _preSeenPolygons = new List<VisibilityPolygon>();
        _preUnseenPolygons = new List<VisibilityPolygon>();

        overallMesh = new List<VisibilityPolygon>();

        _newSeenRegion = new List<Polygon>();
        _newUnseenRegion = new List<Polygon>();
        
        _SeenRegion = new List<Polygon>();
        _UnseenRegion = new List<Polygon>();

        _visMeshPolygons = new List<VisibilityPolygon>();

        // showSeenPolygons = true;
        // showUnseenPolygons = true;

        // showSeenRegions = true;
        showUnseenRegions = true;
    }

    // Reset the variables
    public void Reset()
    {
        // Current Polygons 
        _curSeenPolygons.Clear();
        _curUnseenPolygons.Clear();

        // Previous Polygons
        _preSeenPolygons.Clear();
        _preUnseenPolygons.Clear();

        _newSeenRegion.Clear();
        _newUnseenRegion.Clear();
        
        _SeenRegion.Clear();
        _UnseenRegion.Clear();

        _visMeshPolygons.Clear();
    }
    

    // Get the new partitioning and populate the VisMesh
    public void ConstructVisMesh(List<Guard> guards)
    {
        // Move the current VisMesh to the previous one
        if (_curUnseenPolygons.Count > 0) MigrateVisMesh();

        // Decompose the area 
        CreateVisMesh(guards);

        // Calculate the staleness of the current polygons based on the old previous
        if (_preUnseenPolygons.Count > 0) CalculateCurrentStaleness();

        SetOldestTimestamp();

        // Prepare the NavMesh 
        PrepVisMesh();
    }


    // Copy the current visibility polygons to the previous visibility polygons
    private void MigrateVisMesh()
    {
        _preUnseenPolygons.Clear();
        foreach (var polygon in _curUnseenPolygons)
            _preUnseenPolygons.Add(polygon);
        _curUnseenPolygons.Clear();

        _preSeenPolygons.Clear();
        foreach (var polygon in _curSeenPolygons)
            _preSeenPolygons.Add(polygon);
        _curSeenPolygons.Clear();
    }

    // Create the VisMesh
    private void CreateVisMesh(List<Guard> guards)
    {
        _SeenRegion.Clear();
        _UnseenRegion.Clear();
        
        CreateSeenRegions(guards);

        PrepareRegionsNew();

        CleanRegion(ref _newSeenRegion);
        
        _SeenRegion.AddRange(_newSeenRegion);

        DecomposeRegions(ref _newSeenRegion, ref _curSeenPolygons);

        CleanRegion(ref _newUnseenRegion);
        
        _UnseenRegion.AddRange(_newUnseenRegion);
        
        DecomposeRegions(ref _newUnseenRegion, ref _curUnseenPolygons);
    }
    
    private void CreateSeenRegions(List<Guard> guards)
    {
        // ResetSeenRegion();

        foreach (var guard in guards)
        {
            if (!_guardsSeenRegions.ContainsKey(guard.name))
            {
                _guardsSeenRegions.Add(guard.name, new List<Polygon>());
                continue;
            }

            List<Polygon> guardSeenRegion = _guardsSeenRegions[guard.name];
            PolygonHelper.MergePolygons(guardSeenRegion, guard.GetFov(), ref guardSeenRegion, ClipType.ctUnion);
        }
    }

    private void ResetSeenRegion()
    {
        float totalArea = 0f;

        foreach (var seenRegion in _guardsSeenRegions.Values)
        foreach (var poly in seenRegion)
            totalArea += poly.GetArea();

        float seenAreaPercent = totalArea / MapManager.Instance.mapDecomposer.GetNavMeshArea();

        if (seenAreaPercent > _maxSeenRegionAreaPerGuard)
            foreach (var seenRegion in _guardsSeenRegions.Values)
                seenRegion.Clear();
    }

    private void PrepareRegionsNew()
    {
        _newSeenRegion.Clear();
        foreach (var region in _guardsSeenRegions.Values)
            PolygonHelper.MergePolygons(_newSeenRegion, region, ref _newSeenRegion, ClipType.ctUnion);

        _newUnseenRegion.Clear();
        List<Polygon> walls = MapManager.Instance.mapRenderer.GetInteriorWalls();
        PolygonHelper.MergePolygons(_newSeenRegion, walls, ref _newUnseenRegion, ClipType.ctDifference);
    }


    private void CleanRegion(ref List<Polygon> region)
    {
        for (int j = 0; j < region.Count; j++)
        {
            // region[j].SmoothPolygon(0.1f);

            if (region[j].GetVerticesCount() < 3)
            {
                region.RemoveAt(j);
                j = 0;
            }
        }
    }

    // Triangulate the unseen areas
    void DecomposeRegions(ref List<Polygon> regions, ref List<VisibilityPolygon> output)
    {
        output.Clear();

        List<Polygon> polygonToDecomp = new List<Polygon>();

        int regionsCount = regions.Count;
        while (regions.Count > 0 && regionsCount > 0)
        {
            polygonToDecomp.Clear();

            // Get an outer polygon
            for (int i = 0; i < regions.Count; i++)
            {
                Polygon outerPoly = regions[i];

                if (Equals(outerPoly.DetermineWindingOrder(), Properties.outerPolygonWinding))
                {
                    polygonToDecomp.Add(outerPoly);
                    regions.RemoveAt(i);
                    break;
                }
            }


            // fill the holes
            if (polygonToDecomp.Count > 0)
                for (int i = 0; i < regions.Count; i++)
                {
                    Polygon hole = regions[i];

                    if (!Equals(hole.DetermineWindingOrder(), Properties.outerPolygonWinding) &&
                        polygonToDecomp[0].IsPolygonInside(hole, false))
                    {
                        polygonToDecomp.Add(hole);
                        regions.RemoveAt(i);
                        i = 0;
                    }
                }


            Polygon polygon = PolygonHelper.CutHoles(polygonToDecomp);
            List<MeshPolygon> tempPolys = HertelMelDecomp.ConvexPartition(polygon);

            output.AddRange(tempPolys.ConvertAll(x =>
                new VisibilityPolygon(x, StealthArea.GetElapsedTimeInSeconds())));

            regionsCount--;
        }
    }

    // Move the staleness information from the previous mesh to current mesh
    void CalculateCurrentStaleness()
    {
        overallMesh.Clear();
        overallMesh.AddRange(_preSeenPolygons);
        overallMesh.AddRange(_preUnseenPolygons);

        // Unseen area can only be part of previous unseen area
        MigratePolygonStaleness(_curUnseenPolygons, overallMesh);
    }

    private void SetOldestTimestamp()
    {
        OldestTimestamp = StealthArea.GetElapsedTimeInSeconds();
        foreach (var vp in _curUnseenPolygons)
        {
            if (OldestTimestamp > vp.GetTimestamp())
                OldestTimestamp = vp.GetTimestamp();
        }
    }


    // Alternative method to pass the staleness info
    void MigratePolygonStaleness(List<VisibilityPolygon> newMesh, List<VisibilityPolygon> oldMesh)
    {
        foreach (VisibilityPolygon newVp in newMesh)
        {
            float largestOverlapArea = Mathf.NegativeInfinity;
            float timestamp = Mathf.NegativeInfinity;

            foreach (VisibilityPolygon oldVp in oldMesh)
            {
                // The overlap area
                VisibilityPolygon intersection = PolygonHelper.GetIntersectionArea(newVp, oldVp);

                // if the intersection exists
                if (intersection.GetVerticesCount() > 0)
                {
                    float overlapArea = intersection.GetArea();

                    if (largestOverlapArea < overlapArea)
                    {
                        largestOverlapArea = overlapArea;
                        timestamp = oldVp.GetTimestamp();
                    }
                }
            }

            newVp.SetTimestamp(timestamp);
        }
    }

    void PrepVisMesh()
    {
        _visMeshPolygons.Clear();
        // _visMeshPolygons.AddRange(_curSeenPolygons);
        _visMeshPolygons.AddRange(_curUnseenPolygons);
    }


    public List<VisibilityPolygon> GetVisMesh()
    {
        return _visMeshPolygons;
    }

    private void OnDrawGizmos()
    {
        if (showSeenRegions)
        {
            foreach (var poly in _SeenRegion)
                poly.Draw(poly.DetermineWindingOrder().ToString());
        }

        if (showUnseenRegions)
        {
            foreach (var poly in _UnseenRegion)
                poly.Draw(poly.DetermineWindingOrder().ToString());
        }

        if (showSeenPolygons)
        {
            foreach (var poly in _curSeenPolygons)
            {
                poly.Draw(poly.GetStaleness().ToString());
            }
        }

        if (showUnseenPolygons)
        {
            foreach (var poly in _curUnseenPolygons)
            {
                poly.Draw(poly.GetStaleness().ToString());
            }
        }
    }
}