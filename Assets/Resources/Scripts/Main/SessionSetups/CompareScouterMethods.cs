using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CompareScouterMethods
{
    private static int _episodeLength = 120;
    private static int _episodeCount = 15;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(4);

        List<MapData> maps = new List<MapData>();
        // maps.Add(new MapData("amongUs"));
        maps.Add(new MapData("bloodstainedAngle"));
        maps.Add(new MapData("MgsDock"));

        List<PatrolerParams> patrolerMethods = new List<PatrolerParams>();

        PatrolerParams patrolParams = new VisMeshPatrolerParams(0.9f, 1f, 1f,
            1f, 1f, VMDecision.Weighted);
        patrolerMethods.Add(patrolParams);

        patrolParams = new RoadMapPatrolerParams(1f, 1f, 1f, 0.5f, RMDecision.DijkstraPath,
            RMPassingGuardsSenstivity.Max,0f,0f,0f);
        patrolerMethods.Add(patrolParams);

        patrolParams = new RandomPatrolerParams();
        patrolerMethods.Add(patrolParams);

        // Add scouter methods
        List<ScouterParams> scouterMethods = new List<ScouterParams>();
        ScouterParams scouterMethod = null;

        List<float> weights = new List<float>()
        {
            0f,1f
        };
        
        List<float> safetyThresholds = new List<float>()
        {
            0f,0.5f
        };        

        List<float> projectionMulitpliers = new List<float>()
        {
            0.25f,0.75f
        }; 
        
        
        List<RiskThresholdType> thresholdTypes = new List<RiskThresholdType>()
        {
            RiskThresholdType.Fixed,
            RiskThresholdType.Danger
        };

        foreach (var goalWeight in weights)
        foreach (var costWeight in weights)
        foreach (var coverWeight in weights)
        foreach (var occlusionWeight in weights)
        foreach (var proximityWeight in weights)
        foreach (var goalWeight1 in weights)
        foreach (var costWeight1 in weights)
        foreach (var coverWeight1 in weights)
        foreach (var occlusionWeight1 in weights)
        foreach (var proximityWeight1 in weights)
        foreach (var safetyThreshold in safetyThresholds)
        foreach (var projectionMulitplier in projectionMulitpliers)
        foreach (var thresholdType in thresholdTypes)
        {
            RoadMapScouterWeights safeWeights = new RoadMapScouterWeights(goalWeight, costWeight, coverWeight, occlusionWeight, proximityWeight);    
            
            RoadMapScouterWeights unsafeWeights = new RoadMapScouterWeights(goalWeight1, costWeight1, coverWeight1, occlusionWeight1, proximityWeight1);

            scouterMethod = new RoadMapScouterParams(SpotsNeighbourhoods.LineOfSight, PathCanceller.DistanceCalculation,
                thresholdType, TrajectoryType.Simple, safetyThreshold, GoalPriority.Weighted, safeWeights, SafetyPriority.Weighted,
                unsafeWeights,
                projectionMulitplier);
            
            scouterMethods.Add(scouterMethod);
        }
        

        // scouterMethod = new GreedyToGoalScouterParams();
        // scouterMethods.Add(scouterMethod);
        //
        // scouterMethod = new SimpleGreedyScouterParams();
        // scouterMethods.Add(scouterMethod);


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