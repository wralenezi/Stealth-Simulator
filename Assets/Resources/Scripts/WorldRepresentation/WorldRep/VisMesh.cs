using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisMesh : WorldRep
{
    

    // Previous Polygons
    private List<VisibilityPolygon> m_PreSeenPolygons;
    private List<VisibilityPolygon> m_PreUnseenPolygons;

    // Current Polygons 
    private List<VisibilityPolygon> m_CurSeenPolygons;
    private List<VisibilityPolygon> m_CurUnseenPolygons;

    // Visibility mesh polygons
    private List<VisibilityPolygon> m_VisMeshPolygons;

    

    public override void InitiateWorld(float mapScale)
    {
        base.InitiateWorld(mapScale);
        m_VisMeshPolygons = new List<VisibilityPolygon>();

        // Previous Polygons
        m_PreSeenPolygons = new List<VisibilityPolygon>();
        m_PreUnseenPolygons = new List<VisibilityPolygon>();

        // Current Polygons 
        m_CurSeenPolygons = new List<VisibilityPolygon>();
        m_CurUnseenPolygons = new List<VisibilityPolygon>();
        
    }

    // Reset the variables
    private void ResetVariables()
    {
        m_VisMeshPolygons.Clear();

        // Previous Polygons
        m_PreSeenPolygons.Clear();
        m_PreUnseenPolygons.Clear();

        // Current Polygons 
        m_CurSeenPolygons.Clear();
        m_CurUnseenPolygons.Clear();
    }
    
    
    // Reset the VisMesh
    public override void ResetWorld()
    {
        // Reset the variables
        ResetVariables();
        
        // Reset the time
        SetTimestamp();
        
        // Construct the VisMesh
        ConstructVisMesh();
    }

    public override void UpdateWorld(GuardsManager guardsManager)
    {
        foreach (var guard in guardsManager.GetGuards())
        {
            if (guardsManager.GetState() is Patrol)
            {
                // Update the world once
                UpdateForPatrol(guardsManager);
                break;
            }
        }
        
    }


    // Update the world for the patrol routine
    public void UpdateForPatrol(GuardsManager guardsManager)
    {
        ConstructVisMesh();
        base.UpdateWorld(guardsManager);
    }

    // Copy the current visibility polygons to the previous visibility polygons
    void MigrateVisMesh()
    {
        m_PreUnseenPolygons.Clear();
        foreach (var polygon in m_CurUnseenPolygons)
        {
            m_PreUnseenPolygons.Add(polygon);
        }

        m_CurUnseenPolygons.Clear();
        
        m_PreSeenPolygons.Clear();
        foreach (var polygon in m_CurSeenPolygons)
        {
            m_PreSeenPolygons.Add(polygon);
        }

        m_CurSeenPolygons.Clear();
    }

    // Calculate how stale the polygons are based on time delta; the currently seen polygons do not stale
    void StalePolygons()
    {
        // Get the staleness value since the last update
        float stalenessDelta = GetTimeDelta() * Properties.StalenessRate;
        
        foreach (VisibilityPolygon vp in m_PreUnseenPolygons)
        {
            vp.IncreaseStaleness(stalenessDelta);
        }
    }
    
    // Get the new partitioning and populate the VisMesh
    private void ConstructVisMesh()
    {
        if (m_CurUnseenPolygons.Count > 0)
        {
            // Move the current VisMesh to the previous one
            MigrateVisMesh();
            
            // Increase the staleness of polygons
            StalePolygons();
        }
        
        // Decompose the area 
        m_mapDecomposer.CreateVisMesh();
        
        // Get the current polygons
        m_CurSeenPolygons = m_mapDecomposer.GetSeenPolygons();
        m_CurUnseenPolygons = m_mapDecomposer.GetUnseenPolygons();
        
        // Calculate the staleness of the current polygons based on the old previous
        if (m_PreUnseenPolygons.Count > 0)
            CalculateCurrentStaleness();
        
        // Prepare the NavMesh 
        PrepVisMesh();

        // Calculate the areas
        CalculateAreas();
        
        // Render the visibility mesh
        m_meshManager.RenderVisibilityMesh(GetVisMesh());
    }

    
    // Move the staleness information from the previous mesh to current mesh
    void CalculateCurrentStaleness()
    {
        List<VisibilityPolygon> overallMesh = new List<VisibilityPolygon>();
        overallMesh.AddRange(m_PreSeenPolygons);
        overallMesh.AddRange(m_PreUnseenPolygons);
        
        // Unseen area can only be part of previous unseen area
        MigratePolygonStaleness(m_CurUnseenPolygons, overallMesh);
    }


    // Alternative method to pass the staleness info
    void MigratePolygonStaleness(List<VisibilityPolygon> newMesh, List<VisibilityPolygon> oldMesh)
    {
        foreach (VisibilityPolygon newVp in newMesh)
        {
            // the staleness of the new polygon
            float newStaleness = newVp.GetStaleness();

            foreach (VisibilityPolygon oldVp in oldMesh)
            {
                // The overlap area
                VisibilityPolygon intersection = PolygonHelper.GetIntersectionArea(newVp, oldVp);

                // if the intersection exists
                if (intersection.GetVerticesCount() > 0)
                {
                    float overlapArea = intersection.GetArea(); 

                    float newPolyArea = newVp.GetArea();

                    float areaWeight = overlapArea / newPolyArea;

                    // Add to the navMesh
                    intersection.IncreaseStaleness(areaWeight * oldVp.GetStaleness());
                    
                    // Add the weighted staleness based on the size of the previous polygon
                    newStaleness += areaWeight * oldVp.GetStaleness();

                    newVp.SetStaleness(newStaleness);
                }
            }
        }
    }
    
    void PrepVisMesh()
    {
        m_VisMeshPolygons.Clear();
        m_VisMeshPolygons.AddRange(m_CurSeenPolygons);
        m_VisMeshPolygons.AddRange(m_CurUnseenPolygons);
    }
    
    
    private void CalculateAreas()
    {
        UnseenPortion = 0f;
        SeenPortion = 0f;

        AverageStaleness = 0f;
        
        foreach (var p in m_CurSeenPolygons)
        {
            SeenPortion += p.GetArea();
        }

        foreach (var p in m_CurUnseenPolygons)
        {
            UnseenPortion += p.GetArea();
            AverageStaleness += p.GetStaleness();
        }
        
        AverageStaleness /= m_CurUnseenPolygons.Count;
    }
    
    
    public List<VisibilityPolygon> GetVisMesh()
    {
        return m_VisMeshPolygons;
    }
    
    private void OnDrawGizmos()
    {
        DrawHidingSpots();
        
        if (showVisMesh)
        {
            foreach (var poly in m_VisMeshPolygons)
                poly.Draw(poly.GetStaleness().ToString());
        }
    }
}
