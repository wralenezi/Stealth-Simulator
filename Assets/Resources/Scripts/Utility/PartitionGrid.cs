using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartitionGrid<T>
{
    public bool ShowPartitions;
    private List<T>[,] m_grid;

    // Values for the grid measures
    protected Vector2 worldBottomLeft;

    private Vector2 cellDimensions;
    private float cellWidth;
    private float cellHeight;

    private List<T> m_tempContainer;


    public PartitionGrid(Bounds bounds, int columnCount, int rowCount)
    {
        cellWidth = Mathf.Abs(bounds.min.x - bounds.max.x) / columnCount;
        cellHeight = Mathf.Abs(bounds.min.y - bounds.max.y) / rowCount;

        cellDimensions = new Vector2(cellWidth, -cellHeight);

        // Set the grid size
        m_grid = new List<T>[columnCount, rowCount];

        // Set the left bottom corner position in the world
        worldBottomLeft = new Vector2(bounds.min.x, bounds.max.y);

        m_tempContainer = new List<T>();

        // Establish the walkable areas in the grid
        for (int col = 0; col < columnCount; col++)
        for (int row = 0; row < rowCount; row++)
        {
            m_grid[col, row] = new List<T>();
        }

        ShowPartitions = true;
    }

    public Vector2 GetWorldPosition(int col, int row)
    {
        Vector2 worldPoint = worldBottomLeft + new Vector2(col * cellWidth, -row * cellHeight) +
                             cellDimensions * 0.5f;

        return worldPoint;
    }

    public void Add(T t, Vector3 position)
    {
        Vector2 coordinates = GetCoordinates(position);

        m_grid[Mathf.RoundToInt(coordinates.x), Mathf.RoundToInt(coordinates.y)].Add(t);
    }

    public Vector2 GetCoordinates(Vector3 worldPosition)
    {
        Vector2 coordinates = ((Vector2) worldPosition - worldBottomLeft - cellDimensions * 0.5f) /
                              cellDimensions;
        return new Vector2(Mathf.RoundToInt(coordinates.x), Mathf.RoundToInt(coordinates.y));
    }

    public List<T> GetPartitionsContent(Vector3 worldPosition, int range)
    {
        m_tempContainer.Clear();

        Vector2 coordinates = GetCoordinates(worldPosition);

        for (int colN = 0 - range; colN <= range; colN++)
        for (int rowN = 0 - range; rowN <= range; rowN++)
        {
            int colCoord = colN + Mathf.RoundToInt(coordinates.x);
            int rowCoord = rowN + Mathf.RoundToInt(coordinates.y);

            if (colCoord < 0 || colCoord >= m_grid.GetLength(0)) continue;
            if (rowCoord < 0 || rowCoord >= m_grid.GetLength(1)) continue;

            m_tempContainer.AddRange(m_grid[colCoord, rowCoord]);
        }


        return m_tempContainer;
    }


    public void Draw()
    {
        if (ShowPartitions)
        {
            Gizmos.color = Color.gray;
            for (int col = 0; col < m_grid.GetLength(0); col++)
            for (int row = 0; row < m_grid.GetLength(1); row++)
            {
                Gizmos.DrawCube(GetWorldPosition(col, row), new Vector3(cellWidth * 0.9f, cellHeight * 0.9f, 1f));
            }
        }
    }
}