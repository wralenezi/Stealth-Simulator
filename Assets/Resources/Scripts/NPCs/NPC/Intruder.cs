using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intruder : NPC
{
    
    public override void Heuristic(float[] actionsOut)
    {
        MoveByInput();
    }

    public override LogSnapshot LogNpcProgress()
    {
        return new LogSnapshot(GetTravelledDistance(), Area.episodeTime, Data, 0, 0f);
    }
}
