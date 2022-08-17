using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolUserStudy : MonoBehaviour
{
    private static int _episodeLength = 40;
    private static int _episodeCount = 1;

    private static List<string> _colors = new List<string>()
    {
        "blue",
        "yellow",
        "green"
    };

    private static List<string> _variables = new List<string>()
    {
        "Roadmap",
        "Vismesh",
        "Random"
    };

    private static List<SessionPair> _pairs = new List<SessionPair>();

    private static Dictionary<string, List<ScoreRecord>> _scores = new Dictionary<string, List<ScoreRecord>>();
    
    private static void PairUpColors()
    {
        while (_variables.Count > 0)
        {
            int indexVariables = Random.Range(0, _variables.Count);
            
            _pairs.Add(new SessionPair(GetColor(), _variables[indexVariables]));
            _variables.RemoveAt(indexVariables);
        }
    }
    
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
    
    // public static void LoadScores(Session session, string data)
    // {
    //     if(Equals(session.gameCode, "tutorial")) return;
    //     
    //     List<ScoreRecord> rows = new List<ScoreRecord>();
    //
    //     // Load the scores from the data
    //     _scores.Add(session.sessionVariable, rows);
    // }

    public static List<ScoreRecord> GetScores(string variable)
    {
        return _scores[variable];
    }


    private static void AddSessions(ref List<Session> sessions, MapData mapData, List<int> guardCount, SessionPair pair)
    {
        switch (pair.variable)
        {
            case "Roadmap":
                AddRoadMapSession("", ref sessions, mapData, pair.color, guardCount, SurveyType.EndEpisode);
                break;

            case "Vismesh":
                AddVisMeshSession("", ref sessions, mapData, pair.color, guardCount, SurveyType.EndEpisode);
                break;

            case "Random":
                AddRandomSession("", ref sessions, mapData, pair.color, guardCount, SurveyType.EndEpisode);
                break;
        }
    }

    public static List<Session> GetSessions()
    {
        List<Session> sessions = new List<Session>();
        PairUpColors();
        
        List<int> guardTeams = new List<int>();
        MapData mapData;

        guardTeams.Add(1);
        mapData = new MapData("MgsDock", 2f);
        AddRandomSession("tutorial", ref sessions, mapData, "red", guardTeams, SurveyType.EndTutorial);


        guardTeams.Clear();
        guardTeams.Add(4);
        mapData = new MapData("amongUs", 0.5f);

        foreach (var pair in _pairs)
            AddSessions(ref sessions, mapData, guardTeams, pair);
        
        return sessions;
    }

    private static void AddVisMeshSession(string gameCode, ref List<Session> sessions, MapData mapData, string color,
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

            session.sessionVariable = "Vismesh";

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
        List<int> guardTeams, SurveyType surveyType)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = new RoadMapPatrolerParams(1f, 1f, 0.5f,
                0.5f, RMDecision.DijkstraPath, RMPassingGuardsSenstivity.Max);

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRoadMap, patrolParams);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null);


            Session session = new Session(_episodeLength, "", GameType.CoinCollection, Scenario.Stealth, guardColor,
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
        List<int> guardTeams, SurveyType surveyType)
    {
        foreach (var guardTeam in guardTeams)
        {
            // Set the Hyperparamets for the behavior
            PatrolerParams patrolParams = null;

            GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(PatrolPlanner.gRandom, patrolParams);

            IntruderBehaviorParams intruderBehaviorParams = new IntruderBehaviorParams(PatrolPlanner.UserInput, null);

            Session session = new Session(_episodeLength, gameCode, GameType.CoinCollection, Scenario.Stealth,
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