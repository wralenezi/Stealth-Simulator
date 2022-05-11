using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Vector2 = UnityEngine.Vector2;

public class RoadMapScouter : Scouter
{
    private Intruder _intruder;

    // Road map of the level
    public bool showRoadMap;
    private RoadMap _roadMap;

    // Predicted trajectories of guards
    public bool showProjectedTrajectories;
    private List<PossibleTrajectory> _possibleTrajectories;

    public bool showAvailableHidingSpots;

    // List of hiding spots available for the intruder to choose from.
    private List<HidingSpot> _availableSpots;

    // A dictionary of the riskiest spots by each guard on the intruders current path
    public bool showRiskSpots;
    private ScoutRiskEvaluator _riskEvaluator;

    private RMScoutPathFinder _pathFinder;

    private RMSDecisionMaker _decisionMaker;

    // The number of attempts to find the next spot
    // [SerializeField] private int _attemptCount = 0;
    // int _maxAttempts = 10;

    // The total distance the intruder crossed
    [SerializeField] private float _crossedDistance;

    // // List of curves to determine how utilities are mapped.
    // [SerializeField] private AnimationCurve _SafetyCurve;

    public override void Initiate(MapManager mapManager)
    {
        base.Initiate(mapManager);

        _availableSpots = new List<HidingSpot>();

        _possibleTrajectories = new List<PossibleTrajectory>();

        _roadMap = mapManager.GetRoadMap();

        // SetCurves();

        _riskEvaluator = gameObject.AddComponent<ScoutRiskEvaluator>();
        _riskEvaluator.Initiate();
        _pathFinder = new RMScoutPathFinder();
        _decisionMaker = new RMSDecisionMaker();

        showAvailableHidingSpots = true;
        showRiskSpots = true;
        // showProjectedTrajectories = true;
        // showRoadMap = true;
    }

    // private void SetCurves()
    // {
    //     SetSafetyCurve();
    // }
    //
    // private void SetSafetyCurve()
    // {
    //     _SafetyCurve = new AnimationCurve();
    //
    //     for (float i = 0; i < 1; i += 0.1f)
    //     {
    //         float y = (i <= 0.5) ? i * 0.1f : i;
    //         float x = i;
    //         Keyframe keyframe = new Keyframe(x, y);
    //         _SafetyCurve.AddKey(keyframe);
    //     }
    // }

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

        SetGuardTrajectories(guards);

        _riskEvaluator.UpdateCurrentRisk(_roadMap);

        // if (didIntruderTravel()) _attemptCount = 0;

        CalculateThresholds(0, _riskEvaluator.GetRisk(), out int searchDepth,
            out float pathFindingRiskThreshold, out float abortPathRiskThreshold);

        Vector2? goal = null;

        switch (gameType)
        {
            case GameType.CoinCollection:
                goal = CollectablesManager.Instance.GetGoalPosition(gameType);
                break;

            case GameType.StealthPath:
                break;
        }

        List<Vector2> path = intruder.GetPath();


        Vector2? closestToGoal = null;

        if (!Equals(goal, null) && !intruder.IsBusy() && _riskEvaluator.GetRisk() < 0.1f)
            closestToGoal = _pathFinder.GetClosestPointToGoal(_roadMap, intruder.GetTransform().position,
                goal.Value,
                ref path, pathFindingRiskThreshold);

        if (!intruder.IsBusy())
        {
            Vector2 locationToCheck =
                Equals(closestToGoal, null) ? (Vector2) intruder.GetTransform().position : closestToGoal.Value;

            HidingSpot closestHidingSpot = _HsC.GetClosestHidingSpotToPosition(locationToCheck);
            _HsC.FillAvailableSpots(closestHidingSpot, searchDepth, ref _availableSpots);

            // int numberOfAdjacentCell = 12;
            // _availableSpots = _HsC.GetHidingSpots(intruder.GetTransform().position, numberOfAdjacentCell);

            EvaluateSpots(intruder, goal);
        }

        // Get a new destination for the intruder
        while (!intruder.IsBusy() && _availableSpots.Count > 0)
        {
            HidingSpot bestHs =
                _decisionMaker.GetBestSpot(_availableSpots, _riskEvaluator.GetRisk());

            if (Equals(bestHs, null))
            {
                Debug.Log("No Goal");
                return;
            }

            _pathFinder.GetShortestPath(_roadMap, intruder.GetTransform().position, bestHs, ref path,
                pathFindingRiskThreshold);

            _availableSpots.Remove(bestHs);
        }

        if (!intruder.IsBusy())
        {
            // _attemptCount++;
            // = (_attemptCount + 1) % _maxAttempts;
            Debug.Log("No Goal 2");
            return;
        }

        // Abort the current path if it is too risky
        _riskEvaluator.CheckPathRisk(_roadMap, intruder, guards, abortPathRiskThreshold);
    }


    private void CalculateThresholds(int attemptNumber, float intruderRisk, out int searchDepth,
        out float pathFindingRiskThreshold,
        out float abortPathRiskThreshold)
    {
        // float normalizedAttempts = (float) attemptNumber / _maxAttempts;
        // float searchDepthLvl = Mathf.Clamp(normalizedAttempts, 0.1f, 0.9f);
        //
        // int lineOfSighMaxDepth = 3;
        // searchDepth = Mathf.FloorToInt(searchDepthLvl * lineOfSighMaxDepth);
        // pathFindingRiskThreshold = Mathf.Clamp(normalizedAttempts, 0.1f, 0.8f);
        // abortPathRiskThreshold = Mathf.Clamp(normalizedAttempts, 0.1f, 0.8f);

        // int lineOfSighMaxDepth = 1;
        // float searchDepthLvl = Mathf.Clamp(intruderRisk, 0.1f, 1f);
        // searchDepth = Mathf.FloorToInt(searchDepthLvl * lineOfSighMaxDepth);
        searchDepth = 1;
        pathFindingRiskThreshold = Mathf.Clamp(intruderRisk, 0.5f, 0.8f);
        abortPathRiskThreshold = Mathf.Clamp(intruderRisk, 0.2f, 0.8f);
    }


    /// <summary>
    /// Project the guards position on the road map.
    /// </summary>
    private void SetGuardTrajectories(List<Guard> guards)
    {
        _possibleTrajectories.Clear();
        _roadMap.ClearTempWayPoints();

        float fov = Properties.GetFovRadius(NpcType.Guard);
        foreach (var guard in guards)
        {
            // Get the closest point on the road map to the guard
            Vector2? point = _roadMap.GetClosetWpPairToPoint(guard.GetTransform().position, guard.GetDirection(), true,
                out WayPoint wp1, out WayPoint wp2);

            // if there is no intersection then abort
            if (!point.HasValue) return;

            _roadMap.ProjectPositionsInDirection(ref _possibleTrajectories, point.Value, wp1, wp2, fov * 0.25f,
                GetGuardProjectionDistance(guard), guard);
        }
    }

    private float GetGuardProjectionDistance(NPC npc)
    {
        float fov = Properties.GetFovRadius(NpcType.Guard);
        return fov + GetGuardProjectionOffset(npc);
    }

    private float GetGuardProjectionOffset(NPC npc)
    {
        float fov = Properties.GetFovRadius(NpcType.Guard);

        float speed = Equals(npc, null) ? Properties.NpcSpeed : npc.GetCurrentSpeed();
        return Mathf.Max(speed * fov * 30f, fov * 0.1f);
    }

    private void EvaluateSpots(Intruder intruder, Vector2? goal)
    {
        // Consider the goal as a potential hiding spot
        if (!Equals(goal, null))
        {
            HidingSpot goalSpot = new HidingSpot(goal.Value, 1f);
            _availableSpots.Add(goalSpot);
        }

        int i = 0;
        while (i < _availableSpots.Count)
        {
            HidingSpot hs = _availableSpots[i];

            SetRiskValue(hs);
            SetGoalUtility(hs, goal);
            SetCostUtility(intruder, hs);
            // SetGuardsProximityUtility(hs, NpcsManager.Instance.GetGuards());
            // SetOcclusionUtility(hs, NpcsManager.Instance.GetGuards());

            i++;
        }
    }

    private void SetRiskValue(HidingSpot hs)
    {
        float guardFovRadius = Properties.GetFovRadius(NpcType.Guard);

// Get the closest trajectory to this spot
        float shortestDistanceToTrajectory = Mathf.Infinity;

        PossiblePosition closestPointOnTrajectory = null;
        foreach (var trajectory in _possibleTrajectories)
        {
            Vector2? pointOnTrajectory =
                GeometryHelper.GetClosetPointOnPath(trajectory.GetPath(), hs.Position, Properties.NpcRadius);

            float distance;
            Vector2? closestPoint;
            if (Equals(pointOnTrajectory, null))
            {
                closestPoint = trajectory.GetLastPoint();
                distance =
                    PathFinding.Instance.GetShortestPathDistance(closestPoint.Value, hs.Position);
            }
            else
            {
                closestPoint = pointOnTrajectory.Value;
                distance = Vector2.Distance(hs.Position, closestPoint.Value);
            }


            if (distance < shortestDistanceToTrajectory)
            {
                closestPointOnTrajectory ??= new PossiblePosition(closestPoint.Value, trajectory.npc);

                closestPointOnTrajectory.SetPosition(closestPoint.Value);
                closestPointOnTrajectory.npc = trajectory.npc;
                shortestDistanceToTrajectory = distance;
            }
        }

        hs.ThreateningPosition = closestPointOnTrajectory;

// When there are no threatening positions, it has no risk
        if (Equals(hs.ThreateningPosition, null))
        {
            hs.RiskLikelihood = 0f;
            return;
        }

// Assign the maximum safety value
        float distanceFromBeingSeen = GetGuardProjectionDistance(hs.ThreateningPosition.npc);

        // If the hiding position is approx within radius of guard trajectory, then adjust it's risk value.
        if (shortestDistanceToTrajectory <= guardFovRadius)
        {
            float distanceFromGuardToPoint = PathFinding.Instance.GetShortestPathDistance(
                closestPointOnTrajectory.npc.GetTransform().position,
                closestPointOnTrajectory.GetPosition().Value);

            // Subtract the Fov radius so if the hiding position is already within vision it is not safe anymore.
            distanceFromBeingSeen = distanceFromGuardToPoint - guardFovRadius;
            distanceFromBeingSeen = Mathf.Max(0f, distanceFromBeingSeen);
        }

// Get the orientation of the threatening position to the guard.
        bool isPointInFront = IsPointFrontNpc(hs.ThreateningPosition.npc, hs.Position);

        // The spot is behind the guard
        if (distanceFromBeingSeen < 0.01f && !isPointInFront)
            distanceFromBeingSeen = GetGuardProjectionDistance(hs.ThreateningPosition.npc) * 0.5f;
        hs.RiskLikelihood = 1f - distanceFromBeingSeen / GetGuardProjectionDistance(hs.ThreateningPosition.npc);
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

        float cost = distanceToDestination / PathFinding.Instance.longestShortestPath;
        hs.CostUtility = cost;
    }


// // Get the utility for being away from guards
// private void SetGuardsProximityUtility(HidingSpot hs, List<Guard> guards)
// {
//     float proximityUtility = 0f;
//
//     float denominator = PathFinding.Instance.longestShortestPath;
//
//     foreach (var guard in guards)
//     {
//         float distanceToHidingspot =
//             PathFinding.Instance.GetShortestPathDistance(hs.Position, guard.GetTransform().position);
//
//         float normalizedDistance = distanceToHidingspot / denominator;
//
//         if (proximityUtility < normalizedDistance)
//         {
//             proximityUtility = normalizedDistance;
//         }
//     }
//
//     hs.GuardProximityUtility = proximityUtility;
// }

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
        if (showRoadMap)
            _roadMap.DrawWalkableRoadmap();
        if (showRiskSpots)
            _riskEvaluator.Draw(_intruder.GetTransform().position);
        if (showProjectedTrajectories)
        {
            foreach (var psbTrac in _possibleTrajectories)
                psbTrac.Draw();

            foreach (var t in _roadMap.GetPossibleGuardPositions())
            {
                float value = Mathf.Round(t.GetProbability() * 100f) * 0.01f;
                t.Draw(value.ToString());
            }
        }

        Gizmos.color = Color.blue;
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


public class PossibleTrajectory
{
    private List<Vector2> m_path;

    public NPC npc;

    public PossibleTrajectory(NPC _npc)
    {
        m_path = new List<Vector2>();
        npc = _npc;
    }

    public void AddPoint(Vector2 point)
    {
        m_path.Add(point);
    }

    public void CopyTrajectory(PossibleTrajectory original)
    {
        m_path.Clear();
        npc = original.npc;
        foreach (var pt in original.GetPath())
            m_path.Add(pt);
    }

    public List<Vector2> GetPath()
    {
        return m_path;
    }

    public Vector2 GetFirstPoint()
    {
        return m_path[0];
    }

    public Vector2 GetLastPoint()
    {
        return m_path[m_path.Count - 1];
    }

    public float GetDistanceToPoint(Vector2 point)
    {
        return PathFinding.Instance.GetShortestPathDistance(m_path[0], point);
    }

    public void Draw()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < m_path.Count - 1; i++)
        {
            Gizmos.DrawLine(m_path[i], m_path[i + 1]);
        }
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