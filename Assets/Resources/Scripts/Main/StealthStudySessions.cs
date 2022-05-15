using System.Collections.Generic;
using UnityEngine;

public static class StealthStudySessions
{
    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();
        MapData mapData = new MapData("amongUs", 0.5f);
        NpcLocation? intruderLocation = null; // new NpcLocation(new Vector2(-13.25f, 4.4f), 0f);
        AddSession(ref sessions, mapData, intruderLocation);
        return sessions;
    }

    private static void AddSession(ref List<Session> sessions, MapData mapData, NpcLocation? intruderLocation)
    {
        List<SearchPlanner> guardMethods = new List<SearchPlanner>()
        {
            SearchPlanner.RmPropSimple
            // ,SearchPlanner.Random
        };

        List<PathCanceller> pathCancellers = new List<PathCanceller>()
        {
            PathCanceller.DistanceCalculation,
            // PathCanceller.RiskComparison
        };

        List<RiskThresholdType> riskThresholdTypes = new List<RiskThresholdType>()
        {
            // RiskThresholdType.Danger,
            RiskThresholdType.Binary,
            // RiskThresholdType.Attempts
        };
        
        List<TrajectoryType> trajectoryTypes = new List<TrajectoryType>()
        {
            TrajectoryType.Simple,
            // TrajectoryType.AngleBased
        };

        foreach (var guardMethod in guardMethods)
        foreach (var pathCanceller in pathCancellers)
        foreach (var riskThresholdType in riskThresholdTypes)
        foreach (var trajectoryType in trajectoryTypes)    
        {
            IntruderBehavior intruderBehavior = new IntruderBehavior();
            intruderBehavior.pathCancel = pathCanceller;
            intruderBehavior.thresholdType = riskThresholdType;
            intruderBehavior.trajectoryType = trajectoryType;

            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue", 4, 1,
                intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gRoadMap, AlertPlanner.Simple,
                    guardMethod, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.iRoadMap, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, intruderLocation);
            }

            sessions.Add(session);
        }
    }
}