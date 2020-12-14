using System;
using System.Collections.Generic;
using UnityEngine;

public class MapRenderer : MonoBehaviour
{
    // Reference to the line prefab (for rendering the map)
    private GameObject m_linePrefab;

    // Reference to the wall prefab (for the map collider)
    private GameObject m_wallPrefab;

    // the first polygon is the outer wall and the rest are the obstacles
    private List<Polygon> m_walls;

    // inner walls 
    private List<Polygon> m_interiorWalls;


    // Initiate the map renderer
    public void Initiate()
    {
        m_wallPrefab = (GameObject) Resources.Load("Prefabs/Wall");
        m_linePrefab = (GameObject) Resources.Load("Prefabs/Line");

        m_walls = new List<Polygon>();
        m_interiorWalls = new List<Polygon>();
    }


    // Get the path to the map
    private string GetPath(string mapName)
    {
        // Gets the path to the "Assets" folder 
        return Application.dataPath + "/MapData/" + mapName + ".csv";
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
                if (lineIndex != 0)
                    wall.EnsureWindingOrder(Properties.innerPolygonWinding);
                else
                    wall.EnsureWindingOrder(Properties.outerPolygonWinding);

                m_walls.Add(wall);
            }
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

                    // Debug.Log(point[0] + ", " + point[1]);

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
        foreach (var wall in m_walls)
        {
            var wallObject = Instantiate(m_wallPrefab, transform, true);

            // Set the "Wall" layer for the ray cast purpose
            wallObject.layer = LayerMask.NameToLayer("Wall");

            var wallCollider = wallObject.GetComponent<EdgeCollider2D>();

            // The vertices of the collider
            var colliderVertices = new Vector2[wall.GetVerticesCount() + 1];

            // Loop through the wall vertices and assign them. Add the first vertex again at the end to close the collider
            for (var i = 0; i < wall.GetVerticesCount() + 1; i++) colliderVertices[i] = wall.GetPoint(i);

            wallCollider.points = colliderVertices;

            // Draw a solid color in an obstacle
            if (wall.DetermineWindingOrder() != Properties.outerPolygonWinding)
                wallObject.GetComponent<Wall>().Draw();
        }
    }

    // Draw the lines visible for the player
    void RenderLines()
    {
        // Create the lines for rendering
        foreach (Polygon wall in m_walls)
        {
            // Draw the lines that makes up the wall
            for (int i = 0; i < wall.GetVerticesCount(); i++)
                AddLine(wall.GetPoint(i), wall.GetPoint(i + 1));
        }
    }

    // Add a line for rendering the map
    void AddLine(Vector2 start, Vector2 end)
    {
        GameObject line = Instantiate(m_linePrefab, transform, true);

        LineRenderer lineRenderer = line.GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }


    // Load the map
    public void LoadMap(string mapName, float mapScale)
    {
        // Get the map data
        var mapData = CsvController.ReadString(GetPath(mapName));

        var lines = mapName.Split('_');

        // Parse the map data
        if (lines.Length == 1)
            ParseMapStringAbsolute(mapData);
        else
            ParseMapStringRelative(mapData);


        // Scale the map
        ScaleMap(mapScale);

        // Create the collider for the wall
        CreateCollider();

        CreateIntWalls();

        // Draw the lines to render the map
        RenderLines();
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