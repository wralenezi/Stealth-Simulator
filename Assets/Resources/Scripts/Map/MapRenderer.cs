using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class MapRenderer : MonoBehaviour
{
    private List<GameObject> m_wallOGs;

    // List of the walls in the map
    public bool showWalls;
    // the first polygon is the outer wall and the rest are the obstacles.
    private List<Polygon> m_walls;
    
    private float m_MapBoundingBoxMaxWith;

    public bool showInteriorWalls;
    // inner walls which are on an offset from the actual wall.
    private List<Polygon> m_interiorWalls;


    // Initiate the map renderer
    // public void Initiate(MapData mapData)
    public void Initiate()
    {
        m_wallOGs = new List<GameObject>();

        // Initiate the variables
        m_walls = new List<Polygon>();
        m_interiorWalls = new List<Polygon>();

        LoadMap();
    }

    // Parse the map data where the map is stored in absolute coordinates 
    private void ParseMapStringAbsolute(string mapData)
    {
        // Split data by lines
        var lines = mapData.Split('\n');

        // Each line represents a polygon
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].Length > 0)
            {
                // Wall 
                var wall = new Polygon();

                // Split the line to coordinates
                var data = lines[lineIndex].Split(',');

                // Add the vertices to the wall
                for (var i = 0; i < data.Length; i += 2)
                {
                    // Vertex position
                    var position = new Vector2(float.Parse(data[i]), float.Parse(data[i + 1]));
                    position = transform.TransformPoint(position);

                    // Add the point to the current wall
                    wall.AddPoint(position);
                }

                // if the wall is not the first then it is a hole
                wall.EnsureWindingOrder(
                    lineIndex != 0 ? Properties.innerPolygonWinding : Properties.outerPolygonWinding);

                m_walls.Add(wall);
            }
    }

    private void RenderMap(MapData mapData)
    {
        
        // Each line represents a polygon
        for (int lineIndex = 0; lineIndex < mapData.walls.Count; lineIndex++)
            if (mapData.walls[lineIndex].points.Count > 0)
            {
                List<MapPoint> wallPoints = mapData.walls[lineIndex].points;
                
                // Wall 
                var wall = new Polygon();

                // Add the vertices to the wall
                for (var i = 0; i < wallPoints.Count; i++)
                {
                    // Vertex position
                    var position = new Vector2(wallPoints[i].x, wallPoints[i].y);
                    position = transform.TransformPoint(position);

                    // Add the point to the current wall
                    wall.AddPoint(position);
                }

                // if the wall is not the first then it is a hole
                wall.EnsureWindingOrder(
                    lineIndex != 0 ? Properties.innerPolygonWinding : Properties.outerPolygonWinding);

                m_walls.Add(wall);
            }
    }

    public float GetMaxWidthBoundingBox()
    {
        return m_MapBoundingBoxMaxWith;
    }


    // Parse the map which is inspired by SVG syntax
    private void ParseMapStringRelative(string mapData)
    {
        // Split data by lines
        var lines = mapData.Split('\n');

        // Each line represents a polygon
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].Length > 0)
            {
                // Wall 
                var wall = new Polygon();

                Vector2 pointer = Vector2.zero;

                // Split the line to points
                var data = lines[lineIndex].Split(' ');

                // Add the vertices to the wall
                for (var i = 0; i < data.Length; i++)
                {
                    // split the point to coordinates
                    var point = data[i].Split(',');

                    // Vertex position
                    var position = new Vector2(float.Parse(point[0]), -float.Parse(point[1]));

                    // First point is the starting point
                    if (i == 0)
                        pointer = position;
                    else
                    {
                        position = pointer + position;
                        pointer = position;
                    }

                    position = transform.TransformPoint(position);

                    // Add the point to the current wall
                    wall.AddPoint(position);
                }

                // if the wall is not the first then it is a hole
                if (lineIndex != 0)
                    wall.EnsureWindingOrder(Properties.innerPolygonWinding);
                else
                    wall.EnsureWindingOrder(Properties.outerPolygonWinding);

                m_walls.Add(wall);
            }
    }

    // Scale the map
    private void ScaleMap(float mapScale)
    {
        var centerPoint = new Vector2(0f, 0f);

        foreach (var wall in m_walls)
        foreach (var v in wall.GetPoints())
        {
            var vector = v.position - centerPoint;

            v.position = mapScale * vector;
        }
    }

    // Create Collider for the walls
    private void CreateCollider()
    {
        int wallId = 0;
        GameObject wallsOGContainer = new GameObject("Walls");
        wallsOGContainer.transform.parent = transform;
        foreach (var wall in m_walls)
        {
            GameObject wallObject = new GameObject(wallId.ToString().PadLeft(2, '0'));
            wallObject.transform.parent = wallsOGContainer.transform;

            // Set the "Wall" layer for the ray cast purpose
            wallObject.layer = LayerMask.NameToLayer("Wall");

            var wallCollider = wallObject.AddComponent<EdgeCollider2D>();

            // The vertices of the collider
            var colliderVertices = new Vector2[wall.GetVerticesCount() + 1];

            // Loop through the wall vertices and assign them. Add the first vertex again at the end to close the collider
            for (var i = 0; i < wall.GetVerticesCount() + 1; i++) colliderVertices[i] = wall.GetPoint(i);

            wallCollider.points = colliderVertices;

            Wall wallComponent = wallObject.AddComponent<Wall>();
            wallComponent.Initiate(wallId++, 0.2f);

            // Draw a solid color in an obstacle; there is no need as long as the walkable area is colored.
            // if (wall.DetermineWindingOrder() != Properties.outerPolygonWinding) wallObject.GetComponent<Wall>().Draw();

            m_wallOGs.Add(wallObject);
        }
    }

    // Load the map
    // public void LoadMap(MapData map)
    public void LoadMap()
    {
        string mapData = GameManager.Instance.currentMapData;
        float mapSize = StealthArea.SessionInfo.GetMap().size;
        GameManager.Instance.currentMap = JsonConvert.DeserializeObject<MapData>(mapData);
        MapData map = GameManager.Instance.currentMap;
        
        RenderMap(map);
        
        // Parse the map data
        // if the file name has the world "_relative" then it is an SVG inspired coordinate system
        // if (!map.name.Contains("_relative"))
        //     ParseMapStringAbsolute(mapData);
        // else
        //     ParseMapStringRelative(mapData);

        // Scale the map
        mapSize = Equals(mapSize, 0f) ? map.size: mapSize; 
        ScaleMap(mapSize);
        
        GameManager.Instance.currentRoadMapData = CsvController.ReadString(GetRoadMapPath(map.name, map.size));
        
        // Create the collider for the wall
        CreateCollider();

        CreateIntWalls();

        SetMapMaxWidth();
    }

    // Get the path to the map
    private static string GetRoadMapPath(string mapName, float mapScale)
    {
        // Gets the path to the "Assets" folder 
        return GameManager.RoadMapsPath + mapName + "_" + mapScale + ".csv";
    }

    // Set the maximum with of the bounding box of the map
    public void SetMapMaxWidth()
    {
        Bounds bounds = GetMapBoundingBox();

        float maxWidth = Mathf.Abs(bounds.max.x - bounds.min.x) > Mathf.Abs(bounds.max.y - bounds.min.y)
            ? Mathf.Abs(bounds.max.x - bounds.min.x)
            : Mathf.Abs(bounds.max.y - bounds.min.y);

        m_MapBoundingBoxMaxWith = maxWidth;
    }

    public Bounds GetMapBoundingBox()
    {
        return m_walls[0].BoundingBox();
    }

    public float GetMaxWidth()
    {
        return m_MapBoundingBoxMaxWith;
    }


    // Modify the walls size, the holes to be larger and the outer wall to be smaller. This is hack to prevent triangulation from crashing when there are touching polygons
    public void CreateIntWalls()
    {
        foreach (Polygon p in m_walls)
        {
            Polygon interiorPoly = new Polygon(p);
            interiorPoly.Enlarge(Properties.InterPolygonOffset);
            m_interiorWalls.Add(interiorPoly);
        }
    }

    // Visibility check for two points, a and b, on the map
    public bool VisibilityCheck(Vector2 a, Vector2 b)
    {
        foreach (var wall in m_walls)
            for (int i = 0; i < wall.GetVerticesCount(); i++)
            {
                if (GeometryHelper.DoLinesIntersect(a, b, wall.GetPoint(i), wall.GetPoint(i + 1), false))
                    return false;
            }

        return true;
    }

    public void OnDrawGizmos()
    {
        
        if (showWalls)
        {
            foreach (var poly in GetWalls())
                poly.Draw("");
        }
        
        if (showInteriorWalls)
        {
            foreach (var poly in GetInteriorWalls())
                poly.Draw("");
        }
    }


    public List<Polygon> GetInteriorWalls()
    {
        return m_interiorWalls;
    }


    // Get the walls
    public List<Polygon> GetWalls()
    {
        return m_walls;
    }
}


/// <summary>
/// Contains the map details, like name, size
/// </summary>
[Serializable]
public class MapData
{
    public string name;
    public float size;

    public List<WallData> walls;

    public MapData(string _name, float _size = 0f)
    {
        name = _name;
        size = _size;
        
        walls = new List<WallData>();
    }
    
    
}

[Serializable]
public class WallData
{
    public List<MapPoint> points;

    public WallData()
    {
        points = new List<MapPoint>();
    }
}

[Serializable]
public struct MapPoint
{
    public float x;
    public float y;

    public MapPoint(float _x, float _y)
    {
        x = _x;
        y = _y;
    }
}