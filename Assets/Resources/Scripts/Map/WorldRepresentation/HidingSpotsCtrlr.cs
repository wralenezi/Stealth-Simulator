using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// The controller for the hiding spots the intruders can use
public class HidingSpotsCtrlr
{
    // the hiding spots
    private List<HidingSpot> m_HidingSpots;

    private PartitionGrid<HidingSpot> m_spots;

    private float NPC_RADIUS = 0.05f;

    public HidingSpotsCtrlr(List<Polygon> walls, Bounds bounds, int colCount, int rowCount)
    {
        m_HidingSpots = new List<HidingSpot>();

        m_spots = new PartitionGrid<HidingSpot>(bounds, colCount, rowCount);

        CreateHidingSpots(walls);

        PairHidingSpots();
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

                    HidingSpot leftSpot = null;
                    HidingSpot rightSpot = null;

                    float rightEdgeLength = Vector2.Distance(wall.GetPoint(j), wall.GetPoint(j - 1));
                    if (minEdge < rightEdgeLength)
                    {
                        // Place a spot on the right side of the corner
                        Vector2 rightSide = (wall.GetPoint(j) - wall.GetPoint(j - 1)).normalized * distanceFromCorner;
                        Vector2 rightSpotPosition = wall.GetPoint(j) + angleNormal - rightSide;
                        leftSpot = PlaceHidingSpot(rightSpotPosition, walls);
                    }

                    float leftEdgeLength = Vector2.Distance(wall.GetPoint(j + 1), wall.GetPoint(j));
                    if (minEdge < leftEdgeLength)
                    {
                        // Place a spot on the left side of the corner
                        Vector2 leftSide = (wall.GetPoint(j + 1) - wall.GetPoint(j)).normalized * distanceFromCorner;
                        Vector2 leftSpotPosition = wall.GetPoint(j) + angleNormal + leftSide;
                        rightSpot = PlaceHidingSpot(leftSpotPosition, walls);
                    }

                    if (!Equals(leftSpot, null) && !Equals(rightSpot, null))
                    {
                        leftSpot.reflexNeighbour = rightSpot;
                        rightSpot.reflexNeighbour = leftSpot;
                        leftSpot.PairHidingSpots(rightSpot);
                    }
                }
            }
        }
    }


    private void PairHidingSpots()
    {
        for (int i = 0; i < m_HidingSpots.Count; i++)
        {
            HidingSpot currentSpot = m_HidingSpots[i];
            int visibleSpotsCount = 0;

            for (int j = i + 1; j < m_HidingSpots.Count; j++)
            {
                HidingSpot possibleNeighbour = m_HidingSpots[j];

                bool isVisible =
                    GeometryHelper.IsCirclesVisible(currentSpot.Position, possibleNeighbour.Position, NPC_RADIUS,
                        "Wall");

                if (!isVisible) continue;

                currentSpot.PairHidingSpots(possibleNeighbour);

                if (!Equals(possibleNeighbour.reflexNeighbour, null))
                    currentSpot.PairHidingSpots(possibleNeighbour.reflexNeighbour);

                visibleSpotsCount++;
            }

            currentSpot.VisibleSpotsCount = visibleSpotsCount;
        }


        foreach (var currentSpot in m_HidingSpots)
            currentSpot.OcclusionUtility = 1f - currentSpot.VisibleSpotsCount / m_HidingSpots.Count;
    }

    public void GetSpotsOfInterest(Vector2 intruderPosition, ref List<HidingSpot> hidingSpots)
    {
        hidingSpots.Clear();

        float shortestSqrDistance = Mathf.Infinity;
        HidingSpot closestSpot = null;
        foreach (var spot in m_HidingSpots)
        {
            Vector2 offset = intruderPosition - spot.Position;
            float sqrDistance = offset.SqrMagnitude();
            if (shortestSqrDistance > sqrDistance)
            {
                shortestSqrDistance = sqrDistance;
                closestSpot = spot;
            }
        }

        if (Equals(closestSpot, null)) return;

        hidingSpots.Add(closestSpot);
        List<HidingSpot> neighbours = closestSpot.GetNeighbours();
        foreach (var n in neighbours)
            hidingSpots.Add(n);
    }

    private HidingSpot PlaceHidingSpot(Vector2 position, List<Polygon> interiorWalls)
    {
        // Make sure the position is inside the walls
        if (!PolygonHelper.IsPointInPolygons(interiorWalls, position)) return null;

        HidingSpot hSr = new HidingSpot(position, Isovists.Instance.GetCoverRatio(position));

        m_HidingSpots.Add(hSr);
        m_spots.Add(hSr, hSr.Position);

        return hSr;
    }

    private float GetAverageDistancesToHidingSpots(HidingSpot hidingSpot, List<PossiblePosition> possiblePositions)
    {
        float totalDistances = 0;
        foreach (var possiblePosition in possiblePositions)
            totalDistances += Vector2.Distance(possiblePosition.GetPosition().Value, hidingSpot.Position) *
                              possiblePosition.safetyMultiplier;


        return totalDistances / (PathFinding.Instance.longestShortestPath * possiblePositions.Count);
    }

    private float GetMinDistanceToHidingSpots(HidingSpot hidingSpot, List<PossiblePosition> possiblePositions)
    {
        float minDistance = Mathf.Infinity;
        foreach (var possiblePosition in possiblePositions)
        {
            float distance = Vector2.Distance(possiblePosition.GetPosition().Value, hidingSpot.Position) *
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
    /// Utility of how risky this spot from potential guard movements
    /// </summary>
    public float RiskLikelihood;

    /// <summary>
    /// How close this spot to the goal
    /// </summary>
    public float GoalUtility;

    /// <summary>
    /// The cost of navigating to this spot 
    /// </summary>
    public float CostUtility;

    /// <summary>
    /// How well the spot is generally hidden in the map; 0 is easily observable, 1 is well hidden.
    /// </summary>
    public float CoverUtility;

    /// <summary>
    /// The number of visible hiding spots from this spot.
    /// </summary>
    public float VisibleSpotsCount;

    /// <summary>
    /// How well occluded this spot compare to the other spots, 0 means this spot is visible by all other spots, and 1 means no spots can see this spot.
    /// </summary>
    public float OcclusionUtility;

    /// <summary>
    /// Utility of how far this spot is from guards' current positions.
    /// </summary>
    public float GuardProximityUtility;

    /// <summary>
    /// Indicator of how good a hiding spot is; it is between 0 and 1.
    /// </summary>
    public float Fitness;

    public PossiblePosition ThreateningPosition;

    public float lastFailedTimeStamp;

    /// <summary>
    /// Visible and neighbouring hiding spot
    /// </summary>
    public List<HidingSpot> _neighbouringSpots;

    public HidingSpot reflexNeighbour;

    public HidingSpot(Vector2 _position, float _coverRatio)
    {
        Position = _position;
        CoverUtility = _coverRatio;
        Fitness = 0f;
        ThreateningPosition = null;
        _neighbouringSpots = new List<HidingSpot>();
        reflexNeighbour = null;
        lastFailedTimeStamp = 0f;
    }

    public void PairHidingSpots(HidingSpot spot)
    {
        AddNeighbour(spot);
        spot.AddNeighbour(this);
    }

    public void AddNeighbour(HidingSpot spot)
    {
        if (!_neighbouringSpots.Contains(spot))
            _neighbouringSpots.Add(spot);
    }

    public List<HidingSpot> GetNeighbours()
    {
        return _neighbouringSpots;
    }


    public void Draw()
    {
        Gizmos.DrawSphere(Position + Vector2.down * 0.2f, 0.1f);

#if UNITY_EDITOR
        string label = "";
        label += "Risk: " + (Mathf.Round(RiskLikelihood * 100f) / 100f) + " \n";
        label += "Goal: " + (Mathf.Round(GoalUtility * 100f) / 100f) + " \n";
        label += "Cost: " + (Mathf.Round(CostUtility * 100f) / 100f) + " \n";
        // label += "Occlusion: " + (Mathf.Round(OcclusionUtility * 100f) / 100f) + " \n";
        // label += "CoverRatio: " + (Mathf.Round(CoverUtility * 100f) / 100f) + " \n";
        Handles.Label(Position, label);
#endif
    }
}