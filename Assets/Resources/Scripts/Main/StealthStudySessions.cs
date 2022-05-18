using System.Collections.Generic;
using UnityEngine;

public static class StealthStudySessions
{
    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        MapData mapData = new MapData("amongUs", 0.5f);
        AddDynamicSession(ref sessions, mapData);
        
        mapData = new MapData("Boxes", 1f);
        AddDynamicSession(ref sessions, mapData);
        
        mapData = new MapData("MgsDock", 2f);
        AddDynamicSession(ref sessions, mapData);
        
        mapData = new MapData("Alien_isolation_mod", 0.75f);
        AddDynamicSession(ref sessions, mapData);
        
        mapData = new MapData("CoD_relative", 0.1f);
        AddDynamicSession(ref sessions, mapData);

        mapData = new MapData("valorant_ascent", 1.5f);
        AddDynamicSession(ref sessions, mapData);

        // Add Scripted scenarios
        NpcLocation? intruderLocation = new NpcLocation(new Vector2(-13.25f, 4.4f), 0f);
        List<NpcLocation> guardLocations = new List<NpcLocation>
        {
            new NpcLocation(new Vector2(0.48f, 4.8f), 0f),
            new NpcLocation(new Vector2(-5.4f, -3.63f), 0f)
        };

        mapData = new MapData("MgsDock", 2f);
        AddScriptedSession(ref sessions, mapData, intruderLocation, guardLocations);
        
        
        return sessions;
    }

    private static void AddDynamicSession(ref List<Session> sessions, MapData mapData)
    {
        List<PatrolPlanner> intruderMethods = new List<PatrolPlanner>()
        {
            PatrolPlanner.iSimple,
            PatrolPlanner.iPathFinding
        };

        List<PatrolPlanner> guardMethods = new List<PatrolPlanner>()
        {
            PatrolPlanner.gRoadMap,
            PatrolPlanner.gRandom
        };

        List<PathCanceller> pathCancellers = new List<PathCanceller>()
        {
            PathCanceller.DistanceCalculation,
            PathCanceller.RiskComparison
        };

        List<RiskThresholdType> riskThresholdTypes = new List<RiskThresholdType>()
        {
            RiskThresholdType.Danger,
            RiskThresholdType.Binary,
            RiskThresholdType.Attempts
        };

        List<TrajectoryType> trajectoryTypes = new List<TrajectoryType>()
        {
            TrajectoryType.Simple,
            // TrajectoryType.AngleBased
        };

        List<int> guardTeams = new List<int>()
        {
            6, 5, 4, 3, 2, 1
        };

        foreach (var guardMethod in guardMethods)
        foreach (var guardTeam in guardTeams)
        foreach (var intruderMethod in intruderMethods)
        {
            IntruderBehavior intruderBehavior = new IntruderBehavior
            {
                pathCancel = PathCanceller.None,
                thresholdType = RiskThresholdType.None,
                trajectoryType = TrajectoryType.None
            };

            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue", guardTeam, 1,
                intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(guardMethod, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(intruderMethod, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            sessions.Add(session);
        }

        foreach (var guardMethod in guardMethods)
        foreach (var pathCanceller in pathCancellers)
        foreach (var riskThresholdType in riskThresholdTypes)
        foreach (var trajectoryType in trajectoryTypes)
        foreach (var guardTeam in guardTeams)
        {
            IntruderBehavior intruderBehavior = new IntruderBehavior
            {
                pathCancel = pathCanceller, thresholdType = riskThresholdType, trajectoryType = trajectoryType
            };

            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue", guardTeam, 1,
                intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(guardMethod, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.iRoadMap, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            sessions.Add(session);
        }
    }

    private static void AddScriptedSession(ref List<Session> sessions, MapData mapData, NpcLocation? intruderLocation,
        List<NpcLocation> guardLocations)
    {
        List<PatrolPlanner> intruderMethods = new List<PatrolPlanner>()
        {
            //PatrolPlanner.iSimple,
            PatrolPlanner.iPathFinding
        };

        List<PatrolPlanner> guardMethods = new List<PatrolPlanner>()
        {
            PatrolPlanner.gScripted
        };

        List<PathCanceller> pathCancellers = new List<PathCanceller>()
        {
            PathCanceller.DistanceCalculation,
            PathCanceller.RiskComparison
        };

        List<RiskThresholdType> riskThresholdTypes = new List<RiskThresholdType>()
        {
            RiskThresholdType.Danger,
            RiskThresholdType.Binary,
            RiskThresholdType.Attempts
        };

        List<TrajectoryType> trajectoryTypes = new List<TrajectoryType>()
        {
            TrajectoryType.Simple,
            // TrajectoryType.AngleBased
        };


        foreach (var guardMethod in guardMethods)
        foreach (var intruderMethod in intruderMethods)
        {
            IntruderBehavior intruderBehavior = new IntruderBehavior
            {
                pathCancel = PathCanceller.None,
                thresholdType = RiskThresholdType.None,
                trajectoryType = TrajectoryType.None
            };

            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue", guardLocations.Count, 1,
                intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(guardMethod, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, guardLocations[i]);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(intruderMethod, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, intruderLocation);
            }

            sessions.Add(session);
        }

        foreach (var guardMethod in guardMethods)
        foreach (var pathCanceller in pathCancellers)
        foreach (var riskThresholdType in riskThresholdTypes)
        foreach (var trajectoryType in trajectoryTypes)
        {
            IntruderBehavior intruderBehavior = new IntruderBehavior
            {
                pathCancel = pathCanceller, thresholdType = riskThresholdType, trajectoryType = trajectoryType
            };

            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue", guardLocations.Count, 1,
                intruderBehavior,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(guardMethod, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, guardLocations[i]);
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