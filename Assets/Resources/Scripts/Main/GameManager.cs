using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Logging Enabled 
    [Header("Logging")] [Tooltip("Log the performance")]
    public bool enableLogging;

    [Tooltip("Upload data to the server")]
    public bool uploadData;
    
    // Number of episode to run in each session
    [Tooltip("Log the performance")] public int NumberOfEpisodesPerSession;

    [Header("Time")] [Tooltip("Simulation speed")] [Range(1, 100)]
    public int SimulationSpeed;

    // The path to the stealth area prefab
    private readonly string m_StealthArea = "Prefabs/StealthArea";

    // Active area
    private GameObject m_activeArea;

    // The Scenarios to be executed
    private List<Session> m_scenarios;

    [Tooltip("Rendering")] public bool Render;

    [Header("Scenario Setup")] [Tooltip("If checked run the specified scenario")]
    public bool runTestScenario;

    [Tooltip("Scenario Setup")] public Session testScenario;

    private void Start()
    {
        m_activeArea = null;
        m_scenarios = new List<Session>();

        // Load sessions
        //LoadUserControlledChaseSessions();
        LoadSavedSessions();
        
        // Set the simulation speed
        Time.timeScale = SimulationSpeed;

        // Load the next session
        LoadNextScenario();
    }

    private void LoadSavedSessions()
    {
        // Get the path to the sessions records
        string path = Application.dataPath + "/Sessions.csv";

        // Load the sessions file
        var sessionsString = CsvController.ReadString(path);

        // Split data by lines
        var lines = sessionsString.Split('\n');

        // Each line represents a session
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].Length > 0)
            {
                // Split the elements
                var data = lines[lineIndex].Split(',');

                // Get the session info
                var sc = new Session(data[0], (Scenario) Enum.Parse(typeof(Scenario), data[1], true),
                    int.Parse(data[2]), (WorldRepType) Enum.Parse(typeof(WorldRepType), data[3], true), data[4],
                    float.Parse(data[5]));

                // Set the guard behavior
                GuardBehavior guardBehavior = new GuardBehavior(
                    (GuardPatrolPlanner) Enum.Parse(typeof(GuardPatrolPlanner), data[6], true),
                    (GuardChasePlanner) Enum.Parse(typeof(GuardChasePlanner), data[7], true),
                    (GuardSearchPlanner) Enum.Parse(typeof(GuardSearchPlanner), data[8], true));

                // Add the guards
                for (int i = 0; i < int.Parse(data[9]); i++)
                    sc.AddNpc(NpcType.Guard, guardBehavior, null,
                        (PathFindingHeursitic) Enum.Parse(typeof(PathFindingHeursitic), data[10], true),
                        (PathFollowing) Enum.Parse(typeof(PathFollowing), data[11], true),
                        null);

                // Add the intruders
                for (int i = 0; i < int.Parse(data[12]); i++)
                    sc.AddNpc(NpcType.Intruder, null, (IntruderPlanner) Enum.Parse(typeof(IntruderPlanner), data[13], true), (PathFindingHeursitic) Enum.Parse(typeof(PathFindingHeursitic), data[14], true),
                        (PathFollowing) Enum.Parse(typeof(PathFollowing), data[15], true),
                        null);
                
                m_scenarios.Add(sc);
            }
    }


    // Load a set of predefined sessions
    private void LoadUserControlledChaseSessions()
    {
        // Set the starting location for the npcs
        List<NpcLocation> guard1Locations = new List<NpcLocation>();
        List<NpcLocation> guard2Locations = new List<NpcLocation>();
        List<NpcLocation> intruderLocations = new List<NpcLocation>();

        guard1Locations.Add(new NpcLocation(new Vector2(0.5f, 0f), 90f));
        guard2Locations.Add(new NpcLocation(new Vector2(0f, 0f), 90f));
        intruderLocations.Add(new NpcLocation(new Vector2(0f, 3f), 0f));


        // Add scenarios to list
        // var sc = new Session("SimpleRandom", Scenario.Manual, 100, WorldRepType.VisMesh, "CoD_svg2",
        //       0.1f); 

        // var sc = new Session("SimpleRandom", Scenario.Manual, 100, WorldRepType.VisMesh, "Boxes", 
        //     1f);

        var sc = new Session("SimpleRandom", Scenario.Manual, 100, WorldRepType.VisMesh, "MgsDock",
            2f);

        // var sc = new Session("SimpleRandom", Scenario.Manual, 100, WorldRepType.VisMesh, "LevelA",
        //     1f);

        GuardBehavior guardBehavior = new GuardBehavior(GuardPatrolPlanner.Stalest, GuardChasePlanner.Simple,
            GuardSearchPlanner.Interception);

        // Add NPCs
        sc.AddNpc(NpcType.Intruder, null, IntruderPlanner.Heuristic, PathFindingHeursitic.EuclideanDst,
            PathFollowing.SimpleFunnel,
            null);

        for (int i = 0; i < 2; i++)
            sc.AddNpc(NpcType.Guard, guardBehavior, null, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
                null);


        // Add Scenario
        m_scenarios.Add(sc);


        //   sc = new Session("SimpleHeurisitic", Scenario.Manual, 100, WorldRepType.VisMesh, "CoD_svg");
        //
        //   guardBehavior = new GuardBehavior(GuardPatrolPlanner.Stalest, GuardChasePlanner.Simple,
        //       GuardSearchPlanner.Random); 
        //   
        //   // Add NPCs
        //   sc.AddNpc(NpcType.Intruder, null,IntruderPlanner.Heuristic, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       intruderLocations[0]);
        //   sc.AddNpc(NpcType.Guard, guardBehavior,null, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       guard1Locations[0]);
        //   sc.AddNpc(NpcType.Guard, guardBehavior,null, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       guard2Locations[0]);
        //   
        //   
        // //  m_scenarios.Add(sc);
        //   
        //   sc = new Session("InterceptionHeuristic", Scenario.Manual, 100, WorldRepType.VisMesh, "CoD_svg");
        //
        //   guardBehavior = new GuardBehavior(GuardPatrolPlanner.Stalest, GuardChasePlanner.Intercepting,
        //       GuardSearchPlanner.Interception); 
        //   
        //   // Add NPCs
        //   sc.AddNpc(NpcType.Intruder, null,IntruderPlanner.Heuristic, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       intruderLocations[0]);
        //   sc.AddNpc(NpcType.Guard, guardBehavior,null, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       guard1Locations[0]);
        //   sc.AddNpc(NpcType.Guard, guardBehavior,null, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       guard2Locations[0]);
        //   
        //   
        //   m_scenarios.Add(sc);
        //   
        //   sc = new Session("InterceptionRandom", Scenario.Manual, 100, WorldRepType.VisMesh, "CoD_svg");
        //
        //   guardBehavior = new GuardBehavior(GuardPatrolPlanner.Stalest, GuardChasePlanner.Intercepting,
        //       GuardSearchPlanner.Interception); 
        //   
        //   // Add NPCs
        //   sc.AddNpc(NpcType.Intruder, null,IntruderPlanner.Random, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       intruderLocations[0]);
        //   sc.AddNpc(NpcType.Guard, guardBehavior,null, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       guard1Locations[0]);
        //   sc.AddNpc(NpcType.Guard, guardBehavior,null, PathFindingHeursitic.EuclideanDst, PathFollowing.SimpleFunnel,
        //       guard2Locations[0]);
        //   
        //   
        //   m_scenarios.Add(sc);
    }

    // Create the area and load it with the scenario
    private GameObject CreateArea(Session scenario)
    {
        // Get the area prefab
        var areaPrefab = (GameObject) Resources.Load(m_StealthArea);
        var areaGameObject = Instantiate(areaPrefab, transform, true);

        // Get the script
        var area = areaGameObject.GetComponent<StealthArea>();

        // Load the scenario
        area.InitiateArea(scenario);

        return areaGameObject;
    }

    // Replenish the scenarios
    private void LoadNextScenario()
    {
        if (m_activeArea == null)
        {
            if (runTestScenario)
            {
                m_activeArea = CreateArea(testScenario);
                Camera.main.orthographicSize = 5 * testScenario.GetMapScale();
            }
            else
            {
                if (m_scenarios.Count > 0)
                {
                    m_activeArea = CreateArea(m_scenarios[0]);
                    Camera.main.orthographicSize = 5 * m_scenarios[0].GetMapScale();

                    m_scenarios.RemoveAt(0);
                }
                else
                {
#if UNITY_STANDALONE_WIN
                    Application.Quit();
#endif

#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#endif
                }
            }
        }
    }

    // Remove the current area and load the next scenario
    public void RemoveArea(GameObject area)
    {
        m_activeArea = null;
        Destroy(area);
        LoadNextScenario();
    }
}

// World Representation Type 
public enum WorldRepType
{
    VisMesh,
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

    // Assign roles to guard; one to chase and the others to intercept
    Interception
}

// Intruder behavior 
public enum IntruderPlanner
{
    Random,
    UserInput,
    Heuristic
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
    public static int NpcsCount = 0;
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

    public NpcData(NpcType pNpcType, GuardBehavior? _guardPlanner, IntruderPlanner? _intruderPlanner,
        PathFindingHeursitic pPathFindingHeuristic, PathFollowing pNpcPathFollowing, NpcLocation? _location)
    {
        id = NpcsCount++;
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

    // The map 
    public string map;

    // The map Scale
    public float mapScale;

    // NPCs Data
    public List<NpcData> npcsList;


    public Session(string pGameCode, Scenario pScenario, int pCoveredRegionResetThreshold,
        WorldRepType pWorldRepType,
        string pMap,
        float pMapScale = 1f)
    {
        gameCode = pGameCode;
        scenario = pScenario;
        coveredRegionResetThreshold = pCoveredRegionResetThreshold;
        worldRepType = pWorldRepType;
        map = pMap;
        npcsList = new List<NpcData>();
        mapScale = pMapScale;
    }

    public float GetMapScale()
    {
        return mapScale;
    }

    // Add a NPC to the list
    public void AddNpc(NpcType npcType, GuardBehavior? guardPlanner, IntruderPlanner? intruderPlanner,
        PathFindingHeursitic pathFindingHeuristic, PathFollowing pathFollowing, NpcLocation? npcLocation)
    {
        npcsList.Add(new NpcData(npcType, guardPlanner, intruderPlanner, pathFindingHeuristic, pathFollowing,
            npcLocation));
    }


    // Add the NPC data
    public List<NpcData> GetNpcsData()
    {
        return npcsList;
    }
}