using System;
using System.Collections;
using System.Collections.Generic;
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
    private StealthArea _activeArea;

    // List of scenarios to be executed
    private List<Session> _remainingSessions;
    private List<Session> _closedSessions;

    // To determine which perspective the game is viewed from
    [Header("Game Mode")] [SerializeField] [Tooltip("What elements will be shown?")]
    public GameView gameView;

    // display survey?
    public bool showSurvey;

    [SerializeField] [Tooltip("Is this will be web based?")]
    public bool IsOnlineBuild;

    // Logging Variables 
    [Header("Logging")] [Tooltip("Specify the logging method")]
    public Logging loggingMethod;

    [Header("Time")] [Tooltip("Simulation speed")] [Range(1, 100)]
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
    public static string DialogsPath = "Dialogs/";
    public static string PatrolPathsPath = "NPCs/PatrolPaths/";

    // The main camera
    public static Camera MainCamera;

    // The survey controller
    public static SurveyManager SurveyManager;

    public static LoadingScreenController LoadingScreen;

    // Container for the dialogs
    public static string DialogLines;

    // The containers variables for the current map
    public MapData currentMap { set; get; }
    public string currentMapData { set; get; }
    public string currentRoadMapData { set; get; }

    private List<VoiceParams> m_VoiceParamses;

    // Game manager instance handler
    public static GameManager Instance;

    // Beginning of the game manager.
    private void Start()
    {
        // Set the time of the session
        _timeStamp = GetDateTimestamp();

        // Initiate the references
        _activeArea = null;
        _remainingSessions = new List<Session>();
        _closedSessions = new List<Session>();
        Instance = this;

        // Define the hierarchy of the paths for the game
        // Main path
        DataPath = "Data/";
        // Logs path
        LogsPath = "C:/LogFiles/patrol/"; // DataPath + LogsPath;
        // Map related data paths
        MapsDataPath = DataPath + MapsDataPath;
        MapsPath = MapsDataPath + MapsPath;
        RoadMapsPath = MapsDataPath + RoadMapsPath;
        // Dialogs path
        DialogsPath = DataPath + DialogsPath;
        // Patrol Paths
        PatrolPathsPath = DataPath + PatrolPathsPath;

        // Reference the main camera
        MainCamera = Camera.main;

        // World state storage
        WorldState.Initialize();

        // Initiate the survey handle
        GameObject canvasGO = GameObject.Find("Canvas");
        SurveyManager = canvasGO.transform.Find("Survey").GetComponent<SurveyManager>();
        SurveyManager.Initiate();

        LoadingScreen = canvasGO.transform.Find("Loading Screen").GetComponent<LoadingScreenController>();
        LoadingScreen.Initiate();

        if (IsOnlineBuild) StartCoroutine(FileUploader.GetFile(DialogLines, "dialogs"));

        // Load the sessions to play
        LoadSavedSessions();

        StartCoroutine(LoadGamesWhenReady());

        // Set the simulation speed
        Time.timeScale = SimulationSpeed;

        if (!showSurvey) return;

        // show the survey for new users
        SurveyManager.CreateSurvey(_timeStamp, SurveyType.NewUser, 0f);
        SurveyManager.ShowSurvey();
    }

    public GameType GetGameType()
    {
        return _remainingSessions[0].gameType;
    }

    private void FillVoices()
    {
        m_VoiceParamses = new List<VoiceParams>();

        int[] voiceIndices = {0, 1};
        float[] pitches = {0f, 1f, 2f};

        foreach (var vI in voiceIndices)
        foreach (var p in pitches)
            m_VoiceParamses.Add(new VoiceParams(vI, p));
    }

    public VoiceParams GetVoice()
    {
        if (m_VoiceParamses.Count == 0) return new VoiceParams(0, 0f);

        int randIndex = Random.Range(0, m_VoiceParamses.Count);
        VoiceParams voice = m_VoiceParamses[randIndex];
        m_VoiceParamses.RemoveAt(randIndex);

        return voice;
    }

    // This is to get a unique time stamp composed of seconds
    public static int GetDateTimestamp()
    {
        DateTime epochStart = new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        return (int) (DateTime.UtcNow - epochStart).TotalSeconds;
    }

    public static int GetRunId()
    {
        return _timeStamp;
    }

    private void LoadSavedSessions()
    {
        // var sessions = SessionsSetup.SearchTacticEvaluation();
        // List<Session> sessions = SessionsSetup.StealthStudy();
        // List<Session> sessions = SessionsSetup.StealthStudy002();
        // List<Session> sessions = SessionsSetup.StealthStudyProcedural();
        // List<Session> sessions = SessionsSetup.StealthStudyProcedural01();
        // List<Session> sessions = StealthStudySessions.GetSessions();
        // List<Session> sessions = StealthUserStudySessions.GetSessions();
        // List<Session> sessions = PatrolSessionsAssessment.GetSessions();
        // List<Session> sessions = PatrolSessions.GetSessions();

        List<Session> sessions = PatrolUserStudy.GetSessions();

        // List<Session> sessions = StealthBehavior.GetSessions();
        // List<Session> sessions = SearchSessions.GetSessions();


        StartCoroutine(FileUploader.UploadData(null, FileType.ColorPairing, "text/csv",
            PatrolUserStudy.GetPairsString()));

        // Each line represents a session
        foreach (var sc in sessions)
        {
            if (Equals(loggingMethod, Logging.Local))
            {
                // Set the number of recorded session
                SetEpisodeCount(sc);

                // Check if the required number of Episodes is logged already or skip if logging is not required.
                if (PerformanceLogger.IsLogged(sc)) continue;
            }

            _remainingSessions.Add(sc);
        }
    }

    private bool IsAreaLoaded()
    {
        return !Equals(_activeArea, null);
    }

    public void ClearArea()
    {
        Destroy(_activeArea.gameObject);
        _activeArea = null;
    }


    /// <summary>
    /// Load the map data 
    /// </summary>
    /// <param name="map"> The name of the map</param>
    /// <param name="mapScale"> The scale multiplier of the map</param>
    // private void LoadMapData(MapData map)
    private void LoadMapData(string mapName)
    {
        if (IsOnlineBuild)
        {
            // Load the map data
            StartCoroutine(FileUploader.GetFile(mapName, "map"));

            // Load the road map data
            // StartCoroutine(FileUploader.GetFile(mapName, "roadMap", map.size));
        }
        else
        {
            // Get the map data
            // currentMapData = CsvController.ReadString(GetMapPath(map.name, "csv"));
            currentMapData = CsvController.ReadString(GetMapPath(mapName, "json"));
            // currentRoadMapData = CsvController.ReadString(GetRoadMapPath(mapName, map.size));
        }
    }

    // Get the path to the map
    private static string GetMapPath(string mapName, string fileType)
    {
        // Gets the path to the "Assets" folder 
        return MapsPath + mapName + "." + fileType;
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
        _activeArea = activeArea.GetComponent<StealthArea>();

        // Initiate the session
        _activeArea.InitiateArea(scenario);

        // Hide the area 
        if (showSurvey) _activeArea.gameObject.SetActive(false);
    }

    private IEnumerator LoadGamesWhenReady()
    {
        if (!Equals(PlayerData.PlayerName, "") && Equals(_remainingSessions[0].gameCode, "tutorial"))
            _remainingSessions.RemoveAt(0);

        while (_remainingSessions.Count > 0)
        {
            // if there is an active area then skip
            if (IsAreaLoaded())
            {
                yield return new WaitForSecondsRealtime(0.5f);
                continue;
            }

            LoadingScreen.Activate();

            // Get the first session
            Session currentSession = _remainingSessions[0];

            if (!Equals(currentSession.gameCode, "tutorial")) _closedSessions.Add(currentSession);

            // Load the map data
            currentMapData = "";
            currentRoadMapData = "";
            LoadMapData(currentSession.GetMap().name);

            // wait until the map data is loaded.
            while ((Equals(currentMapData, "") || Equals(currentRoadMapData, "") ||
                    Equals(DialogLines, "")) && IsOnlineBuild)
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }

            LoadingScreen.Deactivate();

            FillVoices();

            // Create the session
            CreateArea(currentSession);

            // Remove the session
            _remainingSessions.RemoveAt(0);
        }
    }

    public void SkipSession()
    {
        Session session = _remainingSessions[0];
        _remainingSessions.RemoveAt(0);
        _closedSessions.Add(session);
    }

    private void SetEpisodeCount(Session sa)
    {
        string episodeCountString = CsvController.ReadString(CsvController.GetPath(sa, FileType.EpisodeCount, null));
        int episode = Equals(episodeCountString.Length, 0) ? 0 : int.Parse(episodeCountString);
        sa.currentEpisode = episode;
    }

    /// <summary>
    /// Start the game episode after the survey
    /// </summary>
    public void StartAreaAfterSurvey()
    {
        Time.timeScale = 1f;

        if (IsSessionsOver())
        {
            // Show the end message
            SurveyManager.CreateSurvey(_timeStamp, SurveyType.End, 0f);
            SurveyManager.ShowSurvey();
        }
    }

    public IEnumerator StartGamePostSurvey()
    {
        while (_remainingSessions.Count > 0)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (IsAreaLoaded()) break;
        }

        if (IsAreaLoaded())
        {
            _activeArea.StartArea();
            yield break;
        }

        // Show the end message
        SurveyManager.CreateSurvey(_timeStamp, SurveyType.End, 0f);
        SurveyManager.ShowSurvey();
    }

    public bool IsSessionsOver()
    {
        return _remainingSessions.Count == 0;
    }

    public StealthArea GetActiveArea()
    {
        if (_activeArea != null)
            return _activeArea;

        return null;
    }

    public void SetGameActive(bool state)
    {
        if (_activeArea != null)
            _activeArea.gameObject.SetActive(state);
    }

    public void EndCurrentGame()
    {
        if (_activeArea != null) RemoveArea(_activeArea.gameObject);
    }

    public void EndNonTutorialGame()
    {
        if (_activeArea != null && !Equals(_activeArea.GetSessionInfo().gameCode, "tutorial"))
        {
            EndCurrentGame();
        }
    }

    public List<Session> GetClosedSessions()
    {
        return _closedSessions;
    }

    // Remove the current area and load the next scenario
    public void RemoveArea(GameObject area)
    {
        _activeArea = null;
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

    Stealth,

    StealthPath
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


[Serializable]
public struct NpcData
{
    // A single source to set NPC IDs
    public int id;

    // The NPC type
    public NpcType npcType;

    // public Behavior behavior;

    // The A* search heuristic
    // public PathFindingHeursitic npcHeuristic;

    // NavMesh following behavior
    // public PathFollowing npcPathFollowing;

    // Initial position for the NPC
    public NpcLocation? location;

    public NpcData(int _id, NpcType pNpcType, Behavior _behavior,
        PathFindingHeursitic pPathFindingHeuristic, PathFollowing pNpcPathFollowing, NpcLocation? _location)
    {
        id = _id;
        npcType = pNpcType;
        // behavior = _behavior;
        // npcHeuristic = pPathFindingHeuristic;
        // npcPathFollowing = pNpcPathFollowing;
        location = _location;
    }

    public static string Headers = "NpcType,ID,NpcPlanner,NpcHeurisitic,NpcPathFollowing";

    public override string ToString()
    {
        var data = "";
        data += npcType + ",";
        data += id + ",";
        // data += behavior + ",";
        // data += npcHeuristic + ",";
        // data += npcPathFollowing;

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
public class Session
{
    public int currentEpisode = 0;
    public readonly int MaxEpisodes = 1;

    public float episodeLengthSec;

    // the ID of the game session
    private string timeStamp;

    // Game Code is the scenario for the NPC
    public string gameCode;

    // Choose the type of game
    public GameType gameType;

    // Session scenario
    public Scenario scenario;

    public string sessionVariable;
    public string guardColor;

    public GuardSpawnType guardSpawnMethod;

    // Number of guards
    public int guardsCount;

    // Guards Data
    public List<NpcData> guardsList;

    // Guard behavior parameters
    public GuardBehaviorParams guardBehaviorParams;

    // Number of Intruders
    public int intruderCount;

    // dialog flag if enabled
    public SpeechType speechType;

    // Intruders Data
    public List<NpcData> intrudersList;

    // public IntruderBehavior intruderBehavior;
    public IntruderBehaviorParams IntruderBehaviorParams;

    private MapData map;

    public List<ScoreRecord> _scores;

    // the type of survey that will be showed after this session 
    public SurveyType surveyType;

    public Session(float _episodeLength, string _gameCode, GameType _gameType, Scenario pScenario, string _guardColor,
        GuardSpawnType _guardSpawnType, int pGuardsCount, GuardBehaviorParams _guardBehaviorParams,
        int pIntruderCount, IntruderBehaviorParams _intruderBehaviorParams,
        MapData _map,
        SpeechType _speechType,
        SurveyType _surveyType = SurveyType.End)
    {
        episodeLengthSec = _episodeLength;
        gameCode = _gameCode;
        scenario = pScenario;
        guardColor = _guardColor;
        guardSpawnMethod = _guardSpawnType;
        guardsCount = pGuardsCount;
        guardBehaviorParams = _guardBehaviorParams;
        intruderCount = pIntruderCount;
        map = _map;
        guardsList = new List<NpcData>();
        intrudersList = new List<NpcData>();
        IntruderBehaviorParams = _intruderBehaviorParams;
        gameType = _gameType;
        speechType = _speechType;
        surveyType = _surveyType;
        timeStamp = "";
        _scores = new List<ScoreRecord>();
    }

    public void SetTimestamp()
    {
        timeStamp = GameManager.GetDateTimestamp().ToString();
    }

    public MapData GetMap()
    {
        return map;
    }

    // Add a NPC to the list
    public void AddNpc(int id, NpcType _type, Behavior _planner,
        PathFindingHeursitic pathFindingHeuristic, PathFollowing pathFollowing, NpcLocation? npcLocation)
    {
        switch (_type)
        {
            case NpcType.Guard:
                guardsList.Add(new NpcData(id, _type, _planner, pathFindingHeuristic, pathFollowing,
                    npcLocation));
                break;

            case NpcType.Intruder:
                intrudersList.Add(new NpcData(id, _type, _planner, pathFindingHeuristic, pathFollowing,
                    npcLocation));
                break;
        }
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

    public void LoadScores(string scoresData)
    {
        _scores.Clear();

        // Split data by lines
        var lines = scoresData.Split('\n');

        // Each line represents a polygon
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].Length > 0)
            {
                if (lineIndex == 0) continue;

                var data = lines[lineIndex].Split(',');

                _scores.Add(new ScoreRecord(lineIndex, float.Parse(data[1]), data[0]));
            }
    }

    public override string ToString()
    {
        // Separator
        string sep = "_";

        string sessionInfo = "";

        // Man name
        sessionInfo += map.name;
        sessionInfo += sep;

        // Guards count 
        sessionInfo += guardsCount;
        sessionInfo += sep;

        // Guard planner 
        sessionInfo += GetGuardsData().Count > 0 ? guardBehaviorParams.ToString() : "";
        sessionInfo += sep;


        // intruder planner
        sessionInfo += guardBehaviorParams.ToString();
        sessionInfo += sep;

        sessionInfo += IntruderBehaviorParams.ToString();
        sessionInfo += sep;


        sessionInfo += guardSpawnMethod;
        // sessionInfo += sep;


        return sessionInfo;
    }
}


[Serializable]
public class SessionPair
{
    public string color;
    public string variable;

    public SessionPair(string _color, string _variable)
    {
        color = _color;
        variable = _variable;
    }
}

public class GuardBehaviorParams
{
    public PatrolPlanner patrolPlanner;
    public PatrolerParams patrolerParams;

    public SearchPlanner searcherPlanner;
    public SearcherParams searcherParams;

    public AlertPlanner alertPlanner;
    public ChaseParams chaseParams;

    public GuardBehaviorParams(PatrolPlanner _planner, PatrolerParams _patrolerParams, SearchPlanner _searchPlanner,
        SearcherParams _searcherParams, AlertPlanner _alertPlanner, ChaseParams _chaseParams)
    {
        patrolPlanner = _planner;
        patrolerParams = _patrolerParams;

        searcherPlanner = _searchPlanner;
        searcherParams = _searcherParams;

        alertPlanner = _alertPlanner;
        chaseParams = _chaseParams;
    }

    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += patrolPlanner;
        output += sep;

        output += patrolerParams;
        // output += sep;

        return output;
    }
}


public class IntruderBehaviorParams
{
    public PatrolPlanner patrolPlanner;
    public ScouterParams scouterParams;

    public SearchPlanner searchPlanner;
    public SearchEvaderParams searchEvaderParams;

    public AlertPlanner alertPlanner;
    public ChaseEvaderParams chaseEvaderParams;
    
    
    public IntruderBehaviorParams(PatrolPlanner _planner, ScouterParams _scouterParams, SearchPlanner _searchPlanner, SearchEvaderParams _searchEvaderParams, AlertPlanner _alertPlanner, ChaseEvaderParams _chaseEvaderParams)
    {
        patrolPlanner = _planner;
        scouterParams = _scouterParams;

        searchPlanner = _searchPlanner;
        searchEvaderParams = _searchEvaderParams;

        alertPlanner = _alertPlanner;
        chaseEvaderParams = _chaseEvaderParams;
    }

    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += patrolPlanner;
        output += sep;

        output += scouterParams;
        // output += sep;

        return output;
    }
}