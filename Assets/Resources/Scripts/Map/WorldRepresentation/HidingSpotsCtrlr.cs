using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// The controller for the hiding spots the intruders can use
public class HidingSpotsCtrlr
{
    // the hiding spots
    private List<HidingSpot> m_HidingSpots;


    private PartitionGrid<HidingSpot> m_spots;


    public HidingSpotsCtrlr(List<Polygon> walls, Bounds bounds, int colCount, int rowCount)
    {
        m_HidingSpots = new List<HidingSpot>();

        m_spots = new PartitionGrid<HidingSpot>(bounds, colCount, rowCount);

        CreateHidingSpots(walls);
    }


    /// <summary>
    /// Place the spots on the map
    /// </summary>
    /// <param name="walls"></param>
    private void CreateHidingSpots(List<Polygon> walls)
    {
        foreach (var wall in walls)
        {
            for (int j = 0; j < wall.GetVerticesCount(); j++)
            {
                Vector2 angleNormal =
                    GeometryHelper.GetNormal(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1));

                // How long is the normal
                angleNormal *= 0.8f;

                // Inverse the sign for the inner polygons which are obstacles 
                if (GeometryHelper.IsReflex(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1)))
                {
                    // // Place one spot in the corner
                    Vector2 spotPosition = wall.GetPoint(j) - angleNormal;
                    PlaceHidingSpot(spotPosition, walls);
                }
                else
                {
                    // How long is the distance from the reflex edge
                    float distanceFromCorner = 1f;

                    // Minimum edge length to place a hiding spot
                    float minEdge = 0.5f;

                    float rightEdgeLength = Vector2.Distance(wall.GetPoint(j), wall.GetPoint(j - 1));
                    if (minEdge < rightEdgeLength)
                    {
                        // Place a spot on the right side of the corner
                        Vector2 rightSide = (wall.GetPoint(j) - wall.GetPoint(j - 1)).normalized * distanceFromCorner;
                        Vector2 rightSpotPosition = wall.GetPoint(j) + angleNormal - rightSide;
                        PlaceHidingSpot(rightSpotPosition, walls);
                    }

                    float leftEdgeLength = Vector2.Distance(wall.GetPoint(j + 1), wall.GetPoint(j));
                    if (minEdge < leftEdgeLength)
                    {
                        // Place a spot on the left side of the corner
                        Vector2 leftSide = (wall.GetPoint(j + 1) - wall.GetPoint(j)).normalized * distanceFromCorner;
                        Vector2 leftSpotPosition = wall.GetPoint(j) + angleNormal + leftSide;
                        PlaceHidingSpot(leftSpotPosition, walls);
                    }
                }
            }
        }
    }


    private void PlaceHidingSpot(Vector2 position, List<Polygon> interiorWalls)
    {
        // Make sure the position is inside the walls
        if (!PolygonHelper.IsPointInPolygons(interiorWalls, position)) return;

        HidingSpot hSr = new HidingSpot(position, Isovists.Instance.GetCoverRatio(position));

        m_HidingSpots.Add(hSr);
        m_spots.Add(hSr, hSr.Position);
    }

    private float GetAverageDistancesToHidingSpots(HidingSpot hidingSpot, List<PossiblePosition> possiblePositions)
    {
        float totalDistances = 0;
        foreach (var possiblePosition in possiblePositions)
            totalDistances += Vector2.Distance(possiblePosition.position, hidingSpot.Position) *
                              possiblePosition.safetyMultiplier;


        return totalDistances / (PathFinding.Instance.longestShortestPath * possiblePositions.Count);
    }

    private float GetMinDistanceToHidingSpots(HidingSpot hidingSpot, List<PossiblePosition> possiblePositions)
    {
        float minDistance = Mathf.Infinity;
        foreach (var possiblePosition in possiblePositions)
        {
            float distance = Vector2.Distance(possiblePosition.position, hidingSpot.Position) *
                             possiblePosition.safetyMultiplier;

            minDistance = (distance < minDistance) ? distance : minDistance;
        }

        return minDistance / PathFinding.Instance.longestShortestPath;
    }

    // Assign the fitness value of each hiding spot based on the guards distance to them
    public void AssignHidingSpotsFitness(List<Guard> guards)
    {
        foreach (var hidingSpot in m_HidingSpots)
        {
            hidingSpot.Fitness = 0f;
            foreach (var g in guards)
                hidingSpot.Fitness +=
                    PathFinding.Instance.GetShortestPathDistance(hidingSpot.Position, g.transform.position);
        }
    }

    public List<HidingSpot> GetHidingSpots(Vector3 position, int range)
    {
        return m_spots.GetPartitionsContent(position, range);
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
            spot.Draw();


        m_spots.Draw();
    }
}

public class HidingSpot
{
    public Vector2 Position;

    /// <summary>
    /// How well the spot is generally hidden in the map; 0 is easily observable, 1 is well hidden.
    /// </summary>
    public float CoverRatio;

    /// <summary>
    /// How close this spot to the goal
    /// </summary>
    public float GoalUtility;

    /// <summary>
    /// Utility of how occluded the spot from guards
    /// </summary>
    public float OcclusionUtility;


    public PossiblePosition ThreateningPosition;

    /// <summary>
    /// Utility of how safe this spot from potential guard movements
    /// </summary>
    public float SafetyUtility;

    // same value but before normalizing
    public float SafetyAbsoluteValue;


    /// <summary>
    /// Utility of how far this spot is from guards' current positions.
    /// </summary>
    public float GuardProximityUtility;

    /// <summary>
    /// Indicator of how good a hiding spot is; it is between 0 and 1.
    /// </summary>
    public float Fitness;

    // A flag if the spot is occluded from all guards on the map
    public bool IsOccludedFromGuards;

    public HidingSpot(Vector2 _position, float _coverRatio)
    {
        Position = _position;
        CoverRatio = _coverRatio;
        Fitness = 0f;
        ThreateningPosition = null;
    }

    public void Draw()
    {
        Gizmos.DrawSphere(Position + Vector2.down * 0.2f, 0.1f);

#if UNITY_EDITOR
        string label = "";
        label += "Safety: " + (Mathf.Round(SafetyUtility * 100f) / 100f) + " \n";
        label += "Goal: " + (Mathf.Round(GoalUtility * 100f) / 100f) + " \n";
        label += "GuardProximity: " + (Mathf.Round(GuardProximityUtility * 100f) / 100f) + " \n";
        label += "CoverRatio: " + (Mathf.Round(CoverRatio * 100f) / 100f) + " \n";
        Handles.Label(Position, label);
#endif
    }
}