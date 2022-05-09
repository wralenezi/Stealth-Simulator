using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ScoutRiskEvaluator : MonoBehaviour
{
    // How risky is the intruder's current position is, 0 is safe and 1 is spotted.
    [SerializeField] private float _currentRiskValue;
    private Dictionary<string, PossiblePosition> _riskSpots;

    // Variables for the coroutine for checking the risk of taking a current path. 
    private bool _isTrajectoryInterceptionCoRunning;

    // Update frequency parameters
    private const float UpdateIntervalInSeconds = 0.1f;

    public static ScoutRiskEvaluator Instance;

    // Start is called before the first frame update
    public void Initiate()
    {
        _riskSpots = new Dictionary<string, PossiblePosition>();
        Instance = this;
    }

    // Update is called once per frame
    public void Clear()
    {
        _riskSpots.Clear();
    }

    public float GetRisk()
    {
        return _currentRiskValue;
    }

    public void UpdateCurrentRisk(RoadMap roadMap)
    {
        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);

        Intruder intruder = NpcsManager.Instance.GetIntruders()[0];
        Vector2 intruderPosition = intruder.GetTransform().position;

        float closestRisk = 0f;
        float closestSqrMag = Mathf.Infinity;

        List<WayPoint> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2 offset = p.GetPosition() - intruderPosition;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > closestSqrMag) continue;
            if (sqrMag > RISK_RANGE * RISK_RANGE) continue;

            RaycastHit2D hit = Physics2D.Linecast(intruderPosition, p.GetPosition(), LayerMask.GetMask("Wall"));
            bool isVisible = Equals(hit.collider, null);
            if (!isVisible) continue;

            closestRisk = p.GetProbability();
            closestSqrMag = sqrMag;
        }

        _currentRiskValue = Mathf.Round(closestRisk * 100f) * 0.01f;
    }

    private bool IsPathRisky(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxRisk)
    {
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

        List<WayPoint> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath =
                GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);

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

        if (Equals(riskiestSpot, null)) return false;

        bool isRisky = highestRisk >= maxRisk;

        return isRisky;
    }

    /// <summary>
    /// Check if it is possible for the intruder to run into the sights of any of the guards.
    /// </summary>
    /// <returns></returns>
    private bool IsRiskyMeetUp(RoadMap roadMap, Intruder intruder, List<Guard> guards, float riskTolerance)
    {
        _riskSpots.Clear();

        // Insert a risk spot for each guard
        foreach (var g in guards)
        {
            PossiblePosition riskSpot = new PossiblePosition(null, g);
            riskSpot.risk = Mathf.NegativeInfinity;
            riskSpot.sqrDistance = Mathf.Infinity;
            _riskSpots[g.name] = riskSpot;
        }

        List<WayPoint> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath =
                GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);

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
        
        // loop through the risky spots for each guard to see if the intruder might be spotted in them
        foreach (var spot in _riskSpots)
        {
            if(spot.Value.risk <= riskTolerance) continue; 
            
            float intruderPathDistanceToSpot = PathFinding.Instance.GetShortestPathDistance(intruder.GetTransform().position,
                spot.Value.GetPosition().Value);
            
            float guardPathDistanceToSpot = PathFinding.Instance.GetShortestPathDistance(spot.Value.npc.GetTransform().position,
                spot.Value.GetPosition().Value);
            guardPathDistanceToSpot -= Properties.GetFovRadius(NpcType.Guard);
            guardPathDistanceToSpot = Mathf.Clamp(guardPathDistanceToSpot, 0f, guardPathDistanceToSpot);

            if (guardPathDistanceToSpot < intruderPathDistanceToSpot)
                return true;
        }

        return false;
    }

    public void CheckPathRisk(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxAcceptedRisk)
    {
        if (_isTrajectoryInterceptionCoRunning && !intruder.IsBusy()) return;
        StartCoroutine(TrajectoryInterceptionCO(roadMap, intruder, guards, maxAcceptedRisk));
    }


    private IEnumerator TrajectoryInterceptionCO(RoadMap roadMap, Intruder intruder, List<Guard> guards,
        float maxAcceptedRisk)
    {
        _isTrajectoryInterceptionCoRunning = true;
        // if (IsPathRisky(roadMap, intruder, guards, maxAcceptedRisk))
        if (IsRiskyMeetUp(roadMap, intruder, guards, maxAcceptedRisk))
        {
            intruder.ClearGoal();
            // Debug.Log("Cancel Plan");
        }
        else
            yield return new WaitForSeconds(UpdateIntervalInSeconds);

        _isTrajectoryInterceptionCoRunning = false;
    }

    public void Draw(Vector2 intruderPos)
    {
        Handles.Label(intruderPos + Vector2.down * 0.5f, _currentRiskValue.ToString());

        foreach (var spot in _riskSpots)
        {
            float value = Mathf.Round(spot.Value.risk * 100f) * 0.01f;
            spot.Value.Draw(value.ToString(), Color.red);
        }
    }
}