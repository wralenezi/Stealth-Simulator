using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPatroler : Patroler
{
    public override void Initiate(MapManager mapManager, GuardBehaviorParams guardParams)
    {

    }

    public override void Start()
    {

    }

    public override void UpdatePatroler(List<Guard> guards, float speed, float timeDelta)
    {
    }

    public override void Patrol(List<Guard> guards)
    {
        foreach (var guard in guards)
            if (!guard.IsBusy())
            {
                Vector2 randomRoadmap =
                    MapManager.Instance.mapDecomposer.GetRandomPolygonInNavMesh().GetRandomPosition();
                guard.SetDestination(randomRoadmap, false, false);
            }
    }
}

public class RandomPatrolerParams : PatrolerParams
{
    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += GetType();
        
        return output;
    }

    
}