using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisMeshPatroler : Patroler
{
    private VisMesh _visMesh;

    public override void Initiate(MapManager mapManager)
    {
        _visMesh = gameObject.AddComponent<VisMesh>();
        _visMesh.Initiate();
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
    }
}