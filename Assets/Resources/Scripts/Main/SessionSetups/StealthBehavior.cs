using System.Collections.Generic;

public static class StealthBehavior
{
    // public static List<Session> GetSessions()
    // {
    //     List<Session> sessions = new List<Session>();
    //
    //     List<int> guardTeams = new List<int>();
    //     guardTeams.Add(2);
    //
    //
    //     MapData mapData;
    //     
    //     mapData = new MapData("MgsDock", 2f);
    //
    //     // mapData = new MapData("amongUs", 0.5f);
    //     // mapData = new MapData("bloodstainedAngle1", 0.5f);
    //
    //     AddDynamicSession(ref sessions, mapData, guardTeams);
    //
    //     return sessions;
    // }
    //
    // private static void AddDynamicSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    // {
    //     // Guard Patrol Behavior
    //
    //     List<PatrolPlanner> guardMethods = new List<PatrolPlanner>()
    //     {
    //         // PatrolPlanner.gRoadMap,
    //         // PatrolPlanner.gVisMesh,
    //         PatrolPlanner.gRandom
    //     };
    //
    //
    //     List<SpotsNeighbourhoods> neighbourhoods = new List<SpotsNeighbourhoods>()
    //     {
    //         SpotsNeighbourhoods.LineOfSight,
    //         // SpotsNeighbourhoods.Grid
    //     };
    //
    //     List<PathCanceller> pathCancellers = new List<PathCanceller>()
    //     {
    //         PathCanceller.DistanceCalculation,
    //         // PathCanceller.RiskComparison
    //     };
    //
    //     List<RiskThresholdType> riskThresholdTypes = new List<RiskThresholdType>()
    //     {
    //         RiskThresholdType.Danger,
    //         // RiskThresholdType.Binary,
    //         // RiskThresholdType.Attempts
    //     };
    //
    //     List<TrajectoryType> trajectoryTypes = new List<TrajectoryType>()
    //     {
    //         TrajectoryType.Simple,
    //         // TrajectoryType.AngleBased
    //     };
    //
    //     List<GoalPriority> goalPriorities = new List<GoalPriority>()
    //     {
    //         GoalPriority.Safety
    //     };
    //
    //     List<SafetyPriority> safetyPriorities = new List<SafetyPriority>()
    //     {
    //         // SafetyPriority.Occlusion,
    //         // SafetyPriority.GuardProximity,
    //         // SafetyPriority.WeightedSpot,
    //         SafetyPriority.ClosestWeightedSpot,
    //         // SafetyPriority.Random
    //     };
    //
    //
    //     List<GuardSpawnType> guardSpawnTypes = new List<GuardSpawnType>()
    //     {
    //         // GuardSpawnType.Random,
    //         GuardSpawnType.Separate,
    //         // GuardSpawnType.Goal
    //     };
    //
    //     List<float> projectionDistances = new List<float>()
    //     {
    //         // 0.75f,
    //         1f,
    //         // 1.25f,
    //         // 1.5f
    //     };
    //
    //     List<float> maxRiskAsSafeValues = new List<float>()
    //     {
    //         0f,
    //         // 0.5f,
    //         // 0.9f
    //     };
    //
    //
    //     foreach (var guardMethod in guardMethods)
    //     foreach (var pathCanceller in pathCancellers)
    //     foreach (var riskThresholdType in riskThresholdTypes)
    //     foreach (var trajectoryType in trajectoryTypes)
    //     foreach (var aGoalPriority in goalPriorities)
    //     foreach (var aSafetyPriority in safetyPriorities)
    //     foreach (var neighbour in neighbourhoods)
    //     foreach (var projectionDistance in projectionDistances)
    //     foreach (var guardSpawnType in guardSpawnTypes)
    //     foreach (var guardTeam in guardTeams)
    //     foreach (var maxRiskAsSafeValue in maxRiskAsSafeValues)
    //     {
    //         
    //         ScouterParams scouterParams = new RMScouterParams(neighbour, pathCanceller, riskThresholdType , trajectoryType , maxRiskAsSafeValue, aGoalPriority, aSafetyPriority, projectionDistance);
    //
    //         IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.iRoadMap, scouterParams, SearchPlanner.None, null, AlertPlanner.None, null);
    //         
    //         Session session = new Session(120, "", GameType.CoinCollection, Scenario.Stealth, "blue",
    //             guardSpawnType, guardTeam, null,  1,
    //             intruderBehaviorParams,
    //             mapData, SpeechType.Simple, SurveyType.EndEpisode);
    //
    //         // Add guards
    //         for (int i = 0; i < session.guardsCount; i++)
    //         {
    //             Behavior behavior = new Behavior(guardMethod, AlertPlanner.Simple,
    //                 SearchPlanner.Cheating, PlanOutput.DijkstraPath);
    //
    //             session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
    //                 PathFollowing.SimpleFunnel, null);
    //         }
    //
    //         // Add intruders
    //         for (int i = 0; i < session.intruderCount; i++)
    //         {
    //             Behavior behavior = new Behavior(PatrolPlanner.iRoadMap, AlertPlanner.iHeuristic,
    //                 SearchPlanner.iHeuristic, PlanOutput.DijkstraPath);
    //
    //             session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
    //                 PathFollowing.SimpleFunnel, null);
    //         }
    //
    //         sessions.Add(session);
    //     }
    // }
}