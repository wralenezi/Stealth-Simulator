using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SessionsSetup : MonoBehaviour
{
    // A Dictionary for each color and what behavior they represent
    private static Dictionary<string, string> _colorLookUp;

    // Available color list
    private static List<string> colorList;

    // The colors of the played sessions
    private static List<string> playedSessions;

    // List of the colors for the 
    public static List<string> behaviorColors;

    // List of the colors for the speech colors
    public static List<string> speechColors;


    public static void AddSessionColor(string color)
    {
        playedSessions.Add(color);
    }

    public static List<string> GetPlayedSessions()
    {
        return playedSessions;
    }

    public static List<Session> SecondStudySessions()
    {
        _colorLookUp = new Dictionary<string, string>();
        playedSessions = new List<string>();

        behaviorColors = new List<string>();
        speechColors = new List<string>();

        colorList = new List<string>() {"blue", "yellow", "cyan", "orange"};

        List<Session> sessions = new List<Session>();

        // Methods that will be considered 
        List<SearchPlanner> guardMethods = new List<SearchPlanner>()
        {
            SearchPlanner.RmPropSimple //, SearchPlanner.RmPropOccupancyDiffusal
        };

        // Randomly choose the other game to compare with
        List<SearchPlanner> benchmarkMethods = new List<SearchPlanner>()
        {
            SearchPlanner.Cheating, SearchPlanner.Random
        };

        int benchMarkIndex = Random.Range(0, benchmarkMethods.Count);
        guardMethods.Add(benchmarkMethods[benchMarkIndex]);


        List<SpeechType> speechTypes = new List<SpeechType>()
        {
            SpeechType.Full,
            SpeechType.Simple
        };

        PlanOutput pathType = PlanOutput.DijkstraPath;

        foreach (var guardMethod in guardMethods)
        foreach (var speechMethod in speechTypes)
        {
            string color = GetUniqueColor();

            Session session = new Session("", GameType.CoinCollection, Scenario.Chase, color, 4, 1,
                new MapData("amongUs", 0.5f), speechMethod, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gStalest, AlertPlanner.Simple,
                    guardMethod, pathType);
                session.AddNpc(i, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                    SearchPlanner.UserInput, pathType);
                session.AddNpc(i, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            _colorLookUp[color] = guardMethod + "-" + session.speechType;
            behaviorColors.Add(color);

            sessions.Add(session);
        }

        // Randomize the methods
        List<Session> randomizedSessions = new List<Session>();

        while (sessions.Count > 0)
        {
            int randomIndex = Random.Range(0, sessions.Count);
            randomizedSessions.Add(sessions[randomIndex]);
            sessions.RemoveAt(randomIndex);
        }

        for (int i = 1; i < randomizedSessions.Count; i += 2)
        {
            randomizedSessions[i].surveyType = SurveyType.BehaviorEval;
        }

        // Add the tutorial session
        Session tutorialSession = new Session("tutorial", GameType.CoinCollection, Scenario.Chase, "grey", 2, 1,
            new MapData("MgsDock", 2f), SpeechType.None, SurveyType.EndTutorial);

        for (int i = 0; i < tutorialSession.guardsCount; i++)
        {
            Behavior behavior = new Behavior(PatrolPlanner.gStalest, AlertPlanner.Simple,
                SearchPlanner.Random, pathType);
            tutorialSession.AddNpc(i, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                PathFollowing.SimpleFunnel, null);
        }

        for (int i = 0; i < tutorialSession.intruderCount; i++)
        {
            Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                SearchPlanner.UserInput, pathType);
            tutorialSession.AddNpc(i, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                PathFollowing.SimpleFunnel, null);
        }

        // randomizedSessions.Insert(0, tutorialSession);

        return randomizedSessions;
    }

    public static List<Session> SearchTacticEvaluation()
    {
        List<Session> sessions = new List<Session>();

        // Methods that will be considered 
        List<SearchPlanner> guardMethods = new List<SearchPlanner>()
        {
            SearchPlanner.RmPropSimple //, SearchPlanner.RmPropOccupancyDiffusal
            // SearchPlanner.Random
        };

        List<PlanOutput> pathTypes = new List<PlanOutput>()
        {
            //PlanOutput.Point,
            PlanOutput.DijkstraPath //,
            //PlanOutput.DijkstraPathMax,
            //PlanOutput.HillClimbPath
        };

        SpeechType speechMethod = SpeechType.Simple;
        string color = "blue";

        foreach (var guardMethod in guardMethods)
        foreach (var pathType in pathTypes)
        {
            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, color, 4, 1,
                new MapData("amongUs", 0.5f), speechMethod, SurveyType.EndEpisode);

            // MgsDock

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gStalest, AlertPlanner.Simple,
                    guardMethod, pathType);
                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                // Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                //     SearchPlanner.UserInput, pathType);

                Behavior behavior = new Behavior(PatrolPlanner.iRoadMap, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, pathType);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            sessions.Add(session);
        }

        return sessions;
    }


    public static List<Session> StealthStudy()
    {
        List<Session> sessions = new List<Session>();

        // Methods that will be considered 
        List<SearchPlanner> guardMethods = new List<SearchPlanner>()
        {
            SearchPlanner.RmPropSimple //, SearchPlanner.RmPropOccupancyDiffusal
            // SearchPlanner.Random
        };

        List<PlanOutput> pathTypes = new List<PlanOutput>()
        {
            //PlanOutput.Point,
            PlanOutput.DijkstraPath //,
            //PlanOutput.DijkstraPathMax,
            //PlanOutput.HillClimbPath
        };

        SpeechType speechMethod = SpeechType.Simple;
        string color = "blue";

        foreach (var guardMethod in guardMethods)
        foreach (var pathType in pathTypes)
        {
            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, color, 2, 1,
                new MapData("MgsDock", 2f), speechMethod, SurveyType.EndEpisode);


            List<NpcLocation> guardLocations = new List<NpcLocation>();

            guardLocations.Add(new NpcLocation(new Vector2(0.48f, 4.8f), 0f));
            guardLocations.Add(new NpcLocation(new Vector2(-5.4f, -3.63f), 0f));
            guardLocations.Add(new NpcLocation(new Vector2(0f, 0f), 0f));
            guardLocations.Add(new NpcLocation(new Vector2(0f, 0f), 0f));

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gScripted, AlertPlanner.Simple,
                    guardMethod, pathType);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, guardLocations[i]);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                // Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                //     SearchPlanner.UserInput, pathType);

                Behavior behavior = new Behavior(PatrolPlanner.iRoadMap, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, pathType);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, new NpcLocation(new Vector2(-13.25f, 4.4f), 0f));
            }

            sessions.Add(session);
        }

        return sessions;
    }


    public static List<Session> StealthStudyProcedural()
    {
        List<Session> sessions = new List<Session>();

        // Methods that will be considered 
        List<SearchPlanner> guardMethods = new List<SearchPlanner>()
        {
            SearchPlanner.RmPropSimple //, SearchPlanner.RmPropOccupancyDiffusal
            // SearchPlanner.Random
        };

        List<PlanOutput> pathTypes = new List<PlanOutput>()
        {
            //PlanOutput.Point,
            PlanOutput.DijkstraPath //,
            //PlanOutput.DijkstraPathMax,
            //PlanOutput.HillClimbPath
        };

        SpeechType speechMethod = SpeechType.Simple;
        string color = "blue";

        foreach (var guardMethod in guardMethods)
        foreach (var pathType in pathTypes)
        {
            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, color, 2, 1,
                new MapData("MgsDock", 2f), speechMethod, SurveyType.EndEpisode);

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gRoadMap, AlertPlanner.Simple,
                    guardMethod, pathType);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                // Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                //     SearchPlanner.UserInput, pathType);

                Behavior behavior = new Behavior(PatrolPlanner.iRoadMap, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, pathType);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, new NpcLocation(new Vector2(-13.25f, 4.4f), 0f));
            }

            sessions.Add(session);
        }

        return sessions;
    }


    public static List<Session> StealthStudy002()
    {
        List<Session> sessions = new List<Session>();

        // Methods that will be considered 
        List<SearchPlanner> guardMethods = new List<SearchPlanner>()
        {
            SearchPlanner.RmPropSimple
        };

        List<PlanOutput> pathTypes = new List<PlanOutput>()
        {
            PlanOutput.DijkstraPath
        };

        SpeechType speechMethod = SpeechType.Simple;
        string color = "blue";

        foreach (var guardMethod in guardMethods)
        foreach (var pathType in pathTypes)
        {
            Session session = new Session("", GameType.CoinCollection, Scenario.Stealth, color, 1, 1,
                new MapData("Hall", 1f), speechMethod, SurveyType.EndEpisode);


            List<NpcLocation> guardLocations = new List<NpcLocation>();

            guardLocations.Add(new NpcLocation(new Vector2(0.48f, 4.8f), 0f));
            guardLocations.Add(new NpcLocation(new Vector2(-5.4f, -3.63f), 0f));

            // Add guards
            for (int i = 0; i < session.guardsCount; i++)
            {
                Behavior behavior = new Behavior(PatrolPlanner.gRoadMap, AlertPlanner.Simple,
                    guardMethod, pathType);

                session.AddNpc(i + 1, NpcType.Guard, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null); // guardLocations[i]);
            }

            // Add intruders
            for (int i = 0; i < session.intruderCount; i++)
            {
                // Behavior behavior = new Behavior(PatrolPlanner.UserInput, AlertPlanner.UserInput,
                //     SearchPlanner.UserInput, pathType);

                Behavior behavior = new Behavior(PatrolPlanner.iRoadMap, AlertPlanner.iHeuristic,
                    SearchPlanner.iHeuristic, pathType);

                session.AddNpc(i + 1, NpcType.Intruder, behavior, PathFindingHeursitic.EuclideanDst,
                    PathFollowing.SimpleFunnel, null); //new NpcLocation(new Vector2(-13.25f, 4.4f), 0f));
            }

            sessions.Add(session);
        }

        return sessions;
    }


    /// <summary>
    /// Get a string for a unique color from the list of available colors
    /// </summary>
    /// <returns>A string for a unique color</returns>
    private static string GetUniqueColor()
    {
        if (colorList.Count == 0) return "White";

        int index = Random.Range(0, colorList.Count);

        string color = colorList[index];

        colorList.RemoveAt(index);

        return color;
    }

    public static string GetCategoryFromColor(string color)
    {
        try
        {
            return _colorLookUp[color];
        }
        catch (Exception e)
        {
            Debug.Log(color);
            Console.WriteLine(e);
            throw;
        }
    }
}

/// <summary>
/// Contains the map details, like name, size
/// </summary>
public struct MapData
{
    public string name;
    public float size;

    public MapData(string _name, float _size)
    {
        name = _name;
        size = _size;
    }
}