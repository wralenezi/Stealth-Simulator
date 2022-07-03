using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisMeshPatroler : Patroler
{
    private VisMesh _visMesh;

    private MapDecomposer _decomposer;
    
    public override void Initiate(MapManager mapManager)
    {
        _visMesh.Initiate(mapManager.mapDecomposer);
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
        throw new System.NotImplementedException();
    }
    
    
}
