using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Create the Scale axis transform of the map
public class SAT : MonoBehaviour
{
    // 2D List of the grid way points
    private DtNode[,] m_grid;

    // Values for the grid measures
    protected float nodeDiameter;
    protected Vector2 worldBottomLeft;

    // Grid dimension 
    int gridSizeRow, gridSizeCol;

    // Relative indices of the Neighborhood nR:rows, nC: columns
    CyclicalList<int> nR = new CyclicalList<int> {-1, -1, -1, 0, 1, 1, 1, 0};
    CyclicalList<int> nC = new CyclicalList<int> {-1, 0, 1, 1, 1, 0, -1, -1};

    // RoadMap of the World
    private List<RoadMapNode> _roadMap;

    // Road map after dividing the edges
    private List<RoadMapNode> m_roadMapDivided;

    // the road map created from the map skeleton.
    private List<RoadMapNode> m_SatRoadMap;

    // Establish the road map
    public void Initiate(MapRenderer mapRenderer, MapData mapData)
    {
        // Import the hand drawn road map
        ImportRoadMap(mapRenderer, mapData);

        // Divide long edges to smaller edges
        DivideRoadMap();
    }

    // This is to create the skeleton of the map using this reference
    // https://www.sciencedirect.com/science/article/abs/pii/104996529290026T
    private void CreateSkeletal(MapRenderer mapRenderer)
    {
        // Create the grid
        CreateGrid(mapRenderer);

        // Calculate the distance transform
        CalculateDistanceTransform();

        // Create local maximals
        SetLocalMaximals();

        SteepestHillClimb();

        // Create saddle points
        Set1by1SaddlePoints();

        Set2by2Saddles();

        HumpUpHillClimb();

        SimplifiedGraph(mapRenderer);
    }


    private void ImportRoadMap(MapRenderer mapRenderer, MapData mapData)
    {
        _roadMap = new List<RoadMapNode>();
        m_roadMapDivided = new List<RoadMapNode>();

        string mapString;

        try
        {
            mapString = GameManager.Instance.currentRoadMapData;

            if (mapString == "")
                throw new Exception("Road map file is empty.");

            ParseMapString(mapString);
        }
        catch (Exception e)
        {
            Debug.Log("Missing Road Map file for: " + mapData.name + " " + mapData.size);

            // Implement SAT to get the road map
            CreateSkeletal(mapRenderer);

            // Save the map data for future use.
            SaveMap(mapData);

            _roadMap = m_SatRoadMap;
        }


        foreach (var wp in _roadMap)
        {
            wp.SafeCons();
        }
    }


    // Save the road map as a csv file 
    private void SaveMap(MapData mapData)
    {
        string data = "";

        for (int i = 0; i < m_SatRoadMap.Count; i++)
        {
            RoadMapNode wp = m_SatRoadMap[i];

            wp.Id = i + 1;

            data += wp.GetPosition().x + "," + wp.GetPosition().y + "," + wp.type + "\n";
        }

        data += "-,-\n";

        foreach (var wp in m_SatRoadMap)
        {
            foreach (var con in wp.GetConnections(true))
            {
                if (wp.Id < con.Id)
                    data += wp.Id + "," + con.Id + "\n";
            }
        }

        CsvController.WriteString(GetPath(mapData), data, false);

        Debug.Log("Map: " + mapData.name + " " + mapData.size + " Saved.");
    }

    // Get the path to the map
    private string GetPath(MapData map)
    {
        // Gets the path to the "Assets" folder 
        return GameManager.RoadMapsPath + map.name + "_" + map.size + ".csv";
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

                    RoadMapNode newPoint = new RoadMapNode(position, lineIndex);
                    newPoint.type = (NodeType) Enum.Parse(typeof(NodeType), data[2]);
                    _roadMap.Add(newPoint);
                    m_roadMapDivided.Add(newPoint);
                }
                else
                {
                    int firstIndex = int.Parse(data[0]) - 1;
                    int secondIndex = int.Parse(data[1]) - 1;

                    // Add two way edge
                    _roadMap[firstIndex].Connect(_roadMap[secondIndex], true, false);
                    m_roadMapDivided[firstIndex].Connect(_roadMap[secondIndex], false, false);
                }
            }
    }


    // Divide the long edges into smaller edges
    private void DivideRoadMap()
    {
        List<RoadMapNode> newWayPoints = new List<RoadMapNode>();

        for (int w = 0; w < m_roadMapDivided.Count; w++)
        {
            RoadMapNode wp = m_roadMapDivided[w];

            for (int i = 0; i < wp.GetConnections(false).Count; i++)
            {
                RoadMapNode connection = wp.GetConnections(false)[i];

                // Divide the edge if it is longer than the max length
                float totalDistance = Vector2.Distance(wp.GetPosition(), connection.GetPosition());

                if (totalDistance > Properties.GetMaxEdgeLength())
                {
                    RoadMapNode prevWayPoint = null;

                    // Remove the two way connection
                    // wp.RemoveEdge(connection);
                    // connection.RemoveEdge(wp);
                    wp.RemoveConnection(connection, false);

                    // Define the direction of placing the intermediate way points
                    Vector2 dir = (connection.GetPosition() - wp.GetPosition()).normalized;

                    // Define number of intermediate edges
                    int edgesCount = Mathf.CeilToInt(totalDistance / Properties.GetMaxEdgeLength());

                    float length = totalDistance / edgesCount;

                    // Place way points and the last way point will connect to original connection
                    for (int j = 1; j < edgesCount; j++)
                    {
                        // Place the way point
                        Vector2 wayPointPos = wp.GetPosition() + dir * (j * length);
                        RoadMapNode wayPoint = new RoadMapNode(wayPointPos);

                        // Connect the first way point to the source way point and the rest with the previous
                        if (j == 1)
                        {
                            wp.Connect(wayPoint, false, false);
                        }
                        else
                        {
                            prevWayPoint.Connect(wayPoint, false, false);
                        }

                        prevWayPoint = wayPoint;
                        // Add to a separate list
                        newWayPoints.Add(wayPoint);
                    }

                    // Make the final connection
                    connection.Connect(prevWayPoint, false, false);

                    // Reset the counter since the connections were changed.
                    w = 0;
                    break;
                }
            }
        }

        m_roadMapDivided.AddRange(newWayPoints);
    }


// Create the grid
    public void CreateGrid(MapRenderer mapRenderer)
    {
        // Set the diameter of a node
        nodeDiameter = Properties.NodeRadius * 2f;

        Bounds bounds = mapRenderer.GetMapBoundingBox();

        // Determine the resolution of the grid
        gridSizeRow = Mathf.RoundToInt(Mathf.Abs(bounds.max.y - bounds.min.y) * 1.1f / nodeDiameter);
        gridSizeCol = Mathf.RoundToInt(Mathf.Abs(bounds.max.x - bounds.min.x) * 1.1f / nodeDiameter);

        // Set the grid size
        m_grid = new DtNode[gridSizeRow, gridSizeCol];

        worldBottomLeft = new Vector2(bounds.min.x, bounds.min.y) - Vector2.one;

        // Establish the walkable areas in the grid
        for (int x = 0; x < gridSizeRow; x++)
        for (int y = 0; y < gridSizeCol; y++)
        {
            // Get the node's world position
            Vector2 worldPoint = worldBottomLeft + Vector2.right * (y * nodeDiameter + Properties.NodeRadius) +
                                 Vector2.up * (x * nodeDiameter + Properties.NodeRadius);

            bool walkable = false;
            float distance = -1f;

            bool isWalkable = PolygonHelper.IsPointInPolygons(mapRenderer.GetInteriorWalls(), worldPoint);
            if (isWalkable)
            {
                walkable = true;
                distance = Properties.StalenessLow;
            }

            m_grid[x, y] = new DtNode(x, y, worldPoint, distance);

            if (walkable)
                m_grid[x, y].code = '-';
            else
                m_grid[x, y].code = '0';
        }
    }


    // Calculate the distance transform using the euclidean distance
    private void CalculateDistanceTransform()
    {
        // Max value for normalization
        float max = Mathf.NegativeInfinity;

        for (int xi = 0; xi < gridSizeRow; xi++)
        for (int yi = 0; yi < gridSizeCol; yi++)
        {
            if (m_grid[xi, yi].code == '0')
            {
                m_grid[xi, yi].distanceTransform = 0f;
                continue;
            }

            float minDistance = Mathf.Infinity;

            for (int x = 0; x < gridSizeRow; x++)
            for (int y = 0; y < gridSizeCol; y++)
            {
                if (m_grid[x, y].code != '0')
                    continue;

                // Euclidean distance
                float distance = Mathf.Max(Mathf.Abs(xi - x), Mathf.Abs(yi - y));

                if (distance < minDistance)
                    minDistance = distance;
            }


            if (max < minDistance)
                max = minDistance;

            m_grid[xi, yi].distanceTransform = minDistance;
        }


        // Normalize the values
        for (int xi = 0; xi < gridSizeRow; xi++)
        for (int yi = 0; yi < gridSizeCol; yi++)
        {
            float normalized = m_grid[xi, yi].distanceTransform / max;

            normalized *= Properties.StalenessHigh;

            m_grid[xi, yi].distanceTransform = normalized;
        }
    }

    // Set the local maximals
    private void SetLocalMaximals()
    {
        for (int x = 0; x < gridSizeRow; x++)
        for (int y = 0; y < gridSizeCol; y++)
        {
            // Skip if the node is un-walkable
            if (m_grid[x, y].code == '0')
                continue;

            int[] xs = {-1, 0, 1};
            int[] ys = {-1, 0, 1};
            bool isLocalMaximum = true;

            // Check the surrounding pixels
            foreach (var xi in xs)
            foreach (var yi in ys)
            {
                if ((xi == yi && xi == 0) || (x + xi < 0 || x + xi >= gridSizeRow) ||
                    (y + yi < 0 || y + yi >= gridSizeCol) || m_grid[x + xi, y + yi].code == '0')
                    continue;

                // Check if the pixel is not a local maximal
                if (m_grid[x, y].distanceTransform < m_grid[x + xi, y + yi].distanceTransform)
                    isLocalMaximum = false;
            }

            if (isLocalMaximum)
                m_grid[x, y].code = '*';
            else
                m_grid[x, y].code = '-';
        }
    }


    public void SteepestHillClimb()
    {
        for (int x = 0; x < gridSizeRow; x++)
        for (int y = 0; y < gridSizeCol; y++)
        {
            // Skip if the node is not a local maximum
            if (m_grid[x, y].code != '*')
                continue;

            UphillClimb(x, y);
        }
    }

    int recursiveCounter = 0;

    public void UphillClimb(int x, int y)
    {
        if (m_grid[x, y].code == '*')
        {
            int[] xs = {-1, 0, 1};
            int[] ys = {-1, 0, 1};

            // Check the surrounding pixels
            foreach (var xi in xs)
            foreach (var yi in ys)
            {
                if ((xi == yi && xi == 0) || (x + xi < 0 || x + xi >= gridSizeRow) ||
                    (y + yi < 0 || y + yi >= gridSizeCol) || m_grid[x + xi, y + yi].code == '0')
                    continue;

                if (m_grid[x, y].distanceTransform == m_grid[x + xi, y + yi].distanceTransform &&
                    m_grid[x + xi, y + yi].code == '-')
                {
                    m_grid[x + xi, y + yi].code = '/';
                    UphillClimb(x + xi, y + yi);
                }
            }
        }
        else if (m_grid[x, y].code != '0')
        {
            // Get the max in the neighbors.
            float max = Mathf.NegativeInfinity;
            int[] rS = {-1, 0, 1};
            int[] cS = {-1, 0, 1};

            foreach (var rI in rS)
            foreach (var cI in cS)
            {
                if ((rI == cI && rI == 0) || (x + rI < 0 || x + rI >= gridSizeRow) ||
                    (y + cI < 0 || y + cI >= gridSizeCol) || m_grid[x + rI, y + cI].code == '0')
                    continue;

                if (m_grid[x + rI, y + cI].distanceTransform > max)
                    max = m_grid[x + rI, y + cI].distanceTransform;
            }


            List<List<DtNode>> sequences = new List<List<DtNode>>();

            int count = 0;
            // Go around the neighbors 
            for (int i = 0; i < nR.Count & count < nR.Count; i++)
            {
                if ((nR[i] == nC[i] && nR[i] == 0) ||
                    x + nR[i] < 0 || x + nR[i] >= gridSizeRow ||
                    y + nC[i] < 0 || y + nC[i] >= gridSizeCol ||
                    m_grid[x + nR[i], y + nC[i]].code == '0')
                {
                    count++;
                    continue;
                }


                if (m_grid[x + nR[i], y + nC[i]].distanceTransform == max)
                {
                    // We found a max then go one step back to start the sequence
                    if (sequences.Count == 0 || sequences[sequences.Count - 1].Count == 0)
                    {
                        // Add the start of the sequence
                        sequences.Add(new List<DtNode>());
                    }

                    sequences[sequences.Count - 1].Add(m_grid[x + nR[i], y + nC[i]]);
                }
                else if (m_grid[x + nR[i], y + nC[i]].distanceTransform < max)
                {
                    if (sequences.Count > 0 && sequences[sequences.Count - 1].Count > 0)
                    {
                        // if we reach a non max and we have a sequence then it is the end of that sequence.
                        sequences.Add(new List<DtNode>());
                    }
                }
            }

            foreach (var seq in sequences)
            {
                if (seq.Count > 0)
                {
                    int index = Mathf.FloorToInt(seq.Count / 2f);
                    if (seq[index].code != '*')
                    {
                        seq[index].code = '/';
                        UphillClimb(seq[index].row, seq[index].col);
                    }
                }
            }
        }
    }


    // Set the 1x1 Saddle points
    private void Set1by1SaddlePoints()
    {
        List<CyclicalList<int>> subSequences = new List<CyclicalList<int>>();

        for (int row = 0; row < gridSizeRow; row++)
        for (int col = 0; col < gridSizeCol; col++)
        {
            if (m_grid[row, col].code == '*' || m_grid[row, col].code == '0')
                continue;

            float dTransform = m_grid[row, col].distanceTransform;

            subSequences.Clear();
            subSequences.Add(new CyclicalList<int>());
            CyclicalList<int> currentSeq = subSequences[subSequences.Count - 1];
            for (int i = 0; i < nR.Count; i++)
            {
                if (m_grid[row + nR[i], col + nC[i]].distanceTransform > dTransform)
                {
                    if (currentSeq.Count > 0)
                    {
                        // Check if the sequence is made up of sequence, else clear it.
                        if (currentSeq[currentSeq.Count - 1] != i - 1)
                        {
                            subSequences.Add(new CyclicalList<int>());
                            currentSeq = subSequences[subSequences.Count - 1];
                        }
                    }

                    currentSeq.Add(i);
                }
            }

            foreach (var subSequence in subSequences)
            {
                // Make sure to have more than one hump
                if (subSequences.Count < 2)
                    break;

                // Skip if the sequence is empty
                if (subSequence.Count == 0)
                    continue;

                int index = subSequence.Count / 2;
                // the neighbor preceding the sequence.
                int preS = subSequence[0] - 1;
                // the neighbor after the sequence.
                int postS = subSequence[subSequence.Count - 1] + 1;
                if (m_grid[row + nR[preS], col + nC[preS]].distanceTransform <= dTransform &&
                    m_grid[row + nR[postS], col + nC[postS]].distanceTransform <= dTransform)
                {
                    m_grid[row, col].code = '&';
                    DtNode n = m_grid[row + nR[subSequence[index]], col + nC[subSequence[index]]];
                    m_grid[row, col].humps.Add(n);
                }
            }

            subSequences.Clear();
            subSequences.Add(new CyclicalList<int>());
            currentSeq = subSequences[subSequences.Count - 1];
            for (int i = 0; i < nR.Count; i++)
            {
                if (m_grid[row + nR[i], col + nC[i]].distanceTransform == dTransform)
                {
                    if (currentSeq.Count > 0)
                    {
                        // Check if the array is made up of sequence, else clear it.
                        if (currentSeq[currentSeq.Count - 1] != i - 1)
                        {
                            subSequences.Add(new CyclicalList<int>());
                            currentSeq = subSequences[subSequences.Count - 1];
                        }
                    }

                    currentSeq.Add(i);
                }
            }

            foreach (var subSequence in subSequences)
            {
                // Make sure to have more than one hump
                if (subSequences.Count < 2)
                    break;

                // Skip if the sequence is empty
                if (subSequence.Count == 0)
                    continue;

                bool isAdded = false;
                int index = subSequence.Count / 2;
                // the neighbor preceding the sequence.
                int preS = subSequence[0] - 1;
                // the neighbor after the sequence.
                int postS = subSequence[subSequence.Count - 1] + 1;
                if (m_grid[row + nR[preS], col + nC[preS]].distanceTransform < dTransform &&
                    m_grid[row + nR[postS], col + nC[postS]].distanceTransform < dTransform)
                {
                    m_grid[row, col].code = '&';
                    DtNode n = m_grid[row + nR[subSequence[index]], col + nC[subSequence[index]]];
                    m_grid[row, col].humps.Add(n);
                }
            }
        }
    }


    public void Set2by2Saddles()
    {
        List<int> saddleX = new List<int> {0, 0, 1, 1};
        List<int> saddleY = new List<int> {0, 1, 0, 1};

        CyclicalList<int> neighborsX = new CyclicalList<int> {-1, -1, -1, -1, 0, 1, 2, 2, 2, 2, 1, 0};
        CyclicalList<int> neighborsY = new CyclicalList<int> {-1, 0, 1, 2, 2, 2, 2, 1, 0, -1, -1, -1};
        List<CyclicalList<int>> subSequences = new List<CyclicalList<int>>();

        for (int x = 0; x < gridSizeRow; x++)
        for (int y = 0; y < gridSizeCol; y++)
        {
            // Make sure none of the 2x2 points are local maximums, 1x1 saddles or non walkalble and has equal transform.
            float dTransform = m_grid[x, y].distanceTransform;
            bool isGood = true;
            for (int i = 0; i < saddleX.Count; i++)
            {
                if (x + saddleX[i] < 0 || x + saddleX[i] >= gridSizeRow ||
                    y + saddleY[i] < 0 || y + saddleY[i] >= gridSizeCol)
                {
                    isGood = false;
                    break;
                }

                DtNode n = m_grid[x + saddleX[i], y + saddleY[i]];

                if (n.code == '*' || n.code == '&' || n.distanceTransform != dTransform || n.code == '0' ||
                    n.code == '#')
                {
                    isGood = false;
                    break;
                }
            }

            if (!isGood)
                continue;


            // Here we have to check if these 2x2 pixels have humps in the 12 neighbors they have
            subSequences.Clear();
            subSequences.Add(new CyclicalList<int>());
            CyclicalList<int> currentSeq = subSequences[subSequences.Count - 1];
            for (int i = 0; i < neighborsX.Count; i++)
            {
                if (m_grid[x + neighborsX[i], y + neighborsY[i]].distanceTransform > dTransform)
                {
                    if (currentSeq.Count > 0)
                    {
                        // Check if the sequence is made up of sequence, else clear it.
                        if (currentSeq[currentSeq.Count - 1] != i - 1)
                        {
                            subSequences.Add(new CyclicalList<int>());
                            currentSeq = subSequences[subSequences.Count - 1];
                        }
                    }

                    currentSeq.Add(i);
                }
            }

            foreach (var subSequence in subSequences)
            {
                // Make sure to have more than one hump
                if (subSequences.Count < 2)
                    break;

                // Skip if the sequence is empty
                if (subSequence.Count == 0)
                    continue;

                int index = subSequence.Count / 2;
                // the neighbor preceding the sequence.
                int preS = subSequence[0] - 1;
                // the neighbor after the sequence.
                int postS = subSequence[subSequence.Count - 1] + 1;

                if (m_grid[x + neighborsX[preS], y + neighborsY[preS]].distanceTransform <= dTransform &&
                    m_grid[x + neighborsX[postS], y + neighborsY[postS]].distanceTransform <= dTransform)
                {
                    // Set the saddle block
                    for (int i = 0; i < saddleX.Count; i++)
                    {
                        DtNode n = m_grid[x + saddleX[i], y + saddleY[i]];
                        n.code = '#';
                    }

                    DtNode hump = m_grid[x + neighborsX[subSequence[index]], y + neighborsY[subSequence[index]]];
                    m_grid[x, y].humps.Add(hump);
                }
            }

            subSequences.Clear();
            subSequences.Add(new CyclicalList<int>());
            currentSeq = subSequences[subSequences.Count - 1];
            for (int i = 0; i < neighborsX.Count; i++)
            {
                if (m_grid[x + neighborsX[i], y + neighborsY[i]].distanceTransform == dTransform)
                {
                    if (currentSeq.Count > 0)
                    {
                        // Check if the sequence is made up of sequence, else clear it.
                        if (currentSeq[currentSeq.Count - 1] != i - 1)
                        {
                            subSequences.Add(new CyclicalList<int>());
                            currentSeq = subSequences[subSequences.Count - 1];
                        }
                    }

                    currentSeq.Add(i);
                }
            }

            foreach (var subSequence in subSequences)
            {
                // Make sure to have more than one hump
                if (subSequences.Count < 2)
                    break;

                // Skip if the sequence is empty
                if (subSequence.Count == 0)
                    continue;

                int index = subSequence.Count / 2;
                // the neighbor preceding the sequence.
                int preS = subSequence[0] - 1;
                // the neighbor after the sequence.
                int postS = subSequence[subSequence.Count - 1] + 1;

                if (m_grid[x + neighborsX[preS], y + neighborsY[preS]].distanceTransform < dTransform &&
                    m_grid[x + neighborsX[postS], y + neighborsY[postS]].distanceTransform < dTransform)
                {
                    // Set the saddle block
                    for (int i = 0; i < saddleX.Count; i++)
                    {
                        DtNode n = m_grid[x + saddleX[i], y + saddleY[i]];
                        n.code = '#';
                    }

                    DtNode hump = m_grid[x + neighborsX[subSequence[index]], y + neighborsY[subSequence[index]]];
                    m_grid[x, y].humps.Add(hump);
                }
            }
        }
    }

    // Hill climb from the humps 
    public void HumpUpHillClimb()
    {
        for (int row = 0; row < gridSizeRow; row++)
        for (int col = 0; col < gridSizeCol; col++)
        {
            foreach (var hump in m_grid[row, col].humps)
            {
                UphillClimb(hump.row, hump.col);
            }
        }
    }


    public void SimplifiedGraph(MapRenderer mapRenderer)
    {
        CreateGridGraph();

        CreateConnections();

        SlimMaximumBlocks();

        LabelBlocks();

        MergeConnections(mapRenderer, 1);

        RemoveReplicateEdges();

        RemoveExtraNodes();

        ConnectLocalMaxIslands(mapRenderer);

        RemoveExtraNodes();

        MergeConnections(mapRenderer, 3);

        RemoveReplicateEdges();

        ConnectCorners(mapRenderer);
    }


    // Create the graph from the grid.
    public void CreateGridGraph()
    {
        m_SatRoadMap = new List<RoadMapNode>();

        int id = 0;
        for (int row = 0; row < gridSizeRow; row++)
        for (int col = 0; col < gridSizeCol; col++)
        {
            DtNode n = m_grid[row, col];

            // Skip the node if it is not on the SAT 
            if (n.code == '0' || n.code == '-' || n.wp != null)
                continue;

            // Create into a way point and add it to the graph.
            RoadMapNode wp = new RoadMapNode(n.worldPosition, n.row, n.col, n.code) {Id = ++id};
            m_SatRoadMap.Add(wp);

            // Create a reference from the grid node to the way point node
            n.wp = wp;

            foreach (var hump in n.humps)
            {
                if (hump.wp != null)
                    continue;

                RoadMapNode hmpWp = new RoadMapNode(hump.worldPosition, hump.row, hump.col, 'H');
                m_SatRoadMap.Add(hmpWp);
                hump.wp = hmpWp;
                hump.code = 'H';
            }
        }
    }


    // Make the connections on the graph
    public void CreateConnections()
    {
        for (int row = 0; row < gridSizeRow; row++)
        for (int col = 0; col < gridSizeCol; col++)
        {
            DtNode n = m_grid[row, col];

            // Skip the node if it is not on the SAT 
            if (n.code == '0' || n.code == '-')
                continue;

            // Create connections to the surrounding nodes 
            for (int i = 0; i < nR.Count; i++)
            {
                DtNode neighborNode = m_grid[row + nR[i], col + nC[i]];

                if (neighborNode.code == '0' || neighborNode.code == '-')
                    continue;

                n.wp.Connect(neighborNode.wp, true, false);
            }
        }
    }


    // Slim the maximum blocks
    // Merge two adjacent node whether vertical or horizontal, whichever is the shortest running line of local maximums.  
    private void SlimMaximumBlocks()
    {
        // Loop the nodes from left to right row by row.
        for (int row = 0; row < gridSizeRow; row++)
        for (int col = 0; col < gridSizeCol; col++)
        {
            DtNode n = m_grid[row, col];

            // Skip the node if it is not on the SAT 
            if (n.code != '*' || n.wp == null)
                continue;

            // Row counters
            int rowPointer = -1;
            int rowCounter = 0;
            bool rowDone = false;
            // Columns counters
            int colPointer = -1;
            int colCounter = 0;
            bool colDone = false;

            // Check for 3 horizontal step and vertical steps.
            while (rowPointer < 3 && colPointer < 3)
            {
                if (m_grid[row + rowPointer, col].code == '*' && !rowDone)
                {
                    rowCounter++;
                    rowPointer++;
                }
                else if (rowCounter > 0)
                {
                    rowDone = true;
                    rowPointer++;
                }
                else
                    rowPointer++;


                if (m_grid[row, col + colPointer].code == '*' && !colDone)
                {
                    colCounter++;
                    colPointer++;
                }
                else if (colCounter > 0)
                {
                    colDone = true;
                    colPointer++;
                }
                else
                    colPointer++;
            }

            bool merged = false;
            // If the rows are shorter and of length two merge vertically
            if (rowCounter == 2)
            {
                DtNode up = m_grid[row - 1, col];
                DtNode down = m_grid[row + 1, col];

                DtNode nodeToMergeWith = up.code == '*' ? up : down;

                if (nodeToMergeWith.wp != null)
                {
                    MergeNodes(n, nodeToMergeWith);
                    merged = true;
                }
            }

            if (colCounter == 2 && !merged)
            {
                DtNode left = m_grid[row, col - 1];
                DtNode right = m_grid[row, col + 1];

                DtNode nodeToMergeWith = left.code == '*' ? left : right;

                if (nodeToMergeWith.wp != null)
                    MergeNodes(n, nodeToMergeWith);
            }
        }
    }


    // Merge the corresponding way points and remove the reference from the originals
    private void MergeNodes(DtNode first, DtNode second)
    {
        RoadMapNode firstWp = first.wp;
        first.wp = null;
        RoadMapNode secWp = second.wp;
        second.wp = null;

        RoadMapNode newWp = new RoadMapNode((firstWp.GetPosition() + secWp.GetPosition()) / 2f)
            {code = first.code, row = 0, col = 0};

        // Take the reconnect the connections from the old way point to the new one
        while (firstWp.GetConnections(true).Count > 0)
        {
            RoadMapNode con = firstWp.GetConnections(true)[0];
            newWp.Connect(con, true, false);
            firstWp.RemoveConnection(con, true);
        }

        while (secWp.GetConnections(true).Count > 0)
        {
            RoadMapNode con = secWp.GetConnections(true)[0];
            newWp.Connect(con, true, false);
            secWp.RemoveConnection(con, true);
        }

        m_SatRoadMap.Remove(firstWp);
        m_SatRoadMap.Remove(secWp);
        m_SatRoadMap.Add(newWp);
    }

    // Label local maximum blocks
    private void LabelBlocks()
    {
        int blockID = 1;
        foreach (var wp in m_SatRoadMap)
        {
            if (wp.code == '*' && wp.BlockId == 0)
            {
                wp.BlockId = blockID++;
                ExpandTerritory(wp);
            }
        }
    }

    // 
    private void ExpandTerritory(RoadMapNode startWp)
    {
        Queue<RoadMapNode> open = new Queue<RoadMapNode>();

        int blockId = startWp.BlockId;

        open.Enqueue(startWp);

        while (open.Count > 0)
        {
            RoadMapNode curWp = open.Dequeue();

            foreach (var con in curWp.GetConnections(true))
            {
                if (con.BlockId != blockId && con.code == '*')
                {
                    con.BlockId = blockId;
                    open.Enqueue(con);
                }
            }
        }
    }

    private void RemoveExtraNodes()
    {
        for (int i = 0; i < m_SatRoadMap.Count; i++)
        {
            RoadMapNode curWp = m_SatRoadMap[i];

            List<RoadMapNode> conns = curWp.GetConnections(true);

            if (conns.Count == 0)
            {
                m_SatRoadMap.Remove(curWp);
                i--;
                continue;
            }

            if (curWp.code != '*' || conns.Count != 2)
                continue;

            bool isRemoved = false;
            for (int j = 0; j < conns.Count; j++)
            {
                if (conns[j].code != '*')
                    continue;

                if (isRemoved)
                    break;

                RoadMapNode firstCon = conns[j];

                for (int k = j + 1; k < conns.Count; k++)
                {
                    RoadMapNode secCon = conns[k];

                    if (firstCon.code != secCon.code)
                        continue;

                    // Decide to remove the node 
                    Vector2 projection = GeometryHelper.ClosestProjectionOnSegment(firstCon.GetPosition(),
                        secCon.GetPosition(),
                        curWp.GetPosition());

                    float distanceToMid = Vector2.Distance(curWp.GetPosition(), projection);

                    bool remove = distanceToMid < 0.3f;

                    if (remove)
                    {
                        m_SatRoadMap.Remove(curWp);

                        curWp.RemoveConnection(firstCon, true);
                        curWp.RemoveConnection(secCon, true);

                        firstCon.Connect(secCon, true, false);
                        i--;
                        isRemoved = true;
                        break;
                    }
                }
            }
        }
    }


    private void MergeConnections(MapRenderer mapRenderer, float mergeDistance)
    {
        for (int i = 0; i < m_SatRoadMap.Count; i++)
        {
            RoadMapNode curWp = m_SatRoadMap[i];

            List<RoadMapNode> conns = curWp.GetConnections(true);

            bool isMerged = false;
            for (int j = 0; j < conns.Count; j++)
            {
                if (isMerged)
                    break;

                RoadMapNode firstCon = conns[j];

                for (int k = j + 1; k < conns.Count; k++)
                {
                    RoadMapNode secCon = conns[k];

                    if (firstCon.code != secCon.code)
                        continue;


                    float distance = Vector2.Distance(firstCon.GetPosition(), secCon.GetPosition());


                    bool merge = firstCon.BlockId == secCon.BlockId &&
                                 distance < mergeDistance &&
                                 firstCon.IsConnected(secCon, true);

                    if (merge)
                    {
                        Vector2 newPosition;

                        if (firstCon.EdgeOfLocalMaximum() && secCon.EdgeOfLocalMaximum())
                            newPosition = firstCon.GetPosition();
                        else if (firstCon.EdgeOfLocalMaximum())
                            newPosition = firstCon.GetPosition();
                        else if (secCon.EdgeOfLocalMaximum())
                            newPosition = secCon.GetPosition();
                        else
                            newPosition = (firstCon.GetPosition() + secCon.GetPosition()) / 2f;

                        if (!mapRenderer.VisibilityCheck(curWp.GetPosition(), newPosition))
                            break;

                        // Remove the connection to the two neighbors to be merged.
                        curWp.RemoveConnection(firstCon, true);
                        curWp.RemoveConnection(secCon, true);

                        RoadMapNode newWp = new RoadMapNode(newPosition, 0, 0, firstCon.code)
                        {
                            BlockId = firstCon.BlockId
                        };

                        // Move the connections to the new way point
                        while (firstCon.GetConnections(true).Count > 0)
                        {
                            RoadMapNode fCon = firstCon.GetConnections(true)[0];

                            // Don't add the connection to the other neighbor.
                            if (fCon != secCon)
                            {
                                newWp.Connect(fCon, true, false);
                            }

                            firstCon.RemoveConnection(fCon, true);
                        }

                        while (secCon.GetConnections(true).Count > 0)
                        {
                            RoadMapNode seCon = secCon.GetConnections(true)[0];
                            if (seCon != firstCon)
                            {
                                newWp.Connect(seCon, true, false);
                            }

                            secCon.RemoveConnection(seCon, true);
                        }

                        curWp.Connect(newWp, true, false);

                        m_SatRoadMap.Remove(firstCon);
                        m_SatRoadMap.Remove(secCon);
                        m_SatRoadMap.Add(newWp);
                        isMerged = true;
                        i--;
                        break;
                    }
                }
            }
        }
    }


    private void RemoveReplicateEdges()
    {
        for (int i = 0; i < m_SatRoadMap.Count; i++)
        {
            RoadMapNode curWp = m_SatRoadMap[i];

            List<RoadMapNode> conns = curWp.GetConnections(true);

            for (int j = 0; j < conns.Count; j++)
            {
                RoadMapNode firstCon = conns[j];

                for (int k = j + 1; k < conns.Count; k++)
                {
                    RoadMapNode secCon = conns[k];

                    if (!firstCon.IsConnected(secCon, true))
                        continue;

                    float firstToSecDistance = Vector2.Distance(firstCon.GetPosition(), secCon.GetPosition());
                    float firstToCurDistance = Vector2.Distance(firstCon.GetPosition(), curWp.GetPosition());
                    float curToSecDistance = Vector2.Distance(curWp.GetPosition(), secCon.GetPosition());

                    RoadMapNode firstWp = null;
                    RoadMapNode secWp = null;

                    if (firstToSecDistance > firstToCurDistance)
                    {
                        if (firstToSecDistance > curToSecDistance)
                            firstWp = firstCon;
                        else
                            firstWp = curWp;

                        secWp = secCon;
                    }
                    else
                    {
                        if (firstToCurDistance > curToSecDistance)
                            firstWp = firstCon;
                        else
                            firstWp = secCon;

                        secWp = curWp;
                    }

                    firstWp.RemoveConnection(secWp, true);
                    j = 0;
                    break;
                }
            }
        }
    }


    // Connect the Islands of the local maximums
    public void ConnectLocalMaxIslands(MapRenderer mapRenderer)
    {
        List<RoadMapNode> nodesToRemove = new List<RoadMapNode>();

        List<RoadMapNode> goals = new List<RoadMapNode>();
        // Open and close lists
        Queue<RoadMapNode> open = new Queue<RoadMapNode>();
        List<RoadMapNode> close = new List<RoadMapNode>();

        foreach (var point in m_SatRoadMap)
        {
            if (!point.EdgeOfLocalMaximum())
                continue;

            goals.Clear();
            // Open and close lists
            open.Clear();
            close.Clear();

            // Load the starting connections.
            foreach (var con in point.GetConnections(true))
            {
                if (con.code != '*')
                    open.Enqueue(con);
            }

            int max = 0;
            // Get the list of goals 
            while (open.Count > 0)
            {
                RoadMapNode curWp = open.Dequeue();

                foreach (var con in curWp.GetConnections(true))
                {
                    if (close.Contains(con) || goals.Contains(con) || point == con)
                        continue;

                    if (con.code == '*')
                    {
                        goals.Add(con);
                    }
                    else
                        open.Enqueue(con);
                }

                close.Add(curWp);
            }

            // Connect to the goal
            foreach (var goal in goals)
            {
                if (mapRenderer.VisibilityCheck(point.GetPosition(), goal.GetPosition()))
                    point.Connect(goal, true, false);
            }

            foreach (var r in close)
            {
                if (!nodesToRemove.Contains(r))
                    nodesToRemove.Add(r);
            }
        }

        while (nodesToRemove.Count > 0)
        {
            m_SatRoadMap.Remove(nodesToRemove[0]);

            while (nodesToRemove[0].GetConnections(true).Count > 0)
                nodesToRemove[0].RemoveConnection(nodesToRemove[0].GetConnections(true)[0], true);

            nodesToRemove.Remove(nodesToRemove[0]);
        }
    }


    public void ConnectCorners(MapRenderer mapRenderer)
    {
        List<Polygon> walls = mapRenderer.GetInteriorWalls();

        for (int i = 0; i < walls.Count; i++)
        {
            for (int j = 0; j < walls[i].GetVerticesCount(); j++)
            {
                Polygon wall = walls[i];

                Vector2 angleNormal =
                    GeometryHelper.GetNormal(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1));

                if (GeometryHelper.IsReflex(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1)))
                {
                    RoadMapNode wp = new RoadMapNode(wall.GetPoint(j) - angleNormal * 0.3f, 0, 0, '*') {Id = 0};

                    wp.type = NodeType.Corner;

                    RoadMapNode projectionWp = GetInterceptionPointOnRoadMap(wp.GetPosition());

                    wp.Connect(projectionWp, true, false);

                    m_SatRoadMap.Add(wp);
                }
            }
        }
    }


    // Find the projection point on the road map
    // Find the closest projection point 
    public RoadMapNode GetInterceptionPointOnRoadMap(Vector2 position)
    {
        float minDistance = Mathf.Infinity;
        Vector2? projectionOnRoadMap = null;
        RoadMapNode projectionWp = null;

        RoadMapNode firstWp = null;
        RoadMapNode secWp = null;
        
        foreach (var wp in m_SatRoadMap)
        foreach (var con in wp.GetConnections(true))
        {
            // Get the point projection on the line
            Vector2 pro = GeometryHelper.ClosestProjectionOnSegment(wp.GetPosition(), con.GetPosition(), position);

            // The distance from the projection point to the intruder position
            float distance = Vector2.Distance(position, pro);

            RaycastHit2D hit = Physics2D.Linecast(pro, position);

            if (distance < minDistance && Equals(hit.collider, null)) //m_mapRenderer.VisibilityCheck(pro, position))
            {
                minDistance = distance;
                projectionOnRoadMap = pro;

                firstWp = wp;
                secWp = con;

                if (wp.GetPosition() == projectionOnRoadMap.Value)
                    projectionWp = wp;
                else if (con.GetPosition() == projectionOnRoadMap.Value)
                    projectionWp = con;
                else
                    projectionWp = null;
            }
        }

        if (projectionWp != null) return projectionWp;

        if (Equals(projectionOnRoadMap, null)) Debug.Log("There is no way point on the road map " + position);

        RoadMapNode newWp = new RoadMapNode(projectionOnRoadMap.Value);
        firstWp.RemoveConnection(secWp, true);

        firstWp.Connect(newWp, true, false);
        secWp.Connect(newWp, true, false);

        m_SatRoadMap.Add(newWp);

        return newWp;
    }

    public List<RoadMapNode> GetOriginalRoadMap()
    {
        return _roadMap;
    }

    public List<RoadMapNode> GetDividedRoadMap()
    {
        return m_roadMapDivided;
    }

    private void OnDrawGizmos()
    {
        if (_roadMap != null)
        {
            // for (int x = 0; x < gridSizeRow; x++)
            // for (int y = 0; y < gridSizeCol; y++)
            // {
            //     DtNode n = m_grid[x, y];
            //
            //     byte c = (byte) n.distanceTransform;
            //     if (n.code == '*')
            //         Gizmos.color = new Color32(255, 0, 0, 255);
            //     else if (n.code == '/')
            //         Gizmos.color = new Color32(0, 255, 0, 255);
            //     else if (n.code == '&')
            //         Gizmos.color = new Color32(255, 0, 255, 255);
            //     else if (n.code == '#')
            //         Gizmos.color = new Color32(255, 255, 0, 255);
            //     else
            //         Gizmos.color = new Color32(c, c, c, 255);
            //
            //     if (n.code != '0' && n.code != '-')
            //         Gizmos.DrawCube(n.worldPosition, Vector3.one * nodeDiameter);
            // }

            // for (int x = 0; x < gridSizeRow; x++)
            // for (int y = 0; y < gridSizeCol; y++)
            // {
            //     DtNode n = m_grid[x, y];
            //
            //     foreach (var hump in n.humps)
            //     {
            //         Gizmos.color = new Color32(255, 122, 0, 255);
            //         Gizmos.DrawCube(hump.worldPosition, Vector3.one * nodeDiameter);
            //     }
            // }

            // byte alpha = 90;
            // for (int set = 0; set < m_SatRoadMap.Count; set++)
            // {
            //     WayPoint wp = m_SatRoadMap[set];
            //     
            //     // if (wp.EdgeOfLocalMaximum())
            //     //     Gizmos.color = new Color32(100, 100, 100, alpha);
            //     // else if (wp.code == '*')
            //     //     Gizmos.color = new Color32(255, 0, 0, alpha);
            //     // else if (wp.code == '/')
            //     //     Gizmos.color = new Color32(0, 255, 0, alpha);
            //     // else if (wp.code == '&')
            //     //     Gizmos.color = new Color32(255, 255, 0, alpha);
            //     // else if (wp.code == 'H')
            //     //     Gizmos.color = new Color32(255, 100, 0, alpha);
            //     // else if (wp.code == '#')
            //     //     Gizmos.color = new Color32(255, 255, 0, alpha);
            //
            //     Gizmos.color = new Color32(255, 0, 0, alpha);
            //
            //
            //     Gizmos.DrawSphere(wp.GetPosition(), nodeDiameter * 0.7f);
            //     // Handles.Label(wp.GetPosition(), wp.code.ToString());
            //     // Handles.Label(wp.GetPosition(), (wp.row + " " + wp.col));
            //
            //     foreach (var con in wp.GetConnections(true))
            //     {
            //         Gizmos.DrawLine(wp.GetPosition(), con.GetPosition());
            //         
            //         // var p1 = wp.GetPosition();
            //         // var p2 = con.GetPosition();
            //         // var thickness = 3;
            //         // Handles.DrawBezier(p1,p2,p1,p2, Color.red,null,thickness);
            //         //
            //
            //         if (con.code == '*')
            //             Gizmos.color = new Color32(255, 0, 0, (byte) (alpha - 20));
            //         else if (con.code == '/')
            //             Gizmos.color = new Color32(0, 255, 0, (byte) (alpha - 20));
            //         else if (con.code == '&')
            //             Gizmos.color = new Color32(255, 255, 0, (byte) (alpha - 20));
            //         else if (con.code == 'H')
            //             Gizmos.color = new Color32(255, 100, 0, (byte) (alpha - 20));
            //         else if (con.code == '#')
            //             Gizmos.color = new Color32(255, 255, 0, (byte) (alpha - 20));
            //
            //         // Gizmos.DrawSphere(con.GetPosition(), nodeDiameter * 0.1f);
            //         // Handles.Label(con.GetPosition(), (con.BlockId + " " + con.code));
            //     }
            // }
        }
    }
}


// Distance transform node
public class DtNode
{
    public Vector2 worldPosition;
    public float distanceTransform;

    /// <summary>
    /// 0 is un-walkable
    /// * is local maximum
    /// - is just a walkable
    /// / is on the uphill path 
    /// </summary>
    public char code;

    // Node position on the grid
    public int row;
    public int col;

    // List of nodes that are humps
    public List<DtNode> humps;

    // A flag if the node is check for detecting the rectangles.
    public bool isChecked;

    // The reference to the corresponding way point
    public RoadMapNode wp;

    public DtNode(int _row, int _col, Vector2 position, float distance)
    {
        row = _row;
        col = _col;
        worldPosition = position;
        distanceTransform = distance;
        humps = new List<DtNode>();
    }
}