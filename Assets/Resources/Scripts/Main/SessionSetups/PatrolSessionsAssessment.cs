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
        AddVisMeshSession(ref sessions, mapData, guardTeams);

        return sessions;
    }

    private static void AddVisMeshSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        // Guard Patrol Behavior
        List<PatrolPlanner> guardMethods = new List<PatrolPlanner>()
        {
            // PatrolPlanner.gRoadMap,
            PatrolPlanner.gVisMesh,
            // PatrolPlanner.gRandom
        };


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
        
        List<float> areaWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            areaWeights.Add(1 / i);
        }

        List<float> stalenessWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            stalenessWeights.Add(1 / i);
        }
        
        List<float> distanceWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            distanceWeights.Add(1 / i);
        }

        List<float> separationWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            separationWeights.Add(1 / i);
        }
        
        List<VMDecision> decisionTypes = new List<VMDecision>()
        {
            VMDecision.Weighted
        };

        foreach (var guardMethod in guardMethods)
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
            PatrolerParams patrolParams;
            switch (guardMethod)
            {
                case PatrolPlanner.gVisMesh:
                    patrolParams = new VisMeshPatrolerParams(maxSeenRegionPortion, areaWeight, stalenessWeight,
                        distanceWeight, separationWeight,decisionType);
                    break;
                
                
                default:
                    patrolParams = null;
                    break;
            }
            
            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolParams);


            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue",
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
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
    
    
    
        private static void AddRoadMapSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        // Guard Patrol Behavior
        List<PatrolPlanner> guardMethods = new List<PatrolPlanner>()
        {
            PatrolPlanner.gRoadMap,
            // PatrolPlanner.gVisMesh,
            // PatrolPlanner.gRandom
        };


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
        
        List<float> areaWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            areaWeights.Add(1 / i);
        }

        List<float> stalenessWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            stalenessWeights.Add(1 / i);
        }
        
        List<float> distanceWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            distanceWeights.Add(1 / i);
        }

        List<float> separationWeights = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            separationWeights.Add(1 / i);
        }
        
        List<VMDecision> decisionTypes = new List<VMDecision>()
        {
            VMDecision.Weighted
        };

        foreach (var guardMethod in guardMethods)
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
            PatrolerParams patrolParams;
            switch (guardMethod)
            {
                case PatrolPlanner.gVisMesh:
                    patrolParams = new VisMeshPatrolerParams(maxSeenRegionPortion, areaWeight, stalenessWeight,
                        distanceWeight, separationWeight,decisionType);
                    break;
                
                
                default:
                    patrolParams = null;
                    break;
            }
            
            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolParams);


            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, "blue",
                guardSpawnType, guardTeam, guardBehaviorParams, 0,
                null,
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

}