using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AdHocMethods
{
    private static int _episodeLength = 220;
    private static int _episodeCount = 2;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(3);

        List<MapData> maps = new List<MapData>();
        // maps.Add(new MapData("amongUs"));
        // maps.Add(new MapData("MgsDock"));
        // maps.Add(new MapData("dragonAgeBrc202d"));
        // maps.Add(new MapData("Boxes"));
        // maps.Add(new MapData("CoD"));
        maps.Add(new MapData("dragon_age2"));
        // maps.Add(new MapData("bloodstainedAngle"));

        List<PatrolerParams> patrolerMethods = new List<PatrolerParams>();
        PatrolerParams patrolParams;

        patrolParams = new VisMeshPatrolerParams(0.5f, 1f, 1f,
            1f, 1f, VMDecision.Weighted);
        // patrolerMethods.Add(patrolParams);
        //
        // patrolParams =
        //     new GridPatrolerParams(0.5f, 1f, 1f, 1f);
        // patrolerMethods.Add(patrolParams);
        //
        // patrolParams = new RandomPatrolerParams();
        // patrolerMethods.Add(patrolParams);

        patrolParams = new RoadMapPatrolerParams(1f, 1f, 1f, 1f, RMDecision.DijkstraPath,
            RMPassingGuardsSenstivity.Max, 0f, 0f, 0f);
        patrolerMethods.Add(patrolParams);

        // Add the search methods
        List<SearcherParams> searcherMethods = new List<SearcherParams>();
        SearcherParams searcherMethod = null;

        // Road Map Searchers
        searcherMethod = new RoadMapSearcherParams(1f, 1f, 1f, 1f, RMDecision.DijkstraPath,
            RMPassingGuardsSenstivity.Max, 0f, 0f, 0f, ProbabilityFlowMethod.Propagation);
        searcherMethods.Add(searcherMethod);

        
        searcherMethod =
            new GridSearcherParams(0.5f, ProbabilityFlowMethod.Diffuse, 1f, 1f, 1f);
        searcherMethods.Add(searcherMethod);
        

        // Add scouter methods
        List<ScouterParams> scouterMethods = new List<ScouterParams>();
        ScouterParams scouterMethod;

        scouterMethod = new RoadMapScouterParams(SpotsNeighbourhoods.LineOfSight,
            PathCanceller.DistanceCalculation,
            RiskThresholdType.Fixed, TrajectoryType.Simple, 0.8f, GoalPriority.Safety, null, SafetyPriority.Weighted,null,
            0.75f);
        scouterMethods.Add(scouterMethod);

        scouterMethod = new GreedyToGoalScouterParams();
        scouterMethods.Add(scouterMethod);

        scouterMethod = new SimpleGreedyScouterParams();
        scouterMethods.Add(scouterMethod);

        // Add search evader
        List<SearchEvaderParams> searchEvaders = new List<SearchEvaderParams>();
        SearchEvaderParams searchEvader;

        searchEvader = new SimpleSearchEvaderParams(DestinationType.Random,0f,0f);
        searchEvaders.Add(searchEvader);


        AddPatrolSessions("", ref sessions, maps, patrolerMethods, scouterMethods, searcherMethods, searchEvaders, "blue", guardTeams);

        return sessions;
    }

    private static void AddPatrolSessions(string gameCode, ref List<Session> sessions, List<MapData> maps,
        List<PatrolerParams> patrolMethods, List<ScouterParams> scouterMethods, List<SearcherParams> searcherMethods, List<SearchEvaderParams> searchEvaders,
        string teamColor, List<int> guardTeams)
    {
        foreach (var map in maps)
        foreach (var guardTeam in guardTeams)
        foreach (var patrolMethod in patrolMethods)
        foreach (var scouterMethod in scouterMethods)
        foreach (var searcherMethod in searcherMethods)
        foreach (var searchEvader in searchEvaders)
        {
            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolMethod,
                searcherMethod, null);

            ChaseEvaderParams chaseEvaderParams = new SimpleChaseEvaderParams();

            IntruderBehaviorParams intruderBehaviorParams =
                new IntruderBehaviorParams(scouterMethod, searchEvader, chaseEvaderParams);

            Session session = new Session(_episodeLength, gameCode, GameType.CoinCollection, Scenario.Chase,
                teamColor,
                GuardSpawnType.Separate, guardTeam, 0.1f,guardBehaviorParams, 0,
                0.1f, intruderBehaviorParams,
                map, SpeechType.Simple, SurveyType.EndEpisode);

            session.SetGameCondition(Mathf.NegativeInfinity, Mathf.Infinity);

            session.sessionVariable = "";
            session.coinCount = 0;

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
                session.AddNpc(i + 1, NpcType.Guard, null);

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
                session.AddNpc(i + 1, NpcType.Intruder, null);


            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }
}