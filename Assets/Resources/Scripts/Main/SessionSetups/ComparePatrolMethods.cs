using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ComparePatrolMethods
{
    private static int _episodeLength = 120;
    private static int _episodeCount = 1;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(3);
        guardTeams.Add(4);
        guardTeams.Add(5);
        guardTeams.Add(6);
        
        List<MapData> maps = new List<MapData>();
        maps.Add(new MapData("amongUs"));
        maps.Add(new MapData("valorantAscent"));
        maps.Add(new MapData("amongUs"));
        maps.Add(new MapData("MgsDock"));
        maps.Add(new MapData("bloodstainedAngle"));
        maps.Add(new MapData("Arkham"));
        maps.Add(new MapData("Boxes"));
        maps.Add(new MapData("AlienIsolation"));
        

        List<PatrolerParams> patrolerMethods = new List<PatrolerParams>();
        PatrolerParams patrolParams = null;
        
        patrolParams =
            new GridPatrolerParams(0.5f, 1f, 0.5f, 0.5f);
        patrolerMethods.Add(patrolParams);
        
        patrolParams = new RoadMapPatrolerParams(1f, 1f, 1f, 0.5f, RMDecision.DijkstraPath,
            RMPassingGuardsSenstivity.Max,0f,0f,0f);
        patrolerMethods.Add(patrolParams);


        patrolParams = new VisMeshPatrolerParams(0.9f, 1f, 1f,
            1f, 1f, VMDecision.Weighted);
        patrolerMethods.Add(patrolParams);
        
        patrolParams = new RandomPatrolerParams();
        patrolerMethods.Add(patrolParams);

        AddPatrolSessions("", ref sessions, maps, patrolerMethods, "blue", guardTeams);

        return sessions;
    }

    private static void AddPatrolSessions(string gameCode, ref List<Session> sessions, List<MapData> maps,
        List<PatrolerParams> patrolMethods,
        string teamColor, List<int> guardTeams)
    {
        foreach (var map in maps)
        foreach (var guardTeam in guardTeams)
        foreach (var patrolMethod in patrolMethods)
        {
            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolMethod,
                null, null);

            Session session = new Session(_episodeLength, gameCode, GameType.CoinCollection, Scenario.Stealth,
                teamColor,
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 0,
                null,
                map, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "VisMesh";
            session.coinCount = 0;

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                session.AddNpc(i + 1, NpcType.Guard, null);
            }

            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }

}