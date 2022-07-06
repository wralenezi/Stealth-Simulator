using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisMeshPatroler : Patroler
{
    private VisMesh _visMesh;
    private VisMeshPatrolDecisionMaker _decisionMaker;

    private VisMeshPatrolerParams _params;

    public override void Initiate(MapManager mapManager)
    {
        _params = (VisMeshPatrolerParams) StealthArea.SessionInfo.guardBehaviorParams.patrolerParams;

        _visMesh = gameObject.AddComponent<VisMesh>();
        _visMesh.Initiate(_params.maxSeenRegionAreaPerGuard);
        
        _decisionMaker = new VisMeshPatrolDecisionMaker();
    }

    public override void Start()
    {
        _visMesh.Reset();
    }

    public override void UpdatePatroler(List<Guard> guards, float speed, float timeDelta)
    {
        _visMesh.ConstructVisMesh(guards);
    }
    
    public override void Patrol(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if(guard.IsBusy()) continue;
            
            _decisionMaker.SetTarget(guard, _visMesh.GetVisMesh());
        }
    }
}

public class VisMeshPatrolerParams : PatrolerParams
{
    public float maxSeenRegionAreaPerGuard;

    public float areaWeight;
    public float stalenessWeight;
    public float distanceWeight;
    public float separationWeight;


    public VisMeshPatrolerParams(float _maxSeenRegionAreaPerGuard, float _areaWeight, float _stalenessWeight, float _distanceWeight, float _separationWeight)
    {
        maxSeenRegionAreaPerGuard = _maxSeenRegionAreaPerGuard;

        areaWeight = _areaWeight;
        stalenessWeight = _stalenessWeight;
        distanceWeight = _distanceWeight;
        separationWeight = _separationWeight;
    }
}