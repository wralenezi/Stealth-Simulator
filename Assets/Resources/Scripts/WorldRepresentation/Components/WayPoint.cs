using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

public class WayPoint
{
    // Position
    private readonly Vector2 m_Position;

    private readonly List<WayPoint> m_Connections;

    private readonly List<RoadMapLine> m_MapLines;

    // Distance to incoming possible intruder position
    private float m_Distance;
    
    // The ID of the way point
    public readonly int Id;

    // the ID of the wall this way point belong to. This is for the visibility graph
    public int WallId;

    // Which guard targeted this way point to intercept in. If it is null then there is no incoming guard
    public Guard InterceptingGuard;

    // the variables for A*
    public float hDistance;
    public float gDistance;
    public WayPoint parent;

    public WayPoint(Vector2 _position, int _id = 0)
    {
        m_Position = _position;
        m_Connections = new List<WayPoint>();
        m_MapLines = new List<RoadMapLine>();
        Id = _id;
    }

    public void AddEdge(WayPoint wp)
    {
        m_Connections.Add(wp);
    }

    public void RemoveEdge(WayPoint wp)
    {
        m_Connections.Remove(wp);
    }

    public Vector2 GetPosition()
    {
        return m_Position;
    }

    public void SetDistance(float distance)
    {
        m_Distance = (distance);
    }
    
    public void AddLines(List<RoadMapLine> lines)
    {
        foreach (var line in lines)
        {
            if (line.IsPointPartOfLine(this))
                AddLine(line);
        }
    }

    public void AddLine(RoadMapLine line)
    {
        m_MapLines.Add(line);
    }

    public void RemoveLine(RoadMapLine line)
    {
        m_MapLines.Remove(line);
    }

    public List<RoadMapLine> GetLines()
    {
        return m_MapLines;
    }

    public float GetFvalue()
    {
        return hDistance + gDistance;
    }


    public List<WayPoint> GetConnections()
    {
        return m_Connections;
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

        // Handles.Label(GetPosition(), m_Distance.ToString());
    }
}