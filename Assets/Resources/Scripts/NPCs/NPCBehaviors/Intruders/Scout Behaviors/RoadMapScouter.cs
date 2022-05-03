using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using Vector2 = UnityEngine.Vector2;

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


    // A dictionary of the riskiest spots by each guard on the intruders current path
    public bool showRiskSpots;
    private Dictionary<string, PossiblePosition> _riskSpots;

    // Variables for the coroutine for checking the risk of taking a current path. 
    private bool _isTrajectoryInterceptionCoRunning;
    private float _TrajectoryInterceptionUpdateFreqPerSec = 0.5f;

    // Update frequency parameters
    private float _lastUpdateTimestamp;
    private const float UpdateIntervalInSeconds = 0.5f;

    // List of curves to determine how utilities are mapped.
    [SerializeField] private AnimationCurve _SafetyCurve;

    // path finding on the road map
    private List<Vector2> _tempPath;
    List<WayPoint> openListRoadMap;
    List<WayPoint> closedListRoadMap;


    private float NPC_RADIUS = 0.05f;


    public override void Initiate(MapManager mapManager)
    {
        base.Initiate(mapManager);

        _availableSpots = new List<HidingSpot>();

        _possibleTrajectories = new List<PossibleTrajectory>();

        _roadMap = mapManager.GetRoadMap();
        _lastUpdateTimestamp = StealthArea.GetElapsedTime();

        SetCurves();

        _riskSpots = new Dictionary<string, PossiblePosition>();

        _tempPath = new List<Vector2>();
        openListRoadMap = new List<WayPoint>();
        closedListRoadMap = new List<WayPoint>();


        showAvailableHidingspots = true;
        showRiskSpots = true;
        showProjectedTrajectories = true;
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
        List<Guard> guards = NpcsManager.Instance.GetGuards();
        Intruder intruder = NpcsManager.Instance.GetIntruders()[0];

        SetGuardTrajectories(guards);

        // Get a new destination for the intruder
        if (!intruder.IsBusy() || IsUpdateDue())
        {
            Vector2? goal = null;

            switch (gameType)
            {
                case GameType.CoinCollection:
                    goal = CollectablesManager.Instance.GetGoalPosition(gameType);
                    break;

                case GameType.StealthPath:
                    break;
            }

            EvaluateSpots(intruder, goal);

            HidingSpot bestHs = GetBestSpot();

            if (Equals(bestHs, null)) return;

            SetPathOnRoadmap(intruder, bestHs.Position, false, 0.5f);
        }

        // Abort the current path if it is too risky
        if (_isTrajectoryInterceptionCoRunning && !intruder.IsBusy()) return;
        StartCoroutine(TrajectoryInterceptionCO(intruder, guards, 0.5f));
    }

    private IEnumerator TrajectoryInterceptionCO(Intruder intruder, List<Guard> guards, float maxAcceptedRisk)
    {
        _isTrajectoryInterceptionCoRunning = true;
        if (IsPathRisky(intruder, guards, maxAcceptedRisk))
            intruder.ClearGoal();
        else
            yield return new WaitForSeconds(UpdateIntervalInSeconds);
        _isTrajectoryInterceptionCoRunning = false;
    }

    private bool IsPathRisky(Intruder intruder, List<Guard> guards, float maxRisk)
    {
        float IGNORE_RISK_RANGE = 1f;
        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);

        _riskSpots.Clear();

        // Insert a risk spot for each guard
        foreach (var g in guards)
        {
            PossiblePosition riskSpot = new PossiblePosition(null, g);
            riskSpot.risk = Mathf.NegativeInfinity;
            riskSpot.sqrDistance = Mathf.Infinity;
            _riskSpots[g.name] = riskSpot;
        }

        List<WayPoint> possiblePositions = _roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath =
                GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), NPC_RADIUS);

            if (Equals(pointOnPath, null)) continue;

            Vector2 offset = pointOnPath.Value - p.GetPosition();
            PossiblePosition riskSpot = _riskSpots[p.GetPassingGuard().name];

            if (riskSpot.sqrDistance > offset.sqrMagnitude)
            {
                riskSpot.SetPosition(pointOnPath);
                riskSpot.npc = p.GetPassingGuard();
                riskSpot.risk = p.GetProbability();
                riskSpot.sqrDistance = offset.sqrMagnitude;
            }
        }

        float highestRisk = Mathf.NegativeInfinity;
        PossiblePosition riskiestSpot = null;

        foreach (var spot in _riskSpots)
        {
            if (spot.Value.sqrDistance > RISK_RANGE * RISK_RANGE) continue;

            if (highestRisk < spot.Value.risk)
            {
                highestRisk = spot.Value.risk;
                riskiestSpot = spot.Value;
            }
        }

        if (Equals(highestRisk, Mathf.NegativeInfinity)) return false;

        bool isRisky = highestRisk >= maxRisk;

        if (!isRisky) return false;

        float distanceFromRisk = Vector2.Distance(riskiestSpot.GetPosition().Value, intruder.GetTransform().position);

        return distanceFromRisk > IGNORE_RISK_RANGE;
    }


    private void SetPathOnRoadmap(Intruder intruder, Vector2 goal, bool isForced, float highestRiskThreshold)
    {
        if (isForced || !intruder.IsBusy())
        {
            List<Vector2> pathToTake = intruder.GetPath();
            GetShortestPath(intruder.GetTransform().position, goal, ref pathToTake, true, highestRiskThreshold);
        }
    }


    // Get shortest path on the road map
    // The start node is a node on the road map and the goal is the position of the phantom 
    // for ease of implementation we start the search from the goal to the start node
    private void GetShortestPath(Vector2 start, Vector2 goal, ref List<Vector2> path, bool isOriginal,
        float highestRiskThreshold)
    {
        WayPoint startWp = _roadMap.GetClosestNodes(start, isOriginal, NodeType.RoadMap);
        WayPoint goalWp = _roadMap.GetClosestNodes(goal, isOriginal, NodeType.RoadMap);

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

            if (highestRiskThreshold >= current.GetProbability())
                foreach (WayPoint p in current.GetConnections(isOriginal))
                {
                    if (closedListRoadMap.Contains(p) || Equals(p.type, NodeType.Corner)) continue;

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

        // PathFinding.Instance.GetShortestPath(start, startWp.GetPosition(),
        //     ref path);

        foreach (var node in _tempPath)
            path.Add(node);

        SimplifyPath(ref path);
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
        return Mathf.Max(speed * fov * 25f, fov * 0.1f);
    }

    private void EvaluateSpots(Intruder intruder, Vector2? goal)
    {
        _HsC.GetSpotsOfInterest(intruder.GetTransform().position, ref _availableSpots);

        // int numberOfAdjacentCell = 12;
        // _availableSpots = _HsC.GetHidingSpots(intruder.GetTransform().position, numberOfAdjacentCell);

        // Consider the goal as a potential hiding spot
        if (!Equals(goal, null))
        {
            HidingSpot goalSpot = new HidingSpot(goal.Value, 1f);
            // if (GeometryHelper.IsCirclesVisible(intruder.GetTransform().position, goal.Value, NPC_RADIUS, "Wall"))
            _availableSpots.Add(goalSpot);
        }

        // Remove spots that are too far away
        float radius = PathFinding.Instance.longestShortestPath * 0.5f;
        int counter = 0;
        while (counter < _availableSpots.Count)
        {
            HidingSpot hs = _availableSpots[counter];

            float distanceToHs =
                PathFinding.Instance.GetShortestPathDistance(hs.Position, intruder.GetTransform().position);

            if (distanceToHs > radius)
            {
                _availableSpots.RemoveAt(counter);
                continue;
            }

            counter++;
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

    private HidingSpot GetBestSpot()
    {
        // return GetClosestToGoalSafeSpot(0.5f);
        return GetClosestCheapestToGoalSafeSpot(0.5f);
        // return GetBestSpot_Simple();
    }


    private HidingSpot GetBestSpot_Simple()
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;
        foreach (var hs in _availableSpots)
        {
            hs.Fitness = hs.RiskLikelihood;

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
    private HidingSpot GetClosestToGoalSafeSpot(float maxAcceptedRisk)
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.NegativeInfinity;

        foreach (var hs in _availableSpots)
        {
            if (hs.RiskLikelihood >= maxAcceptedRisk) continue;

            if (maxFitness >= hs.GoalUtility) continue;

            bestHs = hs;
            maxFitness = hs.GoalUtility;
        }

        return bestHs;
    }


    private HidingSpot GetClosestCheapestToGoalSafeSpot(float maxAcceptedRisk)
    {
        HidingSpot bestHs = null;
        float maxFitness = Mathf.Infinity;

        foreach (var hs in _availableSpots)
        {
            if (hs.RiskLikelihood >= maxAcceptedRisk) continue;

            float fitness = 1f - hs.GoalUtility + hs.CostUtility;

            if (maxFitness < fitness) continue;

            bestHs = hs;
            maxFitness = fitness;
        }

        return bestHs;
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


        if (showRoadmap)
            _roadMap.DrawWalkableRoadmap();


        if (showRiskSpots)
            foreach (var spot in _riskSpots)
            {
                float value = Mathf.Round(spot.Value.risk * 100f) * 0.01f;
                spot.Value.Draw(value.ToString(), Color.green);
            }

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