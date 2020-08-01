using UnityEngine;

public class Node
{
    public bool walkable;
    public Vector2 worldPosition;

    // Node position on the grid
    public int gridX;
    public int gridY;

    // staleness values of a node, 1 highest and 0 lowest
    private float m_staleness;
    private float m_weightedStaleness;

    // flag if the node is initiated
    public bool isNodeSet = false;

    public Node(bool _walkable, Vector2 _worldPos, int _gridX, int _gridY, float _staleness)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
        m_staleness = _staleness;
    }

    public void IncreaseStaleness(float staleness)
    {
        m_staleness += staleness;
    }

    public void SetStaleness(float staleness)
    {
        m_staleness = staleness;
    }

    public void SetWeightedStaleness(int walkableNodesCount)
    {
        m_weightedStaleness = m_staleness / walkableNodesCount;
    }

    public float GetStaleness()
    {
        return m_staleness;
    }

    public float GetWeightedStaleness()
    {
        return m_weightedStaleness;
    }
}