using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSearchEvader : SearchEvader
{
    public override void Begin()
    {
        // foreach (var intruder in GetSA().intrdrManager.GetIntruders())
        //     intruder.ClearIntruderGoal();
    }

    public override void Refresh()
    {
        // foreach (var intruder in GetSA().intrdrManager.GetIntruders())
        // {
        //     // if (intruder.GetNpcData().intruderPlanner == IntruderPlanner.UserInput) return;
        //     
        //     if(intruder.IsBusy()) return;
        //     
        //     m_HsC.AssignHidingSpotsFitness(GetSA().guardsManager.GetGuards(), GetSA().mapDecomposer.GetNavMesh());
        //     intruder.SetDestination(m_HsC.GetBestHidingSpot().Value, true, false);
        //     // StartCoroutine(intruder.waitThenMove(m_HsC.GetBestHidingSpot().Value));
        // }
    }
}

public class SimpleSearchEvaderParams : SearchEvaderParams
{
}