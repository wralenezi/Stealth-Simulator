using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Scouter : MonoBehaviour
{
    public IntruderBehavior intruderBehavior;
    
    // Hiding spots manager
    public bool ShowHidingSpots;
    protected HidingSpotsCtrlr _HsC;

    public virtual void Initiate(MapManager mapManager)
    {
        // ShowHidingSpots = true;
        _HsC = new HidingSpotsCtrlr(mapManager, mapManager.mapRenderer.GetMapBoundingBox(), 10, 10);
    }

    public virtual void Begin()
    {
        foreach (var hs in _HsC.GetHidingSpots())
            hs.lastFailedTimeStamp = 0;

        intruderBehavior = GameManager.Instance.GetActiveArea().GetSessionInfo().intruderBehavior;
    }


    public abstract void Refresh(GameType gameType);


    public void OnDrawGizmos()
    {
        if (ShowHidingSpots)
            _HsC?.DrawHidingSpots();
    }
}

public struct IntruderBehavior
{
    /// <summary>
    /// Path Cancelling method
    /// </summary>
    public PathCanceller pathCancel;

    public RiskThresholdType thresholdType;

}



public enum PathCanceller
{
    DistanceCalculation,
    
    RiskComparison
}
