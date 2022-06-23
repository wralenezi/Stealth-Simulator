using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RoadMapNode
{
    // Position
    private readonly Vector2 _position;

    // Current connections to the way point (including the intermediate way points introduced by dividing the edges into segments.
    private readonly List<RoadMapNode> _connections;

    // The divided line segments connected to this way point. 
    private readonly List<RoadMapLine> _mapLines;

    // Original connections of the way points before dividing the edges into segments. 
    private readonly List<RoadMapNode> _originalCons;

    private readonly List<RoadMapNode> _fixedCons;

    // The original lines connected to this way point
    private List<RoadMapLine> _originalLines;

    // probability that a guard is going to pass through here; when it is zero
    private float _probabilityGuardPassing;
    public float distanceFromGuard;
    private NPC _passingGuard;

    // The ID of the way point
    public int Id;

    // The ID of the max block; used for the Skeletal axis transform algorithm.
    public int BlockId;

    // the ID of the wall this way point belong to. This is for the visibility graph
    public int WallId;

    public NodeType type;
    
    // The node original location on the grid. Used in the grid simplification to a graph.
    public int row;
    public int col;
    public char code;

    // the variables for A*
    public float hDistance;
    public float gDistance;
    public RoadMapNode parent;

    public RoadMapNode(Vector2 _position, int _id = 0)
    {
        this._position = _position;
        _originalCons = new List<RoadMapNode>();
        _fixedCons = new List<RoadMapNode>();
        _connections = new List<RoadMapNode>();
        _mapLines = new List<RoadMapLine>();
        _originalLines = new List<RoadMapLine>();
        _probabilityGuardPassing = 0f;
        _passingGuard = null;
        type = NodeType.RoadMap;
        Id = _id;
    }

    public RoadMapNode(Vector2 _position, int _row, int _col, char _code)
    {
        this._position = _position;
        row = _row;
        col = _col;
        code = _code;
        _originalCons = new List<RoadMapNode>();
        _fixedCons = new List<RoadMapNode>();
        _connections = new List<RoadMapNode>();
        _mapLines = new List<RoadMapLine>();
        _originalLines = new List<RoadMapLine>();
        _passingGuard = null;
        type = NodeType.RoadMap;
        Id = 0;
    }

    public void SafeCons()
    {
        _fixedCons.Clear();
        foreach (var con in _originalCons)
        {
            _fixedCons.Add(con);
        }
    }

    public void LoadCons()
    {
        _originalCons.Clear();
        foreach (var con in _fixedCons)
        {
            _originalCons.Add(con);
        }
    }


    // Add the way points to each others list of connects.
    public void Connect(RoadMapNode wp, bool isOriginal, bool isOverwrite)
    {
        bool sameNode = Equals(wp.GetPosition(), GetPosition());

        if (IsConnected(wp, isOriginal) || (sameNode && !isOverwrite)) return;

        AddEdge(wp, isOriginal);
        wp.AddEdge(this, isOriginal);
    }

    public void RemoveConnection(RoadMapNode wp, bool isOriginal)
    {
        RemoveEdge(wp, isOriginal);
        wp.RemoveEdge(this, isOriginal);
    }


    private void AddEdge(RoadMapNode wp, bool isOriginal)
    {
        if (isOriginal)
            _originalCons.Add(wp);
        else
            _connections.Add(wp);
    }

    public void RemoveEdge(RoadMapNode wp, bool isOriginal)
    {
        if (isOriginal)
            _originalCons.Remove(wp);
        else
            _connections.Remove(wp);
    }

    public bool IsConnected(RoadMapNode wp, bool isOriginal)
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

    public void AddLine(RoadMapLine line, bool isOriginal)
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
        return isOriginal ? _originalLines : _mapLines;
    }

    public RoadMapLine GetLineWithWp(RoadMapNode wp, bool isOriginal)
    {
        List<RoadMapLine> lines = isOriginal ? _originalLines : _mapLines;

        foreach (var line in lines)
        {
            if (line.IsPointPartOfLine(this) && line.IsPointPartOfLine(wp))
                return line;
        }

        return null;
    }

    public float GetFvalue()
    {
        return hDistance + gDistance;
    }

    public void SetProbability(NPC passingGuard, float probability)
    {
        _probabilityGuardPassing = probability;
        _passingGuard = passingGuard;
    }

    public float GetProbability()
    {
        return _probabilityGuardPassing;
    }

    public NPC GetPassingGuard()
    {
        return _passingGuard;
    }

    public List<RoadMapNode> GetConnections(bool isOriginal)
    {
        return isOriginal ? _originalCons : _connections;
    }


    public void Draw(string label)
    {
        Handles.Label(GetPosition(), label);
        Gizmos.DrawSphere(GetPosition(), 0.1f);
    }
}

public enum NodeType
{
    RoadMap,

    Corner
}