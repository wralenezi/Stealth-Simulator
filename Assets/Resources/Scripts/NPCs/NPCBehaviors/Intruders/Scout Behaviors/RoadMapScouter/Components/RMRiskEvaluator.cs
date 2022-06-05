using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RMRiskEvaluator : MonoBehaviour
{
    // How risky is the intruder's current position is, 0 is safe and 1 is spotted.
    [SerializeField] private RiskyPosition _intruderRiskSpot;
    private List<RiskyPosition> _riskSpots;

    // Variables for the coroutine for checking the risk of taking a current path. 
    private bool _isTrajectoryInterceptionCoRunning;

    // Update frequency parameters
    private const float UpdateIntervalInSeconds = 0.05f;

    public static RMRiskEvaluator Instance;

    // Start is called before the first frame update
    public void Initiate()
    {
        _riskSpots = new List<RiskyPosition>();
        Instance = this;
    }


    // Update is called once per frame
    public void Clear()
    {
        _riskSpots.Clear();
    }

    public float GetRisk()
    {
        return _intruderRiskSpot.risk;
    }

    public void UpdateCurrentRisk(RoadMap roadMap)
    {
        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);
        Intruder intruder = NpcsManager.Instance.GetIntruders()[0];

        _intruderRiskSpot ??= new RiskyPosition();

        _intruderRiskSpot.Reset();
        _intruderRiskSpot.position = intruder.GetTransform().position;

        SetMaxRiskWithinRange(roadMap, RISK_RANGE, intruder, ref _intruderRiskSpot);
        // SetClosestRiskWithinRange(roadMap, RISK_RANGE, intruder, ref _intruderRiskSpot);
    }

    private void SetRiskyPositions(RoadMap roadMap, List<Guard> guards, Intruder intruder)
    {
        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);

        if (_riskSpots.Count == 0)
            // Insert a risk spot for each guard
            foreach (var g in guards)
            {
                RiskyPosition riskSpot = new RiskyPosition();
                _riskSpots.Add(riskSpot);
            }


        // foreach (var riskSpot in _riskSpots)
        for (int i = 0; i < _riskSpots.Count; i++)
        {
            RiskyPosition risk = _riskSpots[i];
            risk.Reset();
            SetMaxRiskWithinRange(roadMap, RISK_RANGE, intruder, ref risk);
            // SetClosestRiskWithinRange(roadMap, RISK_RANGE, intruder, ref risk);
        }
    }


    private void SetMaxRiskWithinRange(RoadMap roadMap, float range, Intruder intruder, ref RiskyPosition riskyPosition)
    {
        List<RoadMapNode> possiblePositions = roadMap.GetPossibleGuardPositions();

        bool isIntruderRiskyPoint = !Equals(riskyPosition.position, null);

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath = null;

            if (!isIntruderRiskyPoint)
                pointOnPath =
                    GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);
            else
                pointOnPath = intruder.GetTransform().position;


            if (Equals(pointOnPath, null)) continue;

            Vector2 offset = p.GetPosition() - pointOnPath.Value;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > range * range) continue;

            bool isVisible =
                GeometryHelper.IsCirclesVisible(pointOnPath.Value, p.GetPosition(), Properties.NpcRadius,
                    "Wall");
            if (!isVisible) continue;

            if (riskyPosition.risk < p.GetProbability())
            {
                riskyPosition.position = pointOnPath;
                riskyPosition.npc = p.GetPassingGuard();
                riskyPosition.risk = p.GetProbability();
                riskyPosition.sqrMag = offset.sqrMagnitude;
            }
        }
    }

    private void SetClosestRiskWithinRange(RoadMap roadMap, float range, Intruder intruder, ref
        RiskyPosition riskyPosition)
    {
        List<RoadMapNode> possiblePositions = roadMap.GetPossibleGuardPositions();

        bool isIntruderRiskyPoint = !Equals(riskyPosition.position, null);

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath = null;

            if (!isIntruderRiskyPoint)
                pointOnPath =
                    GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);
            else
                pointOnPath = intruder.GetTransform().position;


            if (Equals(pointOnPath, null)) continue;

            Vector2 offset = p.GetPosition() - pointOnPath.Value;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > range * range) continue;

            bool isVisible =
                GeometryHelper.IsCirclesVisible(pointOnPath.Value, p.GetPosition(), Properties.NpcRadius,
                    "Wall");
            if (!isVisible) continue;

            if (riskyPosition.sqrMag > sqrMag)
            {
                riskyPosition.position = pointOnPath;
                riskyPosition.npc = p.GetPassingGuard();
                riskyPosition.risk = p.GetProbability();
                riskyPosition.sqrMag = offset.sqrMagnitude;
            }
        }
    }

    private bool IsPathRisky(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxPathRisk)
    {
        SetRiskyPositions(roadMap, guards, intruder);

        foreach (var spot in _riskSpots)
        {
            if (Equals(spot.position, null)) continue;

            if (maxPathRisk < spot.risk)
            {
                Vector2 offsetToIntruder = spot.position.Value - _intruderRiskSpot.position.Value;
                float sqrMag = offsetToIntruder.sqrMagnitude;

                if (sqrMag < 1f) continue;

                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Check if it is possible for the intruder to run into the sights of any of the guards.
    /// </summary>
    /// <returns></returns>
    private bool IsRiskyMeetUp(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxPathRisk)
    {
        SetRiskyPositions(roadMap, guards, intruder);

        // loop through the risky spots for each guard to see if the intruder might be spotted in them
        foreach (var spot in _riskSpots)
        {
            if (spot.risk <= maxPathRisk) continue;


            float intruderPathDistanceToSpot = PathFinding.Instance.GetShortestPathDistance(
                intruder.GetTransform().position,
                spot.position.Value);

            float guardPathDistanceToSpot = PathFinding.Instance.GetShortestPathDistance(
                spot.npc.GetTransform().position,
                spot.position.Value);
            guardPathDistanceToSpot -= Properties.GetFovRadius(NpcType.Guard);
            guardPathDistanceToSpot = Mathf.Clamp(guardPathDistanceToSpot, 0f, guardPathDistanceToSpot);


            float arrivalTimeIntruder =
                intruderPathDistanceToSpot / (Properties.IntruderSpeedMulti * Properties.NpcSpeed);
            float arrivalTimeGuard = guardPathDistanceToSpot / Properties.NpcSpeed;

            if (arrivalTimeGuard < arrivalTimeIntruder) return true;
        }

        return false;
    }

    public void CheckPathRisk(PathCanceller pathCancelType, RoadMap roadMap, Intruder intruder, List<Guard> guards,
        ref List<HidingSpot> availableSpots,
        ref HidingSpot goalHs)
    {
        // if (_isTrajectoryInterceptionCoRunning && !intruder.IsBusy()) return;
        // StartCoroutine(TrajectoryInterceptionCO(pathCancelType, roadMap, intruder, guards, goalHs));
        
        if (!intruder.IsBusy()) return;
        TrajectoryInterception(pathCancelType, roadMap, intruder, guards, ref availableSpots, ref goalHs);
    }


    private IEnumerator TrajectoryInterceptionCO(PathCanceller pathCancelType, RoadMap roadMap, Intruder intruder,
        List<Guard> guards, HidingSpot goalHs)
    {
        _isTrajectoryInterceptionCoRunning = true;
        float maxPathRisk = RMThresholds.GetMaxPathRisk(RiskThresholdType.Danger);

        bool isPathRisky = false;

        switch (pathCancelType)
        {
            case PathCanceller.DistanceCalculation:
                isPathRisky = IsRiskyMeetUp(roadMap, intruder, guards, maxPathRisk);
                break;

            case PathCanceller.RiskComparison:
                isPathRisky = IsPathRisky(roadMap, intruder, guards, maxPathRisk);
                break;
        }


        if (isPathRisky)
        {
            goalHs?.MarkAsChecked();
            goalHs = null;
            intruder.ClearGoal();
            Debug.Log("Cancel Plan");
        }
        else
            yield return new WaitForSeconds(UpdateIntervalInSeconds);

        // _riskSpots.Clear();
        _isTrajectoryInterceptionCoRunning = false;
    }

    private void TrajectoryInterception(PathCanceller pathCancelType, RoadMap roadMap, Intruder intruder,
        List<Guard> guards, ref List<HidingSpot> availableSpots, ref HidingSpot goalHs)
    {
        float maxPathRisk = RMThresholds.GetMaxPathRisk(RiskThresholdType.Danger);

        bool isPathRisky = false;

        switch (pathCancelType)
        {
            case PathCanceller.DistanceCalculation:
                isPathRisky = IsRiskyMeetUp(roadMap, intruder, guards, maxPathRisk);
                break;

            case PathCanceller.RiskComparison:
                isPathRisky = IsPathRisky(roadMap, intruder, guards, maxPathRisk);
                break;
        }

        if (isPathRisky)
        {
            goalHs?.MarkAsChecked();
            availableSpots.Remove(goalHs);
            goalHs = null;
            intruder.ClearGoal();
        }
    }

    public void Draw()
    {
        if (!Equals(_intruderRiskSpot, null))
        {
            _intruderRiskSpot.Draw(_intruderRiskSpot.risk.ToString(), Color.magenta);
        }

        foreach (var spot in _riskSpots)
        {
            float value = Mathf.Round(spot.risk * 100f) * 0.01f;
            spot.Draw(value.ToString(), Color.magenta);
        }
    }
}

[Serializable]
public class RiskyPosition
{
    public Vector2? position;
    public NPC npc;
    public float risk;
    public float sqrMag;

    public RiskyPosition()
    {
        Reset();
    }

    public void Reset()
    {
        position = null;
        risk = 0f;
        sqrMag = Mathf.Infinity;
        npc = null;
    }

    public RiskyPosition(Vector2 position, float risk)
    {
        this.position = position;
        this.risk = risk;
    }


    public void Draw(string label, Color32 color)
    {
        if (Equals(position, null)) return;

#if UNITY_EDITOR
        Handles.Label(position.Value + Vector2.down * 0.5f, label);
#endif
        Gizmos.color = color;
        Gizmos.DrawSphere(position.Value, 0.2f);
    }
}