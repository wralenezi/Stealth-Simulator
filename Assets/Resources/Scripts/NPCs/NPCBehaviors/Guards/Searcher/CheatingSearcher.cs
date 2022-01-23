using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatingSearcher : Searcher
{
    public override void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
    }
    
    public override void CommenceSearch(NPC target)
    {
    }

    public override void Search(Guard guard)
    {
        List<Intruder> intrdrs = NpcsManager.Instance.GetIntruders();
        guard.SetDestination(intrdrs[0].GetTransform().position, true, true);
        // m_SA.guardsManager.UpdateWldStNpcs();
        // m_SA.scriptor.ChooseDialog(guard, null, "Plan", m_SA.GetSessionInfo().speechType, 0.9f);
    }

    public override void Clear()
    {
    }
}