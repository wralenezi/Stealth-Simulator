using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public class StealthArea : MonoBehaviour
{
    // Game Manager
    private GameManager m_gameManager;
    private Scenario m_Scenario;

    // Map renderer
    private MapRenderer m_MapRenderer;
    
    // Convex decomposer of the space
    private MapDecomposer m_MapDecomposer;

    // Game world representation
    private WorldRep m_WorldRep;

    // NPC manager
    private NpcManager m_NpcManager;

    // Isovists map
    private Isovists m_Isovists;

    // Coroutine for updating world
    private IEnumerator worldCoroutine;

    [Header("Elapsed Time")] public float episodeTime;

    public float lastLoggedTime;

    // Initiate the area
    public void InitiateArea(Scenario scenario)
    {
        m_gameManager = GameObject.Find("GameSetUp").GetComponent<GameManager>();

        m_Scenario = scenario;

        // Get the map 
        Transform map = transform.Find("Map");

        // Draw the map
        m_MapRenderer = map.GetComponent<MapRenderer>();
        m_MapRenderer.Initiate();
        m_MapRenderer.LoadMap(m_Scenario.map, m_Scenario.GetMapScale());


        // Create the NavMesh
        m_MapDecomposer = map.GetComponent<MapDecomposer>();
        m_MapDecomposer.Initiate();
        m_MapDecomposer.CreateNavMesh();

        // Assign the world representation
        switch (m_Scenario.worldRepType)
        {
            case WorldRepType.VisMesh:
                m_WorldRep = m_MapRenderer.gameObject.AddComponent<VisMesh>();
                break;

            case WorldRepType.Grid:
                m_WorldRep = m_MapRenderer.gameObject.AddComponent<GridWorld>();
                break;
        }

        m_WorldRep.InitiateWorld(m_Scenario.GetMapScale());


        // Assign NPC Manager
        m_NpcManager = transform.Find("NpcManager").GetComponent<NpcManager>();
        m_NpcManager.Initiate();
        m_NpcManager.CreateNpcs(m_Scenario, m_MapDecomposer.GetNavMesh(), this);


        // Isovists map
        m_Isovists = map.GetComponent<Isovists>();
        // m_Isovists.Initiate(m_MapDecomposer.GetNavMesh());

        // The coroutine for updating the world representation
        worldCoroutine = UpdateWorld(1f);

        // Reset World Representation and NPCs
        ResetArea();
    }

    private void ResetArea()
    {
        episodeTime = 0f;
        lastLoggedTime = 0f;
        m_NpcManager.ResetNpcs();
        m_WorldRep.ResetWorld();
        StartCoroutine(worldCoroutine);
    }


    // every 2 seconds perform the print()
    private IEnumerator UpdateWorld(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            // World Update
            m_WorldRep.UpdateWorld(m_NpcManager.GetGuards());

            // Restrict Guards Seen Area
            m_NpcManager.ResetGuardSeenArea(m_Scenario.coveredReigonResetThreshold);

            // In the case of searching for an intruder
            m_NpcManager.AddInterceptionPoints();
        }
    }

    private void FixedUpdate()
    {
        // Update the episode time
        UpdateTime();

        // Update the guards vision
        m_NpcManager.UpdateGuardVision();

        // Idle NPCs make decisions
        m_NpcManager.MakeDecision();

        // Execute existing plans for NPCs
        m_NpcManager.MoveNpcs();

        // Check for game end
        CheckGameEnd();
        
        // Replenish hiding spots 
        m_WorldRep.ReplenishHidingSpots();

    }

    public Scenario GetScenario()
    {
        return m_Scenario;
    }

    public List<MeshPolygon> GetNavMesh()
    {
        return m_MapDecomposer.GetNavMesh();
    }


    void CheckGameEnd()
    {
        bool finished = episodeTime >= Properties.TimeRequiredToCoverOneUnit * m_MapDecomposer.GetNavMeshArea();

        // Log Guards progress
        if (m_gameManager.enableLogging && IsTimeToLog())
        {
            m_NpcManager.LogGuardsPerformance();
        }


        // Check if there are no more nodes to see and end the episodes
        if (finished)
        {
            // Log the overall performance
            if (m_gameManager.enableLogging)
            {
                if (m_NpcManager.FinalizeLogging())
                    Destroy(gameObject);

            }

            // EditorApplication.isPaused = true;
            StopCoroutine(worldCoroutine);


            ResetArea();
        }
    }

    void UpdateTime()
    {
        episodeTime += Time.fixedDeltaTime;
    }

    public bool IsTimeToLog()
    {
        if (episodeTime - lastLoggedTime >= 5f)
        {
            lastLoggedTime = episodeTime;
            return true;
        }

        return false;
    }
}