using System.Collections.Generic;
using UnityEngine;

public class Node
{
    private List<Node> _neighbours;
    public Vector2 worldPosition;
    private float _spottedTime;

    public float oldStaleness;
    public float staleness;
    public bool isSeen;

    public bool isExpansionDone;
    public bool isIncrementedThisRound;

    
    public Node()
    {
        _neighbours = new List<Node>();
        Reset();
    }

    public void AddNeighbours(Node neighbour)
    {
        _neighbours.Add(neighbour);
    }

    public List<Node> GetNeighbors()
    {
        return _neighbours;
    }
    
    public void Spotted(float episodeTime)
    {
        _spottedTime = episodeTime;
        isSeen = true;
        staleness = 0f;
        oldStaleness = 0f;
    }

    public float GetLastSpottedTime()
    {
        return _spottedTime;
    }

    public void Reset()
    {
        isExpansionDone = false;
        _spottedTime = 0f;
        staleness = 0f;
        oldStaleness = 0f;
        isSeen = false;
    }

}