using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolSessionsAssessment
{
    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(2);


        MapData mapData;


        // mapData = new MapData("amongUs", 0.5f);
        mapData = new MapData("bloodstainedAngle1", 0.5f);
        // AddVisMeshSession(ref sessions, mapData, guardTeams);
        AddRoadMapSession(ref sessions, mapData, guardTeams);

        return sessions;
    }

    private static void AddVisMeshSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        // Guard Patrol Behavior
        PatrolPlanner patrolPlanner = PatrolPlanner.gRoadMap;


        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        List<float> maxSeenRegionPortions = new List<float>()
        {
            0.5f,
            // 0.7f,
            // 1f
        };

        float max = 10f;
        float min = 0f;
        float increment = 11f;

        List<float> areaWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            areaWeights.Add(i / max);
        }

        List<float> stalenessWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            stalenessWeights.Add(i / max);
        }

        List<float> distanceWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            distanceWeights.Add(i / max);
        }

        List<float> separationWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            separationWeights.Add(i / max);
        }

        List<VMDecision> decisionTypes = new List<VMDecision>()
        {
            VMDecision.Weighted
        };

        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var areaWeight in areaWeights)
        foreach (var stalenessWeight in stalenessWeights)
        foreach (var distanceWeight in distanceWeights)
        foreach (var separationWeight in separationWeights)
        foreach (var decisionType in decisionTypes)
        foreach (var maxSeenRegionPortion in maxSeenRegionPortions)
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = new VisMeshPatrolerParams(maxSeenRegionPortion, areaWeight, stalenessWeight,
                distanceWeight, separationWeight, decisionType);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolPlanner, patrolParams);


            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue",
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(patrolPlanner, AlertPlanner.Simple,
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


    private static void AddRoadMapSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        PatrolPlanner patrolPlanner = PatrolPlanner.gRoadMap;

        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };


        float max = 10f;
        float min = 0f;
        float increment = 11f;

        List<float> maxNormalizedPathLengths = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            maxNormalizedPathLengths.Add(i / max);
        }

        List<float> guardPassingWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            guardPassingWeights.Add(i / max);
        }

        List<float> stalenessWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            stalenessWeights.Add(i / max);
        }

        List<float> connectivityWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            connectivityWeights.Add(i / max);
        }

        List<float> separationWeights = new List<float>();
        for (float i = min; i <= max; i += increment)
        {
            separationWeights.Add(i / max);
        }

        List<RMDecision> decisionTypes = new List<RMDecision>()
        {
            RMDecision.DijkstraPath,
            RMDecision.EndPoint
        };

        List<RMPassingGuardsSenstivity> passingGuardsSenstivities = new List<RMPassingGuardsSenstivity>()
        {
            RMPassingGuardsSenstivity.Actual,
            RMPassingGuardsSenstivity.Max
        };


        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var maxNormalizedPathLength in maxNormalizedPathLengths)
        foreach (var guardPassingWeight in guardPassingWeights)
        foreach (var stalenessWeight in stalenessWeights)
        foreach (var connectivityWeight in connectivityWeights)
        foreach (var passingGuardsSenstivity in passingGuardsSenstivities)
        foreach (var decisionType in decisionTypes)
        foreach (var guardTeam in guardTeams)
        {
            PatrolerParams patrolParams = new RoadMapPatrolerParams(maxNormalizedPathLength, stalenessWeight,
                guardPassingWeight, connectivityWeight, decisionType, passingGuardsSenstivity);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolPlanner, patrolParams);


            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue",
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(patrolPlanner, AlertPlanner.Simple, SearchPlanner.Cheating,
                    PlanOutput.DijkstraPath);

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
}