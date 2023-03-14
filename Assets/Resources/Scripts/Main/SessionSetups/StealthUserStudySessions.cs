using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StealthUserStudySessions
{
    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(4);


        MapData mapData;
        mapData = new MapData("amongUs", 0.5f);

        AddDynamicSession(ref sessions, mapData, guardTeams);

        return sessions;
    }

    private static void AddDynamicSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        List<SpotsNeighbourhoods> neighbourhoods = new List<SpotsNeighbourhoods>()
        {
            SpotsNeighbourhoods.LineOfSight,
            // SpotsNeighbourhoods.Grid
        };

        List<PathCanceller> pathCancellers = new List<PathCanceller>()
        {
            PathCanceller.DistanceCalculation,
            // PathCanceller.RiskComparison
        };

        List<RiskThresholdType> riskThresholdTypes = new List<RiskThresholdType>()
        {
            RiskThresholdType.Danger,
            // RiskThresholdType.Binary,
            // RiskThresholdType.Attempts
        };

        List<TrajectoryType> trajectoryTypes = new List<TrajectoryType>()
        {
            TrajectoryType.Simple,
            // TrajectoryType.AngleBased
        };

        List<GoalPriority> goalPriorities = new List<GoalPriority>()
        {
            GoalPriority.Safety
        };

        List<SafetyPriority> safetyPriorities = new List<SafetyPriority>()
        {
            // SafetyPriority.Occlusion,
            // SafetyPriority.GuardProximity,
            SafetyPriority.Weighted,
            // SafetyPriority.Random
        };


        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        List<float> projectionDistances = new List<float>()
        {
            // 0.75f,
            1f,
            // 1.25f,
            // 1.5f
        };

        List<float> maxRiskAsSafeValues = new List<float>()
        {
            0f,
            // 0.5f,
            // 0.9f
        };


        foreach (var guardTeam in guardTeams)
        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var projectionDistance in projectionDistances)
        {
            RoadMapScouterParams rmScouterParams = new RoadMapScouterParams(SpotsNeighbourhoods.All, PathCanceller.None,
                RiskThresholdType.None, TrajectoryType.None, 0f, GoalPriority.Safety, null, SafetyPriority.Random, null,
                projectionDistance);

            IntruderBehaviorParams intruderBehavior = new IntruderBehaviorParams(rmScouterParams, null, null);

            Session session = new Session(120, "", GameType.CoinCollection, Scenario.Stealth, "blue", guardSpawnType,
                guardTeam, 0.1f, null, 1,
                0.1f, intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                session.AddNpc(i + 1, NpcType.Guard, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                session.AddNpc(i + 1, NpcType.Intruder, null);
            }

            sessions.Add(session);
        }
    }
}