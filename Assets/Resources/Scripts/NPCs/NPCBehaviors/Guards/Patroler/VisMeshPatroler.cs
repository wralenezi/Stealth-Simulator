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
        _params = (VisMeshPatrolerParams) StealthArea.SessionInfo.patrolerParams;

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
        // throw new System.NotImplementedException();
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


    public VisMeshPatrolerParams(float _maxSeenRegionAreaPerGuard)
    {
        maxSeenRegionAreaPerGuard = _maxSeenRegionAreaPerGuard;
    }
}