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
    private Dictionary<string, RiskyPosition> _riskSpots;

    // Variables for the coroutine for checking the risk of taking a current path. 
    private bool _isTrajectoryInterceptionCoRunning;

    // Update frequency parameters
    private const float UpdateIntervalInSeconds = 0.05f;

    public static RMRiskEvaluator Instance;

    // Start is called before the first frame update
    public void Initiate()
    {
        _riskSpots = new Dictionary<string, RiskyPosition>();
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
        Vector2 intruderPosition = intruder.GetTransform().position;

        if (Equals(_intruderRiskSpot, null))
            _intruderRiskSpot = new RiskyPosition(intruder.GetTransform().position, 0f);

        float closestRisk = 0f;
        float closestSqrMag = Mathf.Infinity;

        List<RoadMapNode> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2 offset = p.GetPosition() - intruderPosition;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > closestSqrMag) continue;
            if (sqrMag > RISK_RANGE * RISK_RANGE) continue;

            // RaycastHit2D hit = Physics2D.Linecast(intruderPosition, p.GetPosition(), LayerMask.GetMask("Wall"));
            // bool isVisible = Equals(hit.collider, null);

            bool isVisible =
                GeometryHelper.IsCirclesVisible(intruderPosition, p.GetPosition(), Properties.NpcRadius, "Wall");
            if (!isVisible) continue;

            closestRisk = p.GetProbability();
            closestSqrMag = sqrMag;
        }

        _intruderRiskSpot.position = intruderPosition;
        _intruderRiskSpot.risk = Mathf.Round(closestRisk * 100f) * 0.01f;
    }


    // private void SetRiskyPositions(RoadMap roadMap, List<Guard> guards, Intruder intruder)
    // {
    //     _riskSpots.Clear();
    //
    //     // Insert a risk spot for each guard
    //     foreach (var g in guards)
    //     {
    //         RiskyPosition riskSpot = new RiskyPosition();
    //         riskSpot.risk = Mathf.NegativeInfinity;
    //         riskSpot.sqrMag = Mathf.Infinity;
    //         _riskSpots[g.name] = riskSpot;
    //     }
    //
    //     List<RoadMapNode> possiblePositions = roadMap.GetPossibleGuardPositions();
    //
    //     foreach (var p in possiblePositions)
    //     {
    //         Vector2? pointOnPath =
    //             GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);
    //
    //         if (Equals(pointOnPath, null)) continue;
    //
    //         Vector2 offset = pointOnPath.Value - p.GetPosition();
    //         RiskyPosition riskSpot = _riskSpots[p.GetPassingGuard().name];
    //
    //         if (riskSpot.sqrMag >= offset.sqrMagnitude)
    //         {
    //             // if (riskSpot.risk < p.GetProbability())
    //             // {
    //             riskSpot.position = pointOnPath.Value;
    //             riskSpot.npc = p.GetPassingGuard();
    //             riskSpot.risk = p.GetProbability();
    //             riskSpot.sqrMag = offset.sqrMagnitude;
    //             // }
    //         }
    //     }
    // }

    private void SetRiskyPositions(RoadMap roadMap, List<Guard> guards, Intruder intruder)
    {
        float fov = Properties.GetFovRadius(NpcType.Guard);
        _riskSpots.Clear();

        // Insert a risk spot for each guard
        foreach (var g in guards)
        {
            RiskyPosition riskSpot = new RiskyPosition();
            riskSpot.risk = Mathf.NegativeInfinity;
            riskSpot.sqrMag = Mathf.Infinity;
            _riskSpots[g.name] = riskSpot;
        }

        List<RoadMapNode> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath =
                GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);

            if (Equals(pointOnPath, null)) continue;

            Vector2 offset = pointOnPath.Value - p.GetPosition();
            RiskyPosition riskSpot = _riskSpots[p.GetPassingGuard().name];
            float sqrMag = offset.sqrMagnitude;

            // if(sqrMag > fov * fov) continue;
            //
            // if (riskSpot.risk < p.GetProbability())
            // {
            //     riskSpot.position = pointOnPath.Value;
            //     riskSpot.npc = p.GetPassingGuard();
            //     riskSpot.risk = p.GetProbability();
            //     riskSpot.sqrMag = offset.sqrMagnitude;
            // }


            if (riskSpot.sqrMag >= offset.sqrMagnitude)
            {
                if (riskSpot.risk < p.GetProbability())
                {
                    riskSpot.position = pointOnPath.Value;
                    riskSpot.npc = p.GetPassingGuard();
                    riskSpot.risk = p.GetProbability();
                    riskSpot.sqrMag = offset.sqrMagnitude;
                }
            }
        }
    }

    // private void SetRiskyPosition(RoadMap roadMap, List<Guard> guards, Intruder intruder)
    // {
    //     _riskSpots.Clear();
    //
    //     // Insert a risk spot for each guard
    //     foreach (var g in guards)
    //     {
    //         RiskyPosition riskSpot = new RiskyPosition();
    //         riskSpot.risk = Mathf.NegativeInfinity;
    //         riskSpot.sqrMag = Mathf.Infinity;
    //         _riskSpots[g.name] = riskSpot;
    //     }
    //
    //     foreach (var riskyPosition in _riskSpots)
    //     {
    //         Vector2? pointOnPath =
    //             GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);
    //
    //         riskyPosition.Value.position = 
    //         SetRisk(riskyPosition.Value, roadMap);
    //     }
    // }


    private void SetRisk(RiskyPosition riskyPosition, RoadMap roadMap, Intruder intruder)
    {
        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);

        float closestRisk = 0f;
        float closestSqrMag = Mathf.Infinity;

        List<RoadMapNode> possiblePositions = roadMap.GetPossibleGuardPositions();

        foreach (var p in possiblePositions)
        {
            Vector2? pointOnPath =
                GeometryHelper.GetClosetPointOnPath(intruder.GetFullPath(), p.GetPosition(), Properties.NpcRadius);

            Vector2 offset = p.GetPosition() - riskyPosition.position;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > closestSqrMag) continue;
            if (sqrMag > RISK_RANGE * RISK_RANGE) continue;

            bool isVisible =
                GeometryHelper.IsCirclesVisible(riskyPosition.position, p.GetPosition(), Properties.NpcRadius, "Wall");
            if (!isVisible) continue;

            closestRisk = p.GetProbability();
            closestSqrMag = sqrMag;
        }

        riskyPosition.risk = Mathf.Round(closestRisk * 100f) * 0.01f;
    }


    private bool IsPathRisky(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxPathRisk)
    {
        SetRiskyPositions(roadMap, guards, intruder);

        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);

        float highestRisk = Mathf.NegativeInfinity;
        RiskyPosition riskiestSpot = null;

        foreach (var spot in _riskSpots)
        {
            if (spot.Value.sqrMag > RISK_RANGE * RISK_RANGE) continue;

            if (highestRisk < spot.Value.risk)
            {
                highestRisk = spot.Value.risk;
                riskiestSpot = spot.Value;
            }
        }

        if (Equals(riskiestSpot, null)) return false;

        bool isRisky = highestRisk > maxPathRisk;

        return isRisky;
    }


    /// <summary>
    /// Check if it is possible for the intruder to run into the sights of any of the guards.
    /// </summary>
    /// <returns></returns>
    private bool IsRiskyMeetUp(RoadMap roadMap, Intruder intruder, List<Guard> guards, float maxPathRisk)
    {
        SetRiskyPositions(roadMap, guards, intruder);

        float RISK_RANGE = Properties.GetFovRadius(NpcType.Guard);

        // loop through the risky spots for each guard to see if the intruder might be spotted in them
        foreach (var spot in _riskSpots)
        {
            if (spot.Value.risk <= maxPathRisk) continue;
            if (spot.Value.sqrMag > RISK_RANGE * RISK_RANGE) continue;


            float intruderPathDistanceToSpot = PathFinding.Instance.GetShortestPathDistance(
                intruder.GetTransform().position,
                spot.Value.position);

            float guardPathDistanceToSpot = PathFinding.Instance.GetShortestPathDistance(
                spot.Value.npc.GetTransform().position,
                spot.Value.position);
            guardPathDistanceToSpot -= Properties.GetFovRadius(NpcType.Guard);
            guardPathDistanceToSpot = Mathf.Clamp(guardPathDistanceToSpot, 0f, guardPathDistanceToSpot);


            float arrivalTimeIntruder =
                intruderPathDistanceToSpot / (Properties.IntruderSpeedMulti * Properties.NpcSpeed);
            float arrivalTimeGuard = guardPathDistanceToSpot / Properties.NpcSpeed;

            if (arrivalTimeGuard < arrivalTimeIntruder) return true;

            // float timedDiff = Mathf.Abs(arrivalTimeGuard - arrivalTimeIntruder);
            // if (timedDiff < 2f) return true;
        }

        return false;
    }

    public void CheckPathRisk(PathCanceller pathCancelType, RoadMap roadMap, Intruder intruder, List<Guard> guards)
    {
        if (_isTrajectoryInterceptionCoRunning && !intruder.IsBusy()) return;
        StartCoroutine(TrajectoryInterceptionCO(pathCancelType, roadMap, intruder, guards));
    }


    private IEnumerator TrajectoryInterceptionCO(PathCanceller pathCancelType, RoadMap roadMap, Intruder intruder,
        List<Guard> guards)
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
            intruder.ClearGoal();
            // Debug.Log("Cancel Plan");
        }
        else
            yield return new WaitForSeconds(UpdateIntervalInSeconds);

        _riskSpots.Clear();
        _isTrajectoryInterceptionCoRunning = false;
    }

    public void Draw(Vector2 intruderPos)
    {
        if (!Equals(_intruderRiskSpot, null))
            Handles.Label(intruderPos + Vector2.down * 0.5f, _intruderRiskSpot.risk.ToString());

        foreach (var spot in _riskSpots)
        {
            float value = Mathf.Round(spot.Value.risk * 100f) * 0.01f;
            spot.Value.Draw(value.ToString(), Color.red);
        }
    }
}


public class RiskyPosition
{
    public Vector2 position;
    public NPC npc;
    public float risk;
    public float sqrMag;

    public RiskyPosition()
    {
    }

    public RiskyPosition(Vector2 position, float risk)
    {
        this.position = position;
        this.risk = risk;
    }


    public void Draw(string label, Color32 color)
    {
#if UNITY_EDITOR
        Handles.Label(position + Vector2.down * 0.5f, label);
#endif
        Gizmos.color = color;
        Gizmos.DrawSphere(position, 0.2f);
    }
}