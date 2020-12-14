using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


public class StealthArea : MonoBehaviour
{
    // Game Manager
    private GameManager m_gameManager;
    private Session m_Scenario;

    // Map renderer
    private MapRenderer m_MapRenderer;

    // Convex decomposer of the space
    private MapDecomposer m_MapDecomposer;

    // Game world representation
    private WorldRep m_WorldRep;

    private HidingSpots m_HidingSpots;

    // NPC manager
    private GuardsManager m_GuardsManager;

    // Isovists map
    private Isovists m_Isovists;

    // Scale Area transform ( to get the skeletal graph of the map)
    private SAT m_Sat;

    private VisibilityGraph m_VisibilityGraph;

    private Interceptor m_Interceptor;

    // Coroutine for updating world
    private IEnumerator worldCoroutine;

    // To determine which perspective the game is viewed from
    public GameView gameView;

    [Header("Elapsed Time")] public static float episodeTime;

    public float lastLoggedTime;

    // Initiate the area
    public void InitiateArea(Session scenario)
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

        m_HidingSpots = map.GetComponent<HidingSpots>();
        m_HidingSpots.Initiate(m_MapRenderer);


        // Assign NPC Manager
        m_GuardsManager = transform.Find("NpcManager").GetComponent<GuardsManager>();
        m_GuardsManager.Initiate(transform.Find("Canvas").Find("Text").GetComponent<Text>());
        m_GuardsManager.CreateNpcs(m_Scenario, m_MapDecomposer.GetNavMesh(), this);

        // Isovists map
        m_Isovists = map.GetComponent<Isovists>();
        // m_Isovists.Initiate(m_MapDecomposer.GetNavMesh());

        // Scale Area Transform
        m_Sat = map.GetComponent<SAT>();
        m_Sat.Initiate(m_Scenario.GetMapScale(), m_Scenario.map);

        // Visibility graph
        m_VisibilityGraph = map.GetComponent<VisibilityGraph>();
        m_VisibilityGraph.Initiate(m_MapRenderer);

        m_Interceptor = map.GetComponent<Interceptor>();
        m_Interceptor.Initiate(m_Sat.GetRoadMap());

        // The coroutine for updating the world representation
        worldCoroutine = UpdateWorld(0.5f);

        // Reset World Representation and NPCs
        ResetArea();

        EditorApplication.isPaused = true;
    }

    private void ResetArea()
    {
        episodeTime = 0f;
        lastLoggedTime = 0f;
        m_GuardsManager.ResetNpcs(m_MapDecomposer.GetNavMesh());
        m_WorldRep.ResetWorld();
        StartCoroutine(worldCoroutine);
    }


    // Update the world every fixed time step
    private IEnumerator UpdateWorld(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            // World Update
            m_WorldRep.UpdateWorld(m_GuardsManager);

            // Update 
            m_GuardsManager.UpdateGuardManager(m_Scenario);
        }
    }

    private void FixedUpdate()
    {
        // Update the episode time
        UpdateTime();

        // Update the guards vision
        m_GuardsManager.UpdateGuardVision();

        // Idle NPCs make decisions
        m_GuardsManager.MakeDecision();

        // Execute existing plans for NPCs
        m_GuardsManager.MoveNpcs();

        // Update metrics for logging
        m_GuardsManager.UpdateMetrics();

        // Check for game end
        CheckGameEnd();

        // Replenish hiding spots 
        m_WorldRep.ReplenishHidingSpots();
    }

    public Session GetScenario()
    {
        return m_Scenario;
    }

    public List<MeshPolygon> GetNavMesh()
    {
        return m_MapDecomposer.GetNavMesh();
    }


    void CheckGameEnd()
    {
        bool finished = episodeTime >= 200f; //Properties.TimeRequiredToCoverOneUnit * m_MapDecomposer.GetNavMeshArea();

        // Log Guards progress
        if (m_gameManager.enableLogging && IsTimeToLog())
        {
            m_GuardsManager.LogPerformance();
        }


        // Check if there are no more nodes to see and end the episodes
        if (finished)
        {
            // Log the overall performance
            if (m_gameManager.enableLogging)
            {
                if (m_GuardsManager.FinalizeLogging())
                {
                    EndArea();
                }
            }

            // EditorApplication.isPaused = true;
            StopCoroutine(worldCoroutine);


            ResetArea();
        }
    }

    public void EndArea()
    {
        m_gameManager.RemoveArea(gameObject);
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

// The view of the game based on the perspective
public enum GameView
{
    Spectator,
    Intruder,
    Guard
}