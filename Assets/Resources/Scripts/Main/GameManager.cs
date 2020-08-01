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

    // Active areas
    private List<GameObject> m_activeAreas;

    // The Scenarios to be executed
    private List<Scenario> m_scenarios;

    // The path to the stealth area prefab
    private readonly string m_stealthArea = "Prefabs/StealthArea";

    [Header("Scenario Setup")] [Tooltip("If checked run the specified scenario")]
    public bool runTestScenario;

    [Tooltip("Rendering")] public bool Render;

    [Tooltip("Number of areas ran at the same time")]
    public int simultaneousAreasCount;

    [Tooltip("Scenario Setup")] public Scenario testScenario;

    private void Start()
    {
        m_activeAreas = new List<GameObject>();
        m_scenarios = new List<Scenario>();
    }


    private void LoadGridScenarios()
    {
        var coverageResetThresholds = new List<int> {25, 100};

        var planners = new List<NpcPlanner>
        {
            NpcPlanner.Stalest
        };

        var pathfinders = new List<PathFindingHeursitic> {PathFindingHeursitic.EuclideanDst};

        var maps = new List<Map> {Map.MgsDock, Map.Arkham};

        // Add scenarios to list
        foreach (var guardPlanner in planners)
        foreach (var coverageResetThreshold in coverageResetThresholds)
        foreach (var pathFinder in pathfinders)
        foreach (var map in maps)
        {
            var sc = new Scenario(guardPlanner.ToString(), coverageResetThreshold, WorldRepType.Grid, map);

            // Add a Guard
            sc.AddNpc("guard", NpcType.Guard, guardPlanner, pathFinder, PathFollowing.SimpleFunnel);

            // Add Scenario
            m_scenarios.Add(sc);
        }
    }

    // Create the area and load it with the scenario
    private GameObject CreateArea(Scenario scenario)
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

    private void FixedUpdate()
    {
        ReplenishScenarios();
    }
}


// Maps and their values are their default scale 
public enum Map
{
    MgsDock,
    AlienIsolation,
    Arkham,
    Test
}


// World Representation Type 
public enum WorldRepType
{
    VisMesh,
    Grid
}


// Guard decision maker
public enum NpcPlanner
{
    WeightedDistanceStaleness,
    Stalest,
    ClosestStale,
    WeightedStalest,
    Random,
    UserInput
}

// Heuristic for path finding 
public enum PathFindingHeursitic
{
    EuclideanDst,
    StalenessVerse
}

// Path following algorithm
public enum PathFollowing
{
    CentroidFollowing,
    SimpleFunnel
}

public enum NpcType
{
    Guard,
    Intruder
}

[Serializable]
public struct NpcData
{
    // The NPC name
    public string npcName;

    // The NPC type
    public NpcType npcType;

    // The Planner the guard uses to find its next move
    public NpcPlanner npcPlanner;

    // The A* search heuristic
    public PathFindingHeursitic npcHeuristic;

    // Navmesh following behavior
    public PathFollowing npcPathFollowing;

    public NpcData(string pNpcName, NpcType pNpcType, NpcPlanner pNpcPlanner,
        PathFindingHeursitic pPathFindingHeursitic, PathFollowing pNpcPathFollowing)
    {
        npcName = pNpcName;
        npcType = pNpcType;
        npcPlanner = pNpcPlanner;
        npcHeuristic = pPathFindingHeursitic;
        npcPathFollowing = pNpcPathFollowing;
    }

    public override string ToString()
    {
        var data = "";
        data += npcName + ",";
        data += npcPlanner + ",";
        data += npcHeuristic + ",";
        data += npcPathFollowing;

        return data;
    }
}

[Serializable]
public struct Scenario
{
    // Game Code is the scenario for the NPC
    public string gameCode;

    // World Representation
    public WorldRepType worldRepType;

    // The Threshold at which the covered region is reset 
    public int coveredReigonResetThreshold;

    // The map 
    public Map map;

    // The map Scale
    private int mapScale;

    // NPCs Data
    public List<NpcData> npcsList;


    public Scenario(string pGameCode, int pCoveredRegionResetThreshold, WorldRepType pWorldRepType, Map pMap,
        int pMapScale = 1)
    {
        gameCode = pGameCode;
        coveredReigonResetThreshold = pCoveredRegionResetThreshold;
        worldRepType = pWorldRepType;
        map = pMap;
        npcsList = new List<NpcData>();
        mapScale = pMapScale;
    }

    public float GetMapScale()
    {
        return Mathf.Max(Properties.GetDefaultMapScale(map), mapScale);
    }

    // Add a NPC to the list
    public void AddNpc(string guardName, NpcType npcType, NpcPlanner guardPlanner,
        PathFindingHeursitic pathFindingHeursitic, PathFollowing pathFollowing)
    {
        npcsList.Add(new NpcData(guardName, npcType, guardPlanner, pathFindingHeursitic, pathFollowing));
    }


    // Add the NPC data
    public List<NpcData> GetNpcsData()
    {
        return npcsList;
    }
}