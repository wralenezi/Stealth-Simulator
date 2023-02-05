using System;
using System.Collections.Generic;
using UnityEngine;

public class RoadMapSearcher : Searcher
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
        RenderSearchSegments = true;
    }

    public override void CommenceSearch(NPC target)
    {
        Clear();
        _RoadMap.CommenceProbabilityFlow(target.GetTransform().position, target.GetDirection());
    }

    protected override void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
        switch (_params.updateMethod)
        {
            case ProbabilityFlowMethod.Propagation:
                PropagateProbability(speed, guards, timeDelta);
                break;

            case ProbabilityFlowMethod.Diffuse:
                DiffuseProbability();
                break;
        }

        foreach (var line in _RoadMap.GetLines(false))
        {
            CheckSeenSs(guards, line);
            line.ExpandSs(speed, timeDelta);
        }

        NormalizeSegments();
    }

    private void PropagateProbability(float speed, List<Guard> guards, float timeDelta)
    {
        float maxProbability = Mathf.NegativeInfinity;

        // Spread the probability similarly to Third eye crime
        foreach (var line in _RoadMap.GetLines(false))
        {
            line.PropagateProb();
            line.IncreaseProbability(speed, timeDelta);
            // line.ExpandSs(speed, timeDelta);

            float prob = line.GetSearchSegment().GetProbability();
            if (maxProbability < prob) maxProbability = prob;

            if (float.IsNaN(prob))
            {
                CommenceSearch(m_Intruder);
                break;
            }
        }
    }


    // Diffusing the probability among neighboring segments 
    // Source: EXPLORATION AND COMBAT IN NETHACK - Johnathan Campbell - Chapter 2.2.1
    public void DiffuseProbability()
    {
        foreach (var line in _RoadMap.GetLines(false))
        {
            SearchSegment sS = line.GetSearchSegment();

            float probabilitySum = 0f;
            int neighborsCount = 0;

            foreach (var con in line.GetWp1Connections())
                if (line != con)
                {
                    probabilitySum += con.GetSearchSegment().OldProbability;
                    neighborsCount++;
                }

            foreach (var con in line.GetWp2Connections())
                if (line != con)
                {
                    probabilitySum += con.GetSearchSegment().OldProbability;
                    neighborsCount++;
                }


            float newProbability = (1f - Properties.ProbDiffFac) * sS.OldProbability +
                                   probabilitySum * Properties.ProbDiffFac / neighborsCount;


            sS.SetProb(newProbability);
        }
    }


    public override void Clear()
    {
        base.Clear();
        _RoadMap.ClearSearchSegments();
    }

    // Normalize the probabilities of the segments
    // if the max prob is zero, then find the max prob
    protected void NormalizeSegments()
    {
        float maxProbability = 0f;
        float minProbability = 0f;
        foreach (var line in _RoadMap.GetLines(false))
        {
            SearchSegment sS = line.GetSearchSegment();
            if (sS.GetProbability() > maxProbability) maxProbability = sS.GetProbability();
            if (sS.GetProbability() < minProbability) minProbability = sS.GetProbability();
        }

        maxProbability = Equals(maxProbability, 0f) ? 1f : maxProbability;


        foreach (var line in _RoadMap.GetLines(false))
        {
            SearchSegment sS = line.GetSearchSegment();

            float normalizedProbability = (sS.GetProbability() - minProbability) / (maxProbability - minProbability);
            sS.SetProb(normalizedProbability);
            sS.OldProbability = sS.GetProbability();
        }
    }

    protected override void Search(List<Guard> guards)
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

    public readonly ProbabilityFlowMethod updateMethod;

    // Search Params

    // The search segment's age weight (How long was it last seen)
    public float ageWeight;

    // Path distance of the search segment to the guard
    public float dstToGuardsWeight;

    // Path distance of the closest goal other guards are coming to visit
    public float dstFromOwnWeight;

    public RoadMapSearcherParams(float _maxNormalizedPathLength, float _stalenessWeight, float _PassingGuardsWeight,
        float _connectivityWeight, RMDecision _decisionType, RMPassingGuardsSenstivity _pgSen, float _ageWeight,
        float _dstToGuardsWeight, float _dstFromOwnWeight, ProbabilityFlowMethod _updateMethod)
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
        updateMethod = _updateMethod;
    }

    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += GetType();
        output += sep;
        
        output += DecisionType;
        output += sep;

        if (DecisionType == RMDecision.DijkstraPath)
        {
            output += MaxNormalizedPathLength;
            output += sep;

            output += StalenessWeight;
            output += sep;

            output += PassingGuardsWeight;
            output += sep;

            output += ConnectivityWeight;
            output += sep;

            output += PGSen;
        }
        else
        {
            output += StalenessWeight;
            output += sep;

            output += ageWeight;
            output += sep;

            output += dstFromOwnWeight;
            output += sep;

            output += dstToGuardsWeight;
        }

        return output;
    }
}