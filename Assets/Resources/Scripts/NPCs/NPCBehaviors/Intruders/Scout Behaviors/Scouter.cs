using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Scouter : MonoBehaviour
{
    // Hiding spots manager
    public bool ShowHidingSpots;
    protected HidingSpotsCtrlr _HsC;

    public virtual void Initiate(MapManager mapManager)
    {
        _HsC = new HidingSpotsCtrlr(mapManager.GetWalls(), mapManager.mapRenderer.GetMapBoundingBox(), 10, 10);
    }

    public virtual void Begin()
    {
        foreach (var hs in _HsC.GetHidingSpots())
        {
            hs.lastFailedTimeStamp = 0;
        }
    }


    public abstract void Refresh(GameType gameType);


    public void OnDrawGizmos()
    {
        if (ShowHidingSpots)
            _HsC?.DrawHidingSpots();
    }
}