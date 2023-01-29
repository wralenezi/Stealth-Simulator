using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public bool isExpanded;
    private List<Node> _neighbours;
    public Vector2 worldPosition;
    private float _spottedTime;

    public float staleness;
    public bool isSeen;
    
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
        isSeen = true;
        _spottedTime = episodeTime;
        staleness = 0f;
    }

    public float GetLastSpottedTime()
    {
        return _spottedTime;
    }

    public void Reset()
    {
        isExpanded = false;
        _spottedTime = 0f;
        staleness = 0f;
        isSeen = false;
    }

}