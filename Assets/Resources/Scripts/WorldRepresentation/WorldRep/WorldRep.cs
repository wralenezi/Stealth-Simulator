using System.Collections.Generic;
using UnityEngine;

public abstract class WorldRep : MonoBehaviour
{
    [Header("Debug")] [Tooltip("VisMesh")] public bool showVisMesh;
    
    [Header("Debug")] [Tooltip("Hiding Spots")]
    public bool showHidingSpots;
    
    // static hiding points
    private List<Vector2> m_hidingSpots;

    // Decompose the area
    protected MapDecomposer m_mapDecomposer;

    // The last timestamp recorded
    private float m_LastTimestamp;

    // Areas
    protected float UnseenPortion;
    protected float SeenPortion;

    protected float AverageStaleness;
    
    public virtual void InitiateWorld(float mapScale)
    {
        m_mapDecomposer = GetComponent<MapDecomposer>();
        m_hidingSpots = new List<Vector2>();
    }

    public virtual void ResetWorld()
    {
        CreateHidingSpots();
    }

    public virtual void UpdateWorld(GuardsManager guardsManager)
    {
        foreach (Guard guard in guardsManager.GetGuards())
        {
            guard.SetSeenPortion();
        }
    }
    
    // Create the hiding spots 
    private void CreateHidingSpots()
    {
        m_hidingSpots.Clear();
        
        for (int i = 0; i < Properties.HidingSpotsCount; i++)
        {
            AddHidingSpot();
        }
    }

    private void AddHidingSpot()
    {
        Vector2 newHiding = m_mapDecomposer.GetRandomPolygonInNavMesh().GetRandomPosition();
            
        m_hidingSpots.Add(newHiding);        
    }

    public void ReplenishHidingSpots()
    {
        while (m_hidingSpots.Count < Properties.HidingSpotsCount)
        {
            AddHidingSpot();
        }
    }

    public List<Vector2> GetHidingSpots()
    {
        return m_hidingSpots;
    }
    
    public float GetTotalArea()
    {
        return SeenPortion + UnseenPortion;
    }

    public float GetAverageStaleness()
    {
        return AverageStaleness;
    }

    // Set the timestamp to the current time
    public void SetTimestamp()
    {
        m_LastTimestamp = Time.time;
    }

    // Get the time delta 
    public float GetTimeDelta()
    {
        float timeDelta = Time.time - m_LastTimestamp;
        SetTimestamp();
        return timeDelta;
    }


    public void DrawHidingSpots()
    {
        if(showHidingSpots)
            foreach (var spots in m_hidingSpots)
            {
                Gizmos.DrawSphere(spots, 0.1f);
            }
    }


}
