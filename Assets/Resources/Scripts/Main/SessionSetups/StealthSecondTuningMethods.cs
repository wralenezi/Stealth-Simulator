using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StealthSecondTuningMethods
{
    private static int _episodeLength = 120;
    private static int _episodeCount = 50;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(4);

        List<MapData> maps = new List<MapData>();
        maps.Add(new MapData("AlienIsolation"));
        maps.Add(new MapData("amongUs"));
        maps.Add(new MapData("valorantAscent"));
        maps.Add(new MapData("MgsDock"));
        maps.Add(new MapData("Arkham"));
        // maps.Add(new MapData("CoD"));
        maps.Add(new MapData("Boxes"));
        maps.Add(new MapData("dragonAge2"));

        List<PatrolerParams> patrolerMethods = new List<PatrolerParams>();

        PatrolerParams patrolParams = new VisMeshPatrolerParams(0.9f, 1f, 1f,
            1f, 1f, VMDecision.Weighted);
        patrolerMethods.Add(patrolParams);

        patrolParams = new RoadMapPatrolerParams(1f, 1f, 1f, 0.5f, RMDecision.DijkstraPath,
            RMPassingGuardsSenstivity.Max, 0f, 0f, 0f);
        patrolerMethods.Add(patrolParams);

        patrolParams = new RandomPatrolerParams();
        patrolerMethods.Add(patrolParams);

        // Add scouter methods
        List<ScouterParams> scouterMethods = new List<ScouterParams>();
        ScouterParams scouterMethod = null;

        List<string> scouterParams = new List<string>();
        scouterParams.Add("Danger_0.75_0_0_1_0_0_1_1_1_1_0_0.5");
        scouterParams.Add("Fixed_0.75_1_0_0_1_1_0_1_1_0_0_0.5");
        scouterParams.Add("Fixed_0.25_1_0_1_0_0_0_0_0_1_1_0");
        scouterParams.Add("Fixed_0.75_1_0_0_0_1_1_0_1_1_0_0.5");
        scouterParams.Add("Fixed_0.75_1_0_1_0_0_1_0_1_1_0_0.5");
        scouterParams.Add("Danger_0.75_1_0_1_0_0_0_0_1_1_0_0.5");
        scouterParams.Add("Fixed_0.25_1_1_1_0_0_0_1_0_1_0_0.5");
        scouterParams.Add("Danger_0.75_1_0_0_0_0_0_0_1_1_0_0.5");
        scouterParams.Add("Fixed_0.25_1_1_1_0_0_0_1_0_1_0_0.5");
        scouterParams.Add("Fixed_0.25_1_1_1_0_0_0_1_0_1_0_0.5");


        foreach (var scouterParam in scouterParams)
        {
            string[] tokens = scouterParam.Split('_');

            Enum.TryParse(tokens[0], true, out RiskThresholdType parsedEnumValue);
            
            RoadMapScouterWeights safeWeights =
                new RoadMapScouterWeights(float.Parse(tokens[2]), float.Parse(tokens[3]), float.Parse(tokens[4]),
                    float.Parse(tokens[5]), float.Parse(tokens[6]));

            RoadMapScouterWeights unsafeWeights = new RoadMapScouterWeights(float.Parse(tokens[7]),
                float.Parse(tokens[8]), float.Parse(tokens[9]), float.Parse(tokens[10]), float.Parse(tokens[11]));

            scouterMethod = new RoadMapScouterParams(SpotsNeighbourhoods.LineOfSight, PathCanceller.DistanceCalculation,
                parsedEnumValue, TrajectoryType.Simple, float.Parse(tokens[12]), GoalPriority.Weighted, safeWeights,
                SafetyPriority.Weighted,
                unsafeWeights,
                float.Parse(tokens[1]));

            scouterMethods.Add(scouterMethod);
        }


        AddPatrolSessions("", ref sessions, maps, patrolerMethods, scouterMethods, "blue", guardTeams);

        return sessions;
    }

    private static void AddPatrolSessions(string gameCode, ref List<Session> sessions, List<MapData> maps,
        List<PatrolerParams> patrolMethods, List<ScouterParams> scouterMethods,
        string teamColor, List<int> guardTeams)
    {
        foreach (var map in maps)
        foreach (var guardTeam in guardTeams)
        foreach (var patrolMethod in patrolMethods)
        foreach (var scouterMethod in scouterMethods)
        {
            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolMethod,
                null, null);

            IntruderBehaviorParams intruderBehaviorParams =
                new IntruderBehaviorParams(scouterMethod, null, null);

            Session session = new Session(_episodeLength, gameCode, GameType.CoinCollection, Scenario.Stealth,
                teamColor,
                GuardSpawnType.Separate, guardTeam, 0.1f, guardBehaviorParams, 1,
                0f, intruderBehaviorParams,
                map, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "VisMesh";
            session.coinCount = 1;

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