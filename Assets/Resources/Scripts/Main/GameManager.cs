using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Barracuda;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    // Run identifier so all data can be grouped for each run
    // The time stamp the game started
    private static int _timeStamp;

    // The path to the stealth area prefab
    private const string StealthArea = "Prefabs/StealthArea";

    // Active area
    private StealthArea m_ActiveArea;

    // List of scenarios to be executed
    private List<Session> m_Sessions;

    // display survey?
    public bool showSurvey;

    [SerializeField] [Tooltip("Is this will be web based?")]
    public bool IsOnlineBuild;

    // Logging Variables 
    [Header("Logging")] [Tooltip("Specify the logging method")]
    public Logging loggingMethod;

    [Header("Time")] [Tooltip("Simulation speed")] [Range(1, 20)]
    public int SimulationSpeed;

    // Rendering colors and certain meshes
    [Tooltip("To render colors and meshes in the game.")]
    public bool Render;

    // Location of the data for the game
    public static string DataPath;
    public static string LogsPath = "../../../Logs/";
    public static string MapsDataPath = "MapsData/";
    public static string MapsPath = "Maps/";
    public static string RoadMapsPath = "RoadMaps/";

    // The main camera
    public static Camera MainCamera;

    // The survey controller
    public static SurveyManager SurveyManager;

    public static LoadingScreenController LoadingScreen;

    // Container for the dialogs
    public static string DialogLines;

    // The containers variables for the current map
    public string currentMapData { set; get; }
    public string currentRoadMapData { set; get; }

    // Game manager instance handler
    public static GameManager Instance;

    // Beginning of the game manager.
    private void Start()
    {
        // Set the time of the session
        _timeStamp = GetDateTimestamp();

        // Initiate the references
        m_ActiveArea = null;
        m_Sessions = new List<Session>();
        Instance = this;

        // Define the hierarchy of the paths for the game
        // Main path
        DataPath = "Data/";
        // Logs path
        LogsPath = DataPath + LogsPath;
        // Map related data paths
        MapsDataPath = DataPath + MapsDataPath;
        MapsPath = MapsDataPath + MapsPath;
        RoadMapsPath = MapsDataPath + RoadMapsPath;

        // Reference the main camera
        MainCamera = Camera.main;

        // Initiate the containers for path finding.
        PathFinding.Initiate();

        // World state storage
        WorldState.Initialize();

        // Initiate the survey handle
        GameObject canvasGO = GameObject.Find("Canvas");
        SurveyManager = canvasGO.transform.Find("Survey").GetComponent<SurveyManager>();
        SurveyManager.Initiate();

        LoadingScreen = canvasGO.transform.Find("Loading Screen").GetComponent<LoadingScreenController>();
        LoadingScreen.Initiate();
        
        StartCoroutine(FileUploader.GetFile(DialogLines, "dialogs"));

        // Load the sessions to play
        LoadSavedSessions();

        StartCoroutine(LoadGamesWhenReady());

        // Set the simulation speed
        Time.timeScale = SimulationSpeed;

        if (!showSurvey) return;

        // show the survey for new users
        SurveyManager.CreateSurvey(_timeStamp, SurveyType.NewUser);
        SurveyManager.ShowSurvey();
    }

    public GameType GetGameType()
    {
        return m_Sessions[0].gameType;
    }


    // This is to get a unique time stamp
    public static int GetDateTimestamp()
    {
        DateTime epochStart = new DateTime(2021, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        return (int) (DateTime.UtcNow - epochStart).TotalSeconds;
    }

    public static int GetRunId()
    {
        return _timeStamp;
    }

    private void LoadSavedSessions()
    {
        // var sessions = SessionsSetup.StealthyPathingStudy();
        // var sessions = SessionsSetup.PrepareStudySessions();
        var sessions = SessionsSetup.PrepareTempSessions();


        // Each line represents a session
        foreach (var session in sessions)
        {
            // Get the session info
            var sc = new Session(session["GameCode"],
                (GameType) Enum.Parse(typeof(GameType), session["GameType"], true),
                (Scenario) Enum.Parse(typeof(Scenario), session["Scenario"], true), session["GuardColor"],
                int.Parse(session["GuardsCount"]), int.Parse(session["IntudersCount"]),
                int.Parse(session["CoverageResetThreshold"]),
                (WorldRepType) Enum.Parse(typeof(WorldRepType), session["WorldRep"], true), session["Map"],
                bool.Parse(session["dialogEnabled"]),
                (SurveyType) Enum.Parse(typeof(SurveyType), session["SurveyType"], true),
                float.Parse(session["MapScale"]));


            // Set the guard behavior
            GuardBehavior guardBehavior = new GuardBehavior(
                (GuardPatrolPlanner) Enum.Parse(typeof(GuardPatrolPlanner), session["GuardPatrolPlanner"], true),
                (GuardChasePlanner) Enum.Parse(typeof(GuardChasePlanner), session["GuardChasePlanner"], true),
                (GuardSearchPlanner) Enum.Parse(typeof(GuardSearchPlanner), session["GuardSearchPlanner"], true));


            // Add the guards
            for (int i = 0; i < int.Parse(session["GuardsCount"]); i++)
                sc.AddGuard(i + 1, NpcType.Guard, guardBehavior, null,
                    (PathFindingHeursitic) Enum.Parse(typeof(PathFindingHeursitic), session["PathFindingHeursitic"],
                        true),
                    (PathFollowing) Enum.Parse(typeof(PathFollowing), session["PathFollowing"], true),
                    null);


            // Add the intruders
            for (int i = 0; i < int.Parse(session["IntudersCount"]); i++)
                sc.AddIntruder(i + 1, NpcType.Intruder, null,
                    (IntruderPlanner) Enum.Parse(typeof(IntruderPlanner), session["IntruderPlanner"], true),
                    (PathFindingHeursitic) Enum.Parse(typeof(PathFindingHeursitic), session["PathFindingHeursitic"],
                        true),
                    (PathFollowing) Enum.Parse(typeof(PathFollowing), session["PathFollowing"], true),
                    null);


            // Check if the required number of Episodes is logged already or skip if logging is not required.
            if (loggingMethod != Logging.Local || !PerformanceMonitor.IsLogged(sc))
                m_Sessions.Add(sc);
        }
    }

    private bool IsAreaLoaded()
    {
        return !Equals(m_ActiveArea, null);
    }

    /// <summary>
    /// Load the map data 
    /// </summary>
    /// <param name="map"> The name of the map</param>
    /// <param name="mapScale"> The scale multiplier of the map</param>
    private void LoadMapData(string map, float mapScale)
    {
        if (IsOnlineBuild)
        {
            // Load the map data
            StartCoroutine(FileUploader.GetFile(map, "map"));

            // Load the road map data
            StartCoroutine(FileUploader.GetFile(map, "roadMap", mapScale));
            
        }
        else
        {
            // Get the map data
            currentMapData = CsvController.ReadString(GetMapPath(map));
            currentRoadMapData = CsvController.ReadString(GetRoadMapPath(map, mapScale));
        }
    }

    // Get the path to the map
    private static string GetMapPath(string mapName)
    {
        // Gets the path to the "Assets" folder 
        return MapsPath + mapName + ".csv";
    }

    // Get the path to the map
    private static string GetRoadMapPath(string mapName, float mapScale)
    {
        // Gets the path to the "Assets" folder 
        return RoadMapsPath + mapName + "_" + mapScale + ".csv";
    }

    // Create the area and load it with the scenario
    private void CreateArea(Session scenario)
    {
        // Get the area prefab
        var areaPrefab = (GameObject) Resources.Load(StealthArea);
        GameObject activeArea = Instantiate(areaPrefab, transform, true);

        // Get the script
        m_ActiveArea = activeArea.GetComponent<StealthArea>();

        // Initiate the session
        m_ActiveArea.InitiateArea(scenario);

        // Hide the area 
        if (showSurvey) m_ActiveArea.gameObject.SetActive(false);
    }

    private IEnumerator LoadGamesWhenReady()
    {
        while (m_Sessions.Count > 0)
        {
            // if there is an active area then skip
            if (IsAreaLoaded())
            {
                yield return new WaitForSecondsRealtime(0.5f);
                continue;
            }

            LoadingScreen.Activate();

            // Get the first session
            Session currentSession = m_Sessions[0];

            // Load the map data
            currentMapData = "";
            currentRoadMapData = "";
            LoadMapData(currentSession.map, currentSession.mapScale);

            // wait until the map data is loaded.
            while ((Equals(currentMapData, "") || Equals(currentRoadMapData, "") ||
                    Equals(DialogLines, "")) && IsOnlineBuild)
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }

            LoadingScreen.Deactivate();

            // Create the session
            CreateArea(currentSession);

            // Remove the session
            m_Sessions.RemoveAt(0);
        }
    }


    /// <summary>
    /// Start the game episode after the survey
    /// </summary>
    public void StartAreaAfterSurvey()
    {
        if (IsAreaLoaded())
        {
            m_ActiveArea.StartArea();
            SurveyManager.SetSession(m_ActiveArea.GetSessionInfo());
        }
        else
        {
            // Show the end message
            SurveyManager.CreateSurvey(_timeStamp, SurveyType.End);
            SurveyManager.ShowSurvey();
        }
    }

    public StealthArea GetActiveArea()
    {
        if (m_ActiveArea != null)
            return m_ActiveArea;

        return null;
    }

    public void SetGameActive(bool state)
    {
        if (m_ActiveArea != null)
            m_ActiveArea.gameObject.SetActive(state);
    }


    public void EndCurrentGame()
    {
        if (m_ActiveArea != null)
        {
            RemoveArea(m_ActiveArea.gameObject);
        }
    }

    public void EndNonTutorialGame()
    {
        if (m_ActiveArea != null && !Equals(m_ActiveArea.GetSessionInfo().gameCode, "tutorial"))
        {
            EndCurrentGame();
        }
    }


    // Remove the current area and load the next scenario
    public void RemoveArea(GameObject area)
    {
        m_ActiveArea = null;
        Destroy(area);
    }
}

// Logging modes
public enum Logging
{
    // Save log files locally.
    Local,

    // Upload log files to a server.
    Cloud,

    // No logging.
    None
}

// World Representation Type 
public enum WorldRepType
{
    Continuous,
    Grid
}

// Guard decision maker for patrol
public enum GuardPatrolPlanner
{
    Stalest,
    Random,
    UserInput
}

// Guard decision maker for chasing an intruder
public enum GuardChasePlanner
{
    Simple,
    Intercepting
}

// Guard decision maker for searching for an intruder
public enum GuardSearchPlanner
{
    // Randomly traverse the nodes of the Abstraction graph
    Random,

    // The guards search the road map while propagating the probability of the intruder's presence.
    // The probability is diffused similarly to Damian Isla's implementation
    RmPropOccupancyDiffusal,

    // The probability is simply propagated through the road map.
    RmPropSimple,

    // The guards know the intruder's position at all times.
    Cheating
}


// The style the guard makes a decision
public enum GuardDecisionStyle
{
    // The guard chooses a point and navigate to it.
    individual,

    // The guard finds a path.
    path
}


// Intruder behavior 
public enum IntruderPlanner
{
    Random,
    RandomMoving,
    UserInput,
    Heuristic,
    HeuristicMoving
}

// Heuristic for path finding 
public enum PathFindingHeursitic
{
    EuclideanDst
}

// Path following algorithm
public enum PathFollowing
{
    SimpleFunnel
}


public enum NpcType
{
    Guard,
    Intruder
}

// the scenario session will be set in
public enum Scenario
{
    // The session starts with randomly allocating the npcs on the map.
    Normal,

    // The session starts with randomly guards placed and intruders placed away from them.
    Stealth,

    // The session starts with the intruder, if present, being at a certain distance from one of the guards 
    Chase,

    // The NPCs are Manually set in the map
    Manual
}

// Game Type
public enum GameType
{
    CoinCollection,

    Stealth
}


// The view of the game based on the perspective
public enum GameView
{
    // The game renders all NPCs at all times.
    Spectator,

    // The game only renders the guards when they are seen by the intruder.
    Intruder,

    // The game only renders the intruder when seen by the guards.
    Guard
}


// Struct for the guard planners
public struct GuardBehavior
{
    public GuardPatrolPlanner patrol;
    public GuardChasePlanner chase;
    public GuardSearchPlanner search;

    public GuardBehavior(GuardPatrolPlanner _patrol, GuardChasePlanner _chase, GuardSearchPlanner _search)
    {
        patrol = _patrol;
        chase = _chase;
        search = _search;
    }
}


[Serializable]
public struct NpcData
{
    // A single source to set NPC IDs
    public int id;

    // The NPC type
    public NpcType npcType;

    // The Planner the guard uses to find its next move
    public GuardBehavior? guardPlanner;

    // Intruder planner
    public IntruderPlanner? intruderPlanner;

    // The A* search heuristic
    public PathFindingHeursitic npcHeuristic;

    // Navmesh following behavior
    public PathFollowing npcPathFollowing;

    // Initial position for the NPC
    public NpcLocation? location;

    public NpcData(int _id, NpcType pNpcType, GuardBehavior? _guardPlanner, IntruderPlanner? _intruderPlanner,
        PathFindingHeursitic pPathFindingHeuristic, PathFollowing pNpcPathFollowing, NpcLocation? _location)
    {
        id = _id;
        npcType = pNpcType;
        guardPlanner = _guardPlanner;
        intruderPlanner = _intruderPlanner;
        npcHeuristic = pPathFindingHeuristic;
        npcPathFollowing = pNpcPathFollowing;
        location = _location;
    }

    public static string Headers = "NpcType,ID,NpcPlanner,NpcHeurisitic,NpcPathFollowing";

    public override string ToString()
    {
        var data = "";
        data += npcType + ",";
        data += id + ",";
        data += guardPlanner + ",";
        data += npcHeuristic + ",";
        data += npcPathFollowing;

        return data;
    }
}

public struct NpcLocation
{
    public Vector2? position;
    public float rotation;

    public NpcLocation(Vector2 _position, float _rotation)
    {
        position = _position;
        rotation = _rotation;
    }
}

// Session info
[Serializable]
public struct Session
{
    // the ID of the game session
    public string id;
    
    // Game Code is the scenario for the NPC
    public string gameCode;

    // Choose the type of game
    public GameType gameType;

    // Session scenario
    public Scenario scenario;

    // World Representation
    public WorldRepType worldRepType;

    // The Threshold at which the covered region is reset 
    public int coveredRegionResetThreshold;

    public string guardColor;

    // Number of guards
    public float guardsCount;

    // Number of Intruders
    public int intruderCount;

    // dialog flag if enabled
    public bool isDialogEnabled;

    // The map 
    public string map;

    // The map Scale
    public float mapScale;

    // Guards Data
    public List<NpcData> guardsList;

    // Intruders Data
    public List<NpcData> intrudersList;

    // the type of survey that will be showed after this session 
    public SurveyType surveyType;

    public Session(string pGameCode, GameType _gameType, Scenario pScenario, string _guardColor, int pGuardsCount,
        int pIntruderCount,
        int pCoveredRegionResetThreshold,
        WorldRepType pWorldRepType,
        string pMap,
        bool _isDialogEnabled,
        SurveyType _surveyType = SurveyType.End,
        float pMapScale = 1f)
    {
        gameCode = pGameCode;
        scenario = pScenario;
        guardColor = _guardColor;
        guardsCount = pGuardsCount;
        intruderCount = pIntruderCount;
        coveredRegionResetThreshold = pCoveredRegionResetThreshold;
        worldRepType = pWorldRepType;
        map = pMap;
        guardsList = new List<NpcData>();
        intrudersList = new List<NpcData>();
        mapScale = pMapScale;
        gameType = _gameType;
        isDialogEnabled = _isDialogEnabled;
        surveyType = _surveyType;
        id = "";
    }


    public float GetMapScale()
    {
        return mapScale;
    }

    // Add a NPC to the list
    public void AddGuard(int id, NpcType npcType, GuardBehavior? guardPlanner, IntruderPlanner? intruderPlanner,
        PathFindingHeursitic pathFindingHeuristic, PathFollowing pathFollowing, NpcLocation? npcLocation)
    {
        guardsList.Add(new NpcData(id, npcType, guardPlanner, intruderPlanner, pathFindingHeuristic, pathFollowing,
            npcLocation));
    }

    public void AddIntruder(int id, NpcType npcType, GuardBehavior? guardPlanner, IntruderPlanner? intruderPlanner,
        PathFindingHeursitic pathFindingHeuristic, PathFollowing pathFollowing, NpcLocation? npcLocation)
    {
        intrudersList.Add(new NpcData(id, npcType, guardPlanner, intruderPlanner, pathFindingHeuristic, pathFollowing,
            npcLocation));
    }


    // Add the NPC data
    public List<NpcData> GetGuardsData()
    {
        return guardsList;
    }

    public List<NpcData> GetIntrudersData()
    {
        return intrudersList;
    }

    public override string ToString()
    {
        // Separator
        string sep = " ";

        string sessionInfo = "";

        // Game code
        sessionInfo += gameCode + sep;

        // Game type
        sessionInfo += gameType + sep;

        // Man name
        sessionInfo += map + sep;

        // Map scale
        sessionInfo += GetMapScale() + sep;

        // Guard planner 
        sessionInfo += (GetGuardsData().Count > 0 ? GetGuardsData()[0].guardPlanner.Value.search.ToString() : "") + sep;

        // Guard FoV percentage of the longest path in the map
        sessionInfo += Properties.GuardsFovRadiusPercentage + sep;

        // Number of guards
        sessionInfo += guardsCount + sep;

        // Intruder planner 
        // sessionInfo += (GetIntrudersData().Count > 0 ? GetIntrudersData()[0].intruderPlanner.Value.ToString() : "") +
        //                sep;

        // Intruder's speed percentage to guards
        sessionInfo += Properties.IntruderSpeedMulti + sep;

        // Length of the episode
        sessionInfo += Properties.EpisodeLength;

        return sessionInfo;
    }
}