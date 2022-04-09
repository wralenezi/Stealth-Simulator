using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadMapScouter : Scouter
{
    // Road map of the level
    public bool showRoadmap;
    private RoadMap m_RoadMap;

    // Predicted trajectories of guards
    public bool showProjectedTrajectories;
    private List<PossibleTrajectory> m_PossibleTrajectories;

    public int trajectoryCounter = 0;


    // The count of possible positions the will be distributed on the projection
    private int m_positionsCount = 1;

    // Update frequency parameters
    private float m_lastUpdateTimestamp;
    private const float UpdateIntervalInSeconds = 1f;

    private List<HidingSpot> m_tempSpots;

    private List<LabelSpot> _labels;

    [SerializeField] private AnimationCurve _SafetyCurve;

    public override void Initiate(MapManager mapManager)
    {
        base.Initiate(mapManager);

        m_PossibleTrajectories = new List<PossibleTrajectory>();

        m_RoadMap = mapManager.GetRoadMap();
        m_lastUpdateTimestamp = StealthArea.GetElapsedTime();

        SetCurves();

        _labels = new List<LabelSpot>();

        showProjectedTrajectories = true;
    }

    private void SetCurves()
    {
        SetSafetyCurve();
    }


    private bool IsUpdateDue()
    {
        float currentTimestamp = StealthArea.GetElapsedTime();
        if (currentTimestamp - m_lastUpdateTimestamp >= UpdateIntervalInSeconds)
        {
            m_lastUpdateTimestamp = currentTimestamp;
            return true;
        }

        return false;
    }

    public override void Begin()
    {
        m_lastUpdateTimestamp = StealthArea.GetElapsedTime();
    }

    public override void Refresh(GameType gameType)
    {
        ProjectGuardPositions(NpcsManager.Instance.GetGuards());

        foreach (var intruder in NpcsManager.Instance.GetIntruders())
        {
            if (intruder.IsBusy() && !IsUpdateDue()) return;

            Vector2? goal = null;

            switch (gameType)
            {
                case GameType.CoinCollection:
                    goal = CollectablesManager.Instance.GetGoalPosition(gameType);
                    break;

                case GameType.StealthPath:
                    // goal =
                    break;
            }

            HidingSpot bestHs = EvaluateHidingSpot(intruder, goal.Value);

            if (Equals(goal, null) || Equals(bestHs, null)) return;

            float distanceToGoal = Vector2.Distance(goal.Value, bestHs.Position);
            goal = distanceToGoal / PathFinding.Instance.longestShortestPath < 0.1f ? goal.Value : bestHs.Position;

            // Update the fitness values of the hiding spots
            intruder.SetDestination(goal.Value, true, false);
        }
    }

    /// <summary>
    /// Project the guards position on the road map.
    /// </summary>
    public void ProjectGuardPositions(List<Guard> guards)
    {
        m_PossibleTrajectories.Clear();

        float fov = Properties.GetFovRadius(NpcType.Guard);

        foreach (var guard in guards)
        {
            // Get the closest point on the road map to the guard
            Vector2? point = m_RoadMap.GetLineToPoint(guard.GetTransform().position, true, out RoadMapLine line);

            // if there is no intersection then abort
            if (!point.HasValue) return;

            m_RoadMap.ProjectPositionsInDirection(ref m_PossibleTrajectories, point.Value, line,
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
        return npc.GetCurrentSpeed() * fov * 20f;
    }

    // private HidingSpot EvaluateHidingSpot(Intruder intruder, Vector2 goal)
    // {
    //     int numberOfAdjacentCell = 2;
    //     float radius = PathFinding.Instance.longestPath * 0.1f;
    //
    //     m_tempSpots = m_HsC.GetHidingSpots(intruder.GetTransform().position, numberOfAdjacentCell);
    //
    //     float guardFovRadius = Properties.GetFovRadius(NpcType.Guard);
    //
    //     HidingSpot bestHs = null;
    //     float maxFitness = Mathf.NegativeInfinity;
    //
    //     foreach (var hs in m_tempSpots)
    //     {
    //         hs.Fitness = 0f;
    //
    //         float distanceToHs =
    //             PathFinding.Instance.GetShortestPathDistance(hs.Position, intruder.GetTransform().position);
    //
    //         if (distanceToHs > radius) continue;
    //
    //         PossiblePosition closestPossiblePosition;
    //         Vector2? closestPointOnTrajectory = null;
    //         foreach (var trajectory in m_PossibleTrajectories)
    //         {
    //             closestPointOnTrajectory =
    //                 GeometryHelper.GetClosetPointOnPath(trajectory.GetPath(), hs.Position);
    //         }
    //
    //         bool isMutuallyVisible =
    //             GeometryHelper.IsCirclesVisible(hs.Position, closestPointOnTrajectory.Value, 0.5f, "Wall");
    //
    //
    //         float distanceHsToTrajectory =
    //             PathFinding.Instance.GetShortestPathDistance(closestPointOnTrajectory.Value,
    //                 intruder.GetTransform().position);
    //
    //         float safetyUtilityInComingGuard = 0f;
    //         if (!isMutuallyVisible && distanceHsToTrajectory >= guardFovRadius)
    //         {
    //             float normalizedDistance = distanceHsToTrajectory / _ProjectionDist;
    //             safetyUtilityInComingGuard = _SafetyCurve.Evaluate(normalizedDistance);
    //         }
    //
    //         float distanceToGoal = Vector2.Distance(hs.Position, goal);
    //         // float distanceToGoal = PathFinding.Instance.GetShortestPathDistance(hs.Position, goal);
    //         float utilityToGoal = 1f - distanceToGoal / PathFinding.Instance.longestPath;
    //
    //         // safetyUtilityInComingGuard = 0f;
    //
    //         hs.Fitness = Mathf.Max(utilityToGoal, safetyUtilityInComingGuard);
    //         // hs.Fitness = (utilityToGoal + safetyUtilityInComingGuard) * 0.5f;
    //
    //         hs.Fitness = Mathf.Round(hs.Fitness * 10000f) * 0.0001f;
    //
    //         if (!(maxFitness < hs.Fitness)) continue;
    //
    //         bestHs = hs;
    //         maxFitness = hs.Fitness;
    //     }
    //
    //     return bestHs;
    // }

    private HidingSpot EvaluateHidingSpot(Intruder intruder, Vector2 goal)
    {
        int numberOfAdjacentCell = 3;
        float radius = PathFinding.Instance.longestShortestPath * 0.5f;
        _labels.Clear();

        m_tempSpots = m_HsC.GetHidingSpots(intruder.GetTransform().position, numberOfAdjacentCell);

        float maxSafetyValue = Mathf.NegativeInfinity;
        foreach (var hs in m_tempSpots)
        {
            float distanceToHs =
                PathFinding.Instance.GetShortestPathDistance(hs.Position, intruder.GetTransform().position);

            if (distanceToHs > radius) continue;

            SetClosestGuardThreatPoint(hs);
            if (hs.SafetyAbsoluteValue > maxSafetyValue) maxSafetyValue = hs.SafetyAbsoluteValue;

            hs.GoalUtility = GetGoalUtility(hs, goal);
            hs.GuardProximityUtility = GetGuardsProximityUtility(hs, NpcsManager.Instance.GetGuards());
            hs.OcclusionUtility = GetOcclusionUtility(hs, NpcsManager.Instance.GetGuards());
        }

        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in m_tempSpots)
        {
            hs.SafetyUtility = hs.SafetyAbsoluteValue; /// maxSafetyValue;

            hs.Fitness = hs.SafetyUtility;

            hs.Fitness = Mathf.Round(hs.Fitness * 10000f) * 0.0001f;

            if (!(maxFitness < hs.Fitness)) continue;

            bestHs = hs;
            maxFitness = hs.Fitness;
        }

        // EditorApplication.isPaused = true;

        return bestHs;
    }

    private void SetSafetyCurve()
    {
        _SafetyCurve = new AnimationCurve();

        for (float i = 0; i < 1; i += 0.1f)
        {
            float y = (i <= 0.5) ? i * 0.1f : i;
            float x = i;
            Keyframe keyframe = new Keyframe(x, y);
            _SafetyCurve.AddKey(keyframe);
        }
    }

    private void SetClosestGuardThreatPoint(HidingSpot hs)
    {
        float NPC_RADIUS = 0.05f;

        float shortestDistance = Mathf.Infinity;
        PossiblePosition closestPointOnTrajectory = null;
        foreach (var trajectory in m_PossibleTrajectories)
        {
            Vector2? pointOnTrajectory =
                GeometryHelper.GetClosetPointOnPath(trajectory.GetPath(), hs.Position, NPC_RADIUS);

            float distance;
            Vector2? closestPoint;
            if (Equals(pointOnTrajectory, null))
            {
                closestPoint = trajectory.GetLastPoint();
                distance =
                    Vector2.Distance(closestPoint.Value, hs.Position);
            }
            else
            {
                closestPoint = pointOnTrajectory.Value;
                distance = Vector2.Distance(hs.Position, closestPoint.Value);
            }


            if (distance < shortestDistance)
            {
                closestPointOnTrajectory ??= new PossiblePosition(closestPoint.Value, trajectory.npc);

                closestPointOnTrajectory.position = closestPoint.Value;
                closestPointOnTrajectory.npc = trajectory.npc;
                shortestDistance = distance;
            }
        }


        hs.ThreateningPosition = closestPointOnTrajectory;
        hs.SafetyAbsoluteValue = shortestDistance;
    }

    // Get the safety utility of a hiding spot.
    private float GetSafetyAbsoluteValue(HidingSpot hs)
    {
        float NPC_RADIUS = 0.05f;

        float shortestDistance = Mathf.Infinity;
        float shortestEndPathDistance = Mathf.Infinity;
        PossiblePosition closestPointOnTrajectory = null;
        foreach (var trajectory in m_PossibleTrajectories)
        {
            Vector2? pointOnTrajectory =
                GeometryHelper.GetClosetPointOnPath(trajectory.GetPath(), hs.Position, NPC_RADIUS);

            float distanceToEndPath =
                PathFinding.Instance.GetShortestPathDistance(trajectory.GetLastPoint(), hs.Position);

            if (distanceToEndPath < shortestEndPathDistance) shortestEndPathDistance = distanceToEndPath;

            if (Equals(pointOnTrajectory, null)) continue;

            float distanceOnRoadmap = Vector2.Distance(hs.Position, pointOnTrajectory.Value);

            if (distanceOnRoadmap >= shortestDistance) continue;

            closestPointOnTrajectory ??= new PossiblePosition(pointOnTrajectory.Value, trajectory.npc);

            closestPointOnTrajectory.position = pointOnTrajectory.Value;
            closestPointOnTrajectory.npc = trajectory.npc;
            shortestDistance = distanceOnRoadmap;
        }

        if (Equals(closestPointOnTrajectory, null)) return shortestEndPathDistance;

        // Vector2 mid = (hs.Position + closestPointOnTrajectory.position) / 2f;
        // _labels.Add(new LabelSpot(shortestDistance.ToString(), mid));
        // Debug.DrawLine(hs.Position, closestPointOnTrajectory.position);

        // bool isPointInFront = IsPointFrontNpc(closestPointOnTrajectory.npc, hs.Position);

        return shortestDistance;
    }


    // private float GetSafetyUtility(HidingSpot hs)
    // {
    //     float denominator = PathFinding.Instance.longestShortestPath;
    //     // Set at 1f to show how safe it is.
    //     float safetyUtility = 1f;
    //
    //     float shortestDistance = Mathf.Infinity;
    //     PossiblePosition closestPointOnTrajectory = null;
    //     foreach (var trajectory in m_PossibleTrajectories)
    //     {
    //         Vector2? pointOnTrajectory =
    //             GeometryHelper.GetClosetPointOnPath(trajectory.GetPath(), hs.Position);
    //
    //         if (Equals(pointOnTrajectory, null)) continue;
    //
    //         // float distance = PathFinding.Instance.GetShortestPathDistance(hs.Position, pointOnTrajectory.Value);
    //         float distance = Vector2.Distance(hs.Position, pointOnTrajectory.Value);
    //         
    //         if (distance >= shortestDistance) continue;
    //
    //         closestPointOnTrajectory ??= new PossiblePosition(pointOnTrajectory.Value, trajectory.npc);
    //
    //         closestPointOnTrajectory.position = pointOnTrajectory.Value;
    //         closestPointOnTrajectory.npc = trajectory.npc;
    //         shortestDistance = distance;
    //     }
    //
    //     if (Equals(closestPointOnTrajectory, null)) return safetyUtility;
    //     
    //     float normalizedDistance = shortestDistance / denominator;
    //
    //     Vector2 mid = ((Vector2) closestPointOnTrajectory.npc.GetTransform().position +
    //                    closestPointOnTrajectory.position) / 2f;
    //
    //     _labels.Add(new LabelSpot(normalizedDistance.ToString(), mid));
    //     Debug.DrawLine(closestPointOnTrajectory.npc.GetTransform().position, closestPointOnTrajectory.position);
    //
    //     bool isPointInFront = IsPointFrontNpc(closestPointOnTrajectory.npc, hs.Position);
    //     
    //     // This means that the projection is on the guard, which means that the hiding spot is behind the guard.
    //     if (isPointInFront && normalizedDistance <= 0.01f) safetyUtility = 1f;
    //     else
    //         safetyUtility = -1f;
    //
    //     // else if (projectionDistanceToGuard > 0.01f && projectionDistanceToGuard <= _GuardFovLength) safetyUtility = 0f;
    //     // else if(hsDistanceToGuard > 0.01f && projectionDistanceToGuard <= _GuardFovLength)  safetyUtility = 0f;
    //     // else if (hsDistanceToGuard > _GuardFovLength)
    //     // {
    //     //     float maxDistance = _GuardFovLength * (_ProjectionMultipler - 1);
    //     //     float distance = projectionDistanceToGuard - _GuardFovLength;
    //     //
    //     //     float distanceNormalized = distance / maxDistance;
    //     //     distanceNormalized = Mathf.Min(distanceNormalized, 1f);
    //     //     safetyUtility = _SafetyCurve.Evaluate(distanceNormalized);
    //     // }
    //
    //
    //     return safetyUtility;
    // }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && m_PossibleTrajectories.Count > 0)
        {
            trajectoryCounter++;
            trajectoryCounter %= m_PossibleTrajectories.Count;
        }
    }


    private bool IsPointFrontNpc(NPC npc, Vector2 point)
    {
        float thresholdCosine = 0.4f;

        Vector2 frontVector = npc.GetDirection();

        Vector2 npcToPoint = point - (Vector2) npc.GetTransform().position;

        float dotProduct = Vector2.Dot(frontVector, npcToPoint);

        return dotProduct > thresholdCosine;
    }


    // Get the utility value for being close to the goal position
    private float GetGoalUtility(HidingSpot hs, Vector2 goal)
    {
        float distanceToGoal = Vector2.Distance(hs.Position, goal);
        // float distanceToGoal = PathFinding.Instance.GetShortestPathDistance(hs.Position, goal);
        float utilityToGoal = 1f - distanceToGoal / PathFinding.Instance.longestShortestPath;

        return utilityToGoal;
    }

    // Get the utility for being away from guards
    private float GetGuardsProximityUtility(HidingSpot hs, List<Guard> guards)
    {
        float proximityUtility = 0f;

        float denominator = PathFinding.Instance.longestShortestPath;

        foreach (var guard in guards)
        {
            float distanceToHidingspot =
                PathFinding.Instance.GetShortestPathDistance(hs.Position, guard.GetTransform().position);

            float normalizedDistance = distanceToHidingspot / denominator;

            if (proximityUtility < normalizedDistance)
            {
                proximityUtility = normalizedDistance;
            }
        }

        return proximityUtility;
    }

    /// <summary>
    /// Get the occlusion value of a hiding spot.
    /// The value is between 0 and 1, it reflects the normalized distance to the closest non occluded guard.
    /// </summary>
    private float GetOcclusionUtility(HidingSpot hs, List<Guard> guards)
    {
        float utility = 1f;
        float denominator = PathFinding.Instance.longestShortestPath;

        foreach (var guard in guards)
        {
            bool isVisible = GeometryHelper.IsCirclesVisible(hs.Position, guard.GetTransform().position, 0.1f, "Wall");

            if (!isVisible) continue;

            float distanceToHidingspot = Vector2.Distance(hs.Position, guard.GetTransform().position);

            float normalizedDistance = distanceToHidingspot / denominator;

            if (utility > normalizedDistance)
            {
                utility = normalizedDistance;
            }
        }

        return utility;
    }

    public void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        // if (!Equals(_labels, null))
        //     foreach (var label in _labels)
        //     {
        //         Handles.Label(label.position, label.label);
        //     }

        if (showRoadmap)
            m_RoadMap.DrawRoadMap();

        // if (!Equals(m_PossibleTrajectories, null) && trajectoryCounter < m_PossibleTrajectories.Count)
        //     m_PossibleTrajectories[trajectoryCounter].Draw();

        if (showProjectedTrajectories)
            foreach (var psbTrac in m_PossibleTrajectories)
                psbTrac.Draw();

//         if (!Equals(m_tempSpots, null))
//             foreach (var s in m_tempSpots)
//             {
//                 // if (s.Fitness <= 0f) continue;
//
//                 Gizmos.DrawSphere(s.Position, 0.2f);
//
// #if UNITY_EDITOR
//                 Handles.Label(0.4f * Vector2.up + s.Position, s.Fitness.ToString());
// #endif
//             }

        if (!Equals(m_tempSpots, null))
            foreach (var hs in m_tempSpots)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(hs.Position, 0.05f);
#if UNITY_EDITOR
                Handles.Label(0.4f * Vector2.up + hs.Position, hs.SafetyAbsoluteValue.ToString());
#endif

                Gizmos.color = Color.yellow;
                if (!Equals(hs.ThreateningPosition, null))
                {
                    Gizmos.DrawSphere(hs.ThreateningPosition.position, 0.05f);
                    Gizmos.DrawLine(hs.Position, hs.ThreateningPosition.position);
                }
            }
    }
}


public class PossiblePosition
{
    public Vector2 position;

    // Distance from the NPC
    private float distance;

    /// <summary>
    /// Safety Multiplier; the lower it is the closer this point to the guard. It ranges between 0 and 1 
    /// </summary>
    public float safetyMultiplier;

    // The NPC this possible position belong to
    public NPC npc;

    public PossiblePosition(Vector2 _position, NPC _npc)
    {
        position = _position;
        npc = _npc;
    }

    public PossiblePosition(Vector2 _position, NPC _npc, float _distance) : this(_position, _npc)
    {
        position = _position;
        // distance = _distance;
        npc = _npc;
        // safetyMultiplier = distance / RoadMapScouter.GetProjectionDistance();
    }

    public void Draw()
    {
        byte alpha = (byte) (55 + 200 * (1f - distance));

#if UNITY_EDITOR
        Handles.Label(position, distance.ToString());
#endif
        Gizmos.color = new Color32(255, 0, 0, alpha);
        Gizmos.DrawSphere(position, 0.5f);
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