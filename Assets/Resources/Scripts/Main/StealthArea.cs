using System;
using System.Collections;
using UnityEngine;


public class StealthArea : MonoBehaviour
{
    // Session data
    public static Session sessionInfo;

    public bool renderRoadMap;

    // Game Manager
    public GameManager gameManager { get; set; }

    // NPC manager
    public GuardsManager guardsManager { get; set; }

    // Map renderer
    public MapRenderer mapRenderer { get; set; }

    // Convex decomposer of the space
    public MapDecomposer mapDecomposer { get; set; }

    // Game world representation
    public WorldRep worldRep { get; set; }

    // The hiding spots controller
    public HidingSpots hidingSpots { get; set; }

    // Isovist map
    public Isovists isovists { get; set; }

    // Scale Area transform ( to get the skeletal graph of the map) and load it into the road map.
    public SAT sat { get; set; }

    // Create the Visibility graph and load into the road map.
    public VisibilityGraph visibilityGraph { get; set; }

    // Road map of the level.
    public RoadMap roadMap { get; set; }

    // Mesh Manager
    public MeshManager meshManager { get; set; }

    // Logging manager
    public PerformanceMonitor performanceMonitor { get; set; }

    // UI label manager
    public AreaUIManager AreaUiManager { get; set; }

    // Coin spawner
    public CoinSpawner coinSpawner { get; set; }

    // To determine which perspective the game is viewed from
    public GameView gameView { get; set; }

    // Coroutine for updating the world
    private IEnumerator worldCoroutine;

    // The episode time 
    // public float episodeTime;
    private static float episodeStartTime;

    // Last timestamp the game was logged.
    private float lastLoggedTime;

    // Initiate the area
    public void InitiateArea(Session scenario)
    {
        // Set a reference to the game manager
        gameManager = GameObject.Find("GameSetUp").GetComponent<GameManager>();
        sessionInfo = scenario;

        // Get the map object 
        Transform map = transform.Find("Map");

        AreaUiManager = transform.Find("Canvas").GetComponent<AreaUIManager>();
        AreaUiManager.Initiate();

        // Draw the map
        mapRenderer = map.GetComponent<MapRenderer>();
        mapRenderer.Initiate();
        mapRenderer.LoadMap(sessionInfo.map, sessionInfo.GetMapScale());

        // Create the NavMesh
        mapDecomposer = map.GetComponent<MapDecomposer>();
        mapDecomposer.Initiate(this);
        mapDecomposer.CreateNavMesh();

        // Assign the world representation
        switch (sessionInfo.worldRepType)
        {
            case WorldRepType.Continuous:
                worldRep = mapRenderer.gameObject.AddComponent<VisMesh>();
                break;

            case WorldRepType.Grid:
                worldRep = mapRenderer.gameObject.AddComponent<GridWorld>();
                break;
        }

        // Initiate the world representation
        worldRep.InitiateWorld(sessionInfo.GetMapScale());

        // The hiding spots manager
        hidingSpots = map.GetComponent<HidingSpots>();
        hidingSpots.Initiate(this);

        // Isovists map
        // isovists = map.GetComponent<Isovists>();
        // isovists.Initiate(mapDecomposer.GetNavMesh());

        // Scale Area Transform
        sat = map.GetComponent<SAT>();
        sat.Initiate(sessionInfo);

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
        guardsManager.Initiate(this, map);

        // Create the NPCs
        guardsManager.CreateNpcs(sessionInfo, mapDecomposer.GetNavMesh(), this);

        // Reference for recording the performance
        performanceMonitor = map.GetComponent<PerformanceMonitor>();
        performanceMonitor.SetArea(GetSessionInfo());
        performanceMonitor.ResetResults();

        // Create the Coin spawner
        coinSpawner = gameObject.AddComponent<CoinSpawner>();
        coinSpawner.Inititate();

        // The coroutine for updating the world representation
        // worldCoroutine = UpdateWorld(2f);

        // World state variables
        WorldState.Reset();
        WorldState.Set("guardsCount", sessionInfo.guardsCount.ToString());

        // Reset World Representation and NPCs
        ResetArea();
    }

    private void ResetArea()
    {
        // StopCoroutine(worldCoroutine);
        episodeStartTime = Time.time;
        WorldState.Set("episodeTime", episodeStartTime.ToString());
        // episodeTime = 0f;
        lastLoggedTime = 0f;
        guardsManager.Reset();
        guardsManager.ResetNpcs(mapDecomposer.GetNavMesh(), this);

        if (GameManager.instance.gameType == GameType.CoinCollection)
            coinSpawner.Reset();
        else if (GameManager.instance.gameType == GameType.Stealth)
            coinSpawner.DisableCoins();

        worldRep.ResetWorld();
        // StartCoroutine(worldCoroutine);

        float camSize = mapRenderer.GetMaxWidth() / 2f;
        GameManager.MainCamera.orthographicSize = camSize;

        if (GameManager.instance.ShowSurvey)
            Time.timeScale = 0f;
    }

    public static float GetElapsedTime()
    {
        return Time.time - episodeStartTime;
    }

    public void StartArea()
    {
        ResetArea();
        gameObject.SetActive(true);

        if (GameManager.instance.ShowSurvey)
            StartCoroutine(Countdown());
    }

    // Show countdown to start the episode
    private IEnumerator Countdown()
    {
        AreaUiManager.DisplayLabel("3");
        yield return new WaitForSecondsRealtime(1f);
        AreaUiManager.DisplayLabel("2");
        yield return new WaitForSecondsRealtime(1f);
        AreaUiManager.DisplayLabel("1");
        yield return new WaitForSecondsRealtime(1f);
        AreaUiManager.DisplayLabel("Go");
        yield return new WaitForSecondsRealtime(0.2f);
        AreaUiManager.DisplayLabel("");
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(6f);
        guardsManager.HideLabels();
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
            guardsManager.UpdateGuardManager(sessionInfo);
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Update the episode time
        UpdateTime(deltaTime);

        // // Let the agents cast their visions
        // guardsManager.CastVision();

        // Update the guards vision and apply the vision affects (seeing intruders,etc) 
        guardsManager.UpdateGuardVision();

        // In the case of searching for an intruder
        guardsManager.UpdateSearcher(deltaTime);

        // Idle NPCs make decisions
        guardsManager.MakeDecision();

        // Execute existing plans for NPCs
        // guardsManager.MoveNpcs(deltaTime);

        // Move the camera with the intruder.
        // guardsManager.FollowIntruder();

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

    private void LateUpdate()
    {
        // Let the agents cast their visions
        guardsManager.CastVision();
    }

    public Session GetSessionInfo()
    {
        return sessionInfo;
    }

    public MapRenderer GetMap()
    {
        return mapRenderer;
    }

    void CheckGameEnd()
    {
        bool finished =
            GetElapsedTime() >= Properties.EpisodeLength;

        // Log Guards progress
        if ((gameManager.loggingMethod != Logging.None) && IsTimeToLog())
            LogPerformance();

        // Check if there are no more nodes to see and end the episodes
        if (finished)
        {
            switch (gameManager.loggingMethod)
            {
                // Log the overall performance in case of local logging.
                case Logging.Local:
                    FinalizeLogging(false, GameManager.TimeStamp);
                    break;

                // Log the performance of this episode and upload it to the server.
                case Logging.Cloud:
                    FinalizeLogging(true, GameManager.TimeStamp);
                    break;

                default:
                    break;
            }

            FinishArea();

            if (GameManager.instance.ShowSurvey)
                DisplayEndAreaSurvey(GameManager.TimeStamp);
        }
    }


    public void DisplayEndAreaSurvey(int timeStamp)
    {
        gameObject.SetActive(false);
        GameManager.SurveyManager.CreateEndAreaSurvey(timeStamp);
    }


    public void FinishArea()
    {
        performanceMonitor.IncrementEpisode();

        // End the episode for the ML agents
        EndEpisode();

        ResetArea();

        EndArea();
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
    public void FinalizeLogging(bool isUpload, int timeStamp)
    {
        LogPerformance();

        if (!isUpload)
            performanceMonitor.LogEpisodeFinish();
        else
            performanceMonitor.UploadEpisodeData(timeStamp);
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
        // episodeTime += timeDelta;
        AreaUiManager.UpdateTime(GetRemainingTime());
    }

    int GetRemainingTime()
    {
        // return Mathf.RoundToInt(Properties.EpisodeLength - episodeTime);
        return Mathf.RoundToInt(Properties.EpisodeLength - GetElapsedTime());
    }

    public bool IsTimeToLog()
    {
        if (GetElapsedTime() - lastLoggedTime >= 5f)
        {
            lastLoggedTime = GetElapsedTime();
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (renderRoadMap)
        {
            Gizmos.color = Color.black;
            roadMap.DrawRoadMap();
        }
    }
}