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
    private List<List<Polygon>> _seenRegions;
    private List<List<Polygon>> _unseenRegions;

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
    List<Polygon> holePolys;
    List<List<Polygon>> _tempSeenRegions;
    List<Polygon> differenceResult;

    public static float OldestTimestamp;

    // Visibility mesh polygons
    private List<VisibilityPolygon> _visMeshPolygons;

    // The last timestamp recorded
    private float _lastTimestamp;

    public void Initiate(float maxSeenRegionAreaPerGuard)
    {
        _maxSeenRegionAreaPerGuard = maxSeenRegionAreaPerGuard;

        _guardsSeenRegions = new Dictionary<string, List<Polygon>>();

        _seenRegions = new List<List<Polygon>>();
        _unseenRegions = new List<List<Polygon>>();

        // Current Polygons 
        _curSeenPolygons = new List<VisibilityPolygon>();
        _curUnseenPolygons = new List<VisibilityPolygon>();

        // Previous Polygons
        _preSeenPolygons = new List<VisibilityPolygon>();
        _preUnseenPolygons = new List<VisibilityPolygon>();

        overallMesh = new List<VisibilityPolygon>();
        holePolys = new List<Polygon>();
        _tempSeenRegions = new List<List<Polygon>>();
        differenceResult = new List<Polygon>();

        _visMeshPolygons = new List<VisibilityPolygon>();

        // showSeenPolygons = true;
        showUnseenPolygons = true;
        // showSeenRegions = true;
    }

    // Reset the variables
    public void Reset()
    {
        _seenRegions.Clear();
        _unseenRegions.Clear();

        // Current Polygons 
        _curSeenPolygons.Clear();
        _curUnseenPolygons.Clear();

        // Previous Polygons
        _preSeenPolygons.Clear();
        _preUnseenPolygons.Clear();

        _visMeshPolygons.Clear();

        // Reset the time
        // SetTimestamp();
    }

    // Set the timestamp to the current time
    // private void SetTimestamp()
    // {
    //     _lastTimestamp = StealthArea.GetElapsedTimeInSeconds();
    // }

    // // Get the time delta 
    // private float GetTimeDelta()
    // {
    //     float timeDelta = StealthArea.GetElapsedTimeInSeconds() - _lastTimestamp;
    //     SetTimestamp();
    //     return timeDelta;
    // }

    private void CreateSeenRegions(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (!_guardsSeenRegions.ContainsKey(guard.name))
            {
                _guardsSeenRegions.Add(guard.name, new List<Polygon>());
                continue;
            }

            ResetSeenRegion(guard);

            List<Polygon> guardSeenRegion = _guardsSeenRegions[guard.name];
            PolygonHelper.MergePolygons(guardSeenRegion, guard.GetFov(), ref guardSeenRegion, ClipType.ctUnion);
        }
    }

    private void ResetSeenRegion(Guard guard)
    {
        float totalArea = 0f;
        List<Polygon> seenRegion = _guardsSeenRegions[guard.name];
        foreach (var poly in seenRegion)
            totalArea += poly.GetArea();

        float seenAreaPercent = totalArea / MapManager.Instance.mapDecomposer.GetNavMeshArea();

        if (seenAreaPercent > _maxSeenRegionAreaPerGuard)
        {
            List<Polygon> guardSeenRegion = _guardsSeenRegions[guard.name];
            guardSeenRegion.Clear();
        }
    }

    // Model and Aggregate the guards seen region
    private void ConsiderGuardVision(List<Guard> guards)
    {
        _seenRegions.Clear();

        // Go through the guards
        foreach (var guard in guards)
        {
            if (_guardsSeenRegions.ContainsKey(guard.name))
                _seenRegions.Add(_guardsSeenRegions[guard.name]);
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

            for (var i = 0; i < _seenRegions.Count; i++)
            {
                for (var j = i + 1; j < _seenRegions.Count; j++)
                {
                    List<Polygon> intersection = new List<Polygon>();
                    PolygonHelper.MergePolygons(_seenRegions[i], _seenRegions[j], ref intersection,
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
                List<Polygon> seenAreaUnion = new List<Polygon>();
                PolygonHelper.MergePolygons(_seenRegions[firstPoly], _seenRegions[secondPoly], ref seenAreaUnion,
                    ClipType.ctUnion);

                // intersect it with the walls to remove overlapping areas
                PolygonHelper.MergePolygons(MapManager.Instance.mapRenderer.GetInteriorWalls(),
                    seenAreaUnion, ref seenAreaUnion,
                    ClipType.ctIntersection);

                _seenRegions.Remove(_seenRegions[secondPoly]);
                _seenRegions.Remove(_seenRegions[firstPoly]);
                _seenRegions.Add(seenAreaUnion);
            }
        }
        
    }

    // Get the new partitioning and populate the VisMesh
    public void ConstructVisMesh(List<Guard> guards)
    {
        if (_curUnseenPolygons.Count > 0)
        {
            // Move the current VisMesh to the previous one
            MigrateVisMesh();

            // Increase the staleness of polygons
            // StalePolygons();
        }

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


    // Calculate how stale the polygons are based on time delta; the currently seen polygons do not stale
    // private void StalePolygons()
    // {
    //     // Get the staleness value since the last update
    //     float stalenessDelta = GetTimeDelta() * Properties.StalenessRate;
    //
    //     foreach (VisibilityPolygon vp in _preUnseenPolygons)
    //         vp.IncreaseStaleness(stalenessDelta);
    // }


    // Create the VisMesh
    private void CreateVisMesh(List<Guard> guards)
    {
        CreateSeenRegions(guards);

        // Prepare the guards vision to be considered in the space decomposition
        ConsiderGuardVision(guards);

        // Modify the regions to facilitate triangulation
        PrepareRegions();

        RegularizePolygons();

        // Decompose the unseen area
        DecomposeUnseenArea();

        // Decompose the seen area
        DecomposeSeenArea();
    }

    // Prepare the unseen polygons of the NavMesh ( important to be done before the seen area since it modifies the seen area)
    private void PrepareRegions()
    {

        // Sort the outer walls and inner walls
        OrganizeUnseenPolygons();
        // OrganizeUnseenRegions();

        OrganizeSeenRegions();
    }

    // Sort the CounterClockWise polygons as Outer polygons and find the holes in them
    void OrganizeUnseenPolygons()
    {
        _unseenRegions.Clear();

        // the difference between the walkable area and seen area is the unseen area
        differenceResult.Clear();
        differenceResult.AddRange(MapManager.Instance.mapRenderer.GetInteriorWalls());

        // Take the difference for each seen region; The polygons in the result are Outer polygons (they have CounterClockWise winding) and holes (opposite winding)
        foreach (List<Polygon> guardSeenArea in _seenRegions)
            PolygonHelper.MergePolygons(guardSeenArea, differenceResult, ref differenceResult,
                ClipType.ctDifference);

        // To triangulate the area without guards
        if (_seenRegions.Count == 0)
        {
            _unseenRegions.Add(MapManager.Instance.mapRenderer.GetInteriorWalls());
            return;
        }
        
        holePolys.Clear();

        foreach (Polygon p in differenceResult)
        {
            if (p.DetermineWindingOrder() == Properties.outerPolygonWinding)
            {
                List<Polygon> _tempWall = new List<Polygon>();
                _tempWall.Clear();
                _tempWall.Add(p);
                _unseenRegions.Add(_tempWall);
            }
            else
            {
                holePolys.Add(p);
            }
        }

        differenceResult.Clear();

        // Add the hole to the outer wall the contains it
        foreach (List<Polygon> wall in _unseenRegions)
        {
            foreach (Polygon hole in holePolys)
                if (wall[0].IsPolygonInside(hole, false))
                {
                    wall.Add(hole);
                }
        }
    }
    
    private void OrganizeUnseenRegions()
    {
        List<Polygon> temp = new List<Polygon>();

        foreach (var region in _seenRegions)
        foreach (var p in region)
        {
            temp.Add(p);
        }
        
        _unseenRegions.Clear();
        List<Polygon> regionTemp = new List<Polygon>();
        _unseenRegions.Add(regionTemp);
        
        List<Polygon> walls = MapManager.Instance.mapRenderer.GetInteriorWalls();

        PolygonHelper.MergePolygons(temp,
            walls, ref regionTemp, ClipType.ctDifference);

        
        // for (int i = 0; i < _unseenRegions.Count; i++)
        // {
        //     List<Polygon> seenRegion = _unseenRegions[i];
        //     List<Polygon> walls = MapManager.Instance.mapRenderer.GetInteriorWalls();
        //     
        //     PolygonHelper.MergePolygons(seenRegion,
        //         walls, ref seenRegion, ClipType.ctIntersection);
        // }
    }


    private void OrganizeSeenRegions()
    {
        for (int i = 0; i < _seenRegions.Count; i++)
        {
            List<Polygon> seenRegion = _seenRegions[i];
            List<Polygon> walls = MapManager.Instance.mapRenderer.GetInteriorWalls();
            
            PolygonHelper.MergePolygons(seenRegion,
                walls, ref seenRegion, ClipType.ctIntersection);
        }
    }


    // void OrganizeSeenRegions()
    // {
    //     holePolys.Clear();
    //     _tempSeenRegions.Clear();
    //
    //     foreach (var seenRegion in _seenRegions)
    //     foreach (Polygon p in seenRegion)
    //     {
    //         if (p.DetermineWindingOrder() == Properties.outerPolygonWinding)
    //         {
    //             List<Polygon> wall = new List<Polygon>();
    //             wall.Add(p);
    //             _tempSeenRegions.Add(wall);
    //         }
    //         else
    //         {
    //             holePolys.Add(p);
    //         }
    //     }
    //
    //
    //     // Add the hole to the outer wall the contains it
    //     foreach (List<Polygon> wall in _tempSeenRegions)
    //     {
    //         foreach (Polygon hole in holePolys)
    //             if (wall[0].IsPolygonInside(hole, false))
    //             {
    //                 wall.Add(hole);
    //             }
    //     }
    //
    //     // Intersect with the borders so it will intersect it
    //     int i = 0;
    //     while (i < _tempSeenRegions.Count)
    //     {
    //         List<Polygon> region = _tempSeenRegions[i];
    //         PolygonHelper.MergePolygons(_tempSeenRegions[i],
    //             MapManager.Instance.mapRenderer.GetInteriorWalls(), ref region, ClipType.ctIntersection);
    //         i++;
    //     }
    //
    //     Debug.Log(_seenRegions.Count);
    //
    //     _seenRegions = _tempSeenRegions;
    //     
    //     Debug.Log(_seenRegions.Count);
    //
    // }


    private void RegularizePolygons()
    {
        for (int i = 0; i < _unseenRegions.Count; i++)
        {
            // Remove the empty lists
            if (_unseenRegions[i].Count == 0)
            {
                _unseenRegions.RemoveAt(i);
                i = 0;
                continue;
            }

            // Remove any non-polygon objects.
            for (int j = 0; j < _unseenRegions[i].Count; j++)
                if (_unseenRegions[i][j].GetVerticesCount() < 3)
                {
                    _unseenRegions[i].RemoveAt(j);
                    j = 0;
                    i = 0;
                }
        }


        for (int i = 0; i < _seenRegions.Count; i++)
        {
            // Remove the empty lists
            if (_seenRegions[i].Count == 0)
            {
                _seenRegions.RemoveAt(i);
                i = 0;
                continue;
            }

            // Remove any non-polygon objects.
            for (int j = 0; j < _seenRegions[i].Count; j++)
                if (_seenRegions[i][j].GetVerticesCount() < 3)
                {
                    _seenRegions[i].RemoveAt(j);
                    j = 0;
                    i = 0;
                }
        }
    }

    // Triangulate the unseen areas
    void DecomposeUnseenArea()
    {
        _curUnseenPolygons.Clear();

        foreach (List<Polygon> pL in _unseenRegions)
        {
            Polygon polygon = PolygonHelper.CutHoles(pL);
            List<MeshPolygon> tempPolys = HertelMelDecomp.ConvexPartition(polygon);

            _curUnseenPolygons.AddRange(tempPolys.ConvertAll(x =>
                new VisibilityPolygon(x, StealthArea.GetElapsedTimeInSeconds())));
        }
    }

    // Triangulate the seen area
    void DecomposeSeenArea()
    {
        _curSeenPolygons.Clear();

        foreach (List<Polygon> guardSeenArea in _seenRegions)
        {
            Polygon polygon = PolygonHelper.CutHoles(guardSeenArea);

            if (polygon != null)
            {
                List<MeshPolygon> tempPolys = HertelMelDecomp.ConvexPartition(polygon);
                _curSeenPolygons.AddRange(tempPolys.ConvertAll(x =>
                    new VisibilityPolygon(x, StealthArea.GetElapsedTimeInSeconds())));
            }
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
            foreach (var region in _seenRegions)
            foreach (var poly in region)
                poly.Draw(poly.DetermineWindingOrder().ToString());
        }

        if (showUnseenRegions)
        {
            foreach (var region in _unseenRegions)
            foreach (var poly in region)
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

        // DrawHidingSpots();

        // if (showVisMesh)
        // {
        //     foreach (var poly in m_VisMeshPolygons)
        //         poly.Draw(poly.GetStaleness().ToString());
        // }
    }
}