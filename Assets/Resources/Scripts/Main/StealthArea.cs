using System;
using System.Collections;
using UnityEngine;


public class StealthArea : MonoBehaviour
{
    // The episode time 
    // public float episodeTime;
    private static float _episodeStartTime;

    // Session data
    public static Session SessionInfo;

    public bool renderRoadMap;

    // Guards manager
    public GuardsManager guardsManager { get; set; }

    // Intruder Manager
    public IntrudersManager intrdrManager { get; set; }

    // Map renderer
    public MapRenderer mapRenderer { get; set; }

    // Convex decomposer of the space
    public MapDecomposer mapDecomposer { get; set; }

    // Game world representation
    public WorldRep worldRep { get; set; }

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

    // Last timestamp the game was logged.
    private float lastLoggedTime;

    // Initiate the area
    public void InitiateArea(Session scenario)
    {
        SessionInfo = scenario;
        SessionInfo.id = GameManager.GetDateTimestamp().ToString();

        // Get the map object 
        Transform map = transform.Find("Map");

        // Set up the UI manager
        AreaUiManager = transform.Find("Canvas").GetComponent<AreaUIManager>();
        AreaUiManager.Initiate();

        // Draw the map
        mapRenderer = map.GetComponent<MapRenderer>();
        mapRenderer.Initiate();
        mapRenderer.LoadMap(SessionInfo.map, SessionInfo.GetMapScale());

        // Create the NavMesh
        mapDecomposer = map.GetComponent<MapDecomposer>();
        mapDecomposer.Initiate(this);
        mapDecomposer.CreateNavMesh();

        // Assign the world representation
        switch (SessionInfo.worldRepType)
        {
            case WorldRepType.Continuous:
                worldRep = mapRenderer.gameObject.AddComponent<VisMesh>();
                break;

            case WorldRepType.Grid:
                worldRep = mapRenderer.gameObject.AddComponent<GridWorld>();
                break;
        }

        // Initiate the world representation
        worldRep.InitiateWorld(SessionInfo.GetMapScale());

        // Isovists map initiate
        isovists = map.GetComponent<Isovists>();
        // isovists.Initiate(mapDecomposer.GetNavMesh());

        // Scale Area Transform
        sat = map.GetComponent<SAT>();
        sat.Initiate(SessionInfo);

        // // Visibility graph
        visibilityGraph = map.GetComponent<VisibilityGraph>();
        // visibilityGraph.Initiate(mapRenderer);

        // Build the road map based on the Scale Area Transform
        roadMap = new RoadMap(sat, mapRenderer);

        // Mesh manager
        meshManager = transform.Find("MeshManager").GetComponent<MeshManager>();
        meshManager.Initiate(this);


        // Add the Intruder manager
        Transform IntruderManager = transform.Find("IntruderManager");
        intrdrManager = IntruderManager.gameObject.AddComponent<IntrudersManager>();
        intrdrManager.Initiate(this, map);

        // Assign the Guard Manager
        Transform GuardManager = transform.Find("GuardManager");
        guardsManager = GuardManager.GetComponent<GuardsManager>();
        guardsManager.Initiate(this, map);

        // Create the Guards
        guardsManager.CreateGuards(SessionInfo, mapDecomposer.GetNavMesh(), this);

        // Create the intruders
        intrdrManager.CreateIntruders(SessionInfo, mapDecomposer.GetNavMesh(), this);

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
        WorldState.Set("guardsCount", SessionInfo.guardsCount.ToString());

        // Reset World Representation and NPCs
        ResetArea();
    }

    public void ResetArea()
    {
        // StopCoroutine(worldCoroutine);
        _episodeStartTime = Time.time;
        WorldState.Set("episodeTime", _episodeStartTime.ToString());

        lastLoggedTime = 0f;

        guardsManager.Reset(mapDecomposer.GetNavMesh(), this);

        intrdrManager.Reset(mapDecomposer.GetNavMesh());

        if (GetSessionInfo().gameType == GameType.CoinCollection)
            coinSpawner.Reset();
        else if (GetSessionInfo().gameType == GameType.Stealth)
            coinSpawner.DisableCoins();

        worldRep.ResetWorld();
        // StartCoroutine(worldCoroutine);

        float camSize = mapRenderer.GetMaxWidth() / 2f;
        GameManager.MainCamera.orthographicSize = camSize;

        AreaUiManager.Reset();

        if (GameManager.Instance.showSurvey) Time.timeScale = 0f;
    }

    public static float GetElapsedTime()
    {
        return Time.time - _episodeStartTime;
    }

    public void StartArea()
    {
        ResetArea();
        gameObject.SetActive(true);
        if (GameManager.Instance.showSurvey) StartCoroutine(Countdown());
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
        intrdrManager.HideLabels();
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
            guardsManager.UpdateGuardManager(SessionInfo);
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
        // guardsManager.UpdateSearcher(deltaTime);

        // Idle NPCs make decisions
        guardsManager.MakeDecision();

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
        guardsManager.Move(deltaTime);
        intrdrManager.Move(deltaTime);
    }

    private void LateUpdate()
    {
        // Let the agents cast their visions
        guardsManager.CastVision();
        intrdrManager.CastVision();
    }

    public Session GetSessionInfo()
    {
        return SessionInfo;
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
        if ((GameManager.Instance.loggingMethod != Logging.None) && IsTimeToLog())
            LogPerformance();

        if (!finished) return;

        // End the episode
        switch (GameManager.Instance.loggingMethod)
        {
            // Log the overall performance in case of local logging.
            case Logging.Local:
                FinalizeLogging(false);
                break;

            // Log the performance of this episode and upload it to the server.
            case Logging.Cloud:
                FinalizeLogging(true);
                break;

            case Logging.None:
                break;
        }

        if (GameManager.Instance.showSurvey)
        {
            GameManager.SurveyManager.CreateSurvey(GameManager.GetRunId(), GetSessionInfo().surveyType);
            GameManager.SurveyManager.ShowSurvey();
        }

        FinishArea();
    }


    public void FinishArea()
    {
        performanceMonitor.IncrementEpisode();

        // End the episode for the ML agents
        EndEpisode();

        ResetArea();

        // Prevent the current area to be removed if it is a tutorial session
        if (Equals(SessionInfo.gameCode, "tutorial"))
            return;

        EndArea();
    }


    public void LogPerformance()
    {
        if (guardsManager.GetGuards() != null)
            foreach (var guard in guardsManager.GetGuards())
                performanceMonitor.UpdateProgress(guard.LogNpcProgress());
        if (intrdrManager.GetIntruders() != null)
            foreach (var intruder in intrdrManager.GetIntruders())
                performanceMonitor.UpdateProgress(intruder.LogNpcProgress());
    }

    // Log the episode's performance and check if required number of episodes is recorded
    // Upload 
    public void FinalizeLogging(bool isUpload)
    {
        LogPerformance();

        if (!isUpload)
            performanceMonitor.LogEpisodeFinish();
        else
            performanceMonitor.UploadEpisodeData();
    }

    // Destroy the area
    public void EndArea()
    {
        // StopCoroutine(worldCoroutine);

        if (!GameManager.Instance.showSurvey && performanceMonitor.IsDone())
            GameManager.Instance.RemoveArea(gameObject);
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
        if (GetElapsedTime() - lastLoggedTime >= 0.5f)
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
            roadMap.DrawDividedRoadMap();
        }
    }
}