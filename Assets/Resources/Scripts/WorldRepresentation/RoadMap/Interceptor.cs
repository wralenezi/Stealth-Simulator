using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Interceptor : MonoBehaviour
{
    private StealthArea m_StealthArea;

    // The distance the interception points are placed on
    private float m_futureDistance = 8f;

    // Interception points; they model the intruder possible positions across the road map
    private List<InterceptionPoint> m_interceptionPoints;

    // Road map and data needed for the interceptor
    private RoadMap m_roadMap;

    public bool IsRenderInterceptionPoints;

    public bool RenderRoadMap;

    // Start is called before the first frame update
    public void Initiate(StealthArea stealthArea)
    {
        m_interceptionPoints = new List<InterceptionPoint>();
        m_StealthArea = stealthArea;
        m_roadMap = m_StealthArea.roadMap;
    }

    // Anticipate future positions based on the distance measure from the current position for the pursuit phase
    public void CreatePossiblePositions(Vector2 position, Vector2 dir)
    {
        Clear();

        // Get the projection point 
        // InterceptionPoint phNode = m_roadMap.GetInterceptionPointOnRoadMap(position, dir);

        // PlacePossiblePositions(phNode, 0, m_futureDistance);
    }

    public Vector2 GetRandomRoadMapNode()
    {
        return m_roadMap.GetRandomRoadMapNode();
    }

    // Return interception points and if all are visited propagate them and return the new interceptions points
    public List<InterceptionPoint> GetPossiblePositions()
    {
        return m_interceptionPoints;
    }

    // The search is over so clear the variables
    public void Clear()
    {
        m_interceptionPoints.Clear();
    }


    // Add priorities to the interception point
    // public void AddDistancesToEndNodes(InterceptionPoint iP)
    // {
    //     float minDistance = Mathf.Infinity;
    //
    //     foreach (var endPoint in m_endPoints)
    //     {
    //         float distance = PathFinding.GetShortestPathDistance(m_roadMap, iP, endPoint);
    //
    //         if (distance < minDistance)
    //         {
    //             minDistance = distance;
    //         }
    //     }
    //
    //
    //     // Add the distance to closest end node to the interception point
    //     iP.distanceToEndNode = minDistance;
    // }


    // Expand the possible future positions in the possible direction of the interception point; this places stationary interception points for the case of pursuit
    public void PlacePossiblePositions(InterceptionPoint phNode, int generation, float distance)
    {
        // Get the distance from the interception point to the next node on the road map
        float distanceToDestination = Vector2.Distance(phNode.position, phNode.destination.GetPosition());

        Vector2 direction = (phNode.destination.GetPosition() - phNode.source.GetPosition()).normalized;

        // Don't add anything if this is at the end line.
        if (distanceToDestination == 0f)
            return;

        // If the distance to expand to the interception point is shorter then simply insert it. 
        if (distanceToDestination >= distance)
        {
            InterceptionPoint newPhNode = new InterceptionPoint(phNode.position + direction * distance,
                phNode.destination, phNode.source, generation);

            // AddDistancesToEndNodes(newPhNode);
            m_interceptionPoints.Add(newPhNode);
        }
        // If not then propagate the nodes through the road map, then propagate to other nodes.
        else
        {
            float remainingDistance = distance - distanceToDestination;

            if (phNode.destination.GetConnections().Count == 1)
            {
                InterceptionPoint newPhNode = new InterceptionPoint(phNode.destination.GetPosition(),
                    phNode.destination, phNode.source, generation);

                // Add metrics to the interception point
                // AddDistancesToEndNodes(newPhNode);
                m_interceptionPoints.Add(newPhNode);
                return;
            }

            // for each connection recursively place the possible position
            foreach (var wayPoint in phNode.destination.GetConnections())
            {
                if (phNode.source == wayPoint)
                    continue;

                InterceptionPoint newPhNode = new InterceptionPoint(phNode.destination.GetPosition(),
                    wayPoint, phNode.destination, generation);

                PlacePossiblePositions(newPhNode, generation, remainingDistance);
            }
        }

        // Remove interception points that are too close
        AggregateInterceptionPoints();
    }


    public void AggregateInterceptionPoints()
    {
        float mergeThreshold = 3f;
        for (int i = 0; i < m_interceptionPoints.Count; i++)
        {
            InterceptionPoint iP1 = m_interceptionPoints[i];
            for (int j = i + 1; j < m_interceptionPoints.Count; j++)
            {
                InterceptionPoint iP2 = m_interceptionPoints[j];

                float distance = Vector2.Distance(iP1.position, iP2.position);

                if (iP1.destination == iP2.destination && iP1.source == iP2.source && mergeThreshold >= distance)
                {
                    // m_interceptionPoints.RemoveAt(j);
                    // j--;
                }
            }
        }
    }


    public void OnDrawGizmos()
    {
        if (IsRenderInterceptionPoints)
        {
            if (m_interceptionPoints != null)
                foreach (var iP in m_interceptionPoints)
                {
                    iP.DrawInterceptionPoint();
                }
        }

        if (RenderRoadMap)
            if (m_roadMap != null)
                m_roadMap.DrawRoadMap();
    }


    // Loop through the interception points and mark them as visited if they are in certain range from guards
    // public void MarkVisitedInterceptionPoints(List<Guard> guards, IState state)
    // {
    //     for (int i = 0; i < m_interceptionPoints.Count; i++)
    //     {
    //         if (state is Chase)
    //         {
    //             foreach (var guard in guards)
    //                 if (Vector2.Distance(guard.transform.position, m_interceptionPoints[i].position) < 2f)
    //                 {
    //                     VisitInterceptionPoint(m_interceptionPoints[i]);
    //                     i--;
    //                     break;
    //                 }
    //         }
    //         else if (state is Search)
    //             foreach (var guard in guards)
    //                 if (guard.IsNodeInFoV(m_interceptionPoints[i].position))
    //                 {
    //                     VisitInterceptionPoint(m_interceptionPoints[i]);
    //                     i--;
    //                     break;
    //                 }
    //     }
    // }


    // public void VisitInterceptionPoint(InterceptionPoint iP)
    // {
    //     PlacePossiblePositions(iP, iP.generationIndex + 1, m_futureDistance);
    //     m_interceptionPoints.Remove(iP);
    // }
}

// Interception point class
public class InterceptionPoint
{
    // Position of the phantom node
    public Vector2 position;

    // Direction of the phantom node's movement
    public Vector2 direction;

    //- Begin - Heuristics ****

    // portability the intruder is in that interception
    public float probability;

    // Interception point generation; the less it is the older the interception point existed
    public int generationIndex;

    // distance to closest end node on the road map
    public float distanceToEndNode;

    // - End - Heuristics *****

    // Destination of the phantom node (the next node on the road map)
    public WayPoint destination;

    // The direction the phantom is coming from; to prevent the propagation from going backwards
    public WayPoint source;

    public InterceptionPoint(Vector2 _position, WayPoint dest, WayPoint src, int _generationIndex)
    {
        position = _position;
        direction = (dest.GetPosition() - src.GetPosition()).normalized;
        destination = dest;
        source = src;
        direction = dest.GetPosition() - src.GetPosition();
        generationIndex = _generationIndex;
    }

    public void DrawInterceptionPoint()
    {
        DrawArrow.ForGizmo(position, direction, 0.2f);
        // Handles.Label(position + Vector2.up, (probability).ToString());
    }
}