using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Scouter : MonoBehaviour
{
    // Hiding spots manager
    public bool ShowHidingSpots;
    protected HidingSpotsCtrlr m_HsC;
    
    public virtual void Initiate(MapManager mapManager)
    {
        m_HsC = new HidingSpotsCtrlr(mapManager.GetWalls());
    }
    
    public abstract void Begin();


    public abstract void Refresh();
}