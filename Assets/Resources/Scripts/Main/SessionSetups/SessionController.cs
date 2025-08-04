using UnityEngine;

public class SessionController : MonoBehaviour
{
    private Session _session;

    [Header("Global Setting")] 
    public float gameDurationInSeconds;
    [Tooltip("The game mode\nCoin Collection: the intruder collect coins while staying out of sight.\nStealth: stay hidden as long as possible from the AI.")]
    public GameType gameType;
    [Tooltip("Scenario")] public Scenario scenario;
    [Tooltip("Map name")] public Map map;
    
    
    [Header("Guard Settings")]
    public int numberOfGuards;

    public float GuardFOV;

    public PatrolMethod patrolMethod;
    public SearchMethod searchMethod;


    [Header("Intruder Setting")] public IntruderMethod intruderMethod;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Session GetSession()
    {
        PatrolerParams patrolParams = null; 
        switch (patrolMethod)
        {
            case PatrolMethod.Grid:
                patrolParams =
                    new GridPatrolerParams(0.5f, 1f, 0.5f, 0.5f);
                break;
            case PatrolMethod.RoadMap:
                patrolParams = new RoadMapPatrolerParams(1f, 1f, 1f, 0.5f, RMDecision.DijkstraPath,
                    RMPassingGuardsSenstivity.Max,0f,0f,0f);
                break;
            case PatrolMethod.VisMesh:
                patrolParams = new VisMeshPatrolerParams(0.9f, 1f, 1f,
                    1f, 1f, VMDecision.Weighted);
                break;
                
            default:
                patrolParams = new RandomPatrolerParams();
                break;
        }

        SearcherParams searchParams = null;
        switch (searchMethod)
        {
            case SearchMethod.Grid:
                searchParams =
                    new GridSearcherParams(0.5f, ProbabilityFlowMethod.Propagation, 1f, 0.5f, 0.5f);
                break;
            
            case SearchMethod.RoadMap:
                searchParams = new RoadMapSearcherParams(0.5f, 1f, 1f, 0.5f, RMDecision.DijkstraPath,
                    RMPassingGuardsSenstivity.Max, 0f, 0f, 0f, ProbabilityFlowMethod.Propagation);
                break;
            
            case SearchMethod.Cheating:
                searchParams = new CheatingSearcherParams();
                break;
            
            default:
                searchParams = new RandomSearcherParams();
                break;
            
        }
        
        GuardBehaviorParams guardBehaviorParameters = new GuardBehaviorParams(patrolParams,searchParams,null);
        
        ScouterParams scoutParams = null;


        switch (intruderMethod)
        {
            case IntruderMethod.RoadMap:
                RoadMapScouterWeights safeWeights = new RoadMapScouterWeights(1f, 1f, 1f, 0f, 0f);

                RoadMapScouterWeights unsafeWeights = new RoadMapScouterWeights(0f, 1f, 0f, 1f, 0f);

                scoutParams = new RoadMapScouterParams(SpotsNeighbourhoods.LineOfSight, PathCanceller.DistanceCalculation,
                    RiskThresholdType.Fixed, TrajectoryType.Simple, 0.5f, GoalPriority.Weighted,
                    safeWeights,
                    SafetyPriority.Weighted,
                    unsafeWeights,
                    0.25f);
                break;
            
        }
        
        IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(scoutParams, null, null);

        string mapName;

        switch (map)
        {
            case Map.Warehouse:
                mapName = "Boxes";
                break;
            case Map.AlienIsolation:
                mapName = "AlienIsolation";
                break;
            case Map.AmongUs:
                mapName = "amongUs";
                break;
            case Map.MgsDock:
                mapName = "MgsDock";
                break;
            case Map.ValorantAscent:
                mapName = "valorantAscent";
                break;
            
            default:
                mapName = "amongUs";
                break;
        }
        
    
        _session = new Session(gameDurationInSeconds, "Test", gameType, scenario, "red", GuardSpawnType.Random,numberOfGuards, GuardFOV, guardBehaviorParameters, 1, 0.1f,intruderBehaviorParams, new MapData(mapName), SpeechType.None);
        
        // Add guards
        for (int i = 0; i < _session.guardsCount; i++)
            _session.AddNpc(i + 1, NpcType.Guard, null);

        // Add intruders
        for (int i = 0; i < _session.intruderCount; i++)
            _session.AddNpc(i + 1, NpcType.Intruder, null);

        
        return _session;
    }

    
}

public enum PatrolMethod
{Grid, RoadMap, VisMesh, Random}

public enum SearchMethod
{Grid, RoadMap, Cheating, Random}

public enum IntruderMethod
{
    Player,
    RoadMap
}

public enum Map
{
    AlienIsolation,
    AmongUs,
    ValorantAscent,
    MgsDock,
    Warehouse
}