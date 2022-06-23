using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class RoadMapScouter : Scouter
{
    private Intruder _intruder;
    private HidingSpot _currentGoalHs;

    // Road map of the level
    public bool showRoadMap;
    private RoadMap _roadMap;

    // Predicted trajectories of guards
    public bool showProjectedTrajectories;
    private RMTrajectoryProjector _trajectoryProjector;

    public bool showRoadMapEndNodes;

    // List of the closest way points to the destination
    private List<RoadMapNode> _closestWpsToDestination;

    public bool showAvailableHidingSpots;

    // List of hiding spots available for the intruder to choose from.
    private List<HidingSpot> _availableSpots;
    private SpotsNeighbourhoods _neighbourhoodType;

    private float _maxSafeRisk;


    // A dictionary of the riskiest spots by each guard on the intruders current path
    public bool showRiskSpots;
    private RMRiskEvaluator _riskEvaluator;

    private RMScoutPathFinder _pathFinder;

    private RMSDecisionMaker _decisionMaker;

    public static RoadMapScouter Instance;

    // The total distance the intruder crossed
    [SerializeField] private float _crossedDistance;


    public override void Initiate(MapManager mapManager, Session session)
    {
        Instance = this;

        base.Initiate(mapManager, session);

        _closestWpsToDestination = new List<RoadMapNode>();
        _availableSpots = new List<HidingSpot>();

        _trajectoryProjector = new RMTrajectoryProjector();
        _trajectoryProjector.Initiate(session.intruderBehavior.trajectoryType,
            session.intruderBehavior.fovProjectionMultiplier);

        _roadMap = mapManager.GetRoadMap();
        _neighbourhoodType = session.intruderBehavior.spotsNeighbourhood;

        _riskEvaluator = gameObject.AddComponent<RMRiskEvaluator>();
        _riskEvaluator.Initiate();

        _pathFinder = new RMScoutPathFinder();

        _decisionMaker = new RMSDecisionMaker();
        _decisionMaker.Initiate(session.intruderBehavior.goalPriority, session.intruderBehavior.safetyPriority);

        _maxSafeRisk = session.intruderBehavior.maxRiskAsSafe;

        // showAvailableHidingSpots = true;
        // showRiskSpots = true;
        showProjectedTrajectories = true;
        // showRoadMapEndNodes = true;
        // showRoadMap = true;
    }


    public override void Begin()
    {
        base.Begin();
        _riskEvaluator.Clear();
        _intruder = NpcsManager.Instance.GetIntruders()[0];
        _crossedDistance = _intruder.GetTravelledDistance();
    }

    public override void Refresh(GameType gameType)
    {
        List<Guard> guards = NpcsManager.Instance.GetGuards();
        Intruder intruder = NpcsManager.Instance.GetIntruders()[0];

        _trajectoryProjector.SetGuardTrajectories(_roadMap, guards);

        _riskEvaluator.UpdateCurrentRisk(_roadMap);

        Vector2? goal = GetDestination(gameType);

        PathFindToDestination(goal, _maxSafeRisk);

        EvaluateSpots(intruder, goal);

        HidingSpot bestHs = null;
        // Get a new destination for the intruder
        while (!intruder.IsBusy() && _availableSpots.Count > 0)
        {
            bestHs =
                _decisionMaker.GetBestSpot(_availableSpots, _riskEvaluator.GetRisk(), _maxSafeRisk);

            if (Equals(bestHs, null)) return;

            List<Vector2> path = intruder.GetPath();
            float maxPathRisk = RMThresholds.GetMaxPathRisk(intruderBehavior.thresholdType);

            _pathFinder.GetShortestPath(_roadMap, intruder.GetTransform().position, bestHs, ref path,
                maxPathRisk);

            _availableSpots.Remove(bestHs);
        }

        if (!Equals(bestHs, null)) _currentGoalHs = bestHs;

        // Abort the current path if it is too risky
        _riskEvaluator.CheckPathRisk(intruderBehavior.pathCancel, _roadMap, intruder, guards, ref _availableSpots,
            ref _currentGoalHs);
    }

    /// <summary>
    /// Try to find a path to the destination, if that fails then provide possible hiding spots that are closer to the destination
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="minSafeRisk"></param>
    private void PathFindToDestination(Vector2? destination, float minSafeRisk)
    {
        if (_intruder.IsBusy()) return;

        float maxDistance = 20f;
        int numOfPossibleRmNodes = 8;
        List<Vector2> path = _intruder.GetPath();
        float maxSearchRisk = RMThresholds.GetMaxSearchRisk(intruderBehavior.thresholdType);
        bool doAstar = _riskEvaluator.GetRisk() <= minSafeRisk && !Equals(destination, null);
        // bool doAstar = !Equals(destination, null);
        // bool doAstar = true;

        _pathFinder.GetClosestPointToGoal(_roadMap, _intruder.GetTransform().position,
            destination.Value, numOfPossibleRmNodes, ref _closestWpsToDestination,
            ref path, maxSearchRisk, doAstar, maxDistance);

        if (_intruder.IsBusy()) return;

        _availableSpots.Clear();
        foreach (var wp in _closestWpsToDestination)
            FillAvailableSpots(wp.GetPosition());
    }

    private void FillAvailableSpots(Vector2 position)
    {
        switch (_neighbourhoodType)
        {
            case SpotsNeighbourhoods.LineOfSight:
                HidingSpot closestHidingSpot =
                    _HsC.GetClosestHidingSpotToPosition(position);

                if (Equals(closestHidingSpot, null)) return;
                // _HsC.AddAvailableSpots(closestHidingSpot, ref _availableSpots);
                _HsC.AddRandomSpots(closestHidingSpot, ref _availableSpots, 4);
                break;

            case SpotsNeighbourhoods.Grid:
                int numberOfAdjacentCell = RMThresholds.GetSearchDepth(intruderBehavior.thresholdType);
                _HsC.AddHidingSpots(ref _availableSpots, position, numberOfAdjacentCell);
                break;
        }
    }


    public void EvaluateSpots(Intruder intruder, Vector2? goal)
    {
        foreach (var hs in _availableSpots)
        {
            if (hs.IsAlreadyChecked()) continue;

            SetRiskValue(hs);
            SetGoalUtility(hs, goal);
            SetCostUtility(intruder, hs);
            SetGuardsProximityUtility(hs, NpcsManager.Instance.GetGuards());
            SetOcclusionUtility(hs, NpcsManager.Instance.GetGuards());
        }
    }

    private void SetRiskValue(HidingSpot hs)
    {
        float RANGE = Properties.GetFovRadius(NpcType.Guard);

        float minSqrMag = Mathf.Infinity;
        RoadMapNode closestPossibleGuardPos = null;

        float highestRisk = Mathf.NegativeInfinity;
        RoadMapNode riskestNode = null;

        foreach (var p in _roadMap.GetPossibleGuardPositions())
        {
            bool isVisible =
                GeometryHelper.IsCirclesVisible(hs.Position, p.GetPosition(), Properties.NpcRadius, "Wall");
            if (!isVisible) continue;

            Vector2 offset = hs.Position - p.GetPosition();
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > RANGE * RANGE) continue;

            if (sqrMag < minSqrMag)
            {
                minSqrMag = sqrMag;
                closestPossibleGuardPos = p;
            }

            if (highestRisk < p.GetProbability())
            {
                highestRisk = p.GetProbability();
                riskestNode = p;
            }
        }

        hs.ClosestRMGuardNode = closestPossibleGuardPos;

        if (Equals(hs.ClosestRMGuardNode, null))
        {
            hs.Risk = 0f;
            return;
        }


        if (hs.ClosestRMGuardNode.distanceFromGuard == 0f)
            hs.Risk = 0.1f;
        else
            hs.Risk = closestPossibleGuardPos.GetProbability();
    }


    private bool didIntruderTravel()
    {
        float distanceThreshold = 3f;
        if (_intruder.GetTravelledDistance() - _crossedDistance >= distanceThreshold)
        {
            _crossedDistance = _intruder.GetTravelledDistance();
            return true;
        }

        return false;
    }


    private bool IsPointFrontNpc(NPC npc, Vector2 point)
    {
        float thresholdCosine = 0.5f;
        Vector2 frontVector = npc.GetDirection();
        Vector2 npcToPoint = point - (Vector2) npc.GetTransform().position;

        float dotProduct = Vector2.Dot(frontVector, npcToPoint);
        return dotProduct > thresholdCosine;
    }


    private void SetGoalUtility(HidingSpot hs, Vector2? goal)
    {
        if (Equals(goal, null))
        {
            hs.GoalUtility = 1f;
            return;
        }

        float distanceToGoal = PathFinding.Instance.GetShortestPathDistance(hs.Position, goal.Value);
        float utilityToGoal = 1f - distanceToGoal / PathFinding.Instance.longestShortestPath;
        hs.GoalUtility = utilityToGoal;
    }


    private void SetCostUtility(Intruder intruder, HidingSpot hs)
    {
        float distanceToDestination =
            PathFinding.Instance.GetShortestPathDistance(intruder.GetTransform().position, hs.Position);

        float cost = 1f - distanceToDestination / PathFinding.Instance.longestShortestPath;
        hs.CostUtility = cost;
    }


// Get the utility for being away from guards
    private void SetGuardsProximityUtility(HidingSpot hs, List<Guard> guards)
    {
        float proximityUtility = 0f;
        float denominator = PathFinding.Instance.longestShortestPath;

        foreach (var guard in guards)
        {
            float distanceToHidingspot =
                PathFinding.Instance.GetShortestPathDistance(hs.Position, guard.GetTransform().position);

            float normalizedDistance = distanceToHidingspot / denominator;

            if (proximityUtility < normalizedDistance)
                proximityUtility = normalizedDistance;
        }

        hs.GuardProximityUtility = proximityUtility;
    }

    private void SetOcclusionUtility(HidingSpot hs, List<Guard> guards)
    {
        float fovRadius = Properties.GetFovRadius(NpcType.Guard);

        float utility = 0f;

        int guardsInRange = 0;
        int visibleGuards = 0;

        foreach (var guard in guards)
        {
            Vector2 offset = (Vector2) guard.GetTransform().position - hs.Position;
            float sqrMag = offset.sqrMagnitude;

            // Make sure the spot is within FoV
            if (sqrMag > fovRadius * fovRadius) continue;

            guardsInRange++;

            bool isVisible = GeometryHelper.IsCirclesVisible(guard.GetTransform().position, hs.Position,
                Properties.NpcRadius, "Wall");

            if (!isVisible) continue;

            visibleGuards++;
        }

        if (guardsInRange == 0)
            utility = 0f;
        else
            utility = (float) visibleGuards / guardsInRange;

        hs.OcclusionUtility = 1f - utility;
    }

// /// <summary>
// /// Get the occlusion value of a hiding spot.
// /// The value is between 0 and 1, it reflects the normalized distance to the closest non occluded guard.
// /// </summary>
// private void SetOcclusionUtility(HidingSpot hs, List<Guard> guards)
// {
//     float utility = 1f;
//     float denominator = PathFinding.Instance.longestShortestPath;
//
//     foreach (var guard in guards)
//     {
//         bool isVisible = GeometryHelper.IsCirclesVisible(hs.Position, guard.GetTransform().position, 0.1f, "Wall");
//
//         if (!isVisible) continue;
//
//         float distanceToHidingspot = Vector2.Distance(hs.Position, guard.GetTransform().position);
//
//         float normalizedDistance = distanceToHidingspot / denominator;
//
//         if (utility > normalizedDistance)
//         {
//             utility = normalizedDistance;
//         }
//     }
//
//     hs.OcclusionUtility = utility;
// }

    public void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showRoadMapEndNodes && !Equals(_closestWpsToDestination, null))
        {
            Gizmos.color = Color.green;
            foreach (var n in _closestWpsToDestination)
            {
                Gizmos.DrawSphere(n.GetPosition(), 0.25f);

#if UNITY_EDITOR
                Handles.Label(n.GetPosition() + 0.5f * Vector2.down, n.GetProbability().ToString());
#endif
            }
        }


        if (showRoadMap)
            _roadMap.DrawWalkableRoadmap(true);

        if (showRiskSpots)
            _riskEvaluator.Draw();

        if (showProjectedTrajectories)
        {
            foreach (var psbTrac in _trajectoryProjector.GetTrajectories())
                psbTrac.Draw();

            foreach (var t in _roadMap.GetPossibleGuardPositions())
            {
                float value = Mathf.Round(t.GetProbability() * 100f) * 0.01f;
                t.Draw(value.ToString());

                foreach (var con in t.GetConnections(true))
                {
                    Gizmos.DrawLine(t.GetPosition(),con.GetPosition());
                }
            }
        }

        Gizmos.color = Color.yellow;
        if (showAvailableHidingSpots && !Equals(_availableSpots, null))
            foreach (var s in _availableSpots)
            {
                s.Draw();
            }
    }
}


public class PossiblePosition
{
    private Vector2? _position;

    // The NPC this possible position belong to
    public NPC npc;

    // Distance from the NPC
    public float sqrDistance;

    /// <summary>
    /// Safety Multiplier; the lower it is the closer this point to the guard. It ranges between 0 and 1 
    /// </summary>
    public float safetyMultiplier;

    public float risk;

    public PossiblePosition(Vector2? position, NPC npc)
    {
        _position = position;
        this.npc = npc;
    }

    public PossiblePosition(Vector2 position, NPC npc, float _distance) : this(position, npc)
    {
        _position = position;
        this.npc = npc;
        // distance = _distance;
        // safetyMultiplier = distance / RoadMapScouter.GetProjectionDistance();
    }

    public void SetPosition(Vector2? position)
    {
        _position = position;
    }

    public Vector2? GetPosition()
    {
        return _position;
    }

    public void Draw(string label, Color32 color)
    {
        if (Equals(_position, null)) return;

#if UNITY_EDITOR
        Handles.Label(_position.Value + Vector2.down * 0.5f, label);
#endif
        Gizmos.color = color;
        Gizmos.DrawSphere(_position.Value, 0.2f);
    }
}


public struct LabelSpot
{
    public string label;
    public Vector2 position;

    public LabelSpot(string _label, Vector2 _position)
    {
        label = _label;
        position = _position;
    }
}

public enum SpotsNeighbourhoods
{
    LineOfSight,
    Grid,
    All
}