using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridGuide : MonoBehaviour
{

    // 2D List of the grid way points
    private Vector2[,] m_grid;
    
    // Values for the grid measures
    protected float nodeDiameter;
    protected Vector2 worldBottomLeft;
    
    // Grid dimension 
    private int gridSizeX, gridSizeY;

    private float mapScale = 6f;
    
    public void Start()
    {

        // Set the diameter of a node
        nodeDiameter = Properties.NodeRadius * 10f;

        // Determine the resolution of the grid
        gridSizeX = Mathf.RoundToInt(mapScale * Properties.GridDefaultSizeX / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(mapScale * Properties.GridDefaultSizeY / nodeDiameter);

        // Set the grid size
        m_grid = new Vector2[gridSizeX, gridSizeY];

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

            m_grid[x, y] = worldPoint;
        }

    }

    
    // get the grid pos from world pos
    public Vector2 GetGridPosFromWorldPos(Vector2 worldPos)
    {
        Vector2 gridLoc = (worldPos - worldBottomLeft - new Vector2(Properties.NodeRadius, Properties.NodeRadius)) /
                          nodeDiameter;

        return m_grid[Mathf.RoundToInt(gridLoc.x), Mathf.RoundToInt(gridLoc.y)];
    }
    
    
    
    

    public void OnDrawGizmos()
    {
        Gizmos.color = new Color32(255,255,255,155);
        for (int i = 0; i < gridSizeX; i++)
        for (int j = 0; j < gridSizeY; j++)
        {
            Gizmos.DrawSphere(m_grid[i,j], 0.1f);            
        }
        
    }
}
