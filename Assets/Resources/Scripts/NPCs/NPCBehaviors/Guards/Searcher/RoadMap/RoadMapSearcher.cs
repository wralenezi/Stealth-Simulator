using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class RoadMapSearcher : Searcher
{
    // Road map used for the search
    protected RoadMap _RoadMap;
    
    private RoadMapSearcherDecisionMaker _decisionMaker;

    protected RoadMapSearcherParams _params;

    public bool RenderSearchSegments;

    public override void Initiate(MapManager mapManager, GuardBehaviorParams guardParams)
    {
        base.Initiate(mapManager, guardParams);

        _RoadMap = mapManager.GetRoadMap();

        _params = (RoadMapSearcherParams) guardParams.searcherParams;

        _decisionMaker = new RoadMapSearcherDecisionMaker();
        _decisionMaker.Initiate();
        
    }

    public override void CommenceSearch(NPC target)
    {
        Clear();
        _RoadMap.CommenceProbabilityFlow(target.GetTransform().position, target.GetDirection());
    }

    // Normalize the probabilities of the segments
    // if the max prob is zero, then find the max prob
    protected void NormalizeSegments(float maxProb)
    {
        foreach (var line in _RoadMap.GetLines(false))
        {
            SearchSegment sS = line.GetSearchSegment();

            if (Math.Abs(maxProb) < 0f)
            {
                sS.SetProb(1f);
                continue;
            }

            sS.SetProb(sS.GetProbability() / maxProb);
        }
    }

    public override void Search(List<Guard> guards)
    {
        AssignGoals(guards);
    }
    
    
    private void AssignGoals(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (isStillCheating)
            {
                guard.SetDestination(m_Intruder.GetTransform().position, true, true);
                return;
            }

            if (!guard.IsBusy())
                _decisionMaker.SetTarget(guard, guards, _params, _RoadMap);
        }
    }

    

    // Check for the seen search segments
    protected void CheckSeenSs(List<Guard> guards, RoadMapLine line)
    {
        foreach (var guard in guards)
        {
            // Trim the parts seen by the guards and reset the section if it is all seen 
            line.CheckSeenSegment(guard);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (RenderSearchSegments)
            if (_RoadMap != null)
            {
                foreach (var line in _RoadMap.GetLines(false))
                {
                    float label = Mathf.Round(line.GetUtility() * 100f) / 100f;
                    line.DrawSearchSegment(label.ToString());
                }
            }
    }
}



public class RoadMapSearcherParams : SearcherParams
{
    public readonly float MaxNormalizedPathLength;
    public readonly float StalenessWeight;
    public readonly float PassingGuardsWeight;
    public readonly float ConnectivityWeight;
    public readonly RMDecision DecisionType;
    public readonly RMPassingGuardsSenstivity PGSen;
    
    // Search Params

    // The search segment's age weight (How long was it last seen)
    public float ageWeight;

    // Path distance of the search segment to the guard
    public float dstToGuardsWeight;

    // Path distance of the closest goal other guards are coming to visit
    public float dstFromOwnWeight;

    public float minSegThreshold;

    public RoadMapSearcherParams(float _maxNormalizedPathLength, float _stalenessWeight, float _PassingGuardsWeight, float _connectivityWeight, RMDecision _decisionType, RMPassingGuardsSenstivity _pgSen, float _ageWeight, float _dstToGuardsWeight, float _dstFromOwnWeight, float _minSegThreshold)
    {
        MaxNormalizedPathLength = _maxNormalizedPathLength;
        StalenessWeight = _stalenessWeight;
        PassingGuardsWeight = _PassingGuardsWeight;
        ConnectivityWeight = _connectivityWeight;
        DecisionType = _decisionType;
        PGSen = _pgSen;
        ageWeight = _ageWeight;
        dstToGuardsWeight = _dstToGuardsWeight;
        dstFromOwnWeight = _dstFromOwnWeight;
        minSegThreshold = _minSegThreshold;
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