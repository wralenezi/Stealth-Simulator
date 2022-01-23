using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    // Map renderer
    public MapRenderer mapRenderer { get; private set; }

    // Convex decomposer of the space
    public MapDecomposer mapDecomposer { get; private set; }

    // Isovist map
    public Isovists isovists { get; private set; }

    // Scale Area transform ( to get the skeletal graph of the map) and load it into the road map.
    public SAT sat { get; private set; }

    // Create the Visibility graph and load into the road map.
    // public VisibilityGraph visibilityGraph { get; private set; }

    // Regions manager; to show information relevant to the map, like region names, etc.
    public RegionLabelsManager regionMgr { get; private set; }

    // Road map of the level.
    private RoadMap roadMap {  get; set; }

    // Mesh Manager
    public FloorTileManager meshManager { get; private set; }

    public static MapManager Instance;

    public void Initiate(MapData mapData)
    {
        Instance ??= this;

        // Draw the map
        mapRenderer = gameObject.AddComponent<MapRenderer>();
        mapRenderer.Initiate(mapData);

        // Create the NavMesh
        mapDecomposer = gameObject.AddComponent<MapDecomposer>();
        mapDecomposer.Initiate(mapRenderer.GetInteriorWalls());

        // Isovists map initiate
        isovists = gameObject.AddComponent<Isovists>();
        isovists.Initiate(GetNavMesh());

        // Scale Area Transform
        sat = gameObject.AddComponent<SAT>();
        sat.Initiate(mapRenderer, mapData);

        // Build the road map based on the Scale Area Transform
        roadMap = new RoadMap(sat, mapRenderer);
        
        // // Visibility graph
        // visibilityGraph = gameObject.AddComponent<VisibilityGraph>();
        // visibilityGraph.Initiate(mapRenderer);

        regionMgr = UnityHelper.AddChildComponent<RegionLabelsManager>(transform, "Regions");
        regionMgr.Initiate();

        // Mesh manager
        meshManager = UnityHelper.AddChildComponent<FloorTileManager>(transform, "MeshManager");
        meshManager.Initiate(GetNavMesh());
    }

    public List<MeshPolygon> GetNavMesh()
    {
        return mapDecomposer.GetNavMesh();
    }

    public List<Polygon> GetWalls()
    {
        return mapRenderer.GetInteriorWalls();
    }

    public RoadMap GetRoadMap()
    {
        return roadMap;
    }




}