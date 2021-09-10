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
    // The path to the stealth area prefab
    private readonly string m_StealthArea = "Prefabs/StealthArea";

    // Active area
    private StealthArea m_activeArea;

    // List of scenarios to be executed
    private List<Session> m_Sessions;

    // display survey?
    public bool ShowSurvey;

    // Choose the type of game
    public GameType gameType;


    [SerializeField] private bool m_IsOnlineBuild;

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

    // Available maps
    private readonly string[] m_Maps = {"Boxes"}; //{"dragon_age2", "valorant_ascent", "Boxes"};
    private readonly int[] m_MapScales = {1}; //{1, 2, 1};
    private readonly int[] m_GuardCounts = {3}; //{3, 4, 6};
    private int chosenIndex;

    // The main camera
    public static Camera MainCamera;

    public static SurveyManager SurveyManager;

    // The time stamp the game started
    public static int TimeStamp;

    public string currentMapData { set; get; }
    public string currentRoadMapData { set; get; }

    // Game manager instance handler
    public static GameManager instance;

    // Beginning of the game manager.
    private void Start()
    {
        // Initiate the references
        m_activeArea = null;
        m_Sessions = new List<Session>();
        instance = this;

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
        
        // Load the sessions to play
        LoadSavedSessions();
        
        ChooseMap();

        // Set the simulation speed
        Time.timeScale = SimulationSpeed;

        SurveyManager = GameObject.Find("Canvas").transform.Find("Survey").GetComponent<SurveyManager>();
        SurveyManager.Initiate();

        TimeStamp = GetDateTime();
    }


    // This is to get a unique time stamp
    public static int GetDateTime()
    {
        DateTime epochStart = new DateTime(2021, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        return (int) (DateTime.UtcNow - epochStart).TotalSeconds;
    }

    private void LoadSavedSessions()
    {
        var sessions = PrepareStudySessions();

        // Each line represents a session
        foreach (var session in sessions)
        {
            // Get the session info
            var sc = new Session(session["GameCode"],
                (Scenario) Enum.Parse(typeof(Scenario), session["Scenario"], true),
                int.Parse(session["GuardsCount"]), int.Parse(session["IntudersCount"]),
                int.Parse(session["CoverageResetThreshold"]),
                (WorldRepType) Enum.Parse(typeof(WorldRepType), session["WorldRep"], true), session["Map"],
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

    private void ChooseMap()
    {
        // chosenIndex = Random.Range(0, m_Maps.Length);
        // string map = m_Maps[chosenIndex];
        // int mapScale = m_MapScales[chosenIndex];

        string map = m_Sessions[0].map;
        float mapScale = m_Sessions[0].mapScale;
        
        LoadMapData(map, mapScale);

        StartCoroutine(LoadGamesWhenReady());
    }

    // Load the map data
    private void LoadMapData(string map, float mapScale)
    {
        if (m_IsOnlineBuild)
        {
            // Load the map data
            StartCoroutine(FileUploader.GetFile(map, "map", 0f));
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
    private string GetMapPath(string mapName)
    {
        // Gets the path to the "Assets" folder 
        return MapsPath + mapName + ".csv";
    }

    // Get the path to the map
    private string GetRoadMapPath(string mapName, float mapScale)
    {
        // Gets the path to the "Assets" folder 
        return RoadMapsPath + mapName + "_" + mapScale + ".csv";
    }

    private IEnumerator LoadGamesWhenReady()
    {
        while (currentMapData == null || currentRoadMapData == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (ShowSurvey)
            SurveyManager.CreateNewUserSurvey(TimeStamp);

        // Load the next session
        LoadNextScenario();
    }

    private List<Dictionary<string, string>> PrepareStudySessions()
    {
        string map = m_Maps[chosenIndex];
        int mapScale = m_MapScales[chosenIndex];
        int guardCount = m_GuardCounts[chosenIndex];

        List<string> originalMethods = new List<string>() {"RmPropSimple"}; //, "Cheating", "RmPropOccupancyDiffusal"};

        List<string> methods = new List<string>();

        while (originalMethods.Count > 0)
        {
            int randomIndex = Random.Range(0, originalMethods.Count);
            methods.Add(originalMethods[randomIndex]);
            originalMethods.RemoveAt(randomIndex);
        }

        List<Dictionary<string, string>> sessions = new List<Dictionary<string, string>>();

        Dictionary<string, string> session; // = new Dictionary<string, string>();

        // session.Add("GameCode", "");
        // session.Add("Scenario", "Chase");
        // session.Add("CoverageResetThreshold", "100");
        // session.Add("WorldRep", "Continuous");
        // session.Add("Map", map);
        // session.Add("MapScale", mapScale.ToString());
        // session.Add("GuardPatrolPlanner", "Stalest");
        // session.Add("GuardChasePlanner", "Simple");
        // session.Add("GuardSearchPlanner", "Random");
        // session.Add("GuardsCount", "2");
        // session.Add("PathFindingHeursitic", "EuclideanDst");
        // session.Add("PathFollowing", "SimpleFunnel");
        // session.Add("IntudersCount", "1");
        // session.Add("IntruderPlanner", "UserInput");
        //
        // sessions.Add(session);


        foreach (var guardMethod in methods)
        {
            // Dictionary for the session 
            session = new Dictionary<string, string>
            {
                {"GameCode", ""},
                {"Scenario", "Chase"},
                {"CoverageResetThreshold", "100"},
                {"WorldRep", "Continuous"},
                {"Map", map},
                {"MapScale", mapScale.ToString()},
                {"GuardPatrolPlanner", "Stalest"},
                {"GuardChasePlanner", "Simple"},
                {"GuardSearchPlanner", guardMethod},
                {"GuardsCount", guardCount.ToString()},
                {"PathFindingHeursitic", "EuclideanDst"},
                {"PathFollowing", "SimpleFunnel"},
                {"IntudersCount", "1"},
                {"IntruderPlanner", "Heuristic"}
            };


            sessions.Add(session);
        }
        
        return sessions;
    }


    // Create the area and load it with the scenario
    private void CreateArea(Session scenario)
    {
        // Get the area prefab
        var areaPrefab = (GameObject) Resources.Load(m_StealthArea);
        GameObject activeArea = Instantiate(areaPrefab, transform, true);

        // Get the script
        m_activeArea = activeArea.GetComponent<StealthArea>();

        // Initiate the session
        m_activeArea.InitiateArea(scenario);

        if (ShowSurvey)
            m_activeArea.gameObject.SetActive(false);
    }

    // Replenish the scenarios
    public void LoadNextScenario()
    {
        if (m_activeArea != null || m_Sessions.Count <= 0) return;

        // Create the session
        CreateArea(m_Sessions[0]);

        // Remove the session from the list.
        m_Sessions.RemoveAt(0);
    }


    public void StartAreaAfterSurvey()
    {
        if (m_activeArea != null)
        {
            m_activeArea.GetComponent<StealthArea>().StartArea();
            SurveyManager.SetSession(m_activeArea.GetComponent<StealthArea>().GetSessionInfo());
        }
        else
            SurveyManager.CreateEndSurvey(TimeStamp);
    }

    public StealthArea GetActiveArea()
    {
        if (m_activeArea != null)
            return m_activeArea;

        return null;
    }

    // Remove the current area and load the next scenario
    public void RemoveArea(GameObject area)
    {
        DestroyImmediate(area);
        m_activeArea = null;
        LoadNextScenario();
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
    // Game Code is the scenario for the NPC
    public string gameCode;

    // Session scenario
    public Scenario scenario;

    // World Representation
    public WorldRepType worldRepType;

    // The Threshold at which the covered region is reset 
    public int coveredRegionResetThreshold;

    // Number of guards
    public float guardsCount;

    // Number of Intruders
    public int intruderCount;

    // The map 
    public string map;

    // The map Scale
    public float mapScale;

    // Guards Data
    public List<NpcData> guardsList;

    // Intruders Data
    public List<NpcData> intrudersList;


    public Session(string pGameCode, Scenario pScenario, int pGuardsCount, int pIntruderCount,
        int pCoveredRegionResetThreshold,
        WorldRepType pWorldRepType,
        string pMap,
        float pMapScale = 1f)
    {
        gameCode = pGameCode;
        scenario = pScenario;
        guardsCount = pGuardsCount;
        intruderCount = pIntruderCount;
        coveredRegionResetThreshold = pCoveredRegionResetThreshold;
        worldRepType = pWorldRepType;
        map = pMap;
        guardsList = new List<NpcData>();
        intrudersList = new List<NpcData>();
        mapScale = pMapScale;
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
        sessionInfo += (GetIntrudersData().Count > 0 ? GetIntrudersData()[0].intruderPlanner.Value.ToString() : "") +
                       sep;

        // Intruder's speed percentage to guards
        sessionInfo += Properties.IntruderSpeedPercentage + sep;

        // Length of the episode
        sessionInfo += Properties.EpisodeLength;

        return sessionInfo;
    }
}