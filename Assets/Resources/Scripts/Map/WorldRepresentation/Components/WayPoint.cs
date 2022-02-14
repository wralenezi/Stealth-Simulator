using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

public class WayPoint
{
    // Position
    private readonly Vector2 m_Position;

    // Current connections to the way point (including the intermediate way points introduced by dividing the edges into segments.
    private readonly List<WayPoint> m_Connections;

    // The divided line segments connected to this way point. 
    private readonly List<RoadMapLine> m_MapLines;

    // Original connections of the way points before dividing the edges into segments. 
    private readonly List<WayPoint> m_OriginalCons;

    // The original lines connected to this way point
    private readonly List<RoadMapLine> m_OriginalLines;

    // Distance to incoming possible intruder position
    private float m_Distance;

    // The ID of the way point
    public int Id;

    // The ID of the max block; used for the Skeletal axis transform algorithm.
    public int BlockId;

    // the ID of the wall this way point belong to. This is for the visibility graph
    public int WallId;

    // Which guard targeted this way point to intercept in. If it is null then there is no incoming guard
    public Guard InterceptingGuard;

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
        m_Position = _position;
        m_OriginalCons = new List<WayPoint>();
        m_Connections = new List<WayPoint>();
        m_MapLines = new List<RoadMapLine>();
        m_OriginalLines = new List<RoadMapLine>();
        Id = _id;
    }

    public WayPoint(Vector2 _position, int _row, int _col, char _code)
    {
        m_Position = _position;
        row = _row;
        col = _col;
        code = _code;
        m_OriginalCons = new List<WayPoint>();
        m_Connections = new List<WayPoint>();
        m_MapLines = new List<RoadMapLine>();
        m_OriginalLines = new List<RoadMapLine>();
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
            m_OriginalCons.Add(wp);
        else
            m_Connections.Add(wp);
    }

    public void RemoveEdge(WayPoint wp, bool isOriginal)
    {
        if (isOriginal)
            m_OriginalCons.Remove(wp);
        else
            m_Connections.Remove(wp);

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
        return m_Position;
    }

    public void SetDistance(float distance)
    {
        m_Distance = (distance);
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
            m_OriginalLines.Add(line);
        else
            m_MapLines.Add(line);
    }

    public void RemoveLine(RoadMapLine line)
    {
        m_MapLines.Remove(line);
    }

    public List<RoadMapLine> GetLines(bool isOriginal)
    {
        if (isOriginal) return m_OriginalLines;

        return m_MapLines;
    }

    public float GetFvalue()
    {
        return hDistance + gDistance;
    }


    public List<WayPoint> GetConnections(bool isOriginal)
    {
        return isOriginal ? m_OriginalCons : m_Connections;
    }

    // Insert the distance to the neighboring way points except the source
    public void InsertDistancesToNeighbors(WayPoint source, float totalDistance)
    {
        foreach (var wayPoint in m_Connections)
        {
            if (!source.GetPosition().Equals(wayPoint.GetPosition()))
                wayPoint.SetDistance(Vector2.Distance(GetPosition(), wayPoint.GetPosition()) + totalDistance);
        }
    }

    public void Draw()
    {
        // string distances = "";
        // foreach (var distance in m_Distances)
        // {
        //     distances += distance + "\n";
        // }

        // Handles.Label(GetPosition(), Id.ToString());
    }
}