using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolUserStudy : MonoBehaviour
{
    private static int _episodeLength = 10;
    private static int _episodeCount = 1;

    private static List<string> _colors = new List<string>()
    {
        "blue",
        "yellow",
        "green"
    };

    private static string GetColor()
    {
        string output = "red";

        if (_colors.Count > 0)
        {
            int index = Random.Range(0, _colors.Count);
            output = _colors[index];
            _colors.RemoveAt(index);
        }

        return output;
    }

    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();

        List<int> guardTeams = new List<int>();
        MapData mapData;

        guardTeams.Add(1);
        mapData = new MapData("MgsDock", 2f);
        AddRandomSession(ref sessions, mapData, "red", guardTeams, SurveyType.EndTutorial);


        guardTeams.Clear();
        guardTeams.Add(4);
        mapData = new MapData("amongUs", 0.5f);
        AddVisMeshSession(ref sessions, mapData, _colors[0], guardTeams, SurveyType.EndEpisode);
        AddRoadMapSession(ref sessions, mapData, _colors[1], guardTeams, SurveyType.EndEpisode);
        AddRandomSession(ref sessions, mapData, _colors[2], guardTeams, SurveyType.EndEpisode);

        return sessions;
    }

    private static void AddVisMeshSession(ref List<Session> sessions, MapData mapData, string color,
        List<int> guardTeams, SurveyType surveyType)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = new VisMeshPatrolerParams(0.95f, 0.5f, 0f,
                0.5f, 0.5f, VMDecision.Weighted);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gVisMesh, patrolParams);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null);


            Session session = new Session(_episodeLength, "", GameType.CoinCollection, Scenario.Stealth, color,
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, surveyType);

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


    private static void AddRoadMapSession(ref List<Session> sessions, MapData mapData, string guardColor,
        List<int> guardTeams, SurveyType surveyType)
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
                mapData, SpeechType.Simple, surveyType);

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

    private static void AddRandomSession(ref List<Session> sessions, MapData mapData, string guardColor,
        List<int> guardTeams, SurveyType surveyType)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = null;

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRandom, patrolParams);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null);


            Session session = new Session(_episodeLength, "", GameType.CoinCollection, Scenario.Stealth, guardColor,
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, surveyType);

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