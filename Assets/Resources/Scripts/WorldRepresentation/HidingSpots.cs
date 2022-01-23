using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// This is class for identifying the hiding spots for the intruder
public class HidingSpots : MonoBehaviour
{
    public bool IsRenderHidingSpots;

    private StealthArea m_stealthArea;

    // the hiding spots
    private List<HidingSpot> m_hidingSpots;

    // Initiate 
    public void Initiate(StealthArea _stealthArea)
    {
        m_stealthArea = _stealthArea;
        m_hidingSpots = new List<HidingSpot>();

        // CreateHidingSpots();
    }

    // Place the hiding spots
    // private void CreateHidingSpots()
    // {
    //     List<Polygon> walls = m_stealthArea.mapRenderer.GetWalls();
    //     for (int i = 0; i < walls.Count; i++)
    //     {
    //         for (int j = 0; j < walls[i].GetVerticesCount(); j++)
    //         {
    //             Polygon wall = walls[i];
    //             
    //             Vector2 angleNormal =
    //                 GeometryHelper.GetNormal(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1));
    //
    //             angleNormal *= 0.8f;
    //             float distanceFromCorner = 1f;
    //
    //             // Inverse the sign for the inner polygons which are obstacles 
    //             if (GeometryHelper.IsReflex(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1)))
    //             {
    //                 HidingSpot hS = new HidingSpot(wall.GetPoint(j) - angleNormal);
    //                 if (PolygonHelper.IsPointInPolygons(m_stealthArea.mapRenderer.GetInteriorWalls(), hS.Position))
    //                     m_hidingSpots.Add(hS);
    //             }
    //             else
    //             {
    //                 Vector2 rightSide = (wall.GetPoint(j) - wall.GetPoint(j - 1)).normalized * distanceFromCorner;
    //                 Vector2 leftSide = (wall.GetPoint(j + 1) - wall.GetPoint(j)).normalized * distanceFromCorner;
    //
    //                 HidingSpot hSr = new HidingSpot(wall.GetPoint(j) + angleNormal - rightSide);
    //                 HidingSpot hSl = new HidingSpot(wall.GetPoint(j) + angleNormal + leftSide);
    //
    //                 if (PolygonHelper.IsPointInPolygons(m_stealthArea.mapRenderer.GetInteriorWalls(), hSr.Position))
    //                     m_hidingSpots.Add(hSr);
    //
    //                 if (PolygonHelper.IsPointInPolygons(m_stealthArea.mapRenderer.GetInteriorWalls(), hSl.Position))
    //                     m_hidingSpots.Add(hSl);
    //             }
    //         }
    //
    //         // Calculate the longest possible path in the map
    //         // Compare the pair-wise path distance between the hiding spots along the outer wall.
    //         if (i == 0)
    //         {
    //             float maxDistance = Mathf.NegativeInfinity;
    //             for (int j = 0; j < m_hidingSpots.Count; j++)
    //             for (int k = j + 1; k < m_hidingSpots.Count; k++)
    //             {
    //                 float distance = PathFinding.GetShortestPathDistance(m_stealthArea.mapDecomposer.GetNavMesh(),
    //                     m_hidingSpots[j].Position, m_hidingSpots[k].Position);
    //
    //                 if (maxDistance < distance)
    //                     maxDistance = distance;
    //             }
    //
    //             Properties.MaxPathDistance = maxDistance;
    //             Debug.Log("Hiding Spots: "+Properties.MaxPathDistance);
    //         }
    //     }
    // }

    // Assign the fitness value of each hiding spot based on the guards distance to them
    public void AssignHidingSpotsFitness(List<Guard> guards, List<MeshPolygon> navMesh)
    {
        foreach (var hidingSpot in m_hidingSpots)
        {
            hidingSpot.Fitness = 0f;

            foreach (var g in guards)
                hidingSpot.Fitness +=
                    PathFinding.GetShortestPathDistance(hidingSpot.Position, g.transform.position);
        }
    }

    public Vector2? GetBestHidingSpot()
    {
        HidingSpot bestHidingSpot = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in m_hidingSpots)
        {
            if (hs.Fitness > maxFitness)
            {
                bestHidingSpot = hs;
                maxFitness = hs.Fitness;
            }
        }

        return bestHidingSpot.Position;
    }


    public Vector2 GetRandomHidingSpot()
    {
        int random = Random.Range(0, m_hidingSpots.Count);
        return m_hidingSpots[random].Position;
    }


    public void OnDrawGizmos()
    {
        if (IsRenderHidingSpots)
            foreach (var spot in m_hidingSpots)
            {
                Gizmos.DrawSphere(spot.Position, 0.1f);
            }
    }
}

