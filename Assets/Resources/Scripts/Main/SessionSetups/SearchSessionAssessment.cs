using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SearchSessionAssessment
{
    private static int _episodeLength = 100;
    private static int _episodeCount = 1;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(4);

        MapData mapData;
        mapData = new MapData("amongUs");
        // mapData = new MapData("MgsDock", 2f);
        // mapData = new MapData("bloodstainedAngle1", 0.5f);

        // AddRoadMapSession("", ref sessions, mapData, "blue", guardTeams, _episodeLength);
        // AddRandomSession("", ref sessions, mapData, "blue", guardTeams, _episodeLength);
        AddGridSession("", ref sessions, mapData, "blue", guardTeams, _episodeLength);
        return sessions;
    }


    private static void AddRoadMapSession(string gameCode, ref List<Session> sessions, MapData mapData,
        string teamColor, List<int> guardTeams, float episodeLength)
    {
        PatrolPlanner patrolPlanner = PatrolPlanner.gRoadMap;

        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        float max = 10f;
        float min = 5f;
        float increment = 5f;

        List<float> maxNormalizedPathLengths = new List<float>()
        {
            0.5f,
            1f
        };

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
            // RMDecision.EndPoint
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
            SearcherParams searchParams = new RoadMapSearcherParams(maxNormalizedPathLength, stalenessWeight,
                guardPassingWeight, connectivityWeight, decisionType, passingGuardsSenstivity,0f,0f,0f,0f);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRandom,null ,
                SearchPlanner.RmPropSimple, searchParams, AlertPlanner.None, null);
            
            ScouterParams scouterParams = new RMScouterParams(SpotsNeighbourhoods.LineOfSight, PathCanceller.DistanceCalculation, RiskThresholdType.Danger
                , TrajectoryType.Simple , 0f, GoalPriority.Safety, SafetyPriority.ClosestWeightedSpot, 1f);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.iRoadMap, scouterParams, SearchPlanner.iHeuristic, null, AlertPlanner.iHeuristic, null);

            Session session = new Session(episodeLength, "", GameType.CoinCollection, Scenario.Chase, teamColor,
                guardSpawnType, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "RoadMap";

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
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }
            
            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.iSimple, AlertPlanner.iHeuristic,
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }




            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }

    private static void AddGridSession(string gameCode, ref List<Session> sessions, MapData mapData,
        string guardColor,
        List<int> guardTeams, float episodeLength)
    {
        PatrolPlanner patrolPlanner = PatrolPlanner.gRandom;

        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        List<GridStalenessMethod> methods = new List<GridStalenessMethod>()
        {
            GridStalenessMethod.Propagation,
            // GridStalenessMethod.Diffuse
        };


        float max = 10f;
        float min = 0f;
        float increment = 5f;

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

        List<float> cellSizes = new List<float>()
        {
            0.5f,
            0.75f,
            1f
        };

        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var guardTeam in guardTeams)
        foreach (var cellSize in cellSizes)
        foreach (var stalenessWeight in stalenessWeights)
        foreach (var distanceWeight in distanceWeights)
        foreach (var separationWeight in separationWeights)
        foreach (var method in methods)
        {
            SearcherParams searcherParams = new GridSearcherParams(cellSize, method);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(patrolPlanner, null,
                SearchPlanner.gSimpleGrid, searcherParams, AlertPlanner.None, null);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.iSimple, null, SearchPlanner.iHeuristic, null, AlertPlanner.iHeuristic, null);

            Session session = new Session(episodeLength, gameCode, GameType.CoinCollection, Scenario.Chase,
                guardColor,
                guardSpawnType, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "Grid";

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(patrolPlanner, AlertPlanner.Simple,
                    SearchPlanner.gSimpleGrid, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }
            
            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.iSimple, AlertPlanner.iHeuristic,
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }


            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }

    private static void AddRandomSession(string gameCode, ref List<Session> sessions, MapData mapData,
        string guardColor,
        List<int> guardTeams, float episodeLength)
    {
        List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
        {
            // GuardSpawnType.Random,
            GuardSpawnType.Separate,
            // GuardSpawnType.Goal
        };

        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyper-parameters for the behavior
            PatrolerParams patrolParams = null;

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRandom, patrolParams,
                SearchPlanner.Random, null, AlertPlanner.None, null);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.iSimple, null, SearchPlanner.iHeuristic, null, AlertPlanner.iHeuristic, null);

            Session session = new Session(episodeLength, gameCode, GameType.CoinCollection, Scenario.Chase,
                guardColor,
                guardSpawnType, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            session.sessionVariable = "Random";

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gRandom, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }
            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.iSimple, AlertPlanner.iHeuristic,
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            session.MaxEpisodes = _episodeCount;

            sessions.Add(session);
        }
    }
}