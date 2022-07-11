using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityPolygon : MeshPolygon
{
    // Timestamp last seen
    private float _timeStampLastSeen;

    public VisibilityPolygon()
    {
    }

    public VisibilityPolygon(Polygon p, float timestamp) : base(p)
    {
        _timeStampLastSeen = timestamp;
    }

    public float GetStaleness()
    {
        float nominator = _timeStampLastSeen - VisMesh.OldestTimestamp;
        float denominator = StealthArea.GetElapsedTimeInSeconds() -  VisMesh.OldestTimestamp;

        float staleness = denominator == 0 ? 1f : 1f - nominator / denominator;
        
        return staleness;
    }


    public void SetTimestamp(float timestamp)
    {
        _timeStampLastSeen = timestamp;
    }

    public float GetTimestamp()
    {
        return _timeStampLastSeen;
    }

}
