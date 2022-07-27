using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisMeshPatroler : Patroler
{
    private VisMesh _visMesh;

    public bool ShowGuardGoals;
    private VisMeshPatrolDecisionMaker _decisionMaker;

    private VisMeshPatrolerParams _params;

    public override void Initiate(MapManager mapManager, GuardBehaviorParams guardParams)
    {
        _params = (VisMeshPatrolerParams) guardParams.patrolerParams;

        _visMesh = gameObject.AddComponent<VisMesh>();
        _visMesh.Initiate(_params.MaxSeenRegionAreaPerGuard);

        _decisionMaker = new VisMeshPatrolDecisionMaker();
        _decisionMaker.Initiate(_params);
    }

    public override void Start()
    {
        _visMesh.Reset();
        _decisionMaker.Reset();
    }

    public override void UpdatePatroler(List<Guard> guards, float speed, float timeDelta)
    {
        _visMesh.ConstructVisMesh(guards);
    }

    public override void Patrol(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (guard.IsBusy()) continue;

            _decisionMaker.SetTarget(guard, guards, _params, _visMesh.GetVisMesh());
        }
    }


    private void OnDrawGizmos()
    {
        if (ShowGuardGoals)
            _decisionMaker?.DrawGoals();
    }
}

public class VisMeshPatrolerParams : PatrolerParams
{
    public readonly float MaxSeenRegionAreaPerGuard;
    public readonly float AreaWeight;
    public readonly float StalenessWeight;
    public readonly float DistanceWeight;
    public readonly float SeparationWeight;
    public readonly VMDecision DecisionType;

    public VisMeshPatrolerParams(float _maxSeenRegionAreaPerGuard, float _areaWeight, float _stalenessWeight,
        float _distanceWeight, float _separationWeight, VMDecision _decisionType)
    {
        MaxSeenRegionAreaPerGuard = _maxSeenRegionAreaPerGuard;
        AreaWeight = _areaWeight;
        StalenessWeight = _stalenessWeight;
        DistanceWeight = _distanceWeight;
        SeparationWeight = _separationWeight;
        DecisionType = _decisionType;
    }

    public override string ToString()
    {
        string output = "";
        string sep = "_";
        
        output += MaxSeenRegionAreaPerGuard;
        output += sep;

        output += AreaWeight;
        output += sep;

        output += StalenessWeight;
        output += sep;

        output += DistanceWeight;
        output += sep;

        output += SeparationWeight;
        output += sep;

        output += DecisionType;
        // output += sep;
        
        
        return output;
    }
}