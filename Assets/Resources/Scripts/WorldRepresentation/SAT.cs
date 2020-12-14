using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Create the Scale axis transform of the map
public class SAT : MonoBehaviour
{
    // The grid of map
    // Map Renderer
    MapRenderer m_mapRenderer;

    // 2D List of the grid way points
    private DtNode[,] m_grid;

    // Values for the grid measures
    protected float nodeDiameter;
    protected Vector2 worldBottomLeft;

    // Grid dimension 
    int gridSizeX, gridSizeY;

    // Mesh Manager
    MeshManager m_meshManager;

    // RoadMap of the World
    private List<WayPoint> m_roadMap;

    // Establish the road map
    public void Initiate(float mapScale, string mapName)
    {
        m_mapRenderer = GetComponent<MapRenderer>();
        m_meshManager = transform.parent.Find("MeshManager").GetComponent<MeshManager>();

        // Import the hand drawn road map
        ImportRoadMap(mapName);

        // CreateSkeletal(mapScale);


        // Divide long edges to smaller edges
        DivideRoadMap();

        // Render the grid
        // m_meshManager.DrawGrid(m_grid);
    }

    // This is to create the skeleton of the map using this reference
    // https://www.sciencedirect.com/science/article/abs/pii/104996529290026T
    private void CreateSkeletal(float mapScale)
    {
        // Create the grid
        CreateGrid(mapScale);

        // Calculate the distance transform
        CalculateDistanceTransform();

        // Create local maximas
        SetLocalMaximas();

        // Create saddle points
        SetSaddlePoints();
    }


    private void ImportRoadMap(string map)
    {
        m_roadMap = new List<WayPoint>();

        var mapData = CsvController.ReadString(GetPath(map));

        ParseMapString(mapData);
    }

    // Get the path to the map
    private string GetPath(string mapName)
    {
        // Gets the path to the "Assets" folder 
        return Application.dataPath + "/RoadMapData/" + mapName + ".csv";
    }

    // Parse the map data
    // The Roadmap data consists of the coordinates of the nodes; each line has a node. After that they are terminated by this line "-,-"
    // after that the edges between the nodes are listed as follows i,j; where i,j are indices of the nodes.
    private void ParseMapString(string mapData)
    {
        // Split data by lines
        var lines = mapData.Split('\n');

        // a flag to check if the nodes are loaded. This is to change the load to the edges.
        bool isNodesLoaded = false;

        // Each line is a vertex or a connection
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].Length > 0)
            {
                // Split the line to coordinates
                var data = lines[lineIndex].Split(',');

                // Check if all nodes are loaded
                if (data[0] == "-")
                {
                    isNodesLoaded = true;
                    continue;
                }

                if (!isNodesLoaded)
                {
                    // Vertex position
                    var position = new Vector2(float.Parse(data[0]), float.Parse(data[1]));
                    position = transform.TransformPoint(position);

                    m_roadMap.Add(new WayPoint(position));
                }
                else
                {
                    int firstIndex = int.Parse(data[0]) - 1;
                    int secondIndex = int.Parse(data[1]) - 1;

                    // Add two way edge
                    m_roadMap[firstIndex].AddEdge(m_roadMap[secondIndex]);
                    m_roadMap[secondIndex].AddEdge(m_roadMap[firstIndex]);
                }
            }
    }


    // Divide the long edges into smaller edges
    private void DivideRoadMap()
    {
        List<WayPoint> newWayPoints = new List<WayPoint>();

        foreach (var wp in m_roadMap)
        {
            for (int i = 0; i < wp.GetConnections().Count; i++)
            {
                WayPoint connection = wp.GetConnections()[i];

                // Divide the edge if it is longer than the max length
                float totalDistance = Vector2.Distance(wp.GetPosition(), connection.GetPosition());
                if (totalDistance > Properties.MaxEdgeLength)
                {
                    // Remove the connections
                    wp.RemoveEdge(connection);
                    connection.RemoveEdge(wp);

                    // Define the direction of placing the intermediate way points
                    Vector2 dir = (connection.GetPosition() - wp.GetPosition()).normalized;

                    // Define number of intermediate edges
                    int edgesCount = Mathf.CeilToInt(totalDistance / Properties.MaxEdgeLength);

                    WayPoint prevWayPoint = null;
                    // Place way points and the last way point will connect to original connection
                    for (int j = 1; j < edgesCount; j++)
                    {
                        // Place the way point
                        Vector2 wayPointPos = wp.GetPosition() + dir * (j * Properties.MaxEdgeLength);
                        WayPoint wayPoint = new WayPoint(wayPointPos);

                        // Connect the first way point to the source way point and the rest with the previous
                        if (j == 1)
                        {
                            wayPoint.AddEdge(wp);
                            wp.AddEdge(wayPoint);
                            prevWayPoint = wayPoint;
                        }
                        else
                        {
                            wayPoint.AddEdge(prevWayPoint);
                            prevWayPoint.AddEdge(wayPoint);

                            prevWayPoint = wayPoint;
                        }

                        // Add to a separate list
                        newWayPoints.Add(wayPoint);
                    }

                    // Make the final connection
                    newWayPoints[newWayPoints.Count - 1].AddEdge(connection);
                    connection.AddEdge(newWayPoints[newWayPoints.Count - 1]);
                    
                }
            }
        }
        
        m_roadMap.AddRange(newWayPoints);
    }


// Create the grid
    public void CreateGrid(float mapScale)
    {
        // Set the diameter of a node
        nodeDiameter = Properties.NodeRadius * 2f;

        // Determine the resolution of the grid
        gridSizeX = Mathf.RoundToInt(mapScale * Properties.GridDefaultSizeX / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(mapScale * Properties.GridDefaultSizeY / nodeDiameter);

        // Set the grid size
        m_grid = new DtNode[gridSizeX, gridSizeY];

        // Set the left bottom corner position in the world
        worldBottomLeft = (Vector2) (transform.position) - Vector2.right * Properties.GridDefaultSizeX / 2 -
                          Vector2.up * Properties.GridDefaultSizeY / 2;

        worldBottomLeft *= mapScale;

        // Establish the walkable areas in the grid
        for (int x = 0; x < gridSizeX; x++)
        for (int y = 0; y < gridSizeY; y++)
        {
            // Get the node's world position
            Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + Properties.NodeRadius) +
                                 Vector2.up * (y * nodeDiameter + Properties.NodeRadius);

            bool walkable = false;
            float distance = -1f;

            if (IsNodeInMap(worldPoint))
            {
                walkable = true;
                distance = Properties.StalenessLow;
            }

            m_grid[x, y] = new DtNode(walkable, x, y, worldPoint, distance);
        }
    }


    // Calculate the distance transform using the euclidean distance
    private void CalculateDistanceTransform()
    {
        // Max and min values for normalization
        float min = Mathf.Infinity;
        float max = Mathf.NegativeInfinity;

        for (int xi = 0; xi < gridSizeX; xi++)
        for (int yi = 0; yi < gridSizeY; yi++)
        {
            if (!m_grid[xi, yi].walkable)
                continue;

            float minDistance = Mathf.Infinity;

            for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
            {
                if (m_grid[x, y].walkable)
                    continue;

                // Euclidean distance
                float distance = Vector2.Distance(m_grid[xi, yi].worldPosition, m_grid[x, y].worldPosition);

                if (distance < minDistance)
                    minDistance = distance;
            }

            // Update the ranges
            if (min > minDistance)
                min = minDistance;
            if (max < minDistance)
                max = minDistance;

            m_grid[xi, yi].distanceTransform = minDistance;
        }

        // Normalize the values
        for (int xi = 0; xi < gridSizeX; xi++)
        for (int yi = 0; yi < gridSizeY; yi++)
        {
            float normalized = (m_grid[xi, yi].distanceTransform - min) / (max - min);

            normalized *= Properties.StalenessHigh;

            m_grid[xi, yi].distanceTransform = normalized;
        }
    }

    private bool IsNodeInMap(Vector2 node)
    {
        return PolygonHelper.IsPointInPolygons(m_mapRenderer.GetInteriorWalls(), node);
    }

    // Set the local maximas
    private void SetLocalMaximas()
    {
        for (int x = 0; x < gridSizeX; x++)
        for (int y = 0; y < gridSizeY; y++)
        {
            // Skip if the node is un-walkable
            if (!m_grid[x, y].walkable)
                continue;


            int[] xs = {-1, 0, 1};
            int[] ys = {-1, 0, 1};
            bool isLocalMaxima = true;

            // Check the surrounding pixels
            foreach (var xi in xs)
            foreach (var yi in ys)
            {
                if (xi == yi && xi == 0)
                    continue;

                // Check if the pixel is not a local maxima
                if (m_grid[x, y].distanceTransform < m_grid[x + xi, y + yi].distanceTransform)
                    isLocalMaxima = false;
            }

            if (isLocalMaxima)
                m_grid[x, y].isMaxima = true;
        }
    }

    // Set the Saddle points
    private void SetSaddlePoints()
    {
        for (int x = 0; x < gridSizeX; x++)
        for (int y = 0; y < gridSizeY; y++)
        {
            if (m_grid[x, y].isMaxima || !m_grid[x, y].walkable)
                continue;

            int[] xs = {-1, 0, 1};
            int[] ys = {-1, 0, 1};

            bool isSaddlePoint = true;
            int maxPixels = 0;

            // Check the surrounding pixels
            foreach (var xi in xs)
            foreach (var yi in ys)
            {
                if (xi == yi && xi == 0)
                    continue;

                if (x + xi < 0 || x + xi >= gridSizeX)
                    continue;

                if (y + yi < 0 || y + yi >= gridSizeY)
                    continue;

                // Check if this is a saddle point
                if (m_grid[x, y].distanceTransform > m_grid[x + xi, y + yi].distanceTransform)
                    maxPixels++;
            }


            if (maxPixels > 1)
                m_grid[x, y].isSaddle = true;
        }
    }


    public List<WayPoint> GetRoadMap()
    {
        return m_roadMap;
    }


    private void OnDrawGizmos()
    {
        // if (m_roadMap != null)
        // {
        //     foreach (var node in m_roadMap)
        //     {
        //         node.Draw();
        //         foreach (var node1 in node.GetConnections())
        //         {
        //             Gizmos.DrawLine(node.GetPosition(), node1.GetPosition());
        //         }
        //     }
        // }
    }
}


// Distance transform node
public class DtNode
{
    public bool walkable;
    public Vector2 worldPosition;
    public float distanceTransform;

    public bool isMaxima;
    public bool isSaddle;

    // Node position on the grid
    public int gridX;
    public int gridY;

    public DtNode(bool isWalkable, int x, int y, Vector2 position, float distance)
    {
        walkable = isWalkable;
        gridX = x;
        gridY = y;
        worldPosition = position;
        distanceTransform = distance;
    }
}