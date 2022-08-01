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
    private List<Polygon> _SeenRegion;
    private List<Polygon> _UnseenRegion;

    private List<Polygon> _tempSeenRegion;
    private List<Polygon> _tempUnseenRegion;

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

        _SeenRegion = new List<Polygon>();
        _UnseenRegion = new List<Polygon>();

        _tempSeenRegion = new List<Polygon>();
        _tempUnseenRegion = new List<Polygon>();

        _visMeshPolygons = new List<VisibilityPolygon>();

        // showSeenPolygons = true;
        // showUnseenPolygons = true;
        // showSeenRegions = true;
        // showUnseenRegions = true;
    }

    // Reset the variables
    public void Reset()
    {
        _guardsSeenRegions.Clear();

        // Current Polygons 
        _curSeenPolygons.Clear();
        _curUnseenPolygons.Clear();

        // Previous Polygons
        _preSeenPolygons.Clear();
        _preUnseenPolygons.Clear();

        _SeenRegion.Clear();
        _UnseenRegion.Clear();

        _tempSeenRegion.Clear();
        _tempUnseenRegion.Clear();

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

        // Prepare the NavMesh 
        PrepVisMesh();

        SetOldestTimestamp(ref _curUnseenPolygons);
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
        _tempSeenRegion.Clear();
        _tempUnseenRegion.Clear();

        CreateSeenRegions(guards);

        PrepareRegionsNew();

        CleanRegion(ref _SeenRegion);

        _tempSeenRegion.AddRange(_SeenRegion);

        DecomposeRegions(ref _SeenRegion, ref _curSeenPolygons);

        CleanRegion(ref _UnseenRegion);

        _tempUnseenRegion.AddRange(_UnseenRegion);

        DecomposeRegions(ref _UnseenRegion, ref _curUnseenPolygons);

        RemoveInsignificantPolygons(ref _curUnseenPolygons);
    }

    // Merge the guards seen reigons
    private void CreateSeenRegions(List<Guard> guards)
    {
        ResetSeenRegion();

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

        foreach (var vp in _preUnseenPolygons)
            totalArea += vp.GetArea();

        float seenAreaPercent = 1.001f - totalArea / MapManager.Instance.mapDecomposer.GetNavMeshArea();

        if (seenAreaPercent > _maxSeenRegionAreaPerGuard)
            foreach (var seenRegion in _guardsSeenRegions.Values)
                seenRegion.Clear();
    }

    // Create the unseen regions
    private void PrepareRegionsNew()
    {
        _SeenRegion.Clear();
        foreach (var region in _guardsSeenRegions.Values)
            PolygonHelper.MergePolygons(_SeenRegion, region, ref _SeenRegion, ClipType.ctUnion);

        _UnseenRegion.Clear();
        List<Polygon> walls = MapManager.Instance.mapRenderer.GetInteriorWalls();
        PolygonHelper.MergePolygons(_SeenRegion, walls, ref _UnseenRegion, ClipType.ctDifference);
    }


    private void CleanRegion(ref List<Polygon> region)
    {
        for (int j = 0; j < region.Count; j++)
        {
            region[j].SmoothPolygon(1f);

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

            if (Equals(polygon, null)) return;

            List<MeshPolygon> tempPolys = HertelMelDecomp.ConvexPartition(polygon);

            if (Equals(tempPolys, null)) return;

            output.AddRange(tempPolys.ConvertAll(x =>
                new VisibilityPolygon(x, StealthArea.GetElapsedTimeInSeconds())));

            regionsCount--;
        }
    }

    private void RemoveInsignificantPolygons(ref List<VisibilityPolygon> polygons)
    {
        float minArea = 0.1f;
        int index = 0;

        while (index < polygons.Count)
        {
            float area = polygons[index].GetArea();

            if (area <= minArea)
            {
                polygons.RemoveAt(index);
                continue;
            }

            index++;
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

    private void SetOldestTimestamp(ref List<VisibilityPolygon> polygons)
    {
        OldestTimestamp = StealthArea.GetElapsedTimeInSeconds();

        foreach (var vp in polygons)
        {
            if (OldestTimestamp >= vp.GetTimestamp())
                OldestTimestamp = vp.GetTimestamp();
        }
    }


    // Alternative method to pass the staleness info
    void MigratePolygonStaleness(List<VisibilityPolygon> newMesh, List<VisibilityPolygon> oldMesh)
    {
        foreach (VisibilityPolygon newVp in newMesh)
        {
            float oldestTimestamp = Mathf.Infinity;

            float largestOverlapArea = Mathf.NegativeInfinity;
            float timestamp = StealthArea.GetElapsedTimeInSeconds();

            foreach (VisibilityPolygon oldVp in oldMesh)
            {
                // The overlap area
                VisibilityPolygon intersection = PolygonHelper.GetIntersectionArea(newVp, oldVp);

                // if the intersection exists
                if (intersection.GetVerticesCount() > 0)
                {
                    float overlapArea = intersection.GetArea();

                    if (oldestTimestamp > oldVp.GetTimestamp())
                        oldestTimestamp = oldVp.GetTimestamp();

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
            foreach (var poly in _tempSeenRegion)
                poly.Draw(poly.DetermineWindingOrder().ToString());
        }

        if (showUnseenRegions)
        {
            foreach (var poly in _tempUnseenRegion)
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