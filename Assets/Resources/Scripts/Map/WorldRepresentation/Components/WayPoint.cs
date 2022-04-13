using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

public class WayPoint
{
    // Position
    private readonly Vector2 _position;

    // Current connections to the way point (including the intermediate way points introduced by dividing the edges into segments.
    private readonly List<WayPoint> _connections;

    // The divided line segments connected to this way point. 
    private readonly List<RoadMapLine> _mapLines;

    // Original connections of the way points before dividing the edges into segments. 
    private readonly List<WayPoint> _originalCons;

    // The original lines connected to this way point
    private readonly List<RoadMapLine> _originalLines;
    
    // probability that a guard is going to pass through here; when it is zero
    private float _probabilityGuardPassing;
    
    // The ID of the way point
    public int Id;

    // The ID of the max block; used for the Skeletal axis transform algorithm.
    public int BlockId;

    // the ID of the wall this way point belong to. This is for the visibility graph
    public int WallId;
    
    // The node original location on the grid. Used in the grid simplification to a graph.
    public int row;
    public int col;
    public char code;

    // the variables for A*
    public float hDistance;
    public float gDistance;
    public WayPoint parent;

    public WayPoint(Vector2 _position, int _id = 0)
    {
        this._position = _position;
        _originalCons = new List<WayPoint>();
        _connections = new List<WayPoint>();
        _mapLines = new List<RoadMapLine>();
        _originalLines = new List<RoadMapLine>();
        Id = _id;
    }

    public WayPoint(Vector2 _position, int _row, int _col, char _code)
    {
        this._position = _position;
        row = _row;
        col = _col;
        code = _code;
        _originalCons = new List<WayPoint>();
        _connections = new List<WayPoint>();
        _mapLines = new List<RoadMapLine>();
        _originalLines = new List<RoadMapLine>();
        Id = 0;
    }


    // Add the way points to each others list of connects.
    public void Connect(WayPoint wp, bool isOriginal)
    {
        bool sameNode = Equals(wp.GetPosition(), GetPosition());

        if (IsConnected(wp, isOriginal) || sameNode) return;

        AddEdge(wp, isOriginal);
        wp.AddEdge(this, isOriginal);
    }

    public void RemoveConnection(WayPoint wp, bool isOriginal)
    {
        RemoveEdge(wp, isOriginal);
        wp.RemoveEdge(this, isOriginal);
    }


    private void AddEdge(WayPoint wp, bool isOriginal)
    {
        if (isOriginal)
            _originalCons.Add(wp);
        else
            _connections.Add(wp);
    }

    public void RemoveEdge(WayPoint wp, bool isOriginal)
    {
        if (isOriginal)
            _originalCons.Remove(wp);
        else
            _connections.Remove(wp);

    }

    public bool IsConnected(WayPoint wp, bool isOriginal)
    {
        bool alreadyExists = GetConnections(isOriginal).Any(x => x.GetPosition() == wp.GetPosition());

        return alreadyExists;
    }


    // Check if the node is connected to more than one local maximum node.
    public bool EdgeOfLocalMaximum()
    {
        int count = 0;

        foreach (var con in GetConnections(true))
        {
            count += con.code == '*' && con.BlockId == BlockId ? 1 : 0;
        }

        return count <= 1 && code == '*';
    }

    public Vector2 GetPosition()
    {
        return _position;
    }

    public void AddLines(List<RoadMapLine> lines, bool isOriginal)
    {
        foreach (var line in lines)
        {
            if (line.IsPointPartOfLine(this))
                AddLine(line, isOriginal);
        }
    }

    private void AddLine(RoadMapLine line, bool isOriginal)
    {
        if (isOriginal)
            _originalLines.Add(line);
        else
            _mapLines.Add(line);
    }

    public void RemoveLine(RoadMapLine line)
    {
        _mapLines.Remove(line);
    }

    public List<RoadMapLine> GetLines(bool isOriginal)
    {
        if (isOriginal) return _originalLines;

        return _mapLines;
    }

    public float GetFvalue()
    {
        return hDistance + gDistance;
    }

    public void SetProbability(float probability)
    {
        _probabilityGuardPassing = probability;
    }

    public float GetProbability()
    {
        return _probabilityGuardPassing;
    }

    public List<WayPoint> GetConnections(bool isOriginal)
    {
        return isOriginal ? _originalCons : _connections;
    }


    public void Draw()
    {
        // string distances = "";
        // foreach (var distance in m_Distances)
        // {
        //     distances += distance + "\n";
        // }

        Handles.Label(GetPosition(), _probabilityGuardPassing.ToString());
        
        
    }
}