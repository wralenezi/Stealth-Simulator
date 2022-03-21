using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadMapScouter : Scouter
{
    // Road map of the level
    public bool showRoadmap;
    private RoadMap m_RoadMap;

    public bool showProjectedTrajectories;
    private List<PossibleTrajectory> m_PossibleTrajectories;

    // The distance of the possible position of the guards; the further it is the more cautious the intruder will be
    private static float _ProjectionDist = 8f;

    // The count of possible positions the will be distributed on the projection
    private int m_positionsCount = 1;

    // Update frequency parameters
    private float m_lastUpdateTimestamp;
    private const float UpdateIntervalInSeconds = 1f;

    private List<HidingSpot> m_tempSpots;

    // Test 
    [SerializeField] private AnimationCurve m_ThreatOfPossiblePositionsCurve;

    public override void Initiate(MapManager mapManager)
    {
        base.Initiate(mapManager);

        m_PossibleTrajectories = new List<PossibleTrajectory>();

        m_RoadMap = mapManager.GetRoadMap();
        m_lastUpdateTimestamp = StealthArea.GetElapsedTime();

        SetCurves();

        _ProjectionDist = 2f * Properties.GetFovRadius(NpcType.Guard);
        showProjectedTrajectories = true;
    }

    private void SetCurves()
    {
        m_ThreatOfPossiblePositionsCurve = new AnimationCurve();

        for (float i = 0; i < 1; i += 0.1f)
        {
            float y = (i <= 0.3f) ? i * 0.1f : i;
            float x = i;
            Keyframe keyframe = new Keyframe(x, y);
            m_ThreatOfPossiblePositionsCurve.AddKey(keyframe);
        }
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


    public static float GetProjectionDistance()
    {
        return _ProjectionDist;
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

            // Update the fitness values of the hiding spots
            intruder.SetDestination(bestHs.Position, true, false);
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

            _ProjectionDist = fov + guard.GetCurrentSpeed() * fov * 20f;
            
            m_RoadMap.ProjectPositionsInDirection(ref m_PossibleTrajectories, point.Value, line,
                _ProjectionDist, guard);
            
            return;

        }
    }

    public HidingSpot EvaluateHidingSpot(Intruder intruder, Vector2 goal)
    {
        int numberOfAdjacentCell = 2;
        float radius = PathFinding.Instance.longestPath * 0.1f;

        m_tempSpots = m_HsC.GetHidingSpots(intruder.GetTransform().position, numberOfAdjacentCell);

        float guardFovRadius = Properties.GetFovRadius(NpcType.Guard);

        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;

        foreach (var hs in m_tempSpots)
        {
            hs.Fitness = 0f;

            float distanceToHs =
                PathFinding.Instance.GetShortestPathDistance(hs.Position, intruder.GetTransform().position);

            if (distanceToHs > radius) continue;

            foreach (var trajectory in m_PossibleTrajectories)
            {
                Vector2? closestPointOnTrajectory =
                    GeometryHelper.GetClosetPointOnPath(trajectory.GetPath(), hs.Position);

                bool isMutuallyVisible =
                    GeometryHelper.IsCirclesVisible(hs.Position, closestPointOnTrajectory.Value, 0.5f, "Wall");

                float distanceHsToTrajectory =
                    PathFinding.Instance.GetShortestPathDistance(closestPointOnTrajectory.Value,
                        intruder.GetTransform().position);

                float safetyUtilityInComingGuard = 0f;
                if (!isMutuallyVisible && distanceHsToTrajectory >= guardFovRadius)
                {
                    float normalizedDistance = distanceHsToTrajectory / _ProjectionDist;
                    safetyUtilityInComingGuard = m_ThreatOfPossiblePositionsCurve.Evaluate(normalizedDistance);
                }

                float distanceToGoal = PathFinding.Instance.GetShortestPathDistance(hs.Position, goal);
                float utilityToGoal = (1f - distanceToGoal / PathFinding.Instance.longestPath);

                hs.Fitness = Mathf.Max(utilityToGoal , safetyUtilityInComingGuard);
                
                if (!(maxFitness < hs.Fitness)) continue;
                bestHs = hs;
                maxFitness = hs.Fitness;
            }
        }

        return bestHs;
    }


    public void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showRoadmap)
            m_RoadMap.DrawRoadMap();

        if (showProjectedTrajectories)
            foreach (var psbTrac in m_PossibleTrajectories)
                psbTrac.Draw();

        if (!Equals(m_tempSpots, null))
            foreach (var s in m_tempSpots)
            {
                if (s.Fitness <= 0f) continue;

                Gizmos.DrawSphere(s.Position, 0.2f);

#if UNITY_EDITOR
                Handles.Label(0.4f * Vector2.up + s.Position, s.Fitness.ToString());
#endif
            }
    }
}


public class PossiblePosition
{
    public Vector2 position;

    // Distance from the NPC
    public float distance;

    /// <summary>
    /// Safety Multiplier; the lower it is the closer this point to the guard. It ranges between 0 and 1 
    /// </summary>
    public float safetyMultiplier;

    // The NPC this possible position belong to
    public NPC npc;

    public PossiblePosition(Vector2 _position, NPC _npc, float _distance)
    {
        position = _position;
        distance = _distance;
        npc = _npc;
        safetyMultiplier = distance / RoadMapScouter.GetProjectionDistance();
    }

    public void Draw()
    {
        byte alpha = (byte) (55 + 200 * (1f - distance / RoadMapScouter.GetProjectionDistance()));

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