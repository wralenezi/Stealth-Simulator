using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSearcher : Searcher
{
    public override void CommenceSearch(NPC target)
    {
    }

    protected override void UpdateSearcher(float speed, List<Guard> guards, float timeDelta)
    {
    }

    protected override void Search(List<Guard> guards)
    {
        foreach (var guard in guards)
            if (!guard.IsBusy())
            {
                Vector2 randomRoadmap =
                    MapManager.Instance.mapDecomposer.GetRandomPolygonInNavMesh().GetRandomPosition();
                guard.SetDestination(randomRoadmap, false, false);
                // m_SA.guardsManager.UpdateWldStNpcs();
                // m_SA.scriptor.ChooseDialog(guard, null, "Plan", m_SA.GetSessionInfo().speechType, 0.9f);
            }
    }


    public override void Clear()
    {
    }
}

public class RandomSearcherParams : SearcherParams
{
}