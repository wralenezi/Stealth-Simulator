using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The controller for the hiding spots the intruders can use
public class HidingSpotsCtrlr
{
    // the hiding spots
    private List<HidingSpot> m_HidingSpots;

    public HidingSpotsCtrlr(MapRenderer mapRndr)
    {
        m_HidingSpots = new List<HidingSpot>();

        CreateHidingSpots(mapRndr);
    }


    // Place the hiding spots
    private void CreateHidingSpots(MapRenderer mapRndr)
    {
        List<Polygon> walls = mapRndr.GetWalls();
        for (int i = 0; i < walls.Count; i++)
        {
            for (int j = 0; j < walls[i].GetVerticesCount(); j++)
            {
                Polygon wall = walls[i];
                Vector2 angleNormal =
                    GeometryHelper.GetNormal(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1));

                angleNormal *= 0.8f;
                float distanceFromCorner = 1f;

                // Inverse the sign for the inner polygons which are obstacles 
                if (GeometryHelper.IsReflex(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1)))
                {
                    HidingSpot hS = new HidingSpot(wall.GetPoint(j) - angleNormal);
                    if (PolygonHelper.IsPointInPolygons(mapRndr.GetInteriorWalls(), hS.Position))
                        m_HidingSpots.Add(hS);
                }
                else
                {
                    Vector2 rightSide = (wall.GetPoint(j) - wall.GetPoint(j - 1)).normalized * distanceFromCorner;
                    Vector2 leftSide = (wall.GetPoint(j + 1) - wall.GetPoint(j)).normalized * distanceFromCorner;

                    HidingSpot hSr = new HidingSpot(wall.GetPoint(j) + angleNormal - rightSide);
                    HidingSpot hSl = new HidingSpot(wall.GetPoint(j) + angleNormal + leftSide);

                    if (PolygonHelper.IsPointInPolygons(mapRndr.GetInteriorWalls(), hSr.Position))
                        m_HidingSpots.Add(hSr);

                    if (PolygonHelper.IsPointInPolygons(mapRndr.GetInteriorWalls(), hSl.Position))
                        m_HidingSpots.Add(hSl);
                }
            }
        }
    }


    /// <summary>
    /// Update the fitness of the hiding positions based on the guards' possible positions.
    /// </summary>
    public void UpdatePointsFitness(List<PossiblePosition> guardsPositions)
    {
        
    }


    // Get the best hiding spot based on its fitness
    public Vector2? GetBestHidingSpot()
    {
        HidingSpot bestHidingSpot = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in m_HidingSpots)
        {
            if (hs.Fitness > maxFitness)
            {
                bestHidingSpot = hs;
                maxFitness = hs.Fitness;
            }
        }

        return bestHidingSpot.Position;
    }


    public void DrawHidingSpots()
    {
        foreach (var spot in m_HidingSpots)
        {
            Gizmos.DrawSphere(spot.Position, 0.1f);
        }
    }
}

public class HidingSpot
{
    public Vector2 Position;
    public float Fitness;

    public HidingSpot(Vector2 _position)
    {
        Position = _position;
        Fitness = 0f;
    }
}