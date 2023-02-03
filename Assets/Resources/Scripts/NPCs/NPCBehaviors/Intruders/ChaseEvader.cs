using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ChaseEvader : MonoBehaviour
{
    // Hiding spots manager
    public bool ShowHidingSpots;
    protected HidingSpotsCtrlr m_HsC;

    public virtual void Initiate(MapManager mapManager)
    {
        m_HsC = new HidingSpotsCtrlr(mapManager, mapManager.mapRenderer.GetMapBoundingBox(), 10,10);
    }

    public abstract void Begin();

    public abstract void Refresh();
}

public abstract class ChaseEvaderParams
{
}