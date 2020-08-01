using System.Collections.Generic;
using UnityEngine;

public abstract class WorldRep : MonoBehaviour
{
    [Header("Debug")] [Tooltip("VisMesh")] public bool showVisMesh;
    
    [Header("Debug")] [Tooltip("Hiding Spots")]
    public bool showHidingSpots;
    
    // Mesh Manager
    protected MeshManager m_meshManager;
    
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
        m_meshManager = transform.parent.Find("MeshManager").GetComponent<MeshManager>();
        m_mapDecomposer = GetComponent<MapDecomposer>();
        m_hidingSpots = new List<Vector2>();
        m_meshManager.Initiate();
    }

    public virtual void ResetWorld()
    {
        CreateHidingSpots();
    }

    public virtual void UpdateWorld(List<Guard> guards)
    {
        foreach (Guard guard in guards)
        {
            guard.SetSeenPortion();
        }
    }
    
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
        Vector2 newHiding = m_mapDecomposer.GetRandomPolygon().GetRandomPosition();
            
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
    
    
    public List<MeshPolygon> GetNavMesh()
    {
        return m_mapDecomposer.GetNavMesh();
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
