using UnityEngine;

public class MapGrid<T> where T : new()
{
    private T[,] _grid;

    // Values for the grid measures
    protected Vector2 worldBottomLeft;

    private Vector2 _cellDimensions;
    private float _cellWidth;
    private float _cellHeight;


    private int _columnCount;
    private int _rowCount;

    public MapGrid(Bounds bounds, int columnCount, int rowCount)
    {
        _columnCount = columnCount;
        _rowCount = rowCount;

        _cellWidth = Mathf.Abs(bounds.min.x - bounds.max.x) / _columnCount;
        _cellHeight = Mathf.Abs(bounds.min.y - bounds.max.y) / _rowCount;

        // Set the left bottom corner position in the world
        worldBottomLeft = new Vector2(bounds.min.x, bounds.max.y);

        SetupGrid();
    }

    public MapGrid(Bounds bounds, float cellWidth, float cellHeight)
    {
        _cellHeight = cellHeight;
        _cellWidth = cellWidth;

        _columnCount = Mathf.CeilToInt(Mathf.Abs(bounds.min.x - bounds.max.x) / _cellWidth);
        _rowCount = Mathf.CeilToInt(Mathf.Abs(bounds.min.y - bounds.max.y) / _cellHeight);

        // Set the left bottom corner position in the world
        worldBottomLeft = new Vector2(bounds.min.x, bounds.max.y);

        SetupGrid();
    }

    private void SetupGrid()
    {
        _cellDimensions = new Vector2(_cellWidth, -_cellHeight);

        // Set the grid size
        _grid = new T[_columnCount, _rowCount];

        for (int i = 0; i < _columnCount; i++)
        for (int j = 0; j < _rowCount; j++)
        {
            _grid[i, j] = new T();
        }
    }

    public T[,] GetGrid()
    {
        return _grid;
    }

    public Vector2 GetWorldPosition(int col, int row)
    {
        Vector2 worldPoint = worldBottomLeft + new Vector2(col * _cellWidth, -row * _cellHeight) +
                             _cellDimensions * 0.5f;

        return worldPoint;
    }

    public Vector2 GetCoordinates(Vector3 worldPosition)
    {
        Vector2 coordinates = ((Vector2) worldPosition - worldBottomLeft - _cellDimensions * 0.5f) /
                              _cellDimensions;
        return new Vector2(Mathf.RoundToInt(coordinates.x), Mathf.RoundToInt(coordinates.y));
    }

    public bool IsNodeInMap(Vector2 node, float cellSide)
    {
        // if the position is on barrier
        if (Physics2D.OverlapCircle(node, cellSide) != null) return false;

        return PolygonHelper.IsPointInPolygons(MapManager.Instance.GetExWalls(), node);
    }

    public void Draw()
    {
        Gizmos.color = Color.gray;
        for (int col = 0; col < _grid.GetLength(0); col++)
        for (int row = 0; row < _grid.GetLength(1); row++)
        {
            Gizmos.DrawCube(GetWorldPosition(col, row), new Vector3(_cellWidth * 0.9f, _cellHeight * 0.9f, 1f));
        }
    }
}