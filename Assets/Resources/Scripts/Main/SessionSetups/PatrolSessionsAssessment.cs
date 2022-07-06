using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  class PatrolSessionsAssessment
{
    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        guardTeams.Add(1);


        MapData mapData;
        // mapData = new MapData("amongUs", 0.5f);
        mapData = new MapData("bloodstainedAngle1", 0.5f);

        AddDynamicSession(ref sessions, mapData, guardTeams);

        return sessions;
    }

    private static void AddDynamicSession(ref List<Session> sessions, MapData mapData, List<int> guardTeams)
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



        foreach (var guardMethod in guardMethods)
        foreach (var guardSpawnType in guardSpawnTypes)
        foreach (var guardTeam in guardTeams)
        {

            PatrolerParams patrolParams = new VisMeshPatrolerParams(0.4f, 1f, 0f, 0f, 0f);
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