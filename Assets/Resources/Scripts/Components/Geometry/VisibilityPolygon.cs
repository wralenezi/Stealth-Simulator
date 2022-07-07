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
        float nominator = StealthArea.GetElapsedTimeInSeconds() - _timeStampLastSeen;
        float denominator = VisMesh.OldestTimestamp - _timeStampLastSeen;

        float staleness = VisMesh.OldestTimestamp == _timeStampLastSeen ? 0f : nominator / denominator;
        
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
