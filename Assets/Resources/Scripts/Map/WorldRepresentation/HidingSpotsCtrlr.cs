using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The controller for the hiding spots the intruders can use
public class HidingSpotsCtrlr
{
    private List<HidingSpot> _tempSpots;

    // the hiding spots
    private List<HidingSpot> _allSpots;
    private PartitionGrid<HidingSpot> _partitionedSpots;

    public static float DistanceMultiplier { get; } = 2f;

    public HidingSpotsCtrlr(MapManager mapManager, Bounds bounds, int colCount, int rowCount)
    {
        _tempSpots = new List<HidingSpot>();
        _allSpots = new List<HidingSpot>();
        _partitionedSpots = new PartitionGrid<HidingSpot>(bounds, colCount, rowCount);


        CreateHidingSpots(false, mapManager);
        PairHidingSpots();

        foreach (var spot in _allSpots)
        {
            // SetDeadEndProximity(spot, mapManager.GetRoadMap(), mapManager.mapRenderer);
        }
    }


    /// <summary>
    /// Place the spots on the map
    /// </summary>
    /// <param name="walls"></param>
    private void
        CreateHidingSpots(bool includeConvexAngels, MapManager mapManager) // List<Polygon> walls)
    {
        foreach (var wall in mapManager.GetWalls())
        {
            for (int j = 0; j < wall.GetVerticesCount(); j++)
            {
                Vector2 angleNormal =
                    GeometryHelper.GetNormal(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1));

                // How long is the normal
                angleNormal *= Properties.NpcRadius * DistanceMultiplier;

                // Inverse the sign for the inner polygons which are obstacles 
                bool isConvex = GeometryHelper.IsReflex(wall.GetPoint(j - 1), wall.GetPoint(j), wall.GetPoint(j + 1));

                if (isConvex)
                {
                    // Place one spot in the corner
                    Vector2 spotPosition = wall.GetPoint(j) - angleNormal;
                    PlaceHidingSpot(spotPosition, mapManager.GetRoadMap(), mapManager);
                }
                else
                {
                    // How long is the distance from the reflex edge
                    float distanceFromCorner = Properties.NpcRadius * DistanceMultiplier * 3f;

                    // Minimum edge length to place a hiding spot
                    float minEdge = distanceFromCorner;

                    HidingSpot leftSpot = null;
                    HidingSpot rightSpot = null;

                    float rightEdgeLength = Vector2.Distance(wall.GetPoint(j), wall.GetPoint(j - 1));
                    if (minEdge < rightEdgeLength)
                    {
                        // Place a spot on the right side of the corner
                        Vector2 rightSide = (wall.GetPoint(j) - wall.GetPoint(j - 1)).normalized * distanceFromCorner;
                        Vector2 rightSpotPosition = wall.GetPoint(j) + angleNormal - rightSide;
                        leftSpot = PlaceHidingSpot(rightSpotPosition, mapManager.GetRoadMap(), mapManager);
                    }

                    float leftEdgeLength = Vector2.Distance(wall.GetPoint(j + 1), wall.GetPoint(j));
                    if (minEdge < leftEdgeLength)
                    {
                        // Place a spot on the left side of the corner
                        Vector2 leftSide = (wall.GetPoint(j + 1) - wall.GetPoint(j)).normalized * distanceFromCorner;
                        Vector2 leftSpotPosition = wall.GetPoint(j) + angleNormal + leftSide;
                        rightSpot = PlaceHidingSpot(leftSpotPosition, mapManager.GetRoadMap(), mapManager);
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
        for (int i = 0; i < _allSpots.Count; i++)
        {
            HidingSpot currentSpot = _allSpots[i];
            int visibleSpotsCount = 0;

            for (int j = i + 1; j < _allSpots.Count; j++)
            {
                HidingSpot possibleNeighbour = _allSpots[j];

                bool isVisible =
                    GeometryHelper.IsCirclesVisible(currentSpot.Position, possibleNeighbour.Position,
                        Properties.NpcRadius,
                        "Wall");

                if (!isVisible) continue;

                currentSpot.PairHidingSpots(possibleNeighbour);

                if (!Equals(possibleNeighbour.reflexNeighbour, null))
                    currentSpot.PairHidingSpots(possibleNeighbour.reflexNeighbour);

                visibleSpotsCount++;
            }

            currentSpot.VisibleSpotsCount = visibleSpotsCount;
        }


        foreach (var currentSpot in _allSpots)
            currentSpot.OcclusionUtility = 1f - currentSpot.VisibleSpotsCount / _allSpots.Count;
    }

    public void AddAvailableSpots(HidingSpot hidingSpot, ref List<HidingSpot> hidingSpots)
    {
        hidingSpots.Add(hidingSpot);

        List<HidingSpot> neighbours = hidingSpot.GetNeighbours();
        foreach (var n in neighbours)
            if (!hidingSpots.Contains(n))
                hidingSpots.Add(n);
    }

    public void AddRandomSpots(HidingSpot hidingSpot, ref List<HidingSpot> hidingSpots, int numOfSpots)
    {
        List<HidingSpot> neighbours = hidingSpot.GetNeighbours();

        hidingSpots.Add(hidingSpot);

        int counter = numOfSpots;
        while (counter > 0)
        {
            int index = Random.Range(0, neighbours.Count);
            if (!hidingSpots.Contains(neighbours[index])) hidingSpots.Add(neighbours[index]);
            counter--;
        }
    }

    public HidingSpot GetClosestHidingSpotToPosition(Vector2 position)
    {
        float shortestSqrDistance = Mathf.Infinity;
        HidingSpot closestSpot = null;
        foreach (var spot in _allSpots)
        {
            if (!GeometryHelper.IsCirclesVisible(position, spot.Position, Properties.NpcRadius, "Wall")
            ) continue;

            Vector2 offset = position - spot.Position;
            float sqrDistance = offset.SqrMagnitude();
            if (shortestSqrDistance > sqrDistance)
            {
                shortestSqrDistance = sqrDistance;
                closestSpot = spot;
            }
        }

        return closestSpot;
    }

    public List<HidingSpot> GetHidingSpots()
    {
        return _allSpots;
    }

    private HidingSpot
        PlaceHidingSpot(Vector2 position, RoadMap roadMap, MapManager mapManager) // List<Polygon> interiorWalls)
    {
        // Make sure the position is inside the walls
        if (!PolygonHelper.IsPointInPolygons(mapManager.mapRenderer.GetWalls(), position)) return null;

        HidingSpot hSr = new HidingSpot(position, Isovists.Instance.GetCoverRatio(position));

        // SetDeadEndProximity(hSr, roadMap, mapManager.mapRenderer);

        _allSpots.Add(hSr);
        _partitionedSpots.Add(hSr, hSr.Position);

        return hSr;
    }


    // private void SetDeadEndProximity(HidingSpot hs, RoadMap roadMap, MapRenderer mapRenderer)
    // {
    //     float minSqrMag = Mathf.Infinity;
    //     RoadMapNode closestNode = null;
    //
    //     foreach (var node in roadMap.GetNode(true).Where(x => !Equals(x.type, NodeType.Corner)))
    //     {
    //         Vector2 offset = hs.Position - node.GetPosition();
    //         float sqrMag = offset.sqrMagnitude;
    //
    //         // float distance = PathFinding.Instance.GetShortestPathDistance(hs.Position, node.GetPosition()); 
    //
    //         if (sqrMag < minSqrMag)
    //         {
    //             bool isVisible =
    //                 GeometryHelper.IsCirclesVisible(hs.Position, node.GetPosition(), Properties.NpcRadius, "Wall");
    //             if (!isVisible) continue;
    //
    //             minSqrMag = sqrMag;
    //             closestNode = node;
    //         }
    //     }
    //
    //     bool isDeadEnd = closestNode.GetConnections(true).Where(x => !Equals(x.type, NodeType.Corner)).Count() == 1;
    //
    //     hs.DeadEndProximity = isDeadEnd ? 0f : 1f;
    // }


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
        foreach (var hidingSpot in _allSpots)
        {
            hidingSpot.Fitness = 0f;
            foreach (var g in guards)
            {
                Vector2 offset = (Vector2) g.GetTransform().position - hidingSpot.Position;
                hidingSpot.Fitness += offset.sqrMagnitude;
            }
        }
    }

    public void AddHidingSpots(ref List<HidingSpot> spots, Vector3 position, int range)
    {
        spots.AddRange(_partitionedSpots.GetPartitionsContent(position, range));
    }


    // Get the best hiding spot based on its fitness
    public Vector2? GetBestHidingSpot()
    {
        HidingSpot bestHidingSpot = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in _allSpots)
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
        foreach (var spot in _allSpots)
            spot.Draw();


        // _partitionedSpots.Draw();
    }
}

public class HidingSpot
{
    public Vector2 Position;

    /// <summary>
    /// Utility of how risky this spot from potential guard movements
    /// </summary>
    public float Risk;

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
    /// Utility of how far this spot is from guards' current positions. Closer to zero means there is a guard that is very close to it, while closer to one is the furthest from all guards.
    /// </summary>
    public float GuardProximityUtility;


    /// <summary>
    /// How close this point is to a dead end node on the road map. 1 is the furthest from a dead end, and 0 is the closest
    /// </summary>
    public float DeadEndProximity;

    /// <summary>
    /// Indicator of how good a hiding spot is; it is between 0 and 1.
    /// </summary>
    public float Fitness;

    private const float CoolDownInSeconds = 1f;
    private float lastCheckTimestamp;

    public PossiblePosition ThreateningPosition;

    // public float lastFailedTimeStamp;

    public RoadMapNode ClosestRMGuardNode;

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
        lastCheckTimestamp = 0f;
    }

    public void ResetCheck()
    {
        lastCheckTimestamp = 0f;
    }

    public void MarkAsChecked()
    {
        lastCheckTimestamp = StealthArea.GetElapsedTimeInSeconds();
    }

    public bool IsAlreadyChecked()
    {
        float timeDiff = StealthArea.GetElapsedTimeInSeconds() - lastCheckTimestamp;

        return timeDiff < CoolDownInSeconds;
    }

    public void PairHidingSpots(HidingSpot spot)
    {
        AddNeighbour(spot);
        spot.AddNeighbour(this);
    }

    public float WeightedFitness()
    {
        float occlusionWeight = 0.3f;
        float guardApproxWeight = 0.3f;
        float coverWeight = 1f - (occlusionWeight + guardApproxWeight);

        return OcclusionUtility * occlusionWeight + GuardProximityUtility * guardApproxWeight +
               CoverUtility * coverWeight;
    }

    public float WeightedOcclusion()
    {
        float occlusionWeight = 0.4f;
        float coverWeight = 1f - occlusionWeight;

        return OcclusionUtility * occlusionWeight + CoverUtility * coverWeight;
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
        Gizmos.DrawSphere(Position, Properties.NpcRadius);

#if UNITY_EDITOR
        string label = "";
        label += "Risk: " + (Mathf.Round(Risk * 100f) / 100f) + " \n";
        // label += "Goal: " + (Mathf.Round(GoalUtility * 100f) / 100f) + " \n";
        // label += "Cost: " + (Mathf.Round(CostUtility * 100f) / 100f) + " \n";
        // label += "Occlusion: " + (Mathf.Round(OcclusionUtility * 100f) / 100f) + " \n";
        // label += "CoverRatio: " + (Mathf.Round(CoverUtility * 100f) / 100f) + " \n";
        // label += "DeadEndProximity: " + (Mathf.Round(DeadEndProximity * 100f) / 100f) + " \n";
        label += IsAlreadyChecked() ? "Checked" : "Ready";
        Handles.Label(Position, label);
#endif
    }
}