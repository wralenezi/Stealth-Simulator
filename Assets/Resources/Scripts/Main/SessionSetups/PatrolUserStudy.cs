using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This sets up the survey session for the patrol assessment study
public class PatrolUserStudy : MonoBehaviour
{
    private static int _episodeCount = 1;

    private static List<string> _colors = new List<string>();

    private static List<string> _variables = new List<string>();

    private static List<SessionPair> _pairs = new List<SessionPair>();

    private static void PairUpColors()
    {
        _colors.Clear();
        _colors.Add("blue");
        _colors.Add("blue");
        _colors.Add("blue");

        // _colors.Add("red");
        // _colors.Add("green");


        _variables.Clear();
        _variables.Add("Roadmap");
        _variables.Add("Roadmap");
        _variables.Add("Roadmap");

        // _variables.Add("Vismesh");
        // _variables.Add("Random");

        _pairs.Clear();
        while (_variables.Count > 0)
        {
            int indexVariables = Random.Range(0, _variables.Count);

            _pairs.Add(new SessionPair(GetColor(), _variables[indexVariables]));
            _variables.RemoveAt(indexVariables);
        }
    }

    private static string GetColor()
    {
        string output = "grey";

        if (_colors.Count > 0)
        {
            int index = Random.Range(0, _colors.Count);
            output = _colors[index];
            _colors.RemoveAt(index);
        }

        return output;
    }

    public static string GetPairsString()
    {
        string output = "";

        output += "color,behavior\n";

        foreach (var pair in _pairs)
        {
            output += pair.color + "," + pair.variable + "\n";
        }

        return output;
    }

    private static void AddSessions(ref List<Session> sessions, MapData mapData, List<int> guardCount, SessionPair pair,
        float episodeLength)
    {
        switch (pair.variable)
        {
            case "Roadmap":
                AddRoadMapSession("", ref sessions, mapData, pair.color, guardCount, SurveyType.EndEpisode,
                    episodeLength);
                break;

            case "Vismesh":
                AddVisMeshSession("", ref sessions, mapData, pair.color, guardCount, SurveyType.EndEpisode,
                    episodeLength);
                break;

            case "Random":
                AddRandomSession("", ref sessions, mapData, pair.color, guardCount, SurveyType.EndEpisode,
                    episodeLength);
                break;
        }
    }

    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();
        PairUpColors();

        List<int> guardTeams = new List<int>();
        MapData mapData;

        float episodeLength = 120f;

        // guardTeams.Add(2);
        // mapData = new MapData("MgsDock", 2f);
        // AddRandomSession("tutorial", ref sessions, mapData, "grey", guardTeams, SurveyType.EndTutorial, episodeLength * 0.35f);


        guardTeams.Clear();
        guardTeams.Add(4);
        // mapData = new MapData("amongUs");
        mapData = new MapData("MgsDock");
        foreach (var pair in _pairs)
            AddSessions(ref sessions, mapData, guardTeams, pair, episodeLength);

        return sessions;
    }

    private static void AddVisMeshSession(string gameCode, ref List<Session> sessions, MapData mapData, string color,
        List<int> guardTeams, SurveyType surveyType, float episodeLength)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparameters for the behavior
            PatrolerParams patrolParams = new VisMeshPatrolerParams(0.95f, 0.5f, 0f,
                0.5f, 0.5f, VMDecision.Weighted);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gVisMesh, patrolParams,
                SearchPlanner.None, null, AlertPlanner.None, null);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null,
                SearchPlanner.UserInput, null, AlertPlanner.UserInput, null);


            Session session = new Session(episodeLength, "", GameType.CoinCollection, Scenario.Stealth, color,
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, surveyType) {sessionVariable = "Vismesh"};


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


    private static void AddRoadMapSession(string gameCode, ref List<Session> sessions, MapData mapData,
        string guardColor,
        List<int> guardTeams, SurveyType surveyType, float episodeLength)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparameters for the behavior
            PatrolerParams patrolParams = new RoadMapPatrolerParams(1f, 1f, 0.5f,
                0.5f, RMDecision.DijkstraPath, RMPassingGuardsSenstivity.Max);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRoadMap, patrolParams,
                SearchPlanner.None, null, AlertPlanner.None, null);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null,
                SearchPlanner.UserInput, null, AlertPlanner.UserInput, null);


            Session session = new Session(episodeLength, "", GameType.CoinCollection, Scenario.Stealth, guardColor,
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, surveyType);

            session.sessionVariable = "Roadmap";

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

    private static void AddRandomSession(string gameCode, ref List<Session> sessions, MapData mapData,
        string guardColor,
        List<int> guardTeams, SurveyType surveyType, float episodeLength)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = null;

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRandom, patrolParams,
                SearchPlanner.None, null, AlertPlanner.None, null);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null,
                SearchPlanner.UserInput, null, AlertPlanner.UserInput, null);

            Session session = new Session(episodeLength, gameCode, GameType.CoinCollection, Scenario.Stealth,
                guardColor,
                GuardSpawnType.Separate, guardTeam, guardBehaviorParams, 1,
                intruderBehaviorParams,
                mapData, SpeechType.Simple, surveyType);

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
                Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                    SearchPlanner.UserInput, PlanOutput.DijkstraPath);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            sessions.Add(session);
        }
    }
}