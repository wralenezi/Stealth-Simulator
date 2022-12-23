using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RoadMapPatroler : Patroler
{
    public bool RenderSegments;
    
    // Road map of the level
    private RoadMap _RoadMap;

    private RoadMapPatrolerDecisionMaker _decisionMaker;

    private RoadMapPatrolerParams _params;

    public override void Initiate(MapManager mapManager, GuardBehaviorParams guardParams)
    {
        _RoadMap = mapManager.GetRoadMap();
        
        _params = (RoadMapPatrolerParams) guardParams.patrolerParams;
        
        _decisionMaker = new RoadMapPatrolerDecisionMaker();
        _decisionMaker.Initiate();
    }

    public override void Start()
    {
        FillSegments();
    }

    public override void UpdatePatroler(List<Guard> guards, float speed, float timeDelta)
    {
        float maxProbability = 0f;

        // Spread the probability similarly to Third eye crime
        foreach (var line in _RoadMap.GetLines(false))
        {
            line.PropagateProb();
            line.IncreaseProbability(speed, timeDelta);
            line.ExpandSs(speed, timeDelta);

            // Get the max probability
            float prob = line.GetSearchSegment().GetProbability();
            if (maxProbability < prob)
                maxProbability = prob;
        }

        foreach (var line in _RoadMap.GetLines(false))
        {
            CheckSeenSs(guards, line);

            SearchSegment sS = line.GetSearchSegment();
            if (Math.Abs(maxProbability) > 0.0001f)
                sS.SetProb(sS.GetProbability() / maxProbability);
            else
                sS.SetProb(sS.GetProbability());
        }
    }

    public override void Patrol(List<Guard> guards)
    {
        AssignGoals(guards);
    }

    private void AssignGoals(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (!guard.IsBusy())
                _decisionMaker.SetTarget(guard, guards, _params, _RoadMap);
        }
    }

    // Check for the seen search segments
    private void CheckSeenSs(List<Guard> guards, RoadMapLine line)
    {
        foreach (var guard in guards)
        {
            // Trim the parts seen by the guards and reset the section if it is all seen 
            line.CheckSeenSegment(guard);
        }
    }

    // Set the road map segments to 1. To mark the beginning of the patrol shift
    public void FillSegments()
    {
        foreach (var line in _RoadMap.GetLines(false))
        {
            line.PropagateToSegment(line.GetMid(), 1f, StealthArea.GetElapsedTimeInSeconds());
        }
    }

    
    private void OnDrawGizmos()
    {
        if (RenderSegments)
            if (_RoadMap != null)
            {
                foreach (var line in _RoadMap.GetLines(false))
                {
                    float label = Mathf.Round(line.GetUtility() * 100f) / 100f;
                    line.DrawSearchSegment("");//label.ToString());
                }
            }
    }
}

public class RoadMapPatrolerParams : PatrolerParams
{
    public readonly float MaxNormalizedPathLength;
    public readonly float StalenessWeight;
    public readonly float PassingGuardsWeight;
    public readonly float ConnectivityWeight;
    public readonly RMDecision DecisionType;
    public readonly RMPassingGuardsSenstivity PGSen;

    public RoadMapPatrolerParams(float _maxNormalizedPathLength, float _stalenessWeight, float _PassingGuardsWeight, float _connectivityWeight, RMDecision _decisionType, RMPassingGuardsSenstivity _pgSen)
    {
        MaxNormalizedPathLength = _maxNormalizedPathLength;
        StalenessWeight = _stalenessWeight;
        PassingGuardsWeight = _PassingGuardsWeight;
        ConnectivityWeight = _connectivityWeight;
        DecisionType = _decisionType;
        PGSen = _pgSen;
    }

    public override string ToString()
    {
        string output = "";
        string sep = "_";
        
        output += MaxNormalizedPathLength;
        output += sep;

        output += StalenessWeight;
        output += sep;

        output += PassingGuardsWeight;
        output += sep;

        output += ConnectivityWeight;
        output += sep;

        output += DecisionType;
        output += sep;

        output += PGSen;
        // output += sep;
        
        
        return output;
    }
}