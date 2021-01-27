using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


public class StealthArea : MonoBehaviour
{
    // Session data
    private Session m_SessionInfo;

    // Game Manager
    private GameManager m_gameManager;

    // NPC manager
    private GuardsManager m_GuardsManager;

    // Map renderer
    private MapRenderer m_MapRenderer;

    // Convex decomposer of the space
    private MapDecomposer m_MapDecomposer;

    // Game world representation
    private WorldRep m_WorldRep;

    // The hiding spots controller
    private HidingSpots m_HidingSpots;

    // Isovist map
    private Isovists m_Isovists;

    // Scale Area transform ( to get the skeletal graph of the map)
    private SAT m_Sat;

    private VisibilityGraph m_VisibilityGraph;

    // Interceptor controller
    private Interceptor m_Interceptor;

    // Coroutine for updating world
    private IEnumerator worldCoroutine;

    // To determine which perspective the game is viewed from
    public GameView gameView;

    // Score prefab location
    private readonly string m_ScorePrefabLocation = "Prefabs/Score";
    private GameObject m_scoreGameObject = null;
    private Score m_score;

    // The episode time 
    public static float episodeTime;
    private float episodeMaxTime = 50f;

    // Last timestamp the game was logged.
    public float lastLoggedTime;

    public int searchSegments;
    
    // Initiate the area
    public void InitiateArea(Session scenario)
    {
        m_gameManager = GameObject.Find("GameSetUp").GetComponent<GameManager>();

        m_SessionInfo = scenario;

        // Get the map 
        Transform map = transform.Find("Map");

        // Draw the map
        m_MapRenderer = map.GetComponent<MapRenderer>();
        m_MapRenderer.Initiate();
        m_MapRenderer.LoadMap(m_SessionInfo.map, m_SessionInfo.GetMapScale());

        // Create the NavMesh
        m_MapDecomposer = map.GetComponent<MapDecomposer>();
        m_MapDecomposer.Initiate();
        m_MapDecomposer.CreateNavMesh();

        // Assign the world representation
        switch (m_SessionInfo.worldRepType)
        {
            case WorldRepType.VisMesh:
                m_WorldRep = m_MapRenderer.gameObject.AddComponent<VisMesh>();
                break;

            case WorldRepType.Grid:
                m_WorldRep = m_MapRenderer.gameObject.AddComponent<GridWorld>();
                break;
        }

        m_WorldRep.InitiateWorld(m_SessionInfo.GetMapScale());

        // The hiding spots manager
        m_HidingSpots = map.GetComponent<HidingSpots>();
        m_HidingSpots.Initiate(m_MapRenderer);

        // Isovists map
        m_Isovists = map.GetComponent<Isovists>();
        // m_Isovists.Initiate(m_MapDecomposer.GetNavMesh());

        // Scale Area Transform
        m_Sat = map.GetComponent<SAT>();
        m_Sat.Initiate(m_SessionInfo.GetMapScale(), m_SessionInfo.map);

        // Visibility graph
        // m_VisibilityGraph = map.GetComponent<VisibilityGraph>();
        // m_VisibilityGraph.Initiate(m_MapRenderer);

        // Interception controller
        m_Interceptor = map.GetComponent<Interceptor>();
        m_Interceptor.Initiate(m_Sat.GetRoadMap());

        // Assign NPC Manager
        m_GuardsManager = transform.Find("NpcManager").GetComponent<GuardsManager>();
        m_GuardsManager.Initiate(transform.Find("Canvas").Find("Guard state label").GetComponent<Text>());
        m_GuardsManager.CreateNpcs(m_SessionInfo, m_MapDecomposer.GetNavMesh(), this);

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
        m_GuardsManager.ResetNpcs(m_MapDecomposer.GetNavMesh(), this);
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
            m_GuardsManager.UpdateGuardManager(m_SessionInfo);
        }
    }


    private void Update()
    {
        // Update the episode time
        UpdateTime();

        // Update the guards vision
        m_GuardsManager.UpdateGuardVision();

        // Idle NPCs make decisions
        m_GuardsManager.MakeDecision();

        // Execute existing plans for NPCs
        // m_GuardsManager.MoveNpcs();

        // Update metrics for logging
        m_GuardsManager.UpdateMetrics();

        // Check for game end
        CheckGameEnd();
        
        // Print number of search segments
        searchSegments = m_Interceptor.GetSearchSegmentCount();

        // Replenish hiding spots 
        m_WorldRep.ReplenishHidingSpots();
    }


    private void FixedUpdate()
    {
        // Update the episode time
        //UpdateTime();

        // Update the guards vision
        //m_GuardsManager.UpdateGuardVision();

        // Idle NPCs make decisions
        //m_GuardsManager.MakeDecision();

        // Execute existing plans for NPCs
        m_GuardsManager.MoveNpcs();

        // Update metrics for logging
        //m_GuardsManager.UpdateMetrics();

        // Check for game end
        //CheckGameEnd();

        // Replenish hiding spots 
        //m_WorldRep.ReplenishHidingSpots();
    }

    public Session GetSessionInfo()
    {
        return m_SessionInfo;
    }

    public MapRenderer GetMap()
    {
        return m_MapRenderer;
    }

    public List<MeshPolygon> GetNavMesh()
    {
        return m_MapDecomposer.GetNavMesh();
    }

    // display the score board
    void DisplayScore()
    {
        // Get the score prefab
        var scorePrefab = (GameObject) Resources.Load(m_ScorePrefabLocation);
        m_scoreGameObject = Instantiate(scorePrefab, transform, true);

        m_score = m_scoreGameObject.GetComponent<Score>();
        m_score.Initiate(this);
    }


    void CheckGameEnd()
    {
        bool finished =
            episodeTime >= episodeMaxTime; //Properties.TimeRequiredToCoverOneUnit * m_MapDecomposer.GetNavMeshArea();

        // Log Guards progress
        if ((m_gameManager.enableLogging || m_gameManager.uploadData) && IsTimeToLog())
        {
            m_GuardsManager.LogPerformance();
        }

        // Check if there are no more nodes to see and end the episodes
        if (finished)
        {
            bool endArea = false;

            // Log the overall performance in case of local logging.
            if (m_gameManager.enableLogging)
                endArea = m_GuardsManager.FinalizeLogging(false);

            // Log the performance of this episode and upload it to the server.
            if (m_gameManager.uploadData)
                endArea = m_GuardsManager.FinalizeLogging(true);


            if (endArea)
            {
                // EndArea();
              //  DisplayScore();
            }
            
            StopCoroutine(worldCoroutine);

            ResetArea();
            
        }
    }

    // Destroy the area
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