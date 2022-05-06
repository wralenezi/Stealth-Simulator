using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoutRiskEvaluator : MonoBehaviour
{
    // How risky is the intruder's current position is, 0 is safe and 1 is spotted.
    private float _currentRiskValue;
    private Dictionary<string, PossiblePosition> _riskSpots;
    
    // Variables for the coroutine for checking the risk of taking a current path. 
    private bool _isTrajectoryInterceptionCoRunning;

    // Update frequency parameters
    private const float UpdateIntervalInSeconds = 0.1f;


    // Start is called before the first frame update
    public void Initiate()
    {
        _riskSpots = new Dictionary<string, PossiblePosition>();
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

    public void UpdateCurrentRisk(RoadMap roadMap, float npcRadius)
    {
        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);

        Intruder intruder = NpcsManager.Instance.GetIntruders()[0];
        Vector2 intruderPosition = intruder.GetTransform().position;

        float minSqrMag = Mathf.Infinity;
        float risk = 0f;

        List<WayPoint> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2 offset = p.GetPosition() - intruderPosition;
            float sqrMag = offset.sqrMagnitude;
            bool isVisible = GeometryHelper.IsCirclesVisible(intruderPosition, p.GetPosition(), npcRadius, "Wall");

            if (minSqrMag > sqrMag && isVisible && sqrMag <= RISK_RANGE * RISK_RANGE)
            {
                minSqrMag = sqrMag;
                risk = p.GetProbability();
            }
        }

        _currentRiskValue = risk;
    }

    private bool IsPathRisky(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxRisk, float npcRadius)
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

        List<WayPoint> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath =
                GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), npcRadius);

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

        // float distanceFromRisk = Vector2.Distance(riskiestSpot.GetPosition().Value, intruder.GetTransform().position);
        // return distanceFromRisk > IGNORE_RISK_RANGE;

        return _currentRiskValue < highestRisk;
    }

    public void CheckPathRisk(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxAcceptedRisk, float npcRadius)
    {
        if (_isTrajectoryInterceptionCoRunning && !intruder.IsBusy()) return;
        StartCoroutine(TrajectoryInterceptionCO(roadMap, intruder, guards, maxAcceptedRisk, npcRadius));
    }



    private IEnumerator TrajectoryInterceptionCO(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxAcceptedRisk, float npcRadius)
    {
        _isTrajectoryInterceptionCoRunning = true;
        if (IsPathRisky(roadMap, intruder, guards, maxAcceptedRisk, npcRadius))
        {
            intruder.ClearGoal();
            // Debug.Log("Cancel Plan");
        }
        else
            yield return new WaitForSeconds(UpdateIntervalInSeconds);

        _isTrajectoryInterceptionCoRunning = false;
    }

    public void Draw()
    {
        foreach (var spot in _riskSpots)
        {
            float value = Mathf.Round(spot.Value.risk * 100f) * 0.01f;
            spot.Value.Draw(value.ToString(), Color.green);
        }
    }
}