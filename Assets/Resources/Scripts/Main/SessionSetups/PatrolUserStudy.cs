using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolUserStudy : MonoBehaviour
{
    private static int _episodeLength = 120;
    private static int _episodeCount = 1;


    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(4);

        MapData mapData;

        mapData = new MapData("amongUs", 0.5f);

        AddVisMeshSession(ref sessions, mapData, guardTeams);
        // AddRoadMapSession(ref sessions, mapData, guardTeams);
        // AddRandomSession(ref sessions, mapData, guardTeams);
        
        return sessions;
    }

    private static void AddVisMeshSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = new VisMeshPatrolerParams(0.95f, 0.5f, 0f,
                0.5f, 0.5f, VMDecision.Weighted);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gVisMesh, patrolParams);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null);


            Session session = new Session(_episodeLength, "", GameType.CoinCollection, Scenario.Stealth, "blue",
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gVisMesh, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            sessions.Add(session);
        }
    }
    
    
    private static void AddRoadMapSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = new RoadMapPatrolerParams(1f, 1f, 0.5f,
                0.5f, RMDecision.DijkstraPath, RMPassingGuardsSenstivity.Max);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRoadMap, patrolParams);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null);


            Session session = new Session(_episodeLength, "", GameType.CoinCollection, Scenario.Stealth, "blue",
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gRoadMap, AlertPlanner.Simple,
                    SearchPlanner.Cheating, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            sessions.Add(session);
        }
    }
    
    private static void AddRandomSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = null;

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRandom, patrolParams);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null);


            Session session = new Session(_episodeLength, "", GameType.CoinCollection, Scenario.Stealth, "blue",
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, SurveyType.EndEpisode);

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
                Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            sessions.Add(session);
        }
    }
}