using System;
using System.Collections;
using UnityEngine;
using Object = System.Object;


public class StealthArea : MonoBehaviour
{
    // The episode time 
    // public float episodeTime;
    private static float _episodeStartTime;

    // Session data
    public static Session SessionInfo;

    // public bool renderRoadMap;

    // Map Components
    // // Map renderer
    // public MapRenderer mapRenderer { get; set; }
    //
    // // Convex decomposer of the space
    // public MapDecomposer mapDecomposer { get; set; }

    // Game world representation
    // public WorldRep worldRep { get; set; }

    // // Isovist map
    // public Isovists isovists { get; set; }
    //
    // // Scale Area transform ( to get the skeletal graph of the map) and load it into the road map.
    // public SAT sat { get; set; }
    //
    // // Create the Visibility graph and load into the road map.
    // public VisibilityGraph visibilityGraph { get; set; }

    // // Regions manager; to show information relevant to the map, like region names, etc.
    // public RegionLabelsManager regionMgr { get; set; }

    // // Road map of the level.
    // public RoadMap roadMap { get; set; }
    //
    // // Mesh Manager
    // public FloorTileManager meshManager { get; set; }

    public MapManager Map { private set; get; }

    public NpcsManager NpcManager { private set; get; }

    // Logging manager
    // public PerformanceMonitor performanceMonitor { get; set; }

    // UI label manager
    // public AreaUIManager AreaUiManager { get; set; }

    // Coin spawner
    // public CoinSpawner coinSpawner { get; set; }

    // public Scriptor scriptor { get; set; }
    
    // Coroutine for updating the world
    private IEnumerator worldCoroutine;

    // Last timestamp the game was logged.
    private float lastLoggedTime;

    // Initiate the area
    public void InitiateArea(Session session)
    {
        SessionInfo = session;
        SessionInfo.SetTimestamp();

        // Get the map object 
        // Transform map = transform.Find("Map");
        GameObject mapOg = new GameObject("Map");
        mapOg.transform.parent = transform;
        Map = mapOg.AddComponent<MapManager>();
        Map.Initiate(SessionInfo.GetMap());
        
        GameObject npcsOg = new GameObject("Npcs");
        npcsOg.transform.parent = transform;
        NpcManager = npcsOg.AddComponent<NpcsManager>();
        NpcManager.Initialize(SessionInfo,Map);
        
        
        // Set up the UI manager
        // AreaUiManager = transform.Find("Canvas").GetComponent<AreaUIManager>();
        // AreaUiManager.Initiate();

        // // Draw the map
        // mapRenderer = map.GetComponent<MapRenderer>();
        // mapRenderer.Initiate();
        // mapRenderer.LoadMap(SessionInfo.GetMap());
        //
        // // Create the NavMesh
        // mapDecomposer = map.GetComponent<MapDecomposer>();
        // mapDecomposer.Initiate(this);
        // mapDecomposer.CreateNavMesh();

        // // Assign the world representation
        // switch (SessionInfo.worldRepType)
        // {
        //     case WorldRepType.Continuous:
        //         worldRep = mapRenderer.gameObject.AddComponent<VisMesh>();
        //         break;
        //
        //     case WorldRepType.Grid:
        //         worldRep = mapRenderer.gameObject.AddComponent<GridWorld>();
        //         break;
        // }
        // // Initiate the world representation
        // worldRep.InitiateWorld(SessionInfo.GetMap());

        // // Isovists map initiate
        // isovists = map.GetComponent<Isovists>();
        // isovists.Initiate(mapDecomposer.GetNavMesh());
        //
        // // Scale Area Transform
        // sat = map.GetComponent<SAT>();
        // sat.Initiate(SessionInfo);
        //
        // // // Visibility graph
        // visibilityGraph = map.GetComponent<VisibilityGraph>();
        // // visibilityGraph.Initiate(mapRenderer);

        // regionMgr = AddChildComponent<RegionLabelsManager>(map.transform, "Regions");
        // regionMgr.Initiate();

        // // Build the road map based on the Scale Area Transform
        // roadMap = new RoadMap(sat, mapRenderer);

        // // Mesh manager
        // meshManager = AddChildComponent<FloorTileManager>(transform, "MeshManager");
        // meshManager.Initiate(this);

        // // Add the Intruder manager
        // Transform IntruderManager = transform.Find("IntruderManager");
        // intrdrManager = IntruderManager.gameObject.AddComponent<IntrudersManager>();
        // intrdrManager.Initiate(this);
        //
        // // Assign the Guard Manager
        // Transform GuardManager = transform.Find("GuardManager");
        // guardsManager = GuardManager.GetComponent<GuardsManager>();
        // guardsManager.Initiate(this, map);
        //
        // // Create the Guards
        // guardsManager.CreateGuards(SessionInfo, mapDecomposer.GetNavMesh(), this);
        //
        // // Create the intruders
        // intrdrManager.CreateIntruders(SessionInfo, mapDecomposer.GetNavMesh(), this);

        // Reference for recording the performance
        // performanceMonitor = map.GetComponent<PerformanceMonitor>();
        // performanceMonitor.SetArea(GetSessionInfo());
        // performanceMonitor.Initialize();
        // performanceMonitor.ResetResults();

        // // Create the Coin spawner
        // coinSpawner = gameObject.AddComponent<CoinSpawner>();
        // coinSpawner.Inititate();

        // Initiate scriptor
        // scriptor = gameObject.AddComponent<Scriptor>();
        // scriptor.Initialize(GetSessionInfo().speechType);

        // The coroutine for updating the world representation
        // worldCoroutine = UpdateWorld(2f);

        // World state variables
        WorldState.Reset();

        // Reset World Representation and NPCs
        ResetArea();
    }
    
    public void ResetArea()
    {
        // StopCoroutine(worldCoroutine);
        _episodeStartTime = Time.time;
        WorldState.Set("episodeTime", _episodeStartTime.ToString());
        WorldState.Set("guardsCount", SessionInfo.guardsCount.ToString());

        lastLoggedTime = 0f;

        // guardsManager.Reset(mapDecomposer.GetNavMesh(), this);
        //
        // intrdrManager.Reset(mapDecomposer.GetNavMesh());
        NpcManager.Reset(Map.GetNavMesh(), SessionInfo);

        // regionMgr.SetRegions(SessionInfo.GetMap());

        // if (GetSessionInfo().gameType == GameType.CoinCollection)
        //     coinSpawner.Reset();
        // else if (GetSessionInfo().gameType == GameType.Stealth)
        //     coinSpawner.DisableCoins();

        // worldRep.ResetWorld();
        // StartCoroutine(worldCoroutine);

        // mapRenderer.GetWalls()[0].BoundingBox(out float minX, out float maxX, out float minY, out float maxY);
        Bounds bounds = Map.mapRenderer.GetMapBoundingBox();
        GameManager.MainCamera.transform.position = new Vector3((bounds.min.x + bounds.max.x) * 0.5f, (bounds.min.y + bounds.max.y) * 0.5f, -1f);

        float mapWidth = bounds.extents.x + 5f;
        float unitsPerPixel = mapWidth / Screen.width;
        float desiredHalfHeight = 0.5f * unitsPerPixel * Screen.height;
        GameManager.MainCamera.orthographicSize = desiredHalfHeight;

        ColorUtility.TryParseHtmlString(GetSessionInfo().guardColor, out Color parsedColor);
        GameManager.MainCamera.backgroundColor = parsedColor - new Color(0.5f, 0.5f, 0.5f, 0.1f);


        // AreaUiManager.Reset();
        //
        // scriptor.Disable();

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
        // if (GameManager.Instance.showSurvey) StartCoroutine(Countdown());
    }

    // Show countdown to start the episode
    // private IEnumerator Countdown()
    // {
    //     // AreaUiManager.DisplayLabel("3");
    //     // yield return new WaitForSecondsRealtime(1f);
    //     // AreaUiManager.DisplayLabel("2");
    //     // yield return new WaitForSecondsRealtime(1f);
    //     // AreaUiManager.DisplayLabel("1");
    //     // yield return new WaitForSecondsRealtime(1f);
    //     // AreaUiManager.DisplayLabel("Go");
    //     // yield return new WaitForSecondsRealtime(0.2f);
    //     // AreaUiManager.DisplayLabel("");
    //     // Time.timeScale = 1f;
    //     // yield return new WaitForSecondsRealtime(6f);
    //     // intrdrManager.HideLabels();
    // }

    private void EndEpisode()
    {
        NpcManager.Done();
    }

    // Update the world every fixed time step
    // private IEnumerator UpdateWorld(float waitTime)
    // {
    //     while (true)
    //     {
    //         yield return new WaitForSeconds(waitTime);
    //
    //         // World Update
    //         worldRep.UpdateWorld(guardsManager);
    //
    //         // Update 
    //         // guardsManager.UpdateGuardManager(SessionInfo);
    //     }
    // }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // Update the episode time
        UpdateTime(deltaTime);

        // Update metrics for logging
        // guardsManager.UpdateMetrics(deltaTime);

        // Check for game end
        CheckGameEnd();

        // Replenish hiding spots 
        // worldRep.ReplenishHidingSpots();
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;

        // Execute existing plans for NPCs
        // guardsManager.Move(deltaTime);
        // intrdrManager.Move(deltaTime);
        NpcManager.Move(deltaTime);
    }

    private void LateUpdate()
    {
        // Let the agents cast their visions
        // guardsManager.CastVision();
        // intrdrManager.CastVision();
        NpcManager.CastVision();

        // Update the guards vision and apply the vision affects (seeing intruders,etc) 
        NpcManager.ProcessNpcsVision(SessionInfo);

        // Idle NPCs make decisions
        NpcManager.MakeDecisions();
    }

    public Session GetSessionInfo()
    {
        return SessionInfo;
    }
    
    // public MapRenderer GetMap()
    // {
    //     return mapRenderer;
    // }

    void CheckGameEnd()
    {
        bool finished =
            GetElapsedTime() >= Properties.EpisodeLength || AreaUIManager.Score >= 100f;

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
            if (!Equals(GetSessionInfo().gameCode, "tutorial"))
                SessionsSetup.AddSessionColor(GetSessionInfo().guardColor);

            GameManager.SurveyManager.CreateSurvey(GameManager.GetRunId(), GetSessionInfo().surveyType,
                AreaUIManager.Score);
            GameManager.SurveyManager.ShowSurvey();
        }

        FinishArea();
    }


    public void FinishArea()
    {
        // performanceMonitor.IncrementEpisode();

        // End the episode for the ML agents
        EndEpisode();

        ResetArea();

        // Prevent the current area to be removed if it is a tutorial session
        if (Equals(SessionInfo.gameCode, "tutorial") && GameManager.Instance.showSurvey) return;

        EndArea();
    }


    public void LogPerformance()
    {
        // performanceMonitor.UpdateProgress();
    }

    // Log the episode's performance and check if required number of episodes is recorded
    // Upload 
    public void FinalizeLogging(bool isUpload)
    {
        LogPerformance();

        // if (!isUpload)
        //     performanceMonitor.LogEpisodeFinish();
        // else
        //     performanceMonitor.UploadEpisodeData();
    }

    // Destroy the area
    public void EndArea()
    {
        // StopCoroutine(worldCoroutine);

        // if (!GameManager.Instance.showSurvey && performanceMonitor.IsDone())
            GameManager.Instance.RemoveArea(gameObject);
    }

    void UpdateTime(float timeDelta)
    {
        // episodeTime += timeDelta;
        // AreaUiManager.UpdateTime(GetRemainingTime());
    }

    int GetRemainingTime()
    {
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
        // if (renderRoadMap)
        // {
        //     Gizmos.color = Color.black;
        //     roadMap.DrawDividedRoadMap();
        // }
    }
}