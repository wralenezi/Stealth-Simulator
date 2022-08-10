using System.Collections.Generic;

public static class StealthStudySessions
{
    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(2);
        // guardTeams.Add(5);
        // guardTeams.Add(6);
        // guardTeams.Add(7);

        // MapData mapData = new MapData("BoxeSet02", 1f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        
        // MapData mapData = new MapData("BoxSet02", 1f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        //
        // MapData mapData = new MapData("BoxSet03", 1f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        //
        // MapData mapData = new MapData("BoxSet04", 1f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);

       
        MapData mapData;
        
        // mapData = new MapData("amongUsMod", 0.5f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        
        // mapData = new MapData("amongUs", 0.5f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        
        // mapData = new MapData("AlienIsolation", 3.5f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        
        // mapData = new MapData("AlienIsolationMod", 0.75f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        //
        // mapData = new MapData("Boxes", 1f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        
        // mapData = new MapData("valorantAscent", 1.5f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        //
        // mapData = new MapData("dragonAge2", 1f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        
        mapData = new MapData("MgsDock", 2f);
        AddDynamicSession(ref sessions, mapData, guardTeams);
        
        // guardTeams.Clear();
        // guardTeams.Add(2);
        
        // guardTeams.Add(3);
        // guardTeams.Add(4);
        // guardTeams.Add(5);
        //
        // mapData = new MapData("dragonAgeBrc202d", 1f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);
        //
        // mapData = new MapData("MgsDock", 2f);
        // AddDynamicSession(ref sessions, mapData, guardTeams);

        
        
        // Add Scripted scenarios
        
        // NpcLocation? intruderLocation = new NpcLocation(new Vector2(-15.69f, 5.91f), 0f);
        // List<NpcLocation> guardLocations = new List<NpcLocation>
        // {
        //     new NpcLocation(new Vector2(6.98f, 5.61f), 0f),
        // };
        //
        // guardTeams.Clear();
        // guardTeams.Add(1);

        
        // mapData = new MapData("Corridor", 0.75f);
        // AddScriptedSession(ref sessions, mapData, intruderLocation, guardLocations);

        
        // NpcLocation? intruderLocation = new NpcLocation(new Vector2(-13.25f, 4.4f), 0f);
        // List<NpcLocation> guardLocations = new List<NpcLocation>
        // {
        //     new NpcLocation(new Vector2(0.48f, 4.8f), 0f),
        //     new NpcLocation(new Vector2(-5.4f, -3.63f), 0f)
        // };
        //
        // mapData = new MapData("MgsDock", 2f);
        // AddScriptedSession(ref sessions, mapData, intruderLocation, guardLocations);


        return sessions;
    }

    private static void AddDynamicSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        List<PatrolPlanner> intruderMethods = new List<PatrolPlanner>()
        {
            PatrolPlanner.iSimple,
            PatrolPlanner.iPathFinding
        };

        List<PatrolPlanner> guardMethods = new List<PatrolPlanner>()
        {
            // PatrolPlanner.gRoadMap,
            PatrolPlanner.gRandom
        };

        
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
            SafetyPriority.WeightedSpot,
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
        

        foreach (var guardMethod in guardMethods)
        foreach (var guardTeam in guardTeams)
        foreach (var intruderMethod in intruderMethods)
        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var projectionDistance in projectionDistances)
        {
            // IntruderBehavior intruderBehavior = new IntruderBehavior
            // {
            //     spotsNeighbourhood = SpotsNeighbourhoods.All,
            //     pathCancel = PathCanceller.None,
            //     thresholdType = RiskThresholdType.None,
            //     trajectoryType = TrajectoryType.None,
            //     goalPriority = GoalPriority.None,
            //     safetyPriority = SafetyPriority.None,
            //     fovProjectionMultiplier = projectionDistance
            // };

            Session session = new Session(120, "", GameType.CoinCollection, Scenario.Stealth, "blue", guardSpawnType,
                guardTeam, null,1,
                null, //intruderBehavior,
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

            // sessions.Add(session);
        }

        foreach (var guardMethod in guardMethods)
        foreach (var pathCanceller in pathCancellers)
        foreach (var riskThresholdType in riskThresholdTypes)
        foreach (var trajectoryType in trajectoryTypes)
        foreach (var guardTeam in guardTeams)
        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var aGoalPriority in goalPriorities)
        foreach (var aSafetyPriority in safetyPriorities)
        foreach (var neighbour in neighbourhoods)
        foreach (var projectionDistance in projectionDistances)
        foreach (var maxRiskAsSafeValue in maxRiskAsSafeValues)
        {
            // IntruderBehavior intruderBehavior = new IntruderBehavior
            // {
            //     spotsNeighbourhood = neighbour, pathCancel = pathCanceller, thresholdType = riskThresholdType,
            //     trajectoryType = trajectoryType, goalPriority = aGoalPriority, safetyPriority = aSafetyPriority,
            //     fovProjectionMultiplier = projectionDistance, maxRiskAsSafe = maxRiskAsSafeValue
            // };

            Session session = new Session(120, "", GameType.CoinCollection, Scenario.Stealth, "blue", guardSpawnType,
                guardTeam, null,1,
                null, //intruderBehavior,
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
            // RiskThresholdType.Danger,
            RiskThresholdType.Binary,
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
            SafetyPriority.Occlusion,
            // SafetyPriority.GuardProximity,
            SafetyPriority.WeightedSpot,
            SafetyPriority.Random
        };


        List<float> projectionDistances = new List<float>()
        {
            // 0.75f,
            1f,
            // 1.25f,
            // 1.5f
        };


        foreach (var guardMethod in guardMethods)
        foreach (var intruderMethod in intruderMethods)
        {
            // IntruderBehavior intruderBehavior = new IntruderBehavior
            // {
            //     pathCancel = PathCanceller.None,
            //     thresholdType = RiskThresholdType.None,
            //     trajectoryType = TrajectoryType.None
            // };

            Session session = new Session(120, "", GameType.CoinCollection, Scenario.Stealth, "blue",
                GuardSpawnType.Scripted, guardLocations.Count, null,1,
                null, //intruderBehavior,
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

            // sessions.Add(session);
        }

        foreach (var guardMethod in guardMethods)
        foreach (var pathCanceller in pathCancellers)
        foreach (var riskThresholdType in riskThresholdTypes)
        foreach (var trajectoryType in trajectoryTypes)
        foreach (var aGoalPriority in goalPriorities)
        foreach (var aSafetyPriority in safetyPriorities)
        foreach (var neighbour in neighbourhoods)
        foreach (var projectionDistance in projectionDistances)
        {
            // IntruderBehavior intruderBehavior = new IntruderBehavior
            // {
            //     spotsNeighbourhood = neighbour, pathCancel = pathCanceller, thresholdType = riskThresholdType,
            //     trajectoryType = trajectoryType, goalPriority = aGoalPriority, safetyPriority = aSafetyPriority,
            //     fovProjectionMultiplier = projectionDistance
            // };

            Session session = new Session(120, "", GameType.CoinCollection, Scenario.Stealth, "blue",
                GuardSpawnType.Scripted, guardLocations.Count, null,1,
                null, //intruderBehavior,
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