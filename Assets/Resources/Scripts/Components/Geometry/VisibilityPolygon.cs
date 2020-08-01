using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityPolygon : MeshPolygon
{
    // area "staleness" Max value is 255. The higher the value the more stale it is
    private float m_Staleness = Properties.StalenessLow;

    public VisibilityPolygon()
    {
    }

    public VisibilityPolygon(Polygon p) : base(p)
    {
        
    }

    public float GetStaleness()
    {
        return m_Staleness;
    }


    public void SetStaleness(float staleness)
    {
        m_Staleness = staleness;
    }
    
    public void IncreaseStaleness(float staleness)
    {
        m_Staleness += staleness;
    }
}
