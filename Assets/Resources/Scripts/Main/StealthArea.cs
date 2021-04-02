using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


public class StealthArea : MonoBehaviour
{
    // Session data
    private Session m_SessionInfo;

    // Game Manager
    public GameManager gameManager;

    // NPC manager
    public GuardsManager guardsManager;

    // Map renderer
    public MapRenderer mapRenderer;

    // Convex decomposer of the space
    public MapDecomposer mapDecomposer;

    // Game world representation
    public WorldRep worldRep;

    // The hiding spots controller
    public HidingSpots hidingSpots;

    // Isovist map
    public Isovists isovists;

    // Scale Area transform ( to get the skeletal graph of the map) and load it into the road map.
    public SAT sat;

    // Create the Visibility graph and load into the road map.
    public VisibilityGraph visibilityGraph;

    // Road map of the level.
    public RoadMap roadMap;

    // Mesh Manager
    public MeshManager meshManager;

    // Logging manager
    public PerformanceMonitor performanceMonitor;

    // Coroutine for updating the world
    private IEnumerator worldCoroutine;

    // To determine which perspective the game is viewed from
    public GameView gameView;

    // Score prefab location
    private readonly string m_ScorePrefabLocation = "Prefabs/Score";
    private GameObject m_scoreGameObject;
    private Score m_score;

    // The episode time 
    public static float episodeTime;

    // Last timestamp the game was logged.
    private float lastLoggedTime;

    // Initiate the area
    public void InitiateArea(Session scenario)
    {
        gameManager = GameObject.Find("GameSetUp").GetComponent<GameManager>();

        m_SessionInfo = scenario;

        // Get the map 
        Transform map = transform.Find("Map");

        // Draw the map
        mapRenderer = map.GetComponent<MapRenderer>();
        mapRenderer.Initiate();
        mapRenderer.LoadMap(m_SessionInfo.map, m_SessionInfo.GetMapScale());
        
        // Create the NavMesh
        mapDecomposer = map.GetComponent<MapDecomposer>();
        mapDecomposer.Initiate(this);
        mapDecomposer.CreateNavMesh();

        // Assign the world representation
        switch (m_SessionInfo.worldRepType)
        {
            case WorldRepType.Continuous:
                worldRep = mapRenderer.gameObject.AddComponent<VisMesh>();
                break;

            case WorldRepType.Grid:
                worldRep = mapRenderer.gameObject.AddComponent<GridWorld>();
                break;
        }

        // Initiate the world
        worldRep.InitiateWorld(m_SessionInfo.GetMapScale());

        // The hiding spots manager
        hidingSpots = map.GetComponent<HidingSpots>();
        hidingSpots.Initiate(this);

        // Isovists map
        // isovists = map.GetComponent<Isovists>();
        // isovists.Initiate(mapDecomposer.GetNavMesh());

        // Scale Area Transform
        sat = map.GetComponent<SAT>();
        sat.Initiate(m_SessionInfo);

        // // Visibility graph
        visibilityGraph = map.GetComponent<VisibilityGraph>();
        // visibilityGraph.Initiate(mapRenderer);

        // Build the road map based on the Scale Area Transform
        roadMap = new RoadMap(sat, mapRenderer);

        // Mesh manager
        meshManager = transform.Find("MeshManager").GetComponent<MeshManager>();
        meshManager.Initiate(this);


        // Assign NPC Manager
        guardsManager = transform.Find("NpcManager").GetComponent<GuardsManager>();
        guardsManager.Initiate(this, map,
            transform.Find("Canvas").Find("Guard state label").GetComponent<Text>());

        // Create the NPCs
        guardsManager.CreateNpcs(m_SessionInfo, mapDecomposer.GetNavMesh(), this);

        performanceMonitor = map.GetComponent<PerformanceMonitor>();
        performanceMonitor.SetArea(GetSessionInfo());
        performanceMonitor.ResetResults();

        // The coroutine for updating the world representation
        // worldCoroutine = UpdateWorld(2f);

        // Reset World Representation and NPCs
        ResetArea();
    }


    private void ResetArea()
    {
        // StopCoroutine(worldCoroutine);
        episodeTime = 0f;
        lastLoggedTime = 0f;
        guardsManager.ResetNpcs(mapDecomposer.GetNavMesh(), this);
        worldRep.ResetWorld();
        // StartCoroutine(worldCoroutine);
    }

    private void EndEpisode()
    {
        guardsManager.Done();
    }

    // Update the world every fixed time step
    private IEnumerator UpdateWorld(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            // World Update
            worldRep.UpdateWorld(guardsManager);

            // Update 
            guardsManager.UpdateGuardManager(m_SessionInfo);
        }
    }


    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Update the episode time
        UpdateTime(deltaTime);

        // Let the agents cast their visions
        guardsManager.CastVision();

        // Update the guards vision and apply the vision affects (seeing intruders,etc) 
        guardsManager.UpdateGuardVision();

        // In the case of searching for an intruder
        guardsManager.UpdateSearcher(deltaTime);

        // Idle NPCs make decisions
        guardsManager.MakeDecision();

        // Execute existing plans for NPCs
        // guardsManager.MoveNpcs(deltaTime);

        // Move the camera with the intruder.
        guardsManager.FollowIntruder();

        // Update metrics for logging
        guardsManager.UpdateMetrics(deltaTime);

        // Check for game end
        CheckGameEnd();

        // Replenish hiding spots 
        worldRep.ReplenishHidingSpots();
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;

        // Execute existing plans for NPCs
        guardsManager.MoveNpcs(deltaTime);
    }

    // private void LateUpdate()
    // {
    // }
    
    public Session GetSessionInfo()
    {
        return m_SessionInfo;
    }

    public MapRenderer GetMap()
    {
        return mapRenderer;
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
            episodeTime >= Properties.EpisodeLength;

        // Log Guards progress
        if ((gameManager.loggingMethod != Logging.None) && IsTimeToLog())
            LogPerformance();

        // Check if there are no more nodes to see and end the episodes
        if (finished)
        {
            bool endArea;
            switch (gameManager.loggingMethod)
            {
                // Log the overall performance in case of local logging.
                case Logging.Local:
                    endArea = FinalizeLogging(false);
                    break;

                // Log the performance of this episode and upload it to the server.
                case Logging.Cloud:
                    endArea = FinalizeLogging(true);
                    break;

                default:
                    performanceMonitor.IncrementEpisode();
                    endArea = performanceMonitor.IsDone();
                    break;
            }

            // End the episode for the ML agents
            EndEpisode();

            ResetArea();

            if (endArea)
            {
                EndArea();
                //  DisplayScore();
            }
        }
    }

    public void LogPerformance()
    {
        if (guardsManager.GetGuards() != null)
            foreach (var guard in guardsManager.GetGuards())
                performanceMonitor.UpdateProgress(guard.LogNpcProgress());

        if (guardsManager.GetIntruders() != null)
            foreach (var intruder in guardsManager.GetIntruders())
                performanceMonitor.UpdateProgress(intruder.LogNpcProgress());
    }

    // Log the episode's performance and check if required number of episodes is recorded
    // Upload 
    public bool FinalizeLogging(bool isUpload)
    {
        LogPerformance();

        if (!isUpload)
            performanceMonitor.LogEpisodeFinish();
        else
            performanceMonitor.UploadEpisodeData();

        return performanceMonitor.IsDone();
    }

    // Destroy the area
    public void EndArea()
    {
        // StopCoroutine(worldCoroutine);

        if (performanceMonitor.IsDone())
            gameManager.RemoveArea(gameObject);
    }

    void UpdateTime(float timeDelta)
    {
        episodeTime += timeDelta;
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