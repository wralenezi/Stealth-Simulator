using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    // Logging Enabled 
    [Header("Logging")] [Tooltip("Log the performance")]
    public bool enableLogging;

    // Number of episode to run in each session    
    [Tooltip("Log the performance")] public int NumberOfEpisodesPerSession;

    [Header("Time")] [Tooltip("Simulation speed")] [Range(1, 100)]
    public int SimulationSpeed;

    // Active areas
    private List<GameObject> m_activeAreas;

    // The Scenarios to be executed
    private List<Session> m_scenarios;

    // The path to the stealth area prefab
    private readonly string m_stealthArea = "Prefabs/StealthArea";

    [Header("Scenario Setup")] [Tooltip("If checked run the specified scenario")]
    public bool runTestScenario;

    [Tooltip("Rendering")] public bool Render;

    [Tooltip("Number of areas ran at the same time")]
    public int simultaneousAreasCount = 1;

    [Tooltip("Scenario Setup")] public Session testScenario;

    private void Start()
    {
        m_activeAreas = new List<GameObject>();
        m_scenarios = new List<Session>();

        // Load sessions
        LoadUserControlledChaseSessions();


        Time.timeScale = SimulationSpeed;
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

        var sc = new Session("SimpleRandom", Scenario.Manual, 100, WorldRepType.VisMesh, "Boxes", 
            1f);
        
        // var sc = new Session("SimpleRandom", Scenario.Manual, 100, WorldRepType.VisMesh, "MgsDock",
        //     2f);
        
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
        var areaPrefab = (GameObject) Resources.Load(m_stealthArea);
        var areaGameObject = Instantiate(areaPrefab, transform, true);

        // Get the script
        var area = areaGameObject.GetComponent<StealthArea>();

        // Load the scenario
        area.InitiateArea(scenario);

        return areaGameObject;
    }

    // Replenish the scenarios
    private void ReplenishScenarios()
    {
        if (runTestScenario)
        {
            if (m_activeAreas.Count == 0)
            {
                m_activeAreas.Add(CreateArea(testScenario));
                Camera.main.orthographicSize = 5 * testScenario.GetMapScale();
            }
        }
        else
        {
            if (m_activeAreas.Count < simultaneousAreasCount)
            {
                if (m_scenarios.Count > 0)
                {
                    m_activeAreas.Add(CreateArea(m_scenarios[0]));
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
            else
            {
                // Debug.Log(m_activeAreas[0].GetComponent<StealthArea>().GetScenario().gameCode);
            }
        }
    }

    private void FixedUpdate()
    {
        ReplenishScenarios();
    }


    public void RemoveArea(GameObject area)
    {
        m_activeAreas.Remove(area);
        Destroy(area);
    }
}


// Maps and their values are their default scale 
public enum Map
{
    MgsDock,
    AlienIsolation,
    Arkham
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


    public Session(string pGameCode, Scenario pScenario, int pCoveredRegionResetThreshold, WorldRepType pWorldRepType,
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