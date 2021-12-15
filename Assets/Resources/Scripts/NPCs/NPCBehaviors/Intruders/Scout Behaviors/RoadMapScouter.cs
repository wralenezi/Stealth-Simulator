using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadMapScouter : Scouter
{
    // Road map of the level
    public bool showRoadmap;
    private RoadMap m_RoadMap;

    // Hiding spots manager
    public bool ShowHidingSpots;
    private HidingSpotsCtrlr m_HsC;


    // Possible positions
    public bool showProjectedPositions;
    private List<PossiblePosition> m_PossiblePositions;

    // The distance of the possible position of the guards; the further it is the more cautious the intruder will be
    private float m_ProjectionDist = 6f;

    // The count of possible positions the will be distributed on the projection
    private int m_positionsCount = 2;

    public override void Initiate(StealthArea stealthArea)
    {
        base.Initiate(stealthArea);

        m_RoadMap = stealthArea.roadMap;
        m_HsC = new HidingSpotsCtrlr(GetSA().mapRenderer);
        m_PossiblePositions = new List<PossiblePosition>();

        // ShowHidingSpots = true;
    }

    private void Update()
    {
        ProjectGuardPositions();
    }


    /// <summary>
    /// Project the guards position on the road map.
    /// </summary>
    public void ProjectGuardPositions()
    {
        m_PossiblePositions.Clear();

        foreach (var guard in GetSA().guardsManager.GetGuards())
        {
            // Get the closest point on the road map to the guard
            Vector2? point = m_RoadMap.GetClosestProjection(guard.GetTransform().position, out RoadMapLine line);

            // if there is no intersection then abort
            if (!point.HasValue) return;

            // Place the possible positions a guard can occupy in the future.
            m_RoadMap.ProjectPositionsInDirection(ref m_PossiblePositions, point.Value, line, m_positionsCount,
                m_ProjectionDist, guard);
            
            // Update the fitness values of the hiding spots
            m_HsC.UpdatePointsFitness(m_PossiblePositions);
        }
    }


    public void OnDrawGizmos()
    {
        if (ShowHidingSpots)
            m_HsC?.DrawHidingSpots();

        if (showRoadmap)
            m_RoadMap.DrawRoadMap();

        if (showProjectedPositions)
        {
            foreach (var psblPstn in m_PossiblePositions)
            {
                byte alpha = (byte) (55 + 200 * (1f - psblPstn.distance / m_ProjectionDist));
                Handles.Label(psblPstn.position, psblPstn.distance.ToString());
                Gizmos.color = new Color32(255, 0, 0, alpha);
                Gizmos.DrawSphere(psblPstn.position, 0.5f);
            }
        }
    }
}


public class PossiblePosition
{
    public Vector2 position;

    // Distance from the NPC
    public float distance;

    // The NPC this possible position belong to
    public NPC npc;

    public PossiblePosition(Vector2 _position, NPC _npc, float _distance)
    {
        position = _position;
        distance = _distance;
        npc = _npc;
    }
}