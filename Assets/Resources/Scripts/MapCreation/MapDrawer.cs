using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MapDrawer : MonoBehaviour
{
    // Grid to snap the polygon points to
    private GridGuide m_grid;

    // Reference to the walls being drawn
    private List<GameObject> m_WallsGameObjects;

    private List<LineRenderer> m_Lines;

    // Reference to the wall prefab
    private GameObject m_wallPrefab;

    // To keep track of the positions, every list is one polygon and the polygon consists of points
    private List<List<Vector2>> m_walls;

    public InputField MapName;

    // Button to start a new wall
    public Button NewWallBtn;

    public Button RemoveLastPointBtn;

    public Button RemoveLastWallBtn;

    public Button SaveBtn;

    public Button LoadBtn;

    // Location of the data for the game
    public static string DataPath;
    public static string MapsDataPath = "MapsData/";
    public static string MapsPath = "Maps/";

    void Start()
    {
        m_walls = new List<List<Vector2>>();
        m_WallsGameObjects = new List<GameObject>();
        m_Lines = new List<LineRenderer>();

        m_wallPrefab = (GameObject) Resources.Load("Prefabs/Line");

        // Check if there is the drawing grid attached, if so link it and reset the grid.
        TryGetComponent(out m_grid);

        StartNewWall();

        NewWallBtn.onClick.AddListener(StartNewWall);

        RemoveLastPointBtn.onClick.AddListener(RemoveLastPoint);

        RemoveLastWallBtn.onClick.AddListener(RemoveLastWall);

        // SaveBtn.onClick.AddListener(SaveMap);
        SaveBtn.onClick.AddListener(SaveCurrentLineRenders);

        LoadBtn.onClick.AddListener(LoadMap);


        // Define the paths for the game
        // Main path
        DataPath = Application.dataPath + "/../Data/";
        // Map related data paths
        MapsDataPath = DataPath + MapsDataPath;
        MapsPath = MapsDataPath + MapsPath;
    }

    string GetPath(string mapName, string fileType)
    {
        // Gets the path to the "Assets" folder 
        return MapsPath + mapName + "."+ fileType;
    }

    private void SaveCurrentLineRenders()
    {
        m_walls.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform childT = transform.GetChild(i);
            
            if (Equals(childT.gameObject.name, "UI")) continue;

            childT.gameObject.TryGetComponent(out LineRenderer lineRenderer);

            if (Equals(lineRenderer, null)) continue;

            List<Vector2> wall = new List<Vector2>();

            for (int j = 0; j < lineRenderer.positionCount; j++)
            {
                wall.Add(lineRenderer.transform.TransformPoint(lineRenderer.GetPosition(j)));
            }

            m_walls.Add(wall);
        }

        SaveMap();

        SaveMapInJson();
    }

    private void LoadMap()
    {
        if (Equals(MapName.text, "")) return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform childTransform = transform.GetChild(i);

            if (Equals(childTransform.gameObject.name, "UI")) continue;
            Destroy(childTransform.gameObject);
        }

        // string mapData = CsvController.ReadString(GetPath(MapName.text,"csv"));
        // ParseCSVMapString(mapData);

        string mapData = CsvController.ReadString(GetPath(MapName.text, "json"));
        RenderMap(JsonConvert.DeserializeObject<MapData>(mapData));
    }

    private void RenderMap(MapData mapData)
    {
        
        // Each line represents a polygon
        for (int lineIndex = 0; lineIndex < mapData.walls.Count; lineIndex++)
            if (mapData.walls[lineIndex].points.Count > 0)
            {
                List<MapPoint> wallPoints = mapData.walls[lineIndex].points;
                
                // Wall 
                GameObject gameObject = new GameObject();
                gameObject.transform.parent = transform;
                LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.loop = true;

                lineRenderer.positionCount = wallPoints.Count;
                int index = 0;

                
                // Add the vertices to the wall
                for (var i = 0; i < wallPoints.Count; i++)
                {
                    // Vertex position
                    var position = new Vector2(wallPoints[i].x, wallPoints[i].y);
                    position = transform.TransformPoint(position);


                    // Add the point to the current wall
                    lineRenderer.SetPosition(index++, position);
                }
            }
    }

    // Parse the map data where the map is stored in absolute coordinates 
    private void ParseCSVMapString(string mapData)
    {
        // Split data by lines
        var lines = mapData.Split('\n');

        // Each line represents a polygon
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].Length > 0)
            {
                GameObject gameObject = new GameObject();
                gameObject.transform.parent = transform;

                LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
                // lineRenderer.useWorldSpace = true;
                lineRenderer.loop = true;

                // Split the line to coordinates
                var data = lines[lineIndex].Split(',');

                lineRenderer.positionCount = Mathf.CeilToInt(data.Length * 0.5f);
                int index = 0;

                // Add the vertices to the wall
                for (var i = 0; i < data.Length; i += 2)
                {
                    // Vertex position
                    var position = new Vector2(float.Parse(data[i]), float.Parse(data[i + 1]));
                    position = transform.TransformPoint(position);

                    // Add the point to the current wall
                    lineRenderer.SetPosition(index++, position);
                }
            }
    }

    void StartNewWall()
    {
        // Complete the previous wall if present
        if (m_walls.Count > 0 && m_walls[m_walls.Count - 1].Count > 1)
        {
            // Add the first vertex to line renderer
            m_Lines[m_Lines.Count - 1].positionCount++;
            m_Lines[m_Lines.Count - 1].SetPosition(m_Lines[m_Lines.Count - 1].positionCount - 1,
                m_walls[m_walls.Count - 1][0]);
        }


        m_walls.Add(new List<Vector2>());

        // GameObject currentWall = Instantiate(m_wallPrefab, transform, true);
        GameObject currentWall = GetLineGameObject();
        currentWall.transform.parent = transform;
        currentWall.layer = LayerMask.NameToLayer("Wall");

        m_WallsGameObjects.Add(currentWall);

        m_Lines.Add(currentWall.GetComponent<LineRenderer>());
        m_Lines[m_Lines.Count - 1].positionCount = 0;
    }


    private GameObject GetLineGameObject()
    {
        GameObject gameObject = new GameObject();

        gameObject.AddComponent<LineRenderer>();

        return gameObject;
    }

    // Add point on mouse position
    void AddMousePosition()
    {
        // Add a new node to the polygon, if there is no grid attached then check where the exactly the mouse is clicked.
        if (m_grid != null)
        {
            Vector2 clickedNode = m_grid.GetGridPosFromWorldPos(Camera.main.ScreenToWorldPoint(Input.mousePosition));

            bool spotEmpty = true;

            foreach (var wall in m_walls)
            foreach (var v in wall)
            {
                if (clickedNode == v)
                {
                    spotEmpty = false;
                    break;
                }
            }

            if (spotEmpty)
                AddPoint(clickedNode);
        }
        else
        {
            AddPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    // Add a point to the line
    void AddPoint(Vector2 point)
    {
        m_walls[m_walls.Count - 1].Add(point);

        // Add the point to line renderer
        m_Lines[m_Lines.Count - 1].positionCount++;
        m_Lines[m_Lines.Count - 1].SetPosition(m_Lines[m_Lines.Count - 1].positionCount - 1, point);
    }

    // Remove last point in the current wall
    void RemoveLastPoint()
    {
        if (m_walls.Count > 0)
        {
            if (m_walls[m_walls.Count - 1].Count > 0)
            {
                m_walls[m_walls.Count - 1].RemoveAt(m_walls[m_walls.Count - 1].Count - 1);

                // Remove the last point
                m_Lines[m_Lines.Count - 1].positionCount--;
            }
        }
    }

    void RemoveLastWall()
    {
        if (m_walls.Count > 0)
        {
            m_walls.RemoveAt(m_walls.Count - 1);

            Destroy(m_WallsGameObjects[m_WallsGameObjects.Count - 1]);
            m_WallsGameObjects.RemoveAt(m_WallsGameObjects.Count - 1);

            Destroy(m_Lines[m_Lines.Count - 1]);
            m_Lines.RemoveAt(m_Lines.Count - 1);

            if (m_walls.Count == 0)
                StartNewWall();
        }
    }

    // Save the map on a csv file
    void SaveMap()
    {
        string mapData = "";

        foreach (List<Vector2> wall in m_walls)
        {
            if (wall.Count > 0)
            {
                for (int i = 0; i < wall.Count; i++)
                    mapData += wall[i].x + "," + wall[i].y + ",";

                mapData = mapData.TrimEnd(',');
                mapData += "\n";
            }
        }

        // Remove the last line return
        mapData = mapData.TrimEnd('\n');

        CsvController.WriteString(GetPath(MapName.text,"csv"), mapData, false);
    }


    private void SaveMapInJson()
    {
        MapData mapData = new MapData(MapName.text,1f);
        
        mapData.walls = new List<WallData>();
        
        foreach (List<Vector2> wall in m_walls)
        {
            WallData wallData = new WallData();
            
            if (wall.Count > 0)
            {
                for (int i = 0; i < wall.Count; i++)
                    wallData.points.Add(new MapPoint(wall[i].x,wall[i].y));
            }
            
            mapData.walls.Add(wallData);
        }

        string mapDataString = JsonUtility.ToJson(mapData);
        CsvController.WriteString(GetPath(MapName.text,"json"), mapDataString, false);

    }



    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            AddMousePosition();
    }
}