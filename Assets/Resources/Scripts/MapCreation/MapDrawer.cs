using System.Collections;
using System.Collections.Generic;
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

        SaveBtn.onClick.AddListener(SaveMap);

        // Define the paths for the game
        // Main path
        DataPath = Application.dataPath + "/Data/";
        // Map related data paths
        MapsDataPath = DataPath + MapsDataPath;
        MapsPath = MapsDataPath + MapsPath;
    }

    string GetPath(string mapName)
    {
        // Gets the path to the "Assets" folder 
        return MapsPath + mapName + ".csv";
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

        GameObject currentWall = Instantiate(m_wallPrefab, transform, true);
        currentWall.layer = LayerMask.NameToLayer("Wall");

        m_WallsGameObjects.Add(currentWall);

        m_Lines.Add(currentWall.GetComponent<LineRenderer>());
        m_Lines[m_Lines.Count - 1].positionCount = 0;
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

        CsvController.WriteString(GetPath(MapName.text), mapData, false);
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            AddMousePosition();
    }
}