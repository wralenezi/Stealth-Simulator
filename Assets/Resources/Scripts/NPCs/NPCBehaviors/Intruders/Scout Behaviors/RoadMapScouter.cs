using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadMapScouter : Scouter
{
    // Road map of the level
    public bool showRoadmap;
    private RoadMap _roadMap;

    // Predicted trajectories of guards
    public bool showProjectedTrajectories;
    private List<PossibleTrajectory> _possibleTrajectories;

    /// <summary>
    /// List of hiding spots available for the intruder to choose from.
    /// </summary>
    public bool showAvailableHidingspots;

    private List<HidingSpot> _availableSpots;

    // Update frequency parameters
    private float _lastUpdateTimestamp;
    private const float UpdateIntervalInSeconds = 0.5f;

    /// <summary>
    /// Maximum safety utility per guard.
    /// </summary>
    private Dictionary<string, float> _maxSafetyUtilitiesPerGuard;


    // List of curves to determine how utilities are mapped.
    [SerializeField] private AnimationCurve _SafetyCurve;

    private List<Vector2> _tempPath;

    // path finding on the road map
    List<WayPoint> openListRoadMap;
    List<WayPoint> closedListRoadMap;


    public override void Initiate(MapManager mapManager)
    {
        base.Initiate(mapManager);

        _possibleTrajectories = new List<PossibleTrajectory>();

        _roadMap = mapManager.GetRoadMap();
        _lastUpdateTimestamp = StealthArea.GetElapsedTime();

        SetCurves();

        _tempPath = new List<Vector2>();

        openListRoadMap = new List<WayPoint>();
        closedListRoadMap = new List<WayPoint>();

        _maxSafetyUtilitiesPerGuard = new Dictionary<string, float>();

        // showAvailableHidingspots = true;
        // showProjectedTrajectories = true;
        showRoadmap = true;
    }

    private void SetCurves()
    {
        SetSafetyCurve();
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


    private bool IsUpdateDue()
    {
        float currentTimestamp = StealthArea.GetElapsedTime();
        if (currentTimestamp - _lastUpdateTimestamp >= UpdateIntervalInSeconds)
        {
            _lastUpdateTimestamp = currentTimestamp;
            return true;
        }

        return false;
    }

    public override void Begin()
    {
        _lastUpdateTimestamp = StealthArea.GetElapsedTime();
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

            EvaluateSpots(intruder, goal.Value, NpcsManager.Instance.GetGuards());

            HidingSpot bestHs = GetBestSpot();

            if (Equals(goal, null) || Equals(bestHs, null)) return;


            float distanceToGoal = Vector2.Distance(goal.Value, bestHs.Position);
            goal = distanceToGoal / PathFinding.Instance.longestShortestPath < 0.1f ? goal.Value : bestHs.Position;

            // Update the fitness values of the hiding spots
            // intruder.SetDestination(goal.Value, true, false);


            SetPathOnRoadmap(intruder, goal.Value, false);
        }
    }

    private void SetPathOnRoadmap(Intruder intruder, Vector2 goal, bool isForced)
    {
        if (isForced || !intruder.IsBusy())
        {
            List<Vector2> pathToTake = intruder.GetPath();
            GetShortestPath(intruder.GetTransform().position, goal, ref pathToTake, true);

            if (pathToTake.Count > 2)
                EditorApplication.isPaused = true;
        }
    }


    // Get shortest path on the road map
    // The start node is a node on the road map and the goal is the position of the phantom 
    // for ease of implementation we start the search from the goal to the start node
    private void GetShortestPath(Vector2 start, Vector2 goal, ref List<Vector2> path, bool isOriginal)
    {
        WayPoint startWp = _roadMap.GetClosestNodes(start, isOriginal);
        WayPoint goalWp = _roadMap.GetClosestNodes(goal, isOriginal);

        openListRoadMap.Clear();
        closedListRoadMap.Clear();

        foreach (WayPoint p in _roadMap.GetNode(isOriginal))
        {
            p.gDistance = Mathf.Infinity;
            p.hDistance = Mathf.Infinity;
            p.parent = null;
        }

        foreach (var p in _roadMap.GetTempNodes())
        {
            p.gDistance = Mathf.Infinity;
            p.hDistance = Mathf.Infinity;
            p.parent = null;
        }

        // Set Cost of starting node
        startWp.gDistance = 0f;
        startWp.hDistance = GetHeuristicValue(startWp, goalWp);

        openListRoadMap.Add(startWp);

        while (openListRoadMap.Count > 0)
        {
            WayPoint current = openListRoadMap[0];

            openListRoadMap.RemoveAt(0);

            foreach (WayPoint p in current.GetConnections(isOriginal))
            {
                if (!closedListRoadMap.Contains(p))
                {
                    float gDistance = GetCostValue(current, p);
                    float hDistance = GetHeuristicValue(current, goalWp);

                    if (p.gDistance + p.hDistance > gDistance + hDistance)
                    {
                        p.hDistance = hDistance;
                        p.gDistance = gDistance;

                        p.parent = current;
                    }


                    openListRoadMap.InsertIntoSortedList(p,
                        (x, y) => x.GetFvalue().CompareTo(y.GetFvalue()), Order.Asc);
                }
            }

            closedListRoadMap.Add(current);

            // Stop the search if we reached the destination way point
            if (current.Equals(goalWp)) break;
        }

        if (Equals(goalWp.parent, null))
            return;

        // Get the path from the goal way point to the start way point.
        _tempPath.Clear();
        _tempPath.Add(goal);
        WayPoint currentWayPoint = goalWp;
        while (currentWayPoint.parent != null)
        {
            _tempPath.Add(currentWayPoint.GetPosition());

            if (currentWayPoint.parent == null) break;

            currentWayPoint = currentWayPoint.parent;
        }

        _tempPath.Reverse();

        path.Clear();

        PathFinding.Instance.GetShortestPath(start, startWp.GetPosition(),
            ref path);

        foreach (var node in _tempPath)
            path.Add(node);


        // SimplifyPath(ref path);
    }


    // Get heuristic value for way points road map
    static float GetHeuristicValue(WayPoint currentWayPoint, WayPoint goal)
    {
        float heuristicValue =
            Vector2.Distance(currentWayPoint.GetPosition(), goal.GetPosition());

        return heuristicValue;
    }

    // Get the Cost Value (G) for the Waypoints roadmap
    static float GetCostValue(WayPoint previousWayPoint, WayPoint currentWayPoint)
    {
        float costValue = previousWayPoint.gDistance;

        // Euclidean Distance
        float distance = Vector2.Distance(previousWayPoint.GetPosition(), currentWayPoint.GetPosition());

        costValue += distance;

        return costValue;
    }

    private void SimplifyPath(ref List<Vector2> path)
    {
        for (int i = 0; i < path.Count - 2; i++)
        {
            Vector2 first = path[i];
            Vector2 second = path[i + 2];
            float margine = 0.1f;
            float distance = Vector2.Distance(first, second);

            bool isMutuallyVisible = GeometryHelper.IsCirclesVisible(first, second, margine, "Wall");

            if (distance < 0.1f || isMutuallyVisible)
            {
                path.RemoveAt(i + 1);
                i--;
            }
        }
    }


    /// <summary>
    /// Project the guards position on the road map.
    /// </summary>
    private void ProjectGuardPositions(List<Guard> guards)
    {
        _possibleTrajectories.Clear();

        _roadMap.ClearTempWayPoints();


        foreach (var guard in guards)
        {
            // Get the closest point on the road map to the guard
            Vector2? point = _roadMap.GetClosetWpPairToPoint(guard.GetTransform().position, guard.GetDirection(), true,
                out WayPoint wp1, out WayPoint wp2);


            // if there is no intersection then abort
            if (!point.HasValue) return;

            float fov = Properties.GetFovRadius(NpcType.Guard);

            _roadMap.ProjectPositionsInDirection(ref _possibleTrajectories, point.Value, wp1, wp2, fov * 0.5f,
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
        return Mathf.Max(speed * fov * 20f, fov * 1.1f);
    }

    private void EvaluateSpots(Intruder intruder, Vector2 goal, List<Guard> guards)
    {
        // Parameters
        int numberOfAdjacentCell = 12;
        float radius = PathFinding.Instance.longestShortestPath * 1f;

        _availableSpots = m_HsC.GetHidingSpots(intruder.GetTransform().position, numberOfAdjacentCell);

        _maxSafetyUtilitiesPerGuard.Clear();
        foreach (var guard in guards)
            _maxSafetyUtilitiesPerGuard.Add(guard.name, Mathf.NegativeInfinity);

        int i = 0;
        while (i < _availableSpots.Count)
        {
            HidingSpot hs = _availableSpots[i];

            float distanceToHs =
                PathFinding.Instance.GetShortestPathDistance(hs.Position, intruder.GetTransform().position);

            if (distanceToHs > radius)
            {
                _availableSpots.RemoveAt(i);
                continue;
            }

            SetSafetyValue(hs);
            SetGoalUtility(hs, goal);
            SetGuardsProximityUtility(hs, NpcsManager.Instance.GetGuards());
            SetOcclusionUtility(hs, NpcsManager.Instance.GetGuards());

            i++;
        }

        // The safety utility is set here after looping through all spots and setting the maximum absolute safety values for normalizing them in this function.
        foreach (var hs in _availableSpots)
            EvaluateSafetyUtility(hs);
    }

    private HidingSpot GetBestSpot()
    {
        // return GetBestHidingSpot_fixedValues();
        return GetBestSpot_Simple();
    }


    private HidingSpot GetBestSpot_Simple()
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in _availableSpots)
        {
            hs.Fitness = hs.SafetyUtility;

            hs.Fitness = Mathf.Round(hs.Fitness * 10000f) * 0.0001f;

            if (!(maxFitness < hs.Fitness)) continue;

            bestHs = hs;
            maxFitness = hs.Fitness;
        }

        return bestHs;
    }


    /// <summary>
    /// Get the best hiding spots by filtering them through each utility sequentially
    /// </summary>
    /// <returns>The best hiding spot</returns>
    private HidingSpot GetBestHidingSpot_fixedValues()
    {
        // Safety threshold which considered spots should be higher. 
        float SAFETY_THRESHOLD = 1f;

        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;

        foreach (var hs in _availableSpots)
        {
            if (hs.SafetyUtility < SAFETY_THRESHOLD) continue;

            if (maxFitness >= hs.GoalUtility) continue;

            bestHs = hs;
            maxFitness = hs.GoalUtility;
        }

        return bestHs;
    }

    private void SetSafetyValue(HidingSpot hs)
    {
        float NPC_RADIUS = 0.05f;
        float guardFovRadius = Properties.GetFovRadius(NpcType.Guard);

        float shortestDistanceToTrajectory = Mathf.Infinity;
        PossiblePosition closestPointOnTrajectory = null;
        foreach (var trajectory in _possibleTrajectories)
        {
            Vector2? pointOnTrajectory =
                GeometryHelper.GetClosetPointOnPath(trajectory.GetPath(), hs.Position, NPC_RADIUS);

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

                closestPointOnTrajectory.position = closestPoint.Value;
                closestPointOnTrajectory.npc = trajectory.npc;
                shortestDistanceToTrajectory = distance;
            }
        }

        hs.ThreateningPosition = closestPointOnTrajectory;

        if (Equals(hs.ThreateningPosition, null))
        {
            hs.SafetyAbsoluteValue = GetGuardProjectionDistance(null);
            return;
        }

        // Assign the maximum safety value
        float safetyValue = GetGuardProjectionDistance(hs.ThreateningPosition.npc);

        // If the hiding position is approx within radius of guard trajectory, then adjust it's safety value.
        if (shortestDistanceToTrajectory <= guardFovRadius)
        {
            float distanceFromGuardToPoint = PathFinding.Instance.GetShortestPathDistance(
                closestPointOnTrajectory.npc.GetTransform().position,
                closestPointOnTrajectory.position);

            // Subtract the Fov radius so if the hiding position is already within vision it is not safe anymore.
            safetyValue = distanceFromGuardToPoint - guardFovRadius;
            safetyValue = Mathf.Max(0f, safetyValue);
        }

        hs.SafetyAbsoluteValue = safetyValue;


        // if (hs.SafetyAbsoluteValue > _maxSafetyUtilitiesPerGuard[hs.ThreateningPosition.npc.name])
        //     _maxSafetyUtilitiesPerGuard[hs.ThreateningPosition.npc.name] = hs.SafetyAbsoluteValue;
    }


    private void EvaluateSafetyUtility(HidingSpot hs)
    {
        bool isPointInFront;
        if (!Equals(hs.ThreateningPosition, null))
            // Get the orientation of the threatening position to the guard.
            isPointInFront = IsPointFrontNpc(hs.ThreateningPosition.npc, hs.Position);
        else
        {
            hs.SafetyUtility = 1f;
            return;
        }

        if (hs.SafetyAbsoluteValue < 0.01f && !isPointInFront)
        {
            // hs.SafetyAbsoluteValue = _maxSafetyUtilitiesPerGuard[hs.ThreateningPosition.npc.name];
            hs.SafetyAbsoluteValue = GetGuardProjectionDistance(hs.ThreateningPosition.npc);
        }

        // hs.SafetyUtility = hs.SafetyAbsoluteValue / _maxSafetyUtilitiesPerGuard[hs.ThreateningPosition.npc.name];
        hs.SafetyUtility = hs.SafetyAbsoluteValue / GetGuardProjectionDistance(hs.ThreateningPosition.npc);
    }

    private bool IsPointFrontNpc(NPC npc, Vector2 point)
    {
        float thresholdCosine = 0.5f;

        Vector2 frontVector = npc.GetDirection();

        Vector2 npcToPoint = point - (Vector2) npc.GetTransform().position;

        float dotProduct = Vector2.Dot(frontVector, npcToPoint);

        return dotProduct > thresholdCosine;
    }


    // Get the utility value for being close to the goal position
    private void SetGoalUtility(HidingSpot hs, Vector2 goal)
    {
        float distanceToGoal = PathFinding.Instance.GetShortestPathDistance(hs.Position, goal);
        float utilityToGoal = 1f - distanceToGoal / PathFinding.Instance.longestShortestPath;

        hs.GoalUtility = utilityToGoal;
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
            {
                proximityUtility = normalizedDistance;
            }
        }

        hs.GuardProximityUtility = proximityUtility;
    }

    /// <summary>
    /// Get the occlusion value of a hiding spot.
    /// The value is between 0 and 1, it reflects the normalized distance to the closest non occluded guard.
    /// </summary>
    private void SetOcclusionUtility(HidingSpot hs, List<Guard> guards)
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

        hs.OcclusionUtility = utility;
    }

    public void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        // if (Equals(_tempPath, null))
        //     for (int i = 0; i < _tempPath.Count - 1; i++)
        //     {
        //         Gizmos.DrawLine(_tempPath[i], _tempPath[i + 1]);
        //     }


        if (showRoadmap)
            _roadMap.DrawWalkableRoadmap();

        if (showProjectedTrajectories)
            foreach (var psbTrac in _possibleTrajectories)
                psbTrac.Draw();

        Gizmos.color = Color.blue;
        if (showAvailableHidingspots && !Equals(_availableSpots, null))
            foreach (var s in _availableSpots)
            {
                s.Draw();
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