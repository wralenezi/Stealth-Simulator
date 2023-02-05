using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CompareSearchMethods
{
    private static int _episodeLength = 30;
    private static int _episodeCount = 1;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(4);

        List<MapData> maps = new List<MapData>();
        maps.Add(new MapData("amongUs"));

        // Add the search methods
        List<SearcherParams> searcherMethods = new List<SearcherParams>();

        SearcherParams searcherMethod =
            new GridSearcherParams(0.5f, ProbabilityFlowMethod.Diffuse, 1f, 1f, 1f);
        // searcherMethods.Add(searcherMethod);
        //
        // searcherMethod =
        //     new GridSearcherParams(0.5f, ProbabilityFlowMethod.Propagation, 1f, 1f, 1f);
        // searcherMethods.Add(searcherMethod);
        //
        // searcherMethod = new RoadMapSearcherParams(1f, 1f, 1f, 1f, RMDecision.DijkstraPath,
        //     RMPassingGuardsSenstivity.Max, 0f, 0f, 0f, ProbabilityFlowMethod.Diffuse);
        // searcherMethods.Add(searcherMethod);
        //
        // searcherMethod = new RoadMapSearcherParams(1f, 1f, 1f, 1f, RMDecision.DijkstraPath,
        //     RMPassingGuardsSenstivity.Max, 0f, 0f, 0f, ProbabilityFlowMethod.Propagation);
        // searcherMethods.Add(searcherMethod);

        searcherMethod = new RandomSearcherParams();
        searcherMethods.Add(searcherMethod);

        searcherMethod = new CheatingSearcherParams();
        searcherMethods.Add(searcherMethod);

        // Add the hiding methods
        List<SearchEvaderParams> searchEvaders = new List<SearchEvaderParams>();

        SearchEvaderParams searchEvader = new SimpleSearchEvaderParams();
        searchEvaders.Add(searchEvader);
        
        AddPatrolSessions("", ref sessions, maps, searcherMethods, searchEvaders, "blue", guardTeams);

        return sessions;
    }

    private static void AddPatrolSessions(string gameCode, ref List<Session> sessions, List<MapData> maps,
        List<SearcherParams> searchMethods, List<SearchEvaderParams> searchEvaders,
        string teamColor, List<int> guardTeams)
    {
        foreach (var map in maps)
        foreach (var guardTeam in guardTeams)
        foreach (var searchMethod in searchMethods)
        foreach (var searchEvader in searchEvaders)
        {
            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(null,
                searchMethod, null);

            ChaseEvaderParams chaseEvaderParams = new SimpleChaseEvaderParams();

            IntruderBehaviorParams intruderBehaviorParams =
                new IntruderBehaviorParams(null, searchEvader, chaseEvaderParams);

            Session session = new Session(_episodeLength, gameCode, GameType.CoinCollection, Scenario.Chase,
                teamColor,
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                map, SpeechType.Simple, SurveyType.EndEpisode);

            session.SetGameCondition(Mathf.NegativeInfinity, Mathf.Infinity);
            session.sessionVariable = "VisMesh";
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