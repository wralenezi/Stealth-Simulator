using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridWorld : WorldRep
{
    // Map Renderer
    MapRenderer m_mapRenderer;

    // 2D List of the grid way points
    private Node[,] m_grid;

    public List<Node> NodeList;

    // Values for the grid measures
    protected float nodeDiameter;
    protected Vector2 worldBottomLeft;

    // Grid dimension 
    public int gridSizeX, gridSizeY;

    private int walkableNodesCount;

    public override void InitiateWorld(float mapScale)
    {
        base.InitiateWorld(mapScale);
        m_mapRenderer = GetComponent<MapRenderer>();
        CreateGrid(mapScale);
    }

    // Create the grid
    public void CreateGrid(float mapScale)
    {
        NodeList = new List<Node>();

        // Set the diameter of a node
        nodeDiameter = Properties.NodeRadius * 2f;

        // Determine the resolution of the grid
        gridSizeX = Mathf.RoundToInt(mapScale * Properties.GridDefaultSizeX / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(mapScale * Properties.GridDefaultSizeY / nodeDiameter);

        // Set the grid size
        m_grid = new Node[gridSizeX, gridSizeY];

        // Set the left bottom corner position in the world
        worldBottomLeft = (Vector2) (transform.position) - Vector2.right * Properties.GridDefaultSizeX / 2 -
                          Vector2.up * Properties.GridDefaultSizeY / 2;

        worldBottomLeft *= mapScale;

        walkableNodesCount = 0;

        // Establish the walkable areas in the grid
        for (int x = 0; x < gridSizeX; x++)
        for (int y = 0; y < gridSizeY; y++)
        {
            // Get the node's world position
            Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + Properties.NodeRadius) +
                                 Vector2.up * (y * nodeDiameter + Properties.NodeRadius);

            bool walkable = false;
            float staleness = -1f;

            if (IsNodeInMap(worldPoint))
            {
                walkable = true;
                staleness = Properties.StalenessLow;
                walkableNodesCount++;
            }

            m_grid[x, y] = new Node(walkable, worldPoint, x, y, staleness);

            if (m_grid[x, y].walkable)
                NodeList.Add(m_grid[x, y]);
        }

        // Render the grid
        // m_meshManager.InitializeGrid(NodeList);
    }

    private bool IsNodeInMap(Vector2 node)
    {
        // if the position is on barrier
        if (Physics2D.OverlapCircle(node, Properties.NodeRadius) != null)
            return false;


        return PolygonHelper.IsPointInPolygons(m_mapRenderer.GetInteriorWalls(), node);
    }


    public override void ResetWorld()
    {
        base.ResetWorld();

        foreach (var node in NodeList)
        {
            node.SetStaleness(Properties.StalenessLow);
        }

        SetTimestamp();
    }


    public override void UpdateWorld(GuardsManager guardsManager)
    {
        List<GridGuard> gridGuards = new List<GridGuard>();

        foreach (var guard in guardsManager.GetGuards())
            gridGuards.Add((GridGuard) guard);

        UpdateGrid(gridGuards);

        base.UpdateWorld(guardsManager);
        // m_meshManager.RenderGrid(NodeList);
    }


    // Update the grid 
    void UpdateGrid(List<GridGuard> guards)
    {
        float totalStaleness = 0f;

        SeenPortion = 0f;
        UnseenPortion = 0f;

        AverageStaleness = 0f;

        foreach (GridGuard guard in guards)
        {
            guard.ResetSeenNodesCount();
        }

        // Get the staleness value since the last update
        float stalenessDelta = GetTimeDelta() * Properties.StalenessRate;

        foreach (Node node in NodeList)
        {
            // Pass if the node is un-walkable
            if (!node.walkable)
                continue;


            // loop through the guards and check what nodes they can see
            foreach (GridGuard guard in guards)
            {
                // Check if point seen by that guard
                if (guard.IsNodeInSeenRegion(node.worldPosition))
                {
                    node.SetStaleness(Properties.StalenessLow);
                    guard.IncrementSeenNodes();
                }
                else
                {
                    // Stale the node
                    node.IncreaseStaleness(stalenessDelta);
                }

                // Set the node weighted staleness
                node.SetWeightedStaleness(walkableNodesCount);
            }

            walkableNodesCount++;
            totalStaleness += node.GetStaleness();

            // Increment the number of unseen nodes
            if (node.GetStaleness() > 0f)
                UnseenPortion++;
            else
                SeenPortion++;
        }

        AverageStaleness = totalStaleness / UnseenPortion;
    }
}